namespace MvpCaseSidePanelBroadcast.Models;

public sealed record ManagedCase(
    string Id,
    string DisplayName,
    string Status,
    string Summary);

public sealed record CaseLink(string CaseId, string DisplayName);

public sealed record CaseLinkPayload(string CaseId, string DisplayName);

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
    CaseLinkPayload? Payload);
