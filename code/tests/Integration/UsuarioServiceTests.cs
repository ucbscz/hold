using BCryptLib = BCrypt.Net.BCrypt;
using FluentAssertions;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Notificacion;
using Microsoft.AspNetCore.Http;
using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Application.Security;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using CarreraEntity = IMT_Reservas.Server.Core.Entities.Carrera;
namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class UsuarioServiceTests : ServiceTest<UsuarioService>
{
    private static readonly JwtSettings TestJwtSettings = new()
    {
        Key = "test_key_at_least_32_chars_long!!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpiresInMinutes = 60,
        RefreshTokenExpiryDays = 7
    };

    protected override UsuarioService CreateService(ApplicationDbContext db)
    {
        var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        return CreateService(db, memoryCache);
    }

    private static UsuarioService CreateService(
        ApplicationDbContext db,
        IDistributedCache distributedCache
    )
    {
        var jwtOptions = Options.Create(TestJwtSettings);
        var mapper = new UsuarioMapper();
        var queries = new UsuarioReadRepository(db);
        var repo = new UsuarioRepository(db, mapper, queries);
        var authRepository = new UsuarioAuthRepository(db);
        var validator = new UsuarioValidator(db);
        var jwt = new JwtService(jwtOptions);
        var cacheService = new CacheRepository(
            distributedCache,
            NullLogger<CacheRepository>.Instance
        );

        var audit = new AuditLogService(new AuditLogRepository(db), new HttpContextAccessor());
        var notifications = new NotificacionService(new NotificacionRepository(db));

        return new UsuarioService(
            repo,
            mapper,
            validator,
            jwt,
            jwtOptions,
            cacheService,
            audit,
            notifications,
            authRepository,
            queries,
            new SensitiveDataProtector(
                new EphemeralDataProtectionProvider(),
                NullLogger<SensitiveDataProtector>.Instance
            )
        );
    }

    [SetUp]
    public async Task SeedCarrera()
    {
        Db.Set<CarreraEntity>().Add(new CarreraEntity { Id = 1, Nombre = "Ingeniería" });
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetAll_LimitsTheNumberOfUsersReturned()
    {
        Db.Usuarios.AddRange(
            Enumerable.Range(1, UsuarioReadRepository.MaxPageSize + 1)
                .Select(index => new Usuario
                {
                    Carnet = $"U{index:0000}",
                    Nombre = $"Usuario {index}",
                    Email = $"user{index}@ucb.edu.bo",
                    Contrasena = "hashed",
                    IdCarrera = 1,
                })
        );
        await Db.SaveChangesAsync();

        var queries = new UsuarioReadRepository(Db);
        var result = await queries.GetAll();
        var secondPage = await queries.GetPage(
            2,
            UsuarioReadRepository.MaxPageSize
        );

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(UsuarioReadRepository.MaxPageSize);
        secondPage.Value.Should().ContainSingle();
    }

    [Test]
    public async Task Login_DeletedUserCannotAuthenticate()
    {
        var user = BuildValidUsuario("U001", "deleted@ucb.edu.bo");
        user.Contrasena = BCryptLib.HashPassword("Test@1234");
        var entity = new UsuarioMapper().ToEntity(user);
        entity.EstadoEliminado = true;
        Db.Usuarios.Add(entity);
        await Db.SaveChangesAsync();

        var result = await Sut.Login("deleted@ucb.edu.bo", "Test@1234");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Create_ValidData_ReturnsSuccessAndHashesPassword()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo");

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeTrue();
        var stored = Db.Usuarios.Single(u => u.Carnet == "U001");

        BCryptLib.Verify("Test@1234", stored.Contrasena).Should().BeTrue();
    }

    [Test]
    public async Task Create_EmptyPassword_ReturnsErrorBeforeValidation()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: "");

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Contraseña requerida");
    }

    [Test]
    public async Task Create_DuplicateCarnet_ReturnsError()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));

        var result = await Sut.Create(BuildValidUsuario("U001", "otro@ucb.edu.bo"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Carnet ya existe");
    }

    [Test]
    public async Task Create_DeletedCarnet_ReactivatesTheAccount()
    {
        var deletedDto = BuildValidUsuario("U001", "anterior@ucb.edu.bo");
        var deleted = new UsuarioMapper().ToEntity(deletedDto);
        deleted.EstadoEliminado = true;
        deleted.Bloqueado = true;
        deleted.MotivoBloqueo = "Cuenta anterior";
        Db.Usuarios.Add(deleted);
        await Db.SaveChangesAsync();

        var result = await Sut.Create(
            BuildValidUsuario("U001", "nueva@ucb.edu.bo", telefono: "78888888")
        );

        result.IsSuccess.Should().BeTrue();
        var stored = await Db.Usuarios.IgnoreQueryFilters().SingleAsync(user => user.Carnet == "U001");
        stored.EstadoEliminado.Should().BeFalse();
        stored.Bloqueado.Should().BeFalse();
        stored.Email.Should().Be("nueva@ucb.edu.bo");
        BCryptLib.Verify("Test@1234", stored.Contrasena).Should().BeTrue();
    }

    [Test]
    public async Task Create_DuplicateEmail_ReturnsError()
    {
        await Sut.Create(BuildValidUsuario("U001", "shared@ucb.edu.bo"));

        var result = await Sut.Create(BuildValidUsuario("U002", "shared@ucb.edu.bo"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Email ya existe");
    }

    [Test]
    public async Task Create_DuplicateTelefono_ReturnsError()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo", telefono: "77777777"));

        var result = await Sut.Create(BuildValidUsuario("U002", "u002@ucb.edu.bo", telefono: "77777777"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Teléfono ya registrado");
    }

    [Test]
    public async Task Create_PasswordWithoutUppercase_ReturnsValidationError()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: "nouppercas@1");

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors
            .Select(error => error.ErrorMessage)
            .Should()
            .Contain("Contraseña debe tener al menos una mayúscula");
    }

    [Test]
    public async Task Create_PasswordWithoutSpecialChar_ReturnsValidationError()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: "NoSpecial123");

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors
            .Select(error => error.ErrorMessage)
            .Should()
            .Contain("Contraseña debe tener al menos un carácter especial");
    }

    [Test]
    public async Task Create_PasswordWithoutNumber_ReturnsValidationError()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: "NoNumber@!");

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors
            .Select(error => error.ErrorMessage)
            .Should()
            .Contain("Contraseña debe tener al menos un número");
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsSuccess()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));

        var result = await Sut.Login("u001@ucb.edu.bo", "Test@1234");

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.Usuario.Carnet.Should().Be("U001");
        Db.ChangeTracker.Clear();
        Db.Usuarios.Single(user => user.Carnet == "U001").RefreshToken
            .Should().Be(JwtService.HashRefreshToken(result.Value.RefreshToken));
        Db.Usuarios.Single(user => user.Carnet == "U001").RefreshToken
            .Should().NotBe(result.Value.RefreshToken);
    }

    [Test]
    public async Task Refresh_ValidToken_ReturnsNewTokenPair()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));
        var loginResult = await Sut.Login("u001@ucb.edu.bo", "Test@1234");

        var result = await Sut.Refresh(loginResult.Value.RefreshToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBe(loginResult.Value.RefreshToken);

        var replayResult = await Sut.Refresh(loginResult.Value.RefreshToken);
        replayResult.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var result = await Sut.Refresh("this-token-does-not-exist");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));

        var usuario = Db.Usuarios.Single(u => u.Carnet == "U001");
        usuario.RefreshToken = JwtService.HashRefreshToken("expired-token");
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1);
        await Db.SaveChangesAsync();

        var result = await Sut.Refresh("expired-token");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));

        var result = await Sut.Login("u001@ucb.edu.bo", "WrongPass@1");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Login_NonExistentEmail_ReturnsUnauthorized()
    {
        var result = await Sut.Login("nobody@ucb.edu.bo", "Test@1234");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Login_EmptyCredentials_ReturnsUnauthorized()
    {
        var result = await Sut.Login("", "");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Test]
    public async Task Update_SameTelefonoSameUser_Succeeds()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo", telefono: "77777777"));

        var result = await Sut.Update("U001", BuildValidUsuario("U001", "u001@ucb.edu.bo", telefono: "77777777", contrasena: null), callerCarnet: "U001");

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Update_ProfileWithoutPassword_PreservesPasswordHash()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));
        var previousHash = Db.Usuarios.Single(user => user.Carnet == "U001").Contrasena;

        var result = await Sut.UpdateProfile(
            "U001",
            BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: null)
        );

        result.IsSuccess.Should().BeTrue();
        Db.Usuarios.Single(user => user.Carnet == "U001").Contrasena.Should().Be(previousHash);
    }

    [Test]
    public async Task Update_DuplicateTelefonoOtherUser_ReturnsError()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo", telefono: "77777777"));
        await Sut.Create(BuildValidUsuario("U002", "u002@ucb.edu.bo", telefono: "88888888"));

        var result = await Sut.Update("U002", BuildValidUsuario("U002", "u002@ucb.edu.bo", telefono: "77777777", contrasena: null), callerCarnet: "U002");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Teléfono ya registrado");
    }

    [Test]
    public async Task Update_DifferentUserNotAdmin_ReturnsForbidden()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));
        await Sut.Create(BuildValidUsuario("U002", "u002@ucb.edu.bo"));

        var result = await Sut.Update("U002", BuildValidUsuario("U002", "u002@ucb.edu.bo", contrasena: null), callerCarnet: "U001");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Test]
    public async Task Update_AdminEditingOtherUser_Succeeds()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));

        var result = await Sut.Update("U001", BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: null), callerCarnet: "U999", isAdmin: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Update_NonAdminSendingRol_IsIgnored()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: null);
        dto.Rol = "administrador";

        var result = await Sut.UpdateProfile("U001", dto);

        result.IsSuccess.Should().BeTrue();
        Db.Usuarios.Single(u => u.Carnet == "U001").Rol.Should().Be(TipoUsuario.Estudiante);
    }

    [Test]
    public async Task Update_OwnProfile_PreservesTraceableIdentityFields()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));
        var dto = BuildValidUsuario("ALTERADO", "alterado@ucb.edu.bo", "78888888", null);
        dto.Nombre = "Nombre alterado";
        dto.ApellidoPaterno = "Apellido alterado";
        dto.IdCarrera = 999;

        var result = await Sut.Update("U001", dto, callerCarnet: "U001");

        result.IsSuccess.Should().BeTrue();
        var stored = Db.Usuarios.Single(user => user.Carnet == "U001");
        stored.Nombre.Should().Be("Test");
        stored.ApellidoPaterno.Should().Be("Usuario");
        stored.Email.Should().Be("u001@ucb.edu.bo");
        stored.IdCarrera.Should().Be(1);
        stored.Telefono.Should().Be("78888888");
    }

    [Test]
    public async Task Update_Profile_EncryptsIdentityImagesAtRestAndReturnsPlainBytes()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));
        var front = new byte[] { 1, 2, 3, 4, 5 };
        var back = new byte[] { 6, 7, 8, 9, 10 };
        var signature = new byte[] { 11, 12, 13, 14, 15 };
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo", contrasena: null);
        dto.ImagenFrenteCarnet = front;
        dto.ImagenAtrasCarnet = back;
        dto.ImagenFirma = signature;

        var result = await Sut.UpdateProfile("U001", dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.ImagenFrenteCarnet.Should().Equal(front);
        result.Value.ImagenAtrasCarnet.Should().Equal(back);
        result.Value.ImagenFirma.Should().Equal(signature);
        var stored = Db.Usuarios.Single(user => user.Carnet == "U001");
        stored.ImagenFrenteCarnet.Should().NotEqual(front);
        stored.ImagenAtrasCarnet.Should().NotEqual(back);
        stored.ImagenFirma.Should().NotEqual(signature);
    }

    [Test]
    public async Task Create_AnonymousWithAdminRole_IsForcedToEstudiante()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo");
        dto.Rol = "administrador";

        var result = await Sut.Create(dto, isAdmin: false);

        result.IsSuccess.Should().BeTrue();
        Db.Usuarios.Single(u => u.Carnet == "U001").Rol.Should().Be(TipoUsuario.Estudiante);
    }

    [Test]
    public async Task Create_AdminCreatingWithRole_IsHonored()
    {
        var dto = BuildValidUsuario("U001", "u001@ucb.edu.bo");
        dto.Rol = "administrador";

        var result = await Sut.Create(dto, isAdmin: true);

        result.IsSuccess.Should().BeTrue();
        Db.Usuarios.Single(u => u.Carnet == "U001").Rol.Should().Be(TipoUsuario.Administrador);
    }

    [Test]
    public async Task Delete_ExistingUser_SoftDeletes()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"));

        var result = await Sut.Delete("U001");

        result.IsSuccess.Should().BeTrue();
        Db.Usuarios.IgnoreQueryFilters().Single(u => u.Carnet == "U001").EstadoEliminado.Should().BeTrue();
    }

    [Test]
    public async Task SetBlocked_Admin_BloqueaUsuario()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"), isAdmin: true);

        var result = await Sut.SetBlocked("U001", true, "Rompió un equipo", isAdmin: true);

        result.IsSuccess.Should().BeTrue();
        Db.ChangeTracker.Clear();
        var stored = Db.Usuarios.Single(u => u.Carnet == "U001");
        stored.Bloqueado.Should().BeTrue();
        stored.MotivoBloqueo.Should().Be("Rompió un equipo");
        Db.AuditLogs.Should().ContainSingle(log => log.Accion == AuditAccion.Bloquear.ToString());
        Db.Notificaciones.Should().ContainSingle(notification =>
            notification.CarnetUsuario == "U001"
            && notification.Tipo == TipoNotificacion.UsuarioBloqueado.ToString()
        );
    }

    [Test]
    public async Task SetBlocked_Desbloquea_LimpiaMotivo()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"), isAdmin: true);
        await Sut.SetBlocked("U001", true, "motivo", isAdmin: true);

        var result = await Sut.SetBlocked("U001", false, null, isAdmin: true);

        result.IsSuccess.Should().BeTrue();
        var stored = Db.Usuarios.Single(u => u.Carnet == "U001");
        stored.Bloqueado.Should().BeFalse();
        stored.MotivoBloqueo.Should().BeNull();
        Db.Notificaciones.Should().ContainSingle(notification =>
            notification.CarnetUsuario == "U001"
            && notification.Tipo == TipoNotificacion.UsuarioDesbloqueado.ToString()
        );
    }

    [Test]
    public async Task SetBlocked_NoAdmin_ReturnsForbidden()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"), isAdmin: true);

        var result = await Sut.SetBlocked("U001", true, "x", isAdmin: false);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Test]
    public async Task SetBlocked_DoesNotWaitForSlowCacheInvalidation()
    {
        await Sut.Create(BuildValidUsuario("U001", "u001@ucb.edu.bo"), isAdmin: true);
        var cache = new Mock<IDistributedCache>();
        var cacheRelease = new TaskCompletionSource();
        cache
            .Setup(instance => instance.RemoveAsync("usuario:U001", It.IsAny<CancellationToken>()))
            .Returns(cacheRelease.Task);
        var service = CreateService(Db, cache.Object);

        var result = await service.SetBlocked("U001", true, "motivo", isAdmin: true);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(
            instance => instance.RemoveAsync("usuario:U001", It.IsAny<CancellationToken>()),
            Times.Once
        );
        cacheRelease.SetResult();
    }

    [TestCase("administrador")]
    [TestCase(" ADMINISTRADOR ")]
    [TestCase("administrador_laboratorio")]
    public async Task LaboratoryAdmin_CannotCreatePrivilegedAccounts(string role)
    {
        var dto = BuildValidUsuario("PRIV", "priv@ucb.edu.bo");
        dto.Rol = role;
        var result = await Sut.Create(dto, isAdmin: true, isLabAdmin: true);
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        Db.Usuarios.Any(u => u.Carnet == "PRIV").Should().BeFalse();
    }

    [Test]
    public async Task LaboratoryAdmin_CanCreateAdministrativeBorrower()
    {
        var dto = BuildValidUsuario("STAFF", "staff@ucb.edu.bo");
        dto.Rol = "administrativo";
        var result = await Sut.Create(dto, isAdmin: true, isLabAdmin: true);
        result.IsSuccess.Should().BeTrue();
        Db.Usuarios.Single(u => u.Carnet == "STAFF").Rol.Should().Be(TipoUsuario.Administrativo);
    }

    [Test]
    public async Task LaboratoryAdmin_CannotBlockRoot()
    {
        var dto = BuildValidUsuario("ROOT", "root@ucb.edu.bo");
        dto.Rol = "administrador";
        await Sut.Create(dto, isAdmin: true);
        var result = await Sut.SetBlocked("ROOT", true, "motivo", isAdmin: true, isLabAdmin: true);
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        Db.Usuarios.Single(u => u.Carnet == "ROOT").Bloqueado.Should().BeFalse();
    }

    private static UsuarioDto BuildValidUsuario(
        string carnet,
        string email,
        string? telefono = null,
        string? contrasena = "Test@1234") => new()
        {
            Carnet = carnet,
            Nombre = "Test",
            ApellidoPaterno = "Usuario",
            Email = email,
            Telefono = telefono,
            Contrasena = contrasena,
            IdCarrera = 1,
            AceptaTerminos = true
        };
}
