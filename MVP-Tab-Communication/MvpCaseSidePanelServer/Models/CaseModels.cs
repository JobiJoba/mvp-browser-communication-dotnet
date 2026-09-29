namespace MvpCaseSidePanelServer.Models;

public sealed record ManagedCase(
    string Id,
    string DisplayName,
    string Status,
    string Summary);

public sealed record CaseLink(string CaseId, string DisplayName);

public sealed record CaseLinkPayload(string CaseId, string DisplayName);

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
    CaseLinkPayload? Payload);
