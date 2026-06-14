using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class AllowanceSavingsService
{
    private readonly ActiveCaseState _activeCaseState;

    public AllowanceSavingsService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public AllowanceSavingsLoadResult LoadItems()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new AllowanceSavingsLoadResult(
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding allowance or savings items.",
                Items: Array.Empty<AllowanceSavingsItem>());
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);

            // This repository predates the newer constructor pattern, so make sure the database exists here.
            DatabaseInitializer.EnsureCreated(databasePath);

            var repository = new AllowanceSavingsRepository(databasePath);
            var items = repository.GetAll();

            return new AllowanceSavingsLoadResult(
                HasActiveCase: true,
                StatusMessage: items.Count == 0
                    ? "No allowance or savings items have been added to this case yet."
                    : $"{items.Count} allowance/savings item(s) loaded.",
                Items: items);
        }
        catch (Exception ex)
        {
            return new AllowanceSavingsLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load allowance/savings items: {ex.Message}",
                Items: Array.Empty<AllowanceSavingsItem>());
        }
    }
}
