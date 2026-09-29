using System.Text.Json;
using MvpAspirePostgres.Web.Models;
using MvpAspirePostgres.Web.Services.Api;

namespace MvpAspirePostgres.Web.Services.Board;

/// <summary>Board state via the backend HTTP API + SignalR refresh notifications.</summary>
public sealed class ApiBoardStore : IBoardStore, IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly BoardApiClient _api;
    private readonly ApiRealtimeConnection _realtime;
    private readonly ILogger<ApiBoardStore> _logger;
    private IDisposable? _subscription;
    private IReadOnlyList<BoardItem> _cache = [];

    public ApiBoardStore(BoardApiClient api, ApiRealtimeConnection realtime, ILogger<ApiBoardStore> logger)
    {
        _api = api;
        _realtime = realtime;
        _logger = logger;
    }

    public event Func<Task>? Changed;

    public IReadOnlyList<BoardItem> GetSnapshot() => _cache;

    public string GetRawJson()
    {
        try
        {
            return JsonSerializer.Serialize(_cache, JsonOptions);
        }
        catch (Exception ex)
        {
            return $"(error: {ex.Message})";
        }
    }

    public bool MoveItem(string itemId, string zoneId)
    {
        var moved = _api.MoveItemAsync(itemId, zoneId).GetAwaiter().GetResult();
        if (moved)
        {
            // Optimistic local refresh; SignalR BoardChanged will refresh peers (and us).
            RefreshCacheAsync().GetAwaiter().GetResult();
        }

        return moved;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshCacheAsync(cancellationToken).ConfigureAwait(false);
        _subscription = _realtime.SubscribeBoardChanged(async () =>
        {
            await RefreshCacheAsync().ConfigureAwait(false);
            await NotifyChangedAsync().ConfigureAwait(false);
        });
        _logger.LogInformation("ApiBoardStore subscribed to backend BoardChanged.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _cache = await _api.GetBoardAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh board from API.");
        }
    }

    private async Task NotifyChangedAsync()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                await handler().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Board store change handler failed.");
            }
        }
    }
}
