using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class DebtsService
{
    private readonly ActiveCaseState _activeCaseState;

    public DebtsService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public DebtsLoadResult LoadDebts()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new DebtsLoadResult(
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding debts.",
                Debts: Array.Empty<Debt>());
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new DebtsRepository(databasePath);
            var debts = repository.GetAll();

            return new DebtsLoadResult(
                HasActiveCase: true,
                StatusMessage: debts.Count == 0
                    ? "No debts have been added to this case yet."
                    : $"{debts.Count} debt record(s) loaded.",
                Debts: debts);
        }
        catch (Exception ex)
        {
            return new DebtsLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load debts: {ex.Message}",
                Debts: Array.Empty<Debt>());
        }
    }
}
