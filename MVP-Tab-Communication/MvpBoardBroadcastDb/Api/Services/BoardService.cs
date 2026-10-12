using Microsoft.EntityFrameworkCore;
using MvpBoardBroadcastDb.Api.Data;
using MvpBoardBroadcastDb.Api.Models;

namespace MvpBoardBroadcastDb.Api.Services;

public sealed class BoardService(BoardDbContext db)
{
    private static readonly BoardItemDto[] SeedItems =
    [
        new("box-a", "Box A", "#c45c26", "tray"),
        new("box-b", "Box B", "#2f6f4e", "tray"),
        new("box-c", "Box C", "#2c5f8a", "tray")
    ];

    public static readonly HashSet<string> KnownZones = new(StringComparer.Ordinal)
    {
        "tray",
        "inbox",
        "archive"
    };

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.BoardItems.AnyAsync(cancellationToken))
        {
            return;
        }

        db.BoardItems.AddRange(SeedItems.Select(item => new BoardItemEntity
        {
            Id = item.Id,
            Title = item.Title,
            Color = item.Color,
            ZoneId = item.ZoneId
        }));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BoardItemDto>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        return await db.BoardItems
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new BoardItemDto(x.Id, x.Title, x.Color, x.ZoneId))
            .ToListAsync(cancellationToken);
    }

    public async Task<MoveItemResult> MoveItemAsync(string itemId, string zoneId, CancellationToken cancellationToken = default)
    {
        var item = await db.BoardItems.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
        if (item is null)
        {
            return MoveItemResult.NotFound;
        }

        if (string.Equals(item.ZoneId, zoneId, StringComparison.Ordinal))
        {
            return MoveItemResult.Unchanged;
        }

        item.ZoneId = zoneId;
        await db.SaveChangesAsync(cancellationToken);
        return MoveItemResult.Moved;
    }
}

public enum MoveItemResult
{
    Moved,
    Unchanged,
    NotFound
}
