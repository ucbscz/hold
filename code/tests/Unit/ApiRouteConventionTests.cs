using System.Reflection;
using IMT_Reservas.Server.Presentation.Controllers.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class ApiRouteConventionTests
{
    private static readonly IReadOnlyDictionary<Type, string> ExpectedRoutes =
        new Dictionary<Type, string>
        {
            [typeof(AccesorioController)] = "api/accesorio",
            [typeof(AuditLogController)] = "api/audit-log",
            [typeof(AvisoDisponibilidadController)] = "api/aviso-disponibilidad",
            [typeof(CarreraController)] = "api/carrera",
            [typeof(CarritoController)] = "api/carrito",
            [typeof(CategoriaController)] = "api/categoria",
            [typeof(ComponenteController)] = "api/componente",
            [typeof(ContratoController)] = "api/contrato",
            [typeof(EmpresaMantenimientoController)] = "api/empresa-mantenimiento",
            [typeof(EquipoController)] = "api/equipo",
            [typeof(GaveteroController)] = "api/gavetero",
            [typeof(GrupoEquipoController)] = "api/grupo-equipo",
            [typeof(MantenimientoController)] = "api/mantenimiento",
            [typeof(MuebleController)] = "api/mueble",
            [typeof(NotificacionController)] = "api/notificacion",
            [typeof(PrestamoController)] = "api/prestamo",
            [typeof(UsuarioController)] = "api/usuario",
        };

    [TestCaseSource(nameof(RouteCases))]
    public void Controller_UsesExplicitLowercaseRoute(Type controllerType, string expectedRoute)
    {
        var route = controllerType.GetCustomAttribute<RouteAttribute>();

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Template, Is.EqualTo(expectedRoute));
        Assert.That(route.Template, Does.Not.Contain("[controller]"));
        Assert.That(route.Template, Is.EqualTo(route.Template.ToLowerInvariant()));
    }

    private static IEnumerable<TestCaseData> RouteCases() =>
        ExpectedRoutes.Select(pair =>
            new TestCaseData(pair.Key, pair.Value).SetName($"{pair.Key.Name}_route_is_lowercase")
        );
}
