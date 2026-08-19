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
            [typeof(AccesorioController)] = "api/accesorios",
            [typeof(AuditLogController)] = "api/auditoria",
            [typeof(AvisoDisponibilidadController)] = "api/avisos-disponibilidad",
            [typeof(CarreraController)] = "api/carreras",
            [typeof(CarritoController)] = "api/carrito",
            [typeof(CategoriaController)] = "api/categorias",
            [typeof(ComponenteController)] = "api/componentes",
            [typeof(ContratoController)] = "api/contratos",
            [typeof(EmpresaMantenimientoController)] = "api/empresas-mantenimiento",
            [typeof(EquipoController)] = "api/equipos",
            [typeof(GaveteroController)] = "api/gaveteros",
            [typeof(GrupoEquipoController)] = "api/grupos-equipos",
            [typeof(MantenimientoController)] = "api/mantenimientos",
            [typeof(MuebleController)] = "api/muebles",
            [typeof(NotificacionController)] = "api/notificaciones",
            [typeof(PrestamoController)] = "api/prestamos",
            [typeof(UsuarioController)] = "api/usuarios",
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
