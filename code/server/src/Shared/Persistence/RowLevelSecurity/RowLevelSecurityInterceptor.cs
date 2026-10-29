using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace UCB_Hold.Server.Shared.Persistence.RowLevelSecurity;

public sealed class RowLevelSecurityInterceptor(IHttpContextAccessor httpContextAccessor)
    : DbConnectionInterceptor
{
    private const string ApplyIdentitySql =
        "SELECT set_config('app.carnet', @carnet, false), set_config('app.rol', @rol, false)";

    private DbConnection? _openConnection;
    private int _systemScopes;

    public static Task<IAsyncDisposable> RunAsSystemAsync(
        DbContext context,
        CancellationToken cancellationToken
    ) =>
        context
            .GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()
            ?.Interceptors?.OfType<RowLevelSecurityInterceptor>()
            .FirstOrDefault()
            is { } interceptor
            ? interceptor.BeginSystemScopeAsync(cancellationToken)
            : Task.FromResult<IAsyncDisposable>(NoScope.Instance);

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        _openConnection = connection;
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        _openConnection = connection;
        await ApplyAsync(connection, cancellationToken);
    }

    public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData) =>
        _openConnection = null;

    public override Task ConnectionClosedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData
    )
    {
        _openConnection = null;
        return Task.CompletedTask;
    }

    private async Task<IAsyncDisposable> BeginSystemScopeAsync(CancellationToken cancellationToken)
    {
        _systemScopes++;
        await ReapplyAsync(cancellationToken);
        return new SystemScope(this);
    }

    private async Task EndSystemScopeAsync()
    {
        _systemScopes--;
        await ReapplyAsync(CancellationToken.None);
    }

    private Task ReapplyAsync(CancellationToken cancellationToken) =>
        _openConnection is { State: ConnectionState.Open } connection
            ? ApplyAsync(connection, cancellationToken)
            : Task.CompletedTask;

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var identity = DatabaseIdentity.From(httpContextAccessor.HttpContext);
        if (_systemScopes > 0)
            identity = identity.AsSystem();

        var command = connection.CreateCommand();
        command.CommandText = ApplyIdentitySql;
        AddParameter(command, "carnet", identity.Carnet);
        AddParameter(command, "rol", identity.Rol);
        return command;
    }

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed class SystemScope(RowLevelSecurityInterceptor owner) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new(owner.EndSystemScopeAsync());
    }

    private sealed class NoScope : IAsyncDisposable
    {
        public static readonly NoScope Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
