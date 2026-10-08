using MvpDockerMessages.Web.Models;

namespace MvpDockerMessages.Web.Services;

public sealed class MessagesApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(CancellationToken ct = default)
    {
        var messages = await http.GetFromJsonAsync<List<MessageDto>>("/api/messages", ct);
        return messages ?? [];
    }

    public async Task<MessageDto?> GetMessageAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<MessageDto>($"/api/messages/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<MessageDto?> UpdateStateAsync(Guid id, MessageState state, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"/api/messages/{id}/state",
            new UpdateMessageStateRequest(state),
            ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: ct);
    }
}
