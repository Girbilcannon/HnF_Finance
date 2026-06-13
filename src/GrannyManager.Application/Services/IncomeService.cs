using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class IncomeService
{
    private readonly ActiveCaseState _activeCaseState;

    public IncomeService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public IncomeLoadResult LoadSources()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new IncomeLoadResult(
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding income sources.",
                Sources: Array.Empty<IncomeSource>());
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new IncomeSourcesRepository(databasePath);
            var sources = SortForIncomeList(repository.GetAll());

            return new IncomeLoadResult(
                HasActiveCase: true,
                StatusMessage: sources.Count == 0
                    ? "No income sources have been added to this case yet."
                    : $"{sources.Count} income source(s) loaded.",
                Sources: sources);
        }
        catch (Exception ex)
        {
            return new IncomeLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load income sources: {ex.Message}",
                Sources: Array.Empty<IncomeSource>());
        }
    }

    private static IReadOnlyList<IncomeSource> SortForIncomeList(IEnumerable<IncomeSource> sources)
    {
        return sources
            .OrderBy(source => source.IsActive ? 0 : 1)
            .ThenBy(source => source.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
