using System.Reflection;
using System.Text.RegularExpressions;
using IMT_Reservas.Server.Presentation.Controllers.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class ApiRouteConventionTests
{
    private static readonly IReadOnlyDictionary<Type, string> ExpectedRoutes =
        new Dictionary<Type, string>
        {
            [typeof(AccesorioController)] = "api/accesorios",
            [typeof(AuthController)] = "api/auth",
            [typeof(AuditLogController)] = "api/auditoria",
            [typeof(AvisoDisponibilidadController)] = "api/avisos",
            [typeof(CarreraController)] = "api/carreras",
            [typeof(CarritoController)] = "api/carrito",
            [typeof(CategoriaController)] = "api/categorias",
            [typeof(ComponenteController)] = "api/componentes",
            [typeof(ContratoController)] = "api/contratos",
            [typeof(EmpresaMantenimientoController)] = "api/empresas",
            [typeof(EquipoController)] = "api/equipos",
            [typeof(GaveteroController)] = "api/gaveteros",
            [typeof(GrupoEquipoController)] = "api/grupos",
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
        Assert.That(route.Template, Does.Not.Contain("-"));
    }

    private static IEnumerable<TestCaseData> RouteCases() =>
        ExpectedRoutes.Select(pair =>
            new TestCaseData(pair.Key, pair.Value).SetName($"{pair.Key.Name}_route_is_lowercase")
        );

    [TestCaseSource(nameof(ActionRouteCases))]
    public void ActionRoute_UsesLowercaseResourceSegments(string controller, string action, string route)
    {
        var literalSegments = Regex.Replace(route, "\\{[^}]+\\}", string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(literalSegments, Is.EqualTo(literalSegments.ToLowerInvariant()));
            Assert.That(route, Does.Not.Contain("crear"));
            Assert.That(route, Does.Not.Contain("buscar"));
            Assert.That(route, Does.Not.Contain("historial"));
            Assert.That(route, Does.Not.Contain("por-"));
            Assert.That(literalSegments, Does.Not.Contain("-"));
        });
    }

    private static IEnumerable<TestCaseData> ActionRouteCases() =>
        ExpectedRoutes.Keys.SelectMany(controllerType =>
            controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .SelectMany(method =>
                    method
                        .GetCustomAttributes<HttpMethodAttribute>()
                        .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Template))
                        .Select(attribute =>
                            new TestCaseData(controllerType.Name, method.Name, attribute.Template!)
                        )
                )
        );
}
