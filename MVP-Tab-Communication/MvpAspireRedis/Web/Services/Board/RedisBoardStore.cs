using System.Text.Json;
using MvpAspireRedis.Web.Infrastructure;
using MvpAspireRedis.Web.Models;
using StackExchange.Redis;

namespace MvpAspireRedis.Web.Services.Board;

/// <summary>
/// Board state in Redis so every app replica sees the same items.
/// </summary>
public sealed class RedisBoardStore : IBoardStore, IHostedService
{
    private static readonly BoardItem[] SeedItems =
    [
        new("box-a", "Box A", "#c45c26", "tray"),
        new("box-b", "Box B", "#2f6f4e", "tray"),
        new("box-c", "Box C", "#2c5f8a", "tray")
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisBoardStore> _logger;

    public RedisBoardStore(IConnectionMultiplexer redis, ILogger<RedisBoardStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public event Func<Task>? Changed;

    public IReadOnlyList<BoardItem> GetSnapshot()
    {
        var db = _redis.GetDatabase();
        var json = db.StringGet(RedisKeys.BoardItems);
        if (json.IsNullOrEmpty)
        {
            EnsureSeedAsync(db).GetAwaiter().GetResult();
            json = db.StringGet(RedisKeys.BoardItems);
        }

        return JsonSerializer.Deserialize<List<BoardItem>>(json.ToString(), JsonOptions) ?? [];
    }

    public string GetRawJson()
    {
        var db = _redis.GetDatabase();
        var json = db.StringGet(RedisKeys.BoardItems);
        if (json.IsNullOrEmpty)
        {
            return "(empty)";
        }

        try
        {
            using var doc = JsonDocument.Parse(json.ToString());
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json.ToString();
        }
    }

    public bool MoveItem(string itemId, string zoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

        var db = _redis.GetDatabase();
        var json = db.StringGet(RedisKeys.BoardItems);
        if (json.IsNullOrEmpty)
        {
            EnsureSeedAsync(db).GetAwaiter().GetResult();
            json = db.StringGet(RedisKeys.BoardItems);
        }

        var items = JsonSerializer.Deserialize<List<BoardItem>>(json.ToString(), JsonOptions) ?? [];
        var index = items.FindIndex(i => i.Id == itemId);
        if (index < 0)
        {
            return false;
        }

        if (items[index].ZoneId == zoneId)
        {
            return false;
        }

        items[index] = items[index] with { ZoneId = zoneId };
        db.StringSet(RedisKeys.BoardItems, JsonSerializer.Serialize(items, JsonOptions));

        _ = PublishChangedAsync();
        return true;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        await EnsureSeedAsync(db).ConfigureAwait(false);

        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisKeys.BoardChangedChannel),
            (_, _) => _ = NotifyChangedAsync()).ConfigureAwait(false);

        _logger.LogInformation("Subscribed to Redis board change notifications.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal Task PublishChangedAsync()
    {
        var subscriber = _redis.GetSubscriber();
        return subscriber.PublishAsync(RedisChannel.Literal(RedisKeys.BoardChangedChannel), "1");
    }

    private static async Task EnsureSeedAsync(IDatabase db)
    {
        var created = await db.StringSetAsync(
            RedisKeys.BoardItems,
            JsonSerializer.Serialize(SeedItems.ToList(), JsonOptions),
            when: When.NotExists).ConfigureAwait(false);

        if (created)
        {
            return;
        }

        if (!await db.KeyExistsAsync(RedisKeys.BoardItems).ConfigureAwait(false))
        {
            await db.StringSetAsync(
                RedisKeys.BoardItems,
                JsonSerializer.Serialize(SeedItems.ToList(), JsonOptions)).ConfigureAwait(false);
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
