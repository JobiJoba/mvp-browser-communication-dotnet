using MvpDockerMessages.Api.Models;

namespace MvpDockerMessages.Api.Services;

public sealed class MessageStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, MessageDto> _messages;

    public MessageStore()
    {
        var now = DateTimeOffset.UtcNow;
        _messages = new Dictionary<Guid, MessageDto>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Welcome to the inbox",
                "This is the first seeded message. Open it to change its state.",
                MessageState.Waiting,
                now.AddMinutes(-30),
                null),
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Invoice #4821",
                "Please review and mark as processed when done.",
                MessageState.Waiting,
                now.AddMinutes(-20),
                null),
            [Guid.Parse("33333333-3333-3333-3333-333333333333")] = new(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Spam candidate",
                "Looks suspicious — candidate for delete.",
                MessageState.Waiting,
                now.AddMinutes(-10),
                null),
            [Guid.Parse("44444444-4444-4444-4444-444444444444")] = new(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "Already handled",
                "This one starts in Processed so you can see mixed states on the overview.",
                MessageState.Processed,
                now.AddHours(-2),
                now.AddHours(-1)),
            [Guid.Parse("55555555-5555-5555-5555-555555555555")] = new(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "Removed earlier",
                "Soft-deleted sample for the list badges.",
                MessageState.Deleted,
                now.AddDays(-1),
                now.AddHours(-3))
        };
    }

    public IReadOnlyList<MessageDto> GetAll()
    {
        lock (_gate)
        {
            return _messages.Values
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
        }
    }

    public MessageDto? Get(Guid id)
    {
        lock (_gate)
        {
            return _messages.TryGetValue(id, out var message) ? message : null;
        }
    }

    public MessageDto? UpdateState(Guid id, MessageState state)
    {
        lock (_gate)
        {
            if (!_messages.TryGetValue(id, out var existing))
            {
                return null;
            }

            var updated = existing with
            {
                State = state,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _messages[id] = updated;
            return updated;
        }
    }
}
