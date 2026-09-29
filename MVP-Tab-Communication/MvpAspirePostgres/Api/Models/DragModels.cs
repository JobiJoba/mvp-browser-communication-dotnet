namespace MvpAspirePostgres.Api.Models;

public sealed record BoardItem(string Id, string Title, string Color, string ZoneId);

public sealed record DragPayload(string ItemId, string Title, string Color, string SourceZoneId);

public enum DragEventKind
{
    Started = 1,
    Cancelled = 2,
    DropAccepted = 3
}

public sealed record DragSessionEvent(
    int Version,
    DragEventKind Kind,
    string SessionId,
    string SourceCircuitId,
    string? TargetCircuitId,
    string? TargetZoneId,
    DragPayload? Payload);

public sealed record MoveItemRequest(string ItemId, string ZoneId);

public sealed record BeginDragRequest(string CircuitId, string SessionId, DragPayload Payload);

public sealed record CancelDragRequest(string CircuitId, string SessionId);

public sealed record AcceptDropRequest(
    string TargetCircuitId,
    string SessionId,
    string SourceCircuitId,
    string TargetZoneId,
    DragPayload Payload);
