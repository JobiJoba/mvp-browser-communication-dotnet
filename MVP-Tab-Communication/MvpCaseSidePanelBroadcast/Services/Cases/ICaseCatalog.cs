using MvpCaseSidePanelBroadcast.Models;

namespace MvpCaseSidePanelBroadcast.Services.Cases;

public interface ICaseCatalog
{
    ManagedCase? GetById(string caseId);

    IReadOnlyList<CaseLink> GetSeedLinks(string workspaceCaseId);

    IReadOnlyList<ManagedCase> GetAll();
}
