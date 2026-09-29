using MvpAspireRedis.Web.Models;

namespace MvpAspireRedis.Web.Services.Board;

public interface IBoardStore
{
    IReadOnlyList<BoardItem> GetSnapshot();

    /// <summary>Raw Redis JSON for the board key (debug UI).</summary>
    string GetRawJson();

    bool MoveItem(string itemId, string zoneId);

    event Func<Task>? Changed;
}
