using MvpCaseSidePanelBroadcast.Models;
using MvpCaseSidePanelBroadcast.Services.Cases;

namespace MvpCaseSidePanelBroadcast.Services.SidePanel;

public sealed class SidePanelState
{
    private readonly ICaseCatalog _catalog;
    private readonly List<CaseLink> _links = [];

    public SidePanelState(ICaseCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<CaseLink> Links => _links;

    public string? GeneratedAs { get; private set; }

    public string? SelectedCaseId { get; private set; }

    public event Action? Changed;

    public void GenerateForTab(string tabKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tabKey);

        _links.Clear();
        foreach (var link in _catalog.GetSeedLinks(tabKey))
        {
            _links.Add(link);
        }

        GeneratedAs = tabKey.Equals("A", StringComparison.OrdinalIgnoreCase) ? "Tab A"
            : tabKey.Equals("B", StringComparison.OrdinalIgnoreCase) ? "Tab B"
            : tabKey;

        SelectedCaseId = _links.FirstOrDefault()?.CaseId;
        Changed?.Invoke();
    }

    public void SelectCase(string caseId)
    {
        SelectedCaseId = caseId;
        Changed?.Invoke();
    }

    public bool Add(CaseLink link)
    {
        if (_links.Any(l => l.CaseId.Equals(link.CaseId, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _links.Add(link);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(string caseId)
    {
        var removed = _links.RemoveAll(l => l.CaseId.Equals(caseId, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed)
        {
            if (SelectedCaseId is not null
                && SelectedCaseId.Equals(caseId, StringComparison.OrdinalIgnoreCase))
            {
                SelectedCaseId = _links.FirstOrDefault()?.CaseId;
            }

            Changed?.Invoke();
        }

        return removed;
    }
}
