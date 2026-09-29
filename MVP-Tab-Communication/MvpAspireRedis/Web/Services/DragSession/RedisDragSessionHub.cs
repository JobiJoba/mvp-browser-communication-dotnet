using System.Collections.Concurrent;
using System.Text.Json;
using MvpAspireRedis.Web.Infrastructure;
using MvpAspireRedis.Web.Models;
using StackExchange.Redis;

namespace MvpAspireRedis.Web.Services.DragSession;

/// <summary>
/// Redis pub/sub drag hub: local subscribers per replica, events shared across containers.
/// </summary>
public sealed class RedisDragSessionHub : IDragSessionHub, IHostedService
{
    private const int SchemaVersion = 1;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDragSessionHub> _logger;
    private readonly ConcurrentDictionary<string, Func<DragSessionEvent, Task>> _subscribers = new();

    public RedisDragSessionHub(IConnectionMultiplexer redis, ILogger<RedisDragSessionHub> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public IDisposable Subscribe(string circuitId, Func<DragSessionEvent, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitId);
        ArgumentNullException.ThrowIfNull(handler);

        _subscribers[circuitId] = handler;
        return new Subscription(() => _subscribers.TryRemove(circuitId, out _));
    }

    public async Task BeginDragAsync(string circuitId, string sessionId, DragPayload payload)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(RedisKeys.DragSession(sessionId), circuitId, SessionTtl).ConfigureAwait(false);

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Started,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            TargetZoneId: null,
            payload);

        await PublishEventAsync(evt).ConfigureAwait(false);
    }

    public async Task CancelDragAsync(string circuitId, string sessionId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.DragSession(sessionId)).ConfigureAwait(false);

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.Cancelled,
            sessionId,
            circuitId,
            TargetCircuitId: null,
            TargetZoneId: null,
            Payload: null);

        await PublishEventAsync(evt).ConfigureAwait(false);
    }

    public async Task AcceptDropAsync(
        string targetCircuitId,
        string sessionId,
        string sourceCircuitId,
        string targetZoneId,
        DragPayload payload)
    {
        var db = _redis.GetDatabase();
        if (await db.KeyExistsAsync(RedisKeys.DragSession(sessionId)).ConfigureAwait(false))
        {
            var stored = await db.StringGetAsync(RedisKeys.DragSession(sessionId)).ConfigureAwait(false);
            if (!stored.IsNullOrEmpty)
            {
                sourceCircuitId = stored!;
            }

            await db.KeyDeleteAsync(RedisKeys.DragSession(sessionId)).ConfigureAwait(false);
        }

        var evt = new DragSessionEvent(
            SchemaVersion,
            DragEventKind.DropAccepted,
            sessionId,
            sourceCircuitId,
            targetCircuitId,
            targetZoneId,
            payload);

        await PublishEventAsync(evt).ConfigureAwait(false);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisKeys.DragEventsChannel),
            async (_, message) =>
            {
                if (message.IsNullOrEmpty)
                {
                    return;
                }

                try
                {
                    var evt = JsonSerializer.Deserialize<DragSessionEvent>(message.ToString(), JsonOptions);
                    if (evt is not null)
                    {
                        await BroadcastLocalAsync(evt).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to handle drag event from Redis.");
                }
            }).ConfigureAwait(false);

        _logger.LogInformation("Subscribed to Redis drag session channel.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Task PublishEventAsync(DragSessionEvent evt)
    {
        var json = JsonSerializer.Serialize(evt, JsonOptions);
        var subscriber = _redis.GetSubscriber();
        return subscriber.PublishAsync(RedisChannel.Literal(RedisKeys.DragEventsChannel), json);
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
