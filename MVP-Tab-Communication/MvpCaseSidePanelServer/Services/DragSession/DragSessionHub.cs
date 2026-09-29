using System.Collections.Concurrent;
using MvpCaseSidePanelServer.Models;

namespace MvpCaseSidePanelServer.Services.DragSession;

public sealed class DragSessionHub : IDragSessionHub
{
    private const int SchemaVersion = 1;

    private readonly ConcurrentDictionary<string, Func<DragSessionEvent, Task>> _subscribers = new();
    private readonly ConcurrentDictionary<string, string> _activeSessions = new();

    public IDisposable Subscribe(string circuitId, Func<DragSessionEvent, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitId);
        ArgumentNullException.ThrowIfNull(handler);

        _subscribers[circuitId] = handler;
        return new Subscription(() => _subscribers.TryRemove(circuitId, out _));
    }

    public Task BeginDragAsync(string circuitId, string sessionId, CaseLinkPayload payload)
    {
        _activeSessions[sessionId] = circuitId;

        return BroadcastAsync(new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Started,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            payload));
    }

    public Task CancelDragAsync(string circuitId, string sessionId)
    {
        _activeSessions.TryRemove(sessionId, out _);

        return BroadcastAsync(new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Cancelled,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            Payload: null));
    }

    public Task AcceptDropAsync(string targetCircuitId, string sessionId, CaseLinkPayload payload)
    {
        if (!_activeSessions.TryRemove(sessionId, out var sourceCircuitId))
        {
            sourceCircuitId = targetCircuitId;
        }

        return BroadcastAsync(new DragSessionEvent(
            SchemaVersion,
            DragEventKind.DropAccepted,
            sessionId,
            sourceCircuitId,
            targetCircuitId,
            payload));
    }

    private async Task BroadcastAsync(DragSessionEvent evt)
    {
        foreach (var (_, handler) in _subscribers.ToArray())
        {
            try
            {
                await handler(evt).ConfigureAwait(false);
            }
            catch
            {
                // Isolate subscriber failures.
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                unsubscribe();
            }
        }
    }
}
