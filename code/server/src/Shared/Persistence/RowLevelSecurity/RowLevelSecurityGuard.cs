using Npgsql;

namespace UCB_Hold.Server.Shared.Persistence.RowLevelSecurity;

public static class RowLevelSecurityGuard
{
    private const string EnforcementSql = """
        SELECT NOT (r.rolsuper OR r.rolbypassrls) AND c.relrowsecurity AND c.relforcerowsecurity
        FROM pg_roles r, pg_class c
        WHERE r.rolname = current_user AND c.oid = 'public.usuarios'::regclass
        """;

    public static async Task EnsureEnforcedAsync(NpgsqlDataSource dataSource)
    {
        await using var command = dataSource.CreateCommand(EnforcementSql);

        if (await command.ExecuteScalarAsync() is not true)
            throw new InvalidOperationException(
                "La base de datos no aplica Row Level Security: conecta con un rol NOSUPERUSER NOBYPASSRLS y aplica code/database/schema.sql."
            );
    }
}
