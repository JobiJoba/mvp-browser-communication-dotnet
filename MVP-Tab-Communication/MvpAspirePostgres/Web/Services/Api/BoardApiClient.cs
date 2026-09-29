using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MvpAspirePostgres.Web.Models;

namespace MvpAspirePostgres.Web.Services.Api;

public sealed class BoardApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BoardItem>> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var items = await http.GetFromJsonAsync<List<BoardItem>>("/api/board", JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return items ?? [];
    }

    public async Task<bool> MoveItemAsync(string itemId, string zoneId, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
                "/api/board/move",
                new { itemId, zoneId },
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return response.IsSuccessStatusCode;
    }

    public async Task BeginDragAsync(string circuitId, string sessionId, DragPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
                "/api/drag/begin",
                new { circuitId, sessionId, payload },
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelDragAsync(string circuitId, string sessionId, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
                "/api/drag/cancel",
                new { circuitId, sessionId },
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        // 404 = already accepted/expired — benign race with DropAccepted.
        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task AcceptDropAsync(
        string targetCircuitId,
        string sessionId,
        string sourceCircuitId,
        string targetZoneId,
        DragPayload payload,
        CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
                "/api/drag/accept",
                new { targetCircuitId, sessionId, sourceCircuitId, targetZoneId, payload },
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        // 404 = already cancelled/expired — still allow MoveItem to commit board state.
        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }
}
