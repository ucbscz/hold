using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Tests.Unit.Architecture;

[TestFixture]
internal class ModuleDependencyTests
{
    private const string ModulesNamespace = "UCB_Hold.Server.Modules.";

    private static readonly string[] Order =
    [
        "Auditoria",
        "Notificaciones",
        "Usuarios",
        "Configuracion",
        "Inventario",
        "Mantenimientos",
        "Prestamos",
    ];

    [Test]
    public void Modules_DependOnlyOnModulesBelowThem()
    {
        var modulesPath = FindModulesPath();
        var tables = TablesByModule();
        var violations = new List<string>();

        foreach (var module in Directory.GetDirectories(modulesPath).Select(Path.GetFileName).OfType<string>())
        {
            Order.Should().Contain(module);
            var allowed = Order.Take(Array.IndexOf(Order, module)).ToHashSet();

            foreach (var file in Directory.GetFiles(Path.Combine(modulesPath, module), "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                var used = Regex.Matches(source, @"Modules\.(\w+)\.")
                    .Select(match => match.Groups[1].Value)
                    .Concat(tables
                        .Where(table => Regex.IsMatch(source, $@"(?<!Modules)\.\s*{table.Key}\b(?!\.(Domain|Application|Infrastructure|Api)\b)"))
                        .Select(table => table.Value));

                violations.AddRange(used
                    .Where(other => other != module && Order.Contains(other) && !allowed.Contains(other))
                    .Distinct()
                    .Select(other => $"{module} -> {other} ({Path.GetFileName(file)})"));
            }
        }

        violations.Should().BeEmpty();
    }

    private static Dictionary<string, string> TablesByModule() =>
        typeof(ApplicationDbContext)
            .GetProperties()
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => (property.Name, Namespace: property.PropertyType.GetGenericArguments()[0].Namespace ?? string.Empty))
            .Where(table => table.Namespace.StartsWith(ModulesNamespace, StringComparison.Ordinal))
            .ToDictionary(
                table => table.Name,
                table => table.Namespace[ModulesNamespace.Length..].Split('.')[0]
            );

    private static string FindModulesPath()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "server", "src", "Modules");
            if (Directory.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("server/src/Modules");
    }
}
