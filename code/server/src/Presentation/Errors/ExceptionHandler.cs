using System.Text.Json;
using IMT_Reservas.Server.Application.Abstraction;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMT_Reservas.Server.Presentation.Errors;

public sealed class ExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
    };

    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, errors) = MapException(exception);
        LogException(exception, statusCode, httpContext.TraceIdentifier);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;

        var response = new Response<object>
        {
            Status = statusCode,
            Value = null,
            Errors = errors,
            ValidationErrors = [],
        };

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions),
            cancellationToken
        );

        return true;
    }

    private static (int StatusCode, List<string> Errors) MapException(Exception exception) =>
        exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, ["Recurso no encontrado"]),
            DbUpdateException { InnerException: PostgresException postgresException }
                when IsConstraintViolation(postgresException.SqlState) => (
                StatusCodes.Status409Conflict,
                ["Conflicto al guardar: registro duplicado o restricción violada"]
            ),
            DbUpdateException { InnerException: PostgresException } => (
                StatusCodes.Status500InternalServerError,
                ["Error interno del servidor. Por favor intenta de nuevo más tarde."]
            ),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                ["Conflicto al guardar: registro duplicado o restricción violada"]
            ),
            InvalidOperationException => (
                StatusCodes.Status500InternalServerError,
                ["Error interno del servidor. Por favor intenta de nuevo más tarde."]
            ),
            ArgumentException => (StatusCodes.Status400BadRequest, ["Solicitud inválida"]),
            _ => (
                StatusCodes.Status500InternalServerError,
                ["Error interno del servidor. Por favor intenta de nuevo más tarde."]
            ),
        };

    private static bool IsConstraintViolation(string sqlState) =>
        sqlState
            is PostgresErrorCodes.UniqueViolation
                or PostgresErrorCodes.ForeignKeyViolation
                or PostgresErrorCodes.CheckViolation
                or PostgresErrorCodes.NotNullViolation;

    private void LogException(Exception exception, int statusCode, string traceIdentifier)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(
                "Error no controlado {ExceptionType}. TraceId: {TraceId}",
                exception.GetType().Name,
                traceIdentifier
            );
        else
            _logger.LogWarning(
                "Error controlado {ExceptionType}. TraceId: {TraceId}",
                exception.GetType().Name,
                traceIdentifier
            );
    }
}
