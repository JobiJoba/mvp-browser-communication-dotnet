using MvpCaseSidePanelBroadcast.Models;

namespace MvpCaseSidePanelBroadcast.Services.CrossTab;

public interface ICrossTabBus : IAsyncDisposable
{
    string TabId { get; }

    Task EnsureStartedAsync();

    Task PublishAsync(CrossTabEnvelope message);

    event Func<CrossTabEnvelope, Task>? MessageReceived;
}
