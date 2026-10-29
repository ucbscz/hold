using FluentAssertions;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class DbContextMappingTests
{
    private static ApplicationDbContext BuildContext()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Test]
    public void Contrato_DoesNotMap_EstadoEliminado()
    {
        using var db = BuildContext();

        var prop = db.Model.FindEntityType(typeof(Contrato))!.FindProperty("EstadoEliminado");

        prop.Should().BeNull("contratos no tiene una columna estado_eliminado y contrato tiene hard-delete");
    }

    [Test]
    public void Usuario_DoesNotMap_Id()
    {
        using var db = BuildContext();

        var prop = db.Model.FindEntityType(typeof(Usuario))!.FindProperty("Id");

        prop.Should().BeNull("usuarios está identificado por el carnet, no por el id");
    }

    [Test]
    public void Usuario_Rol_UsesAnInvalidSentinel()
    {
        using var db = BuildContext();

        var property = db.Model.FindEntityType(typeof(Usuario))!.FindProperty(nameof(Usuario.Rol));

        property!.Sentinel.Should().Be((TipoUsuario)(-1));
    }

    [Test]
    public void Equipo_FechaIngreso_UsesDatabaseCurrentDate()
    {
        using var db = BuildContext();

        var property = db.Model
            .FindEntityType(typeof(Equipo))!
            .FindProperty(nameof(Equipo.FechaIngresoEquipo));

        property!.GetDefaultValueSql().Should().Be("CURRENT_DATE");
    }

    [TestCase(
        typeof(Equipo),
        nameof(Equipo.IdGrupoEquipo),
        nameof(Equipo.EstadoEquipo),
        nameof(Equipo.EstadoEliminado)
    )]
    [TestCase(
        typeof(Prestamo),
        nameof(Prestamo.EstadoPrestamo),
        nameof(Prestamo.EstadoEliminado),
        nameof(Prestamo.FechaDevolucionEsperada)
    )]
    [TestCase(
        typeof(DetallePrestamo),
        nameof(DetallePrestamo.IdGrupoEquipo),
        nameof(DetallePrestamo.IdEquipo),
        nameof(DetallePrestamo.EstadoEliminado)
    )]
    public void QueryIndex_IsConfigured(Type entityType, params string[] propertyNames)
    {
        using var db = BuildContext();

        var indexes = db.Model.FindEntityType(entityType)!.GetIndexes();

        indexes.Should().Contain(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames)
        );
    }
}
