using System.Collections.Concurrent;
using MvpServerSync.Models;

namespace MvpServerSync.Services.DragSession;

/// <summary>
/// In-process pub/sub for cross-circuit drag sessions.
/// Replace with a SignalR/Redis backplane for multi-node deployments.
/// </summary>
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

    public Task BeginDragAsync(string circuitId, string sessionId, DragPayload payload)
    {
        _activeSessions[sessionId] = circuitId;

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Started,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            TargetZoneId: null,
            payload);

        return BroadcastAsync(evt);
    }

    public Task CancelDragAsync(string circuitId, string sessionId)
    {
        _activeSessions.TryRemove(sessionId, out _);

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Cancelled,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            TargetZoneId: null,
            Payload: null);

        return BroadcastAsync(evt);
    }

    public Task AcceptDropAsync(string targetCircuitId, string sessionId, string targetZoneId, DragPayload payload)
    {
        if (!_activeSessions.TryRemove(sessionId, out var sourceCircuitId))
        {
            sourceCircuitId = targetCircuitId;
        }

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.DropAccepted,
            sessionId,
            sourceCircuitId,
            targetCircuitId,
            targetZoneId,
            payload);

        return BroadcastAsync(evt);
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
                // Isolate subscriber failures; circuits remove themselves via Dispose.
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
