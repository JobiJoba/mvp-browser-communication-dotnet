using MvpAspireMessages.Web.Models;

namespace MvpAspireMessages.Web.Services;

/// <summary>
/// Circuit-scoped list cache for /messages-cache. Survives remounts within the same
/// Blazor circuit; other browser tabs have their own circuit and get patches via BroadcastChannel.
/// </summary>
public sealed class MessagesListCache
{
    private List<MessageDto>? _items;

    public IReadOnlyList<MessageDto>? Items => _items;

    public bool IsLoaded => _items is not null;

    public event Action? Changed;

    public async Task EnsureLoadedAsync(MessagesApiClient api, CancellationToken ct = default)
    {
        if (_items is not null)
        {
            return;
        }

        _items = (await api.GetMessagesAsync(ct)).ToList();
        Changed?.Invoke();
    }

    public void Apply(MessageDto updated)
    {
        if (_items is null)
        {
            return;
        }

        var index = _items.FindIndex(m => m.Id == updated.Id);
        if (index >= 0)
        {
            _items[index] = updated;
        }
        else
        {
            _items.Insert(0, updated);
        }

        Changed?.Invoke();
    }
}
