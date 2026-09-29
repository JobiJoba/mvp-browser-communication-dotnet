using Microsoft.AspNetCore.SignalR;
using MvpAspirePostgres.Api.Hubs;
using MvpAspirePostgres.Api.Models;
using Npgsql;

namespace MvpAspirePostgres.Api.Services;

/// <summary>Ephemeral drag sessions in PostgreSQL; events fan out over SignalR to all frontends.</summary>
public sealed class DragSessionService
{
    private const int SchemaVersion = 1;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(2);

    private readonly NpgsqlDataSource _dataSource;
    private readonly IHubContext<BoardRealtimeHub> _hub;
    private readonly BoardService _board;

    public DragSessionService(
        NpgsqlDataSource dataSource,
        IHubContext<BoardRealtimeHub> hub,
        BoardService board)
    {
        _dataSource = dataSource;
        _hub = hub;
        _board = board;
    }

    public async Task BeginAsync(string circuitId, string sessionId, DragPayload payload, CancellationToken cancellationToken = default)
    {
        await _board.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await CleanupExpiredSessionsAsync(conn, cancellationToken).ConfigureAwait(false);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                """
                INSERT INTO drag_sessions (session_id, source_circuit_id, expires_at)
                VALUES (@sessionId, @circuitId, @expiresAt)
                ON CONFLICT (session_id) DO UPDATE
                SET source_circuit_id = EXCLUDED.source_circuit_id,
                    expires_at = EXCLUDED.expires_at
                """;
            cmd.Parameters.AddWithValue("sessionId", sessionId);
            cmd.Parameters.AddWithValue("circuitId", circuitId);
            cmd.Parameters.AddWithValue("expiresAt", DateTimeOffset.UtcNow.Add(SessionTtl));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Started,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            TargetZoneId: null,
            payload);

        await BroadcastAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelAsync(string circuitId, string sessionId, CancellationToken cancellationToken = default)
    {
        await _board.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM drag_sessions WHERE session_id = @sessionId";
            cmd.Parameters.AddWithValue("sessionId", sessionId);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Cancelled,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            TargetZoneId: null,
            Payload: null);

        await BroadcastAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    public async Task AcceptAsync(
        string targetCircuitId,
        string sessionId,
        string sourceCircuitId,
        string targetZoneId,
        DragPayload payload,
        CancellationToken cancellationToken = default)
    {
        await _board.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                """
                DELETE FROM drag_sessions
                WHERE session_id = @sessionId
                RETURNING source_circuit_id
                """;
            cmd.Parameters.AddWithValue("sessionId", sessionId);
            var stored = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (stored is string circuit && !string.IsNullOrWhiteSpace(circuit))
            {
                sourceCircuitId = circuit;
            }
        }

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.DropAccepted,
            sessionId,
            sourceCircuitId,
            targetCircuitId,
            targetZoneId,
            payload);

        await BroadcastAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    private Task BroadcastAsync(DragSessionEvent evt, CancellationToken cancellationToken) =>
        _hub.Clients.All.SendAsync(BoardRealtimeHub.DragEvent, evt, cancellationToken);

    private static async Task CleanupExpiredSessionsAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM drag_sessions WHERE expires_at < NOW()";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
