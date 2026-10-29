using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace IMT_Reservas.Server.Application.Features.AuditLog;

public static class AuditChangeDetail
{
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "EstadoEliminado",
        "CodigoGoogle",
        "EmailVerificacionEnviada",
        "AceptaTerminos",
    };

    private static readonly HashSet<string> ProtectedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Carnet",
        "CarnetAdministrador",
        "Contrasena",
        "ImagenPerfil",
        "ImagenFrenteCarnet",
        "ImagenAtrasCarnet",
        "ImagenFirma",
        "RefreshToken",
        "GoogleId",
        "TokenVerificacionHash",
    };

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CodigoImt"] = "Código interno",
        ["CodigoUcb"] = "Código institucional",
        ["NumeroSerial"] = "Número de serie",
        ["EstadoEquipo"] = "Estado del equipo",
        ["IdAmbiente"] = "Ambiente",
        ["IdProcedencia"] = "Procedencia",
        ["IdGrupoEquipo"] = "Grupo de equipos",
        ["IdGavetero"] = "Gavetero",
        ["CostoReferencia"] = "Costo de referencia",
        ["FechaIngresoEquipo"] = "Fecha de ingreso",
        ["EstadoPrestamo"] = "Estado del préstamo",
        ["CarreraNombre"] = "Carrera",
        ["Bloqueado"] = "Estado de bloqueo",
        ["Observacion"] = "Observación",
        ["ApellidoPaterno"] = "Apellido paterno",
        ["ApellidoMaterno"] = "Apellido materno",
        ["IdCarrera"] = "Carrera",
        ["ImagenPerfil"] = "Foto de perfil",
        ["ImagenFrenteCarnet"] = "Anverso del carnet",
        ["ImagenAtrasCarnet"] = "Reverso del carnet",
        ["ImagenFirma"] = "Firma",
        ["Contrasena"] = "Contraseña",
    };

    public static string? Build<T>(T? previous, T? current)
    {
        if (previous == null || current == null)
            return null;

        var changes = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsVisibleProperty)
            .Select(property => CreateChange(property, previous, current))
            .Where(change => change != null)
            .ToList();

        if (changes.Count == 0)
            return JsonSerializer.Serialize(new { texto = "No se detectaron cambios visibles." });

        return JsonSerializer.Serialize(new
        {
            texto = changes.Count == 1 ? "Se modificó 1 campo." : $"Se modificaron {changes.Count} campos.",
            cambios = changes,
        });
    }

    public static string? BuildCreated<T>(T? current) => BuildSnapshot(current, true);

    public static string? BuildDeleted<T>(T? previous) => BuildSnapshot(previous, false);

    private static string? BuildSnapshot<T>(T? value, bool created)
    {
        if (value == null)
            return null;

        var changes = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsVisibleProperty)
            .Select(property => CreateSnapshot(property, value, created))
            .Where(change => change != null)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            texto = created ? "Se registraron los datos iniciales." : "Se conservaron los datos visibles previos a la eliminación.",
            cambios = changes,
        });
    }

    private static object? CreateChange<T>(PropertyInfo property, T previous, T current)
    {
        var previousValue = property.GetValue(previous);
        var currentValue = property.GetValue(current);

        return ValuesEqual(previousValue, currentValue)
            ? null
            : CreateEntry(property, FormatValue(previousValue), FormatValue(currentValue));
    }

    private static object? CreateSnapshot<T>(PropertyInfo property, T value, bool created)
    {
        var propertyValue = property.GetValue(value);

        if (propertyValue == null || propertyValue is string text && string.IsNullOrWhiteSpace(text))
            return null;

        var formatted = FormatValue(propertyValue);
        return created
            ? CreateEntry(property, null, formatted)
            : CreateEntry(property, formatted, null);
    }

    private static object CreateEntry(PropertyInfo property, string? previous, string? current)
    {
        var label = Labels.GetValueOrDefault(property.Name, SplitName(property.Name));

        if (ProtectedProperties.Contains(property.Name))
            return new
            {
                campo = label,
                protegido = true,
                anterior = (string?)null,
                nuevo = (string?)null,
            };

        return new
        {
            campo = label,
            protegido = false,
            anterior = previous,
            nuevo = current,
        };
    }

    private static bool ValuesEqual(object? previous, object? current)
    {
        if (ReferenceEquals(previous, current))
            return true;
        if (previous == null || current == null)
            return false;
        if (previous is byte[] previousBytes && current is byte[] currentBytes)
            return previousBytes.SequenceEqual(currentBytes);
        if (previous is IEnumerable previousItems && current is IEnumerable currentItems
            && previous is not string && current is not string)
            return previousItems.Cast<object?>().SequenceEqual(currentItems.Cast<object?>());

        return Equals(previous, current);
    }

    private static bool IsVisibleProperty(PropertyInfo property) =>
        property.CanRead
        && !IgnoredProperties.Contains(property.Name)
        && !property.Name.StartsWith("Id", StringComparison.OrdinalIgnoreCase);

    private static string FormatValue(object? value)
    {
        var formatted = value switch
        {
            null => "Sin valor",
            bool boolean => boolean ? "Sí" : "No",
            DateTime dateTime => dateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            IEnumerable values when value is not string => string.Join(", ", values.Cast<object?>().Select(FormatValue)),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "Sin valor",
            _ => value.ToString() ?? "Sin valor",
        };

        return formatted.Length <= 180 ? formatted : $"{formatted[..177]}...";
    }

    private static string SplitName(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {char.ToLowerInvariant(character)}" : character.ToString()));
}
