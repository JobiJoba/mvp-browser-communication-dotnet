namespace MvpBoardBroadcastDb.Api.Models;

public sealed class BoardItemEntity
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
}

public sealed record BoardItemDto(string Id, string Title, string Color, string ZoneId);

public sealed record MoveItemRequest(string ItemId, string ZoneId);
