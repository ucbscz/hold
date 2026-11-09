using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCB_Hold.Server.Modules.Mantenimientos.Api;
using UCB_Hold.Server.Modules.Usuarios.Api;
using UCB_Hold.Server.Shared.Security;

namespace UCB_Hold.Tests.Unit.Shared;

[TestFixture]
internal class CrudControllerAuthorizationTests
{
    [Test]
    public void CarreraController_ListIsPublicAndOtherActionsAreNot()
    {
        var getAll = Action(typeof(CarreraController), "GetAll");
        var get = Action(typeof(CarreraController), "Get");

        getAll.GetCustomAttributes<AllowAnonymousAttribute>(true).Should().ContainSingle();
        getAll.GetCustomAttributes<HttpGetAttribute>(true).Should().ContainSingle();
        get.GetCustomAttributes<AllowAnonymousAttribute>(true).Should().BeEmpty();
    }

    [Test]
    public void EmpresaMantenimientoController_RequiresAdministratorForEveryAction()
    {
        typeof(EmpresaMantenimientoController)
            .GetCustomAttributes<AuthorizeAttribute>(true)
            .Should()
            .Contain(attribute => attribute.Roles == Permisos.Administrador);
        Action(typeof(EmpresaMantenimientoController), "GetAll")
            .GetCustomAttributes<AllowAnonymousAttribute>(true)
            .Should()
            .BeEmpty();
    }

    private static MethodInfo Action(Type controller, string name) =>
        controller.GetMethods(BindingFlags.Instance | BindingFlags.Public).Single(method => method.Name == name);
}
