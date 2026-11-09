using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Modules.Usuarios.Domain;

namespace UCB_Hold.Tests.Unit.Architecture;

[TestFixture]
internal class LayerBoundaryTests
{
    private static readonly Assembly ServerAssembly = typeof(Usuario).Assembly;

    [Test]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var forbidden = new[] { ".Application", ".Infrastructure", ".Api" };
        var dependencies = ServerAssembly.GetTypes()
            .Where(type => type.Namespace?.EndsWith(".Domain", StringComparison.Ordinal) == true)
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
            .Where(type => type.Namespace?.EndsWith(".Application", StringComparison.Ordinal) == true)
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .SelectMany(property => Expand(property.PropertyType))
            .Where(type => type.Namespace?.EndsWith(".Domain", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .Distinct()
            .ToList();

        leakedTypes.Should().BeEmpty();
    }

    [Test]
    public void SharedAbstractions_HaveMultipleImplementations()
    {
        var types = ServerAssembly.GetTypes();
        var abstractions = types
            .Where(type => type.IsInterface)
            .Where(type => type.Namespace?.StartsWith("UCB_Hold.Server.Shared", StringComparison.Ordinal) == true)
            .ToList();

        var singleUseAbstractions = abstractions
            .Where(abstraction => CountImplementations(abstraction, types) < 2)
            .Where(abstraction => !IsPortImplementedByModules(abstraction, types))
            .Select(abstraction => abstraction.FullName)
            .ToList();

        singleUseAbstractions.Should().BeEmpty();
    }

    [Test]
    public void Shared_DependsOnlyOnModuleDomainsThroughPersistence()
    {
        var dependencies = ServerAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("UCB_Hold.Server.Shared", StringComparison.Ordinal) == true)
            .Where(type => type.Namespace != "UCB_Hold.Server.Shared.Persistence")
            .SelectMany(GetDeclaredDependencies)
            .Where(type => type.Namespace?.StartsWith("UCB_Hold.Server.Modules", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .Distinct()
            .ToList();

        dependencies.Should().BeEmpty();
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

    private static bool IsPortImplementedByModules(Type abstraction, IEnumerable<Type> types) =>
        types
            .Where(type => !type.IsAbstract && !type.IsInterface && abstraction.IsAssignableFrom(type))
            .All(type => type.Namespace?.StartsWith("UCB_Hold.Server.Modules", StringComparison.Ordinal) == true)
        && CountImplementations(abstraction, types) > 0;

    private static int CountImplementations(Type abstraction, IEnumerable<Type> types) =>
        types.Count(type =>
            !type.IsAbstract
            && !type.IsInterface
            && type.GetInterfaces().Any(candidate =>
                abstraction.IsGenericTypeDefinition
                    ? candidate.IsGenericType
                        && candidate.GetGenericTypeDefinition() == abstraction
                    : candidate == abstraction
            )
        );
}
