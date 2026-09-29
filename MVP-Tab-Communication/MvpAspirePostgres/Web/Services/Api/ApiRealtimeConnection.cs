using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using MvpAspirePostgres.Web.Models;

namespace MvpAspirePostgres.Web.Services.Api;

/// <summary>
/// One SignalR connection per web replica to the board API.
/// Fans BoardChanged / DragEvent out to local store/hub subscribers.
/// </summary>
public sealed class ApiRealtimeConnection : IHostedService, IAsyncDisposable
{
    public const string BoardChangedEvent = "BoardChanged";
    public const string DragEventName = "DragEvent";

    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiRealtimeConnection> _logger;
    private readonly ConcurrentBagSet<Func<Task>> _boardHandlers = new();
    private readonly ConcurrentBagSet<Func<DragSessionEvent, Task>> _dragHandlers = new();
    private HubConnection? _connection;

    public ApiRealtimeConnection(IConfiguration configuration, ILogger<ApiRealtimeConnection> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IDisposable SubscribeBoardChanged(Func<Task> handler)
    {
        _boardHandlers.Add(handler);
        return new Subscription(() => _boardHandlers.Remove(handler));
    }

    public IDisposable SubscribeDragEvents(Func<DragSessionEvent, Task> handler)
    {
        _dragHandlers.Add(handler);
        return new Subscription(() => _dragHandlers.Remove(handler));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hubUrl = ResolveHubUrl();
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On(BoardChangedEvent, async () =>
        {
            foreach (var handler in _boardHandlers.Snapshot())
            {
                try
                {
                    await handler().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "BoardChanged handler failed.");
                }
            }
        });

        _connection.On<DragSessionEvent>(DragEventName, async evt =>
        {
            foreach (var handler in _dragHandlers.Snapshot())
            {
                try
                {
                    await handler(evt).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DragEvent handler failed.");
                }
            }
        });

        _connection.Reconnected += _ =>
        {
            _logger.LogInformation("Reconnected to board API hub at {HubUrl}.", hubUrl);
            return Task.CompletedTask;
        };

        await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Connected to board API hub at {HubUrl}.", hubUrl);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }

    private string ResolveHubUrl()
    {
        var baseUrl = _configuration["BoardApi:BaseUrl"]
            ?? _configuration["services:api:http:0"]
            ?? _configuration["Services:api:http:0"]
            ?? "http://127.0.0.1:5295";

        return $"{baseUrl.TrimEnd('/')}/hubs/board";
    }

    private sealed class ConcurrentBagSet<T> where T : notnull
    {
        private readonly ConcurrentDictionary<T, byte> _items = new();

        public void Add(T item) => _items[item] = 0;

        public void Remove(T item) => _items.TryRemove(item, out _);

        public T[] Snapshot() => _items.Keys.ToArray();
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
