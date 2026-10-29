namespace IMT_Reservas.Server.Application.Abstraction;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Disponibilidad request missing fields or empty IDs: Inicio={Inicio}, Fin={Fin}, IdsCount={IdsCount}"
    )]
    public static partial void InvalidAvailabilityRequest(
        this ILogger logger,
        DateTime? inicio,
        DateTime? fin,
        int? idsCount
    );

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "No se pudo enviar el correo")]
    public static partial void EmailDeliveryFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "No se pudo descifrar un dato sensible"
    )]
    public static partial void SensitiveDataDecryptionFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "No se pudo descifrar un contrato")]
    public static partial void ContractDecryptionFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Search reindex skipped on startup. The database or search engine may not be ready yet. The index will populate as data is added."
    )]
    public static partial void SearchReindexSkipped(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "No se pudo leer un valor de la caché")]
    public static partial void CacheReadFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Warning,
        Message = "No se pudo escribir un valor en la caché"
    )]
    public static partial void CacheWriteFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Warning,
        Message = "No se pudo invalidar un valor de la caché"
    )]
    public static partial void CacheRemoveFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Error,
        Message = "Error no controlado {ExceptionType}. TraceId: {TraceId}"
    )]
    public static partial void UnhandledException(
        this ILogger logger,
        string exceptionType,
        string traceId
    );

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Warning,
        Message = "Error controlado {ExceptionType}. TraceId: {TraceId}"
    )]
    public static partial void HandledException(
        this ILogger logger,
        string exceptionType,
        string traceId
    );
}
