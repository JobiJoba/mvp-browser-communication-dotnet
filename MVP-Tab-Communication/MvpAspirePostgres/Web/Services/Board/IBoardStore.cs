using MvpAspirePostgres.Web.Models;

namespace MvpAspirePostgres.Web.Services.Board;

public interface IBoardStore
{
    IReadOnlyList<BoardItem> GetSnapshot();

    /// <summary>JSON of the in-memory cache last fetched from the API (debug UI).</summary>
    string GetRawJson();

    Task<bool> MoveItemAsync(string itemId, string zoneId);

    event Func<Task>? Changed;
}
