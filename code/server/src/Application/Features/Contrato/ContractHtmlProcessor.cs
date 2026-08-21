using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace IMT_Reservas.Server.Application.Features.Contrato;

public sealed partial class ContractHtmlProcessor
{
    public const int MaxHtmlLength = 2_000_000;

    private static readonly string[] AllowedTags =
    [
        "div",
        "h1",
        "img",
        "p",
        "strong",
        "table",
        "thead",
        "tbody",
        "tr",
        "th",
        "td",
        "br",
    ];

    private static readonly string[] AllowedAttributes =
    [
        "class",
        "id",
        "src",
        "alt",
        "colspan",
        "style",
        "data-grupo-id",
    ];

    private readonly HtmlSanitizer _sanitizer;
    private readonly HtmlParser _parser = new();

    public ContractHtmlProcessor()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.UnionWith(AllowedTags);
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.UnionWith(AllowedAttributes);
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedCssProperties.Add("text-align");
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("data");
    }

    public string Sanitize(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        if (html.Length > MaxHtmlLength)
            throw new ArgumentException("El contrato supera el tamaño máximo permitido.", nameof(html));

        var sanitized = _sanitizer.Sanitize(html);
        var document = _parser.ParseDocument($"<body>{sanitized}</body>");

        var images = document.Body?.QuerySelectorAll("img");

        if (images == null)
            return string.Empty;

        foreach (var image in images)
        {
            var source = image.GetAttribute("src");

            if (source == null || !SafeImageDataUri().IsMatch(source))
                image.RemoveAttribute("src");
        }

        return document.Body?.InnerHtml ?? string.Empty;
    }

    public string RenderEquipment(
        string html,
        IReadOnlyCollection<ContractEquipmentData> equipment
    )
    {
        var document = _parser.ParseDocument($"<body>{Sanitize(html)}</body>");

        foreach (var group in equipment.GroupBy(item => item.IdGrupoEquipo))
        {
            SetCellValue(
                document,
                "imt-code",
                group.Key,
                group.Select(item => item.CodigoImt.ToString(CultureInfo.InvariantCulture))
            );
            SetCellValue(
                document,
                "ucb-code",
                group.Key,
                group.Select(item => FormatOptionalCode(item.CodigoUcb))
            );
            SetCellValue(
                document,
                "serial-code",
                group.Key,
                group.Select(item => FormatOptionalCode(item.NumeroSerial))
            );
        }

        return document.Body?.InnerHtml ?? string.Empty;
    }

    private static void SetCellValue(
        IDocument document,
        string className,
        int groupId,
        IEnumerable<string> values
    )
    {
        var value = string.Join(", ", values);
        var selector = $".{className}[data-grupo-id='{groupId}']";

        foreach (var cell in document.QuerySelectorAll(selector))
            cell.TextContent = value;
    }

    private static string FormatOptionalCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "No registrado" : value.Trim();

    [GeneratedRegex(
        "^data:image/(?:png|jpeg|gif|webp);base64,[A-Za-z0-9+/]+={0,2}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex SafeImageDataUri();
}
