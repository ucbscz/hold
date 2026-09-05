using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class LayerBoundaryTests
{
    private static readonly Assembly ServerAssembly = typeof(Usuario).Assembly;

    [Test]
    public void Core_DoesNotReferenceOuterLayers()
    {
        var forbidden = new[] { ".Application", ".Infrastructure", ".Presentation" };
        var dependencies = ServerAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("IMT_Reservas.Server.Core", StringComparison.Ordinal) == true)
            .SelectMany(GetDeclaredDependencies)
            .Where(type => type.Namespace != null)
            .Where(type => forbidden.Any(segment => type.Namespace!.Contains(segment, StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .Distinct()
            .ToList();

        dependencies.Should().BeEmpty();
    }

    [Test]
    public void ApiDtos_DoNotExposeDomainEntities()
    {
        var leakedTypes = ServerAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("IMT_Reservas.Server.Application.Features", StringComparison.Ordinal) == true)
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .SelectMany(property => Expand(property.PropertyType))
            .Where(type => type.Namespace?.StartsWith("IMT_Reservas.Server.Core.Entities", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .Distinct()
            .ToList();

        leakedTypes.Should().BeEmpty();
    }

    [Test]
    public void UsuarioMapper_DoesNotReleaseCredentialsOrDocuments()
    {
        var dto = new UsuarioMapper().ToDto(new Usuario
        {
            Carnet = "123",
            Nombre = "Usuario",
            Email = "usuario@ucb.edu.bo",
            Contrasena = "hash",
            ImagenPerfil = [1],
            ImagenFrenteCarnet = [2],
            ImagenAtrasCarnet = [3],
            ImagenFirma = [4],
        });

        dto.Contrasena.Should().BeNull();
        dto.ImagenPerfil.Should().BeNull();
        dto.ImagenFrenteCarnet.Should().BeNull();
        dto.ImagenAtrasCarnet.Should().BeNull();
        dto.ImagenFirma.Should().BeNull();
        JsonSerializer.Serialize(dto).Should().NotContain("Contrasena");
    }

    private static IEnumerable<Type> GetDeclaredDependencies(Type type)
    {
        if (type.BaseType != null)
            foreach (var dependency in Expand(type.BaseType))
                yield return dependency;

        foreach (var memberType in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Select(field => field.FieldType)
            .Concat(type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Select(property => property.PropertyType))
            .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))))
            foreach (var dependency in Expand(memberType))
                yield return dependency;
    }

    private static IEnumerable<Type> Expand(Type type)
    {
        yield return type;
        if (type.HasElementType && type.GetElementType() is { } element)
            foreach (var nested in Expand(element))
                yield return nested;
        foreach (var argument in type.GetGenericArguments())
            foreach (var nested in Expand(argument))
                yield return nested;
    }
}
