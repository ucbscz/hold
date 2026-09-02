using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Ardalis.Result;

namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

public sealed class GrupoEquipoImportacionDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? UrlImagen { get; set; }
    public string? UrlDataSheet { get; set; }
    public string UrlOrigen { get; set; } = string.Empty;
}

public sealed class GrupoEquipoImportacionService
{
    private const int MaxResponseBytes = 1024 * 1024;
    private const int MaxRedirects = 3;

    public async Task<Result<GrupoEquipoImportacionDto>> Preview(
        string? url,
        CancellationToken cancellationToken
    )
    {
        if (!TryValidateUri(url, out var currentUri))
            return Result<GrupoEquipoImportacionDto>.Error(
                "Ingresa una URL HTTPS pública y válida"
            );

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));

            for (var redirect = 0; redirect <= MaxRedirects; redirect++)
            {
                using var client = CreateSecureClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.UserAgent.ParseAdd("UCBHold-MetadataPreview/1.0");
                request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token
                );

                if (IsRedirect(response.StatusCode))
                {
                    if (redirect == MaxRedirects || response.Headers.Location == null)
                        return Result<GrupoEquipoImportacionDto>.Error(
                            "La URL contiene demasiadas redirecciones"
                        );

                    var next = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(currentUri, response.Headers.Location);
                    if (!TryValidateUri(next.AbsoluteUri, out currentUri))
                        return Result<GrupoEquipoImportacionDto>.Error(
                            "La redirección conduce a una dirección no permitida"
                        );
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    return Result<GrupoEquipoImportacionDto>.Error(
                        "No se pudo leer la página indicada"
                    );

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is not "text/html" and not "application/xhtml+xml")
                    return Result<GrupoEquipoImportacionDto>.Error(
                        "La URL debe apuntar a una página HTML"
                    );

                if (response.Content.Headers.ContentLength > MaxResponseBytes)
                    return Result<GrupoEquipoImportacionDto>.Error(
                        "La página supera el tamaño permitido"
                    );

                var html = await ReadLimited(response.Content, timeout.Token);
                var preview = Parse(html, currentUri);
                return string.IsNullOrWhiteSpace(preview.Nombre)
                    ? Result<GrupoEquipoImportacionDto>.Error(
                        "No se encontraron datos de producto reconocibles"
                    )
                    : Result<GrupoEquipoImportacionDto>.Success(preview);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<GrupoEquipoImportacionDto>.Error(
                "La página tardó demasiado en responder"
            );
        }
        catch (Exception exception) when (
            exception is HttpRequestException
                or IOException
                or SocketException
                or JsonException
        )
        {
            return Result<GrupoEquipoImportacionDto>.Error(
                "No fue posible obtener una vista previa segura"
            );
        }

        return Result<GrupoEquipoImportacionDto>.Error("No fue posible importar la página");
    }

    private static GrupoEquipoImportacionDto Parse(string html, Uri source)
    {
        var document = new HtmlParser().ParseDocument(html);
        string? name = null;
        string? model = null;
        string? brand = null;
        string? description = null;
        string? image = null;

        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            if (string.IsNullOrWhiteSpace(script.TextContent))
                continue;

            try
            {
                using var json = JsonDocument.Parse(
                    script.TextContent,
                    new JsonDocumentOptions { MaxDepth = 32 }
                );
                var product = FindProduct(json.RootElement);
                if (product == null)
                    continue;

                var value = product.Value;
                name ??= ReadString(value, "name");
                model ??= ReadString(value, "model") ?? ReadString(value, "mpn");
                brand ??= ReadBrand(value);
                description ??= ReadString(value, "description");
                image ??= ReadImage(value);
                break;
            }
            catch (JsonException)
            {
            }
        }

        name ??= Meta(document, "og:title") ?? document.Title;
        brand ??= Meta(document, "product:brand");
        description ??= Meta(document, "og:description") ?? MetaName(document, "description");
        image ??= Meta(document, "og:image");

        return new GrupoEquipoImportacionDto
        {
            Nombre = Limit(Normalize(name), 50),
            Modelo = Limit(Normalize(model), 50),
            Marca = Limit(Normalize(brand), 50),
            Descripcion = Limit(Normalize(description), 200),
            UrlImagen = SecureAbsoluteUrl(image, source),
            UrlDataSheet = source.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? Limit(source.AbsoluteUri, 200)
                : null,
            UrlOrigen = source.AbsoluteUri,
        };
    }

    private static JsonElement? FindProduct(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (IsProduct(element))
                return element;
            if (element.TryGetProperty("@graph", out var graph))
                return FindProduct(graph);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                var product = FindProduct(child);
                if (product != null)
                    return product;
            }
        }

        return null;
    }

    private static bool IsProduct(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
            return false;
        if (type.ValueKind == JsonValueKind.String)
            return string.Equals(type.GetString(), "Product", StringComparison.OrdinalIgnoreCase);
        return type.ValueKind == JsonValueKind.Array
            && type.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String
                && string.Equals(item.GetString(), "Product", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? ReadBrand(JsonElement element)
    {
        if (!element.TryGetProperty("brand", out var brand))
            return null;
        return brand.ValueKind == JsonValueKind.String
            ? brand.GetString()
            : brand.ValueKind == JsonValueKind.Object
                ? ReadString(brand, "name")
                : null;
    }

    private static string? ReadImage(JsonElement element)
    {
        if (!element.TryGetProperty("image", out var image))
            return null;
        if (image.ValueKind == JsonValueKind.String)
            return image.GetString();
        if (image.ValueKind == JsonValueKind.Array)
            return image.EnumerateArray().FirstOrDefault().ValueKind == JsonValueKind.String
                ? image.EnumerateArray().First().GetString()
                : null;
        return image.ValueKind == JsonValueKind.Object ? ReadString(image, "url") : null;
    }

    private static string? Meta(AngleSharp.Dom.IDocument document, string property) =>
        document.QuerySelector($"meta[property='{property}']")?.GetAttribute("content");

    private static string? MetaName(AngleSharp.Dom.IDocument document, string name) =>
        document.QuerySelector($"meta[name='{name}']")?.GetAttribute("content");

    private static string Normalize(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Limit(string value, int length) =>
        value.Length <= length ? value : value[..length].TrimEnd();

    private static string? SecureAbsoluteUrl(string? value, Uri source)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!Uri.TryCreate(source, value, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return null;
        return uri.AbsoluteUri.Length <= 200 ? uri.AbsoluteUri : null;
    }

    private static HttpClient CreateSecureClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ConnectCallback = ConnectPublicAddress,
        };
        return new HttpClient(handler, disposeHandler: true);
    }

    private static async ValueTask<Stream> ConnectPublicAddress(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken
    )
    {
        var addresses = await Dns.GetHostAddressesAsync(
            context.DnsEndPoint.Host,
            cancellationToken
        );
        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw new HttpRequestException("Destino de red no permitido");

        Exception? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception exception) when (exception is SocketException or OperationCanceledException)
            {
                socket.Dispose();
                lastError = exception;
            }
        }

        throw new HttpRequestException("No se pudo conectar al destino", lastError);
    }

    private static bool TryValidateUri(string? value, out Uri uri)
    {
        if (
            !Uri.TryCreate(value, UriKind.Absolute, out uri!)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.Port != 443
            || uri.AbsoluteUri.Length > 2048
            || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(uri.Host, out var address) && !IsPublic(address)
        )
        {
            uri = null!;
            return false;
        }

        return true;
    }

    private static bool IsPublic(IPAddress address)
    {
        if (
            IPAddress.IsLoopback(address)
            || address.IsIPv6LinkLocal
            || address.IsIPv6Multicast
            || address.IsIPv6SiteLocal
        )
            return false;
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return bytes[0] != 0
                && bytes[0] != 10
                && bytes[0] != 127
                && !(bytes[0] == 169 && bytes[1] == 254)
                && !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                && !(bytes[0] == 192 && bytes[1] == 168)
                && !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
                && !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
                && !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2)
                && !(bytes[0] == 198 && bytes[1] is 18 or 19)
                && !(bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
                && !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113)
                && bytes[0] < 224;
        return bytes[0] is not 0xfc and not 0xfd
            && !address.Equals(IPAddress.IPv6None)
            && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static async Task<string> ReadLimited(
        HttpContent content,
        CancellationToken cancellationToken
    )
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[16 * 1024];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            total += read;
            if (total > MaxResponseBytes)
                throw new HttpRequestException("Respuesta demasiado grande");
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        memory.Position = 0;
        using var reader = new StreamReader(memory);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
