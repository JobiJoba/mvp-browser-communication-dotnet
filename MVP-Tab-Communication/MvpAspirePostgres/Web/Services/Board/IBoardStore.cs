using MvpAspirePostgres.Web.Models;

namespace MvpAspirePostgres.Web.Services.Board;

public interface IBoardStore
{
    IReadOnlyList<BoardItem> GetSnapshot();

    /// <summary>JSON snapshot of board items from the API (debug UI).</summary>
    string GetRawJson();

    bool MoveItem(string itemId, string zoneId);

    event Func<Task>? Changed;
}
