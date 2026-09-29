using MvpAspireRedis.Web.Models;

namespace MvpAspireRedis.Web.Services.DragSession;

public interface IDragSessionHub
{
    IDisposable Subscribe(string circuitId, Func<DragSessionEvent, Task> handler);

    Task BeginDragAsync(string circuitId, string sessionId, DragPayload payload);

    Task CancelDragAsync(string circuitId, string sessionId);

    Task AcceptDropAsync(
        string targetCircuitId,
        string sessionId,
        string sourceCircuitId,
        string targetZoneId,
        DragPayload payload);
}
