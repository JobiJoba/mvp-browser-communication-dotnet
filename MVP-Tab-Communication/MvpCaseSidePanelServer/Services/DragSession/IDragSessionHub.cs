using MvpCaseSidePanelServer.Models;

namespace MvpCaseSidePanelServer.Services.DragSession;

public interface IDragSessionHub
{
    IDisposable Subscribe(string circuitId, Func<DragSessionEvent, Task> handler);

    Task BeginDragAsync(string circuitId, string sessionId, CaseLinkPayload payload);

    Task CancelDragAsync(string circuitId, string sessionId);

    Task AcceptDropAsync(string targetCircuitId, string sessionId, CaseLinkPayload payload);
}
