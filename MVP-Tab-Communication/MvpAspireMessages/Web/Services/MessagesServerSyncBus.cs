using System.Collections.Concurrent;
using MvpAspireMessages.Web.Models;

namespace MvpAspireMessages.Web.Services;

/// <summary>
/// In-process pub/sub across Blazor Server circuits on this Web instance.
/// Used by /messages-cache-server instead of browser BroadcastChannel.
/// Single-node only — use a SignalR/Redis backplane if you scale out Web replicas.
/// </summary>
public sealed class MessagesServerSyncBus
{
    private readonly ConcurrentDictionary<string, Func<MessageDto, Task>> _subscribers = new();

    public IDisposable Subscribe(string subscriberId, Func<MessageDto, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberId);
        ArgumentNullException.ThrowIfNull(handler);

        _subscribers[subscriberId] = handler;
        return new Subscription(() => _subscribers.TryRemove(subscriberId, out _));
    }

    public async Task PublishAsync(MessageDto message)
    {
        foreach (var (_, handler) in _subscribers.ToArray())
        {
            try
            {
                await handler(message).ConfigureAwait(false);
            }
            catch
            {
                // Isolate subscriber failures; circuits unsubscribe via Dispose.
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
