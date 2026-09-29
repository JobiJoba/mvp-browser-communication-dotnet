using Npgsql;

namespace MvpAspirePostgres.Api.Infrastructure;

internal static class PostgresSchema
{
    public static async Task EnsureCreatedAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS board_items (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                color TEXT NOT NULL,
                zone_id TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS drag_sessions (
                session_id TEXT PRIMARY KEY,
                source_circuit_id TEXT NOT NULL,
                expires_at TIMESTAMPTZ NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_drag_sessions_expires_at
                ON drag_sessions (expires_at);
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
