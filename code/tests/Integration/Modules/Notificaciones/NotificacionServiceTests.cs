using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;
using System.Text.Json;

namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class NotificacionServiceTests : ServiceTest<NotificacionService>
{
    protected override NotificacionService CreateService(ApplicationDbContext db) =>
        new(new NotificacionRepository(db));

    [Test]
    public async Task Create_GuardaYListaPorCarnet()
    {
        await Sut.Create(
            "U001",
            TipoNotificacion.PrestamoAprobado,
            "Préstamo aprobado",
            "contenido"
        );

        var result = await Sut.GetByCarnet("U001");

        result.IsSuccess.Should().BeTrue();
        result
            .Value.Should()
            .ContainSingle(n => n.Titulo == "Préstamo aprobado" && n.Leido == false);
        GetEmitter(result.Value.Single().Detalle).Should().Be("Sistema");
    }

    [Test]
    public async Task GetByCarnet_SoloDelUsuario()
    {
        await Sut.Create("U001", TipoNotificacion.PrestamoAprobado, "Para U001");
        await Sut.Create("U002", TipoNotificacion.PrestamoRechazado, "Para U002");

        var result = await Sut.GetByCarnet("U001");

        result.Value.Should().OnlyContain(n => n.CarnetUsuario == "U001");
    }

    [Test]
    public async Task CreateForAdmins_NotificaSoloAdministradores()
    {
        Db.Usuarios.Add(BuildUsuario("A001", TipoUsuario.Administrador));
        Db.Usuarios.Add(BuildUsuario("E001", TipoUsuario.Estudiante));
        await Db.SaveChangesAsync();

        await Sut.CreateForAdmins(
            TipoNotificacion.AdminNuevoPrestamo,
            "Nueva reserva",
            "detalle",
            "Ana Pérez"
        );

        var adminNotifications = (await Sut.GetByCarnet("A001")).Value;

        adminNotifications.Should().HaveCount(1);
        GetEmitter(adminNotifications.Single().Detalle).Should().Be("Ana Pérez");
        (await Sut.GetByCarnet("E001")).Value.Should().BeEmpty();
    }

    private static string? GetEmitter(string? detail)
    {
        using var document = JsonDocument.Parse(detail!);
        return document.RootElement.GetProperty("emisor").GetString();
    }

    private static Usuario BuildUsuario(string carnet, TipoUsuario rol) =>
        new()
        {
            Carnet = carnet,
            Nombre = "Test",
            ApellidoPaterno = "User",
            ApellidoMaterno = "User",
            Email = carnet + "@ucb.edu.bo",
            Contrasena = "x",
            Telefono = carnet,
            Rol = rol,
            IdCarrera = 1,
        };
}
