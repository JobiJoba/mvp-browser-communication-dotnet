using System.Collections.Concurrent;
using MvpAspirePostgres.Web.Models;
using MvpAspirePostgres.Web.Services.Api;

namespace MvpAspirePostgres.Web.Services.DragSession;

/// <summary>
/// Drag hub over the backend API: HTTP for begin/cancel/accept, SignalR for fan-out.
/// </summary>
public sealed class ApiDragSessionHub : IDragSessionHub, IHostedService
{
    private readonly BoardApiClient _api;
    private readonly ApiRealtimeConnection _realtime;
    private readonly ILogger<ApiDragSessionHub> _logger;
    private readonly ConcurrentDictionary<string, Func<DragSessionEvent, Task>> _subscribers = new();
    private IDisposable? _subscription;

    public ApiDragSessionHub(BoardApiClient api, ApiRealtimeConnection realtime, ILogger<ApiDragSessionHub> logger)
    {
        _api = api;
        _realtime = realtime;
        _logger = logger;
    }

    public IDisposable Subscribe(string circuitId, Func<DragSessionEvent, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitId);
        ArgumentNullException.ThrowIfNull(handler);

        _subscribers[circuitId] = handler;
        return new Subscription(() => _subscribers.TryRemove(circuitId, out _));
    }

    public Task BeginDragAsync(string circuitId, string sessionId, DragPayload payload) =>
        _api.BeginDragAsync(circuitId, sessionId, payload);

    public Task CancelDragAsync(string circuitId, string sessionId) =>
        _api.CancelDragAsync(circuitId, sessionId);

    public Task AcceptDropAsync(
        string targetCircuitId,
        string sessionId,
        string sourceCircuitId,
        string targetZoneId,
        DragPayload payload) =>
        _api.AcceptDropAsync(targetCircuitId, sessionId, sourceCircuitId, targetZoneId, payload);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _realtime.SubscribeDragEvents(BroadcastLocalAsync);
        _logger.LogInformation("ApiDragSessionHub subscribed to backend DragEvent.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    private async Task BroadcastLocalAsync(DragSessionEvent evt)
    {
        foreach (var (_, handler) in _subscribers.ToArray())
        {
            try
            {
                await handler(evt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local drag subscriber failed.");
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
