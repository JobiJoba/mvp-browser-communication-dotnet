using Microsoft.JSInterop;

namespace MvpDockerMessages.Web.Services;

/// <summary>
/// Thin JS helper to close the current browser tab after Save/Cancel.
/// Notify path for /messages-cache-server is C# (MessagesServerSyncBus), not BroadcastChannel.
/// </summary>
public sealed class TabCloser(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task CloseAsync()
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/tabActions.js");
        await _module.InvokeVoidAsync("closeTab");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        finally
        {
            _module = null;
        }
    }
}
