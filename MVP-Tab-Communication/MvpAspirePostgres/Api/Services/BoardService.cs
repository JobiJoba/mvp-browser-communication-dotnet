using Microsoft.AspNetCore.SignalR;
using MvpAspirePostgres.Api.Hubs;
using MvpAspirePostgres.Api.Infrastructure;
using MvpAspirePostgres.Api.Models;
using Npgsql;

namespace MvpAspirePostgres.Api.Services;

/// <summary>Authoritative board state in PostgreSQL. Broadcasts changes over SignalR.</summary>
public sealed class BoardService
{
    private static readonly BoardItem[] SeedItems =
    [
        new("box-a", "Box A", "#c45c26", "tray"),
        new("box-b", "Box B", "#2f6f4e", "tray"),
        new("box-c", "Box C", "#2c5f8a", "tray")
    ];

    private readonly NpgsqlDataSource _dataSource;
    private readonly IHubContext<BoardRealtimeHub> _hub;
    private readonly ILogger<BoardService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public BoardService(
        NpgsqlDataSource dataSource,
        IHubContext<BoardRealtimeHub> hub,
        ILogger<BoardService> logger)
    {
        _dataSource = dataSource;
        _hub = hub;
        _logger = logger;
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await PostgresSchema.EnsureCreatedAsync(_dataSource, cancellationToken).ConfigureAwait(false);
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSeedAsync(conn, cancellationToken).ConfigureAwait(false);
            _initialized = true;
            _logger.LogInformation("Board schema ready and seeded.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IReadOnlyList<BoardItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT id, title, color, zone_id
            FROM board_items
            ORDER BY id
            """;

        var items = new List<BoardItem>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new BoardItem(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }

        return items;
    }

    public async Task<bool> MoveItemAsync(string itemId, string zoneId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

        await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE board_items
            SET zone_id = @zoneId
            WHERE id = @id AND zone_id IS DISTINCT FROM @zoneId
            """;
        cmd.Parameters.AddWithValue("id", itemId);
        cmd.Parameters.AddWithValue("zoneId", zoneId);

        var updated = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
        if (!updated)
        {
            return false;
        }

        await _hub.Clients.All.SendAsync(BoardRealtimeHub.BoardChanged, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    private static async Task EnsureSeedAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM board_items";
            var count = (long)(await countCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        foreach (var item in SeedItems)
        {
            await using var insert = conn.CreateCommand();
            insert.CommandText =
                """
                INSERT INTO board_items (id, title, color, zone_id)
                VALUES (@id, @title, @color, @zoneId)
                ON CONFLICT (id) DO NOTHING
                """;
            insert.Parameters.AddWithValue("id", item.Id);
            insert.Parameters.AddWithValue("title", item.Title);
            insert.Parameters.AddWithValue("color", item.Color);
            insert.Parameters.AddWithValue("zoneId", item.ZoneId);
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
