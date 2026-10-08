namespace MvpDockerMessages.Web.Models;

public enum MessageState
{
    Waiting = 0,
    Processed = 1,
    Deleted = 2
}

public sealed record MessageDto(
    Guid Id,
    string Subject,
    string Body,
    MessageState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateMessageStateRequest(MessageState State);
