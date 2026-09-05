using System.Text.Json;
using FluentAssertions;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Presentation.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
public class ExceptionHandlerTests
{
    private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
    private DefaultHttpContext _httpContext = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandler>>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Test]
    public async Task TryHandleAsync_KeyNotFoundException_Returns404()
    {
        await Handle(new KeyNotFoundException("Item not found"));

        _httpContext.Response.StatusCode.Should().Be(404);
        var response = await GetResponse();
        response.Should().NotBeNull();
        response!.Status.Should().Be(404);
        response.Errors.Should().Contain("Recurso no encontrado");
        response.Errors.Should().NotContain("Item not found");
    }

    [Test]
    public async Task TryHandleAsync_DbUpdateException_Returns409()
    {
        await Handle(new DbUpdateException("DB Error"));

        _httpContext.Response.StatusCode.Should().Be(409);
        var response = await GetResponse();
        response.Should().NotBeNull();
        response!.Status.Should().Be(409);
        response.Errors.Should().Contain("Conflicto al guardar: registro duplicado o restricción violada");
    }

    [Test]
    public async Task TryHandleAsync_DbSchemaException_Returns500()
    {
        var postgresException = new PostgresException(
            "relation does not exist",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UndefinedTable
        );

        await Handle(new DbUpdateException("DB Error", postgresException));

        _httpContext.Response.StatusCode.Should().Be(500);
        var response = await GetResponse();
        response.Should().NotBeNull();
        response!.Status.Should().Be(500);
        response.Errors.Should().Contain(
            "Error interno del servidor. Por favor intenta de nuevo más tarde."
        );
    }

    [Test]
    public async Task TryHandleAsync_InvalidOperationException_Returns500WithoutInternalDetails()
    {
        await Handle(new InvalidOperationException("Invalid Op"));

        _httpContext.Response.StatusCode.Should().Be(500);
        var response = await GetResponse();
        response.Should().NotBeNull();
        response!.Status.Should().Be(500);
        response.Errors.Should().Contain(
            "Error interno del servidor. Por favor intenta de nuevo más tarde."
        );
        response.Errors.Should().NotContain("Invalid Op");
    }

    [Test]
    public async Task TryHandleAsync_ArgumentException_Returns400()
    {
        await Handle(new ArgumentException("Bad Arg"));

        _httpContext.Response.StatusCode.Should().Be(400);
        var response = await GetResponse();
        response.Should().NotBeNull();
        response!.Status.Should().Be(400);
        response.Errors.Should().Contain("Solicitud inválida");
        response.Errors.Should().NotContain("Bad Arg");
    }

    [Test]
    public async Task TryHandleAsync_UnhandledException_Returns500()
    {
        await Handle(new InvalidDataException("Unexpected error"));

        _httpContext.Response.StatusCode.Should().Be(500);
        var response = await GetResponse();
        response.Should().NotBeNull();
        response!.Status.Should().Be(500);
        response.Errors.Should().Contain("Error interno del servidor. Por favor intenta de nuevo más tarde.");
    }

    private async Task<Response<object>?> GetResponse()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(_httpContext.Response.Body);
        var text = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<Response<object>>(
            text,
            new JsonSerializerOptions { PropertyNamingPolicy = null }
        );
    }

    private async Task Handle(Exception exception)
    {
        var handler = new ExceptionHandler(_loggerMock.Object);

        var handled = await handler.TryHandleAsync(
            _httpContext,
            exception,
            CancellationToken.None
        );

        handled.Should().BeTrue();
    }
}
