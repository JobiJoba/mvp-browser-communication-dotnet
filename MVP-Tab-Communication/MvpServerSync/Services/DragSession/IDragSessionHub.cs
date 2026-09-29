using MvpServerSync.Models;

namespace MvpServerSync.Services.DragSession;

public interface IDragSessionHub
{
    IDisposable Subscribe(string circuitId, Func<DragSessionEvent, Task> handler);

    Task BeginDragAsync(string circuitId, string sessionId, DragPayload payload);

    Task CancelDragAsync(string circuitId, string sessionId);

    Task AcceptDropAsync(string circuitId, string sessionId, string targetZoneId, DragPayload payload);
}
