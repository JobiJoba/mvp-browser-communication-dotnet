using MvpCaseSidePanelBroadcast.Models;

namespace MvpCaseSidePanelBroadcast.Services.SidePanel;

public sealed record SidePanelSnapshot(
    string? GeneratedAs,
    string? SelectedCaseId,
    IReadOnlyList<CaseLink> Links);
