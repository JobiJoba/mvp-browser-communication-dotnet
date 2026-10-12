using System.Net.Http.Json;
using System.Text.Json;
using MvpBoardBroadcastDb.Web.Models;

namespace MvpBoardBroadcastDb.Web.Services.Api;

public sealed class BoardApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BoardItem>> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var items = await http.GetFromJsonAsync<List<BoardItem>>("/api/board", JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return items ?? [];
    }

    public async Task MoveItemAsync(string itemId, string zoneId, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
                "/api/board/move",
                new { itemId, zoneId },
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
