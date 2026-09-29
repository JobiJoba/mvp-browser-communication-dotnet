using MvpServerSync.Models;

namespace MvpServerSync.Services.Board;

/// <summary>
/// Shared board state across all Blazor circuits in this process.
/// </summary>
public sealed class BoardStore : IBoardStore
{
    private readonly object _gate = new();
    private readonly List<BoardItem> _items =
    [
        new("box-a", "Box A", "#c45c26", "tray"),
        new("box-b", "Box B", "#2f6f4e", "tray"),
        new("box-c", "Box C", "#2c5f8a", "tray")
    ];

    public event Func<Task>? Changed;

    public IReadOnlyList<BoardItem> GetSnapshot()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }

    public bool MoveItem(string itemId, string zoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

        lock (_gate)
        {
            var index = _items.FindIndex(i => i.Id == itemId);
            if (index < 0)
            {
                return false;
            }

            if (_items[index].ZoneId == zoneId)
            {
                return false;
            }

            _items[index] = _items[index] with { ZoneId = zoneId };
        }

        _ = NotifyChangedAsync();
        return true;
    }

    private async Task NotifyChangedAsync()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                await handler().ConfigureAwait(false);
            }
            catch
            {
                // Isolate subscriber failures.
            }
        }
    }
}
