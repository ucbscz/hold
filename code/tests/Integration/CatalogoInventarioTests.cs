using Ardalis.Result;
using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Inventario;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Application.Features.Mueble;

namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class CatalogoInventarioTests : ServiceTest<CatalogoInventarioRepository<Ambiente>>
{
    protected override CatalogoInventarioRepository<Ambiente> CreateService(ApplicationDbContext db) => new(db, new AmbienteMapper());

    [Test]
    public async Task Create_TrimsAndRejectsDuplicateName()
    {
        (await Sut.Create(new Ambiente { Nombre = " Laboratorio " })).IsSuccess.Should().BeTrue();
        (await Sut.Create(new Ambiente { Nombre = "Laboratorio" })).Status.Should().Be(ResultStatus.Conflict);
    }

    [Test]
    public async Task Delete_RejectsCatalogAssignedToEquipment()
    {
        var result = await Sut.Create(new Ambiente { Nombre = "Sala principal" });
        Db.Equipos.Add(new Equipo { Id = 1, IdAmbiente = result.Value.Id, CodigoImt = 1 });
        await Db.SaveChangesAsync();
        (await Sut.Delete(result.Value.Id!.Value)).Status.Should().Be(ResultStatus.Conflict);
    }

    [Test]
    public async Task Services_ResolveAndListBothCatalogs()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Db);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<AuditLogRepository>();
        services.AddScoped<AuditLogService>();
        services.AddInventoryCatalogs();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var ambientes = scope.ServiceProvider.GetRequiredService<Service<Ambiente, Repository<Ambiente, CatalogoInventarioDto>, CatalogoInventarioDto>>();
        var procedencias = scope.ServiceProvider.GetRequiredService<Service<Procedencia, Repository<Procedencia, CatalogoInventarioDto>, CatalogoInventarioDto>>();
        (await ambientes.GetAll()).IsSuccess.Should().BeTrue();
        (await procedencias.GetAll()).IsSuccess.Should().BeTrue();
    }

    [TestCase(TipoUsuario.Estudiante, false)]
    [TestCase(TipoUsuario.Administrador, false)]
    [TestCase(TipoUsuario.Administrador_Laboratorio, true)]
    public async Task Create_RejectsInvalidResponsible(TipoUsuario rol, bool bloqueado)
    {
        Db.Usuarios.Add(new Usuario { Carnet = "123", Rol = rol, Bloqueado = bloqueado });
        await Db.SaveChangesAsync();
        (await Sut.Create(new Ambiente { Nombre = "Sala", CarnetAdministrador = "123" })).Status.Should().Be(ResultStatus.Invalid);
    }

    [Test]
    public async Task GetAll_ProjectsResponsibleFullName()
    {
        Db.Usuarios.Add(new Usuario { Carnet = "123", Nombre = "Fernando", ApellidoPaterno = "Terrazas", ApellidoMaterno = "Llanos", Rol = TipoUsuario.Administrador_Laboratorio });
        await Db.SaveChangesAsync();
        (await Sut.Create(new Ambiente { Nombre = "Sala", CarnetAdministrador = "123" })).IsSuccess.Should().BeTrue();
        var result = await Sut.GetAll();
        result.Value.Single().NombreAdministrador.Should().Be("Fernando Terrazas Llanos");
    }

    [Test]
    public async Task Delete_RejectsCatalogAssignedToFurniture()
    {
        var result = await Sut.Create(new Ambiente { Nombre = "Sala" });
        Db.Muebles.Add(new Mueble { IdAmbiente = result.Value.Id, Nombre = "Armario" });
        await Db.SaveChangesAsync();
        (await Sut.Delete(result.Value.Id!.Value)).Status.Should().Be(ResultStatus.Conflict);
    }

    [Test]
    public async Task LoanLocation_UsesFurnitureEnvironmentBeforeLegacyEquipmentEnvironment()
    {
        Db.Usuarios.Add(new Usuario { Carnet = "123", Nombre = "Ana", ApellidoPaterno = "Perez", ApellidoMaterno = "Lopez", Rol = TipoUsuario.Administrador_Laboratorio });
        Db.Ambientes.AddRange(new Ambiente { Id = 1, Nombre = "Laboratorio", CarnetAdministrador = "123" }, new Ambiente { Id = 2, Nombre = "Antiguo" });
        Db.Muebles.Add(new Mueble { Id = 1, Nombre = "Mueble A", IdAmbiente = 1, Ubicacion = "Entrada" });
        Db.Gaveteros.Add(new Gavetero { Id = 1, Nombre = "Gavetero 2", IdMueble = 1 });
        Db.GruposEquipos.Add(new GrupoEquipo { Id = 1, Nombre = "Sensor" });
        Db.Equipos.Add(new Equipo { Id = 1, CodigoImt = 123, IdGavetero = 1, IdAmbiente = 2, IdGrupoEquipo = 1 });
        Db.Prestamos.Add(new Prestamo { Id = 1, Carnet = "123" });
        Db.DetallesPrestamos.Add(new DetallePrestamo { IdPrestamo = 1, IdEquipo = 1, IdGrupoEquipo = 1 });
        await Db.SaveChangesAsync();
        var result = await new PrestamoReadRepository(Db).Get(1);
        result.IsSuccess.Should().BeTrue();
        result.Value.UbicacionEquipo.Should().Be("Laboratorio");
        result.Value.NombreMueble.Should().Be("Mueble A");
        result.Value.NombreGavetero.Should().Be("Gavetero 2");
        result.Value.AdministradorAmbiente.Should().Be("Ana Perez Lopez");
        var equipment = await new EquipoRepository(Db, new EquipoMapper()).Get(1);
        equipment.IsSuccess.Should().BeTrue();
        equipment.Value.Ubicacion.Should().Be("Laboratorio");
        var furniture = await new Repository<Mueble, MuebleDto>(Db, new MuebleMapper()).Get(1);
        furniture.IsSuccess.Should().BeTrue();
        furniture.Value.NombreAmbiente.Should().Be("Laboratorio");
    }
}
