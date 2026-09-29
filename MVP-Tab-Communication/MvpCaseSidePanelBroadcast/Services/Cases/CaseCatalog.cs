using MvpCaseSidePanelBroadcast.Models;

namespace MvpCaseSidePanelBroadcast.Services.Cases;

public sealed class CaseCatalog : ICaseCatalog
{
    private static readonly IReadOnlyList<ManagedCase> Cases =
    [
        new("mariana", "Latte Mariana", "Active",
            "Primary managed case for Mariana Latte. Housing follow-up and family coordination."),
        new("jean", "Latte Jean", "Active",
            "Primary managed case for Jean Latte. Benefits review and appointment scheduling."),
        new("sophie", "Latte Sophie", "Active",
            "Related family case (Mariana’s household). School enrollment support."),
        new("pierre", "Latte Pierre", "Monitoring",
            "Related family case (Jean’s household). Medical paperwork pending."),
        new("dossier-m-housing", "Dossier Housing — Mariana", "In progress",
            "Housing application dossier linked to Mariana’s managed case."),
        new("dossier-j-benefits", "Dossier Benefits — Jean", "In progress",
            "Benefits dossier linked to Jean’s managed case.")
    ];

    private static readonly Dictionary<string, string[]> Seeds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = ["mariana", "sophie", "dossier-m-housing"],
        ["B"] = ["jean", "pierre", "dossier-j-benefits"],
        ["mariana"] = ["mariana", "sophie", "dossier-m-housing"],
        ["jean"] = ["jean", "pierre", "dossier-j-benefits"]
    };

    public ManagedCase? GetById(string caseId) =>
        Cases.FirstOrDefault(c => c.Id.Equals(caseId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ManagedCase> GetAll() => Cases;

    public IReadOnlyList<CaseLink> GetSeedLinks(string workspaceCaseId)
    {
        if (!Seeds.TryGetValue(workspaceCaseId, out var ids))
        {
            var single = GetById(workspaceCaseId);
            return single is null
                ? []
                : [new CaseLink(single.Id, single.DisplayName)];
        }

        return ids
            .Select(GetById)
            .Where(c => c is not null)
            .Select(c => new CaseLink(c!.Id, c.DisplayName))
            .ToList();
    }
}
