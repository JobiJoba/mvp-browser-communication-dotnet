namespace MvpBroadcastChannel.Models;

public sealed record BoardItem(string Id, string Title, string Color, string ZoneId);

public sealed record DragPayload(string ItemId, string Title, string Color, string SourceZoneId);

public enum CrossTabMessageType
{
    DragStarted = 1,
    DragCancelled = 2,
    DropAccepted = 3
}

public sealed record CrossTabEnvelope(
    int Version,
    CrossTabMessageType Type,
    string SessionId,
    string TabId,
    string? TargetZoneId,
    DragPayload? Payload);
