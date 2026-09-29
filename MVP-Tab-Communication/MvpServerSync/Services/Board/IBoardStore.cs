using MvpServerSync.Models;

namespace MvpServerSync.Services.Board;

public interface IBoardStore
{
    IReadOnlyList<BoardItem> GetSnapshot();

    bool MoveItem(string itemId, string zoneId);

    event Func<Task>? Changed;
}
