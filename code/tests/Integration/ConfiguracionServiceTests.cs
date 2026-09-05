using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Presentation.Controllers.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using IMT_Reservas.Server.Application.Security;
using System.Text.Json;

namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class ConfiguracionServiceTests
{
    private ApplicationDbContext _db = null!;
    private ConfiguracionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _db.Usuarios.AddRange(
            new Usuario { Carnet = "1111111", Nombre = "Ana", ApellidoPaterno = "Perez", Rol = TipoUsuario.Administrador },
            new Usuario { Carnet = "2222222", Nombre = "Luis", ApellidoPaterno = "Lopez", Rol = TipoUsuario.Docente },
            new Usuario { Carnet = "3333333", Nombre = "Bloqueado", Bloqueado = true },
            new Usuario { Carnet = "4444444", Nombre = "Eliminado", EstadoEliminado = true }
        );
        _db.SaveChanges();

        IDistributedCache distributedCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions())
        );
        var cache = new CacheRepository(
            distributedCache,
            NullLogger<CacheRepository>.Instance
        );
        var repository = new ConfiguracionRepository(_db, cache);
        var mapper = new ConfiguracionMapper();
        var sensitiveData = new SensitiveDataProtector(
            new EphemeralDataProtectionProvider(),
            NullLogger<SensitiveDataProtector>.Instance
        );
        _service = new ConfiguracionService(
            repository,
            new ConfiguracionValidator(),
            mapper,
            sensitiveData
        );
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task UpdateConfiguracion_PersistsChangesAndInvalidatesCache()
    {
        var current = await _service.GetConfiguracion();
        current.MontoMinimoContrato += 25;
        current.HorarioFinMinutos = 17 * 60;
        current.NombreJefeCarrera = "Jefe actualizado";
        current.CarnetJefeCarrera = "2222222";
        current.FirmaJefeCarreraBase64 = "nueva-firma";

        var result = await _service.UpdateConfiguracion(current);
        var reloaded = await _service.GetConfiguracion();

        result.IsSuccess.Should().BeTrue();
        reloaded.MontoMinimoContrato.Should().Be(current.MontoMinimoContrato);
        reloaded.HorarioFinMinutos.Should().Be(17 * 60);
        reloaded.NombreJefeCarrera.Should().Be("Luis Lopez");
        reloaded.CarnetJefeCarrera.Should().Be("2222222");
        reloaded.FirmaJefeCarreraBase64.Should().Be("nueva-firma");
        _db.ConfiguracionesSistema.Single().FirmaJefeCarreraBase64
            .Should().StartWith("ucbhold:v1:");
    }

    [Test]
    public async Task DefaultResponsable_IsGeneralAdministrator_WithoutBorrowingLegacySignature()
    {
        var config = await _service.GetConfiguracion();
        config.CarnetJefeCarrera.Should().Be("1111111");
        config.NombreJefeCarrera.Should().Be("Ana Perez");
        config.FirmaJefeCarreraBase64.Should().BeEmpty();
    }

    [TestCase("9999999")]
    [TestCase("3333333")]
    [TestCase("4444444")]
    public async Task Update_RejectsUnavailableUsers(string carnet)
    {
        var dto = await _service.GetConfiguracion();
        dto.CarnetJefeCarrera = carnet;
        dto.FirmaJefeCarreraBase64 = "firma";
        (await _service.UpdateConfiguracion(dto)).Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Test]
    public async Task Search_IsLimitedAndMatchesMultipleNameParts()
    {
        for (var i = 0; i < 40; i++)
            _db.Usuarios.Add(new Usuario { Carnet = $"user{i}", Nombre = "Ana", ApellidoPaterno = "Perez" });
        await _db.SaveChangesAsync();
        var users = await _service.BuscarResponsables("PEREZ ana", CancellationToken.None);
        users.Should().HaveCount(30);
        users.Should().OnlyContain(u => u.Nombre == "Ana Perez");
        (await _service.BuscarResponsables("Bloqueado", CancellationToken.None)).Should().BeEmpty();
        (await _service.BuscarResponsables("Eliminado", CancellationToken.None)).Should().BeEmpty();
    }

    [Test]
    public async Task Reassignment_RequiresNewSignature_AndNameComesFromUser()
    {
        var dto = await _service.GetConfiguracion();
        dto.FirmaJefeCarreraBase64 = "primera-firma";
        (await _service.UpdateConfiguracion(dto)).IsSuccess.Should().BeTrue();
        dto.CarnetJefeCarrera = "2222222";
        (await _service.UpdateConfiguracion(dto)).Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
        (await _service.GetConfiguracion()).CarnetJefeCarrera.Should().Be("1111111");
    }

    [Test]
    public async Task Update_RejectsDailyHoursShorterThanMinimumReservation()
    {
        var dto = await _service.GetConfiguracion();
        dto.FirmaJefeCarreraBase64 = "firma";
        dto.TiempoMinimoReservaMinutos = 60;
        dto.Horarios = new List<HorarioAtencionDto> { new() { DiaSemana = 1, Abierto = true, InicioMinutos = 480, FinMinutos = 510 } };
        (await _service.UpdateConfiguracion(dto)).Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Test]
    public async Task Get_ExposesResponsibleIdentityToRoot()
    {
        var stored = await _service.GetConfiguracion();
        stored.FirmaJefeCarreraBase64 = "firma-institucional";
        (await _service.UpdateConfiguracion(stored)).IsSuccess.Should().BeTrue();
        var controller = new ConfiguracionController(_service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Role, "administrador") }))
                }
            }
        };
        var result = (OkObjectResult)await controller.Get(CancellationToken.None);
        var config = (ConfiguracionDto)result.Value!;
        config.CarnetJefeCarrera.Should().Be("1111111");
        config.NombreJefeCarrera.Should().Be("Ana Perez");
        config.FirmaJefeCarreraBase64.Should().Be("firma-institucional");
        controller.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Test]
    public async Task Get_AnonymousResponseHasNoResponsibleIdentityFields()
    {
        var controller = new ConfiguracionController(_service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = (OkObjectResult)await controller.Get(CancellationToken.None);
        result.Value.Should().BeOfType<ConfiguracionPublicaDto>();
        var json = JsonSerializer.Serialize(result.Value);
        json.Should().NotContain("CarnetJefeCarrera");
        json.Should().NotContain("NombreJefeCarrera");
        json.Should().NotContain("FirmaJefeCarreraBase64");
        controller.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }
}
