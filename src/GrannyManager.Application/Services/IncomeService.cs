using System;
using System.Collections.Generic;
using System.Linq;
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
            var repository = CreateRepository(activeCase.CaseFolderPath);
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

    public IReadOnlyList<HouseholdPerson> LoadHouseholdPeople()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<HouseholdPerson>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new HouseholdPeopleRepository(databasePath);
            return repository.GetAll().Where(p => p.IsActive).OrderBy(p => p.FullName).ToList();
        }
        catch
        {
            return Array.Empty<HouseholdPerson>();
        }
    }


    public IReadOnlyList<AssetItem> LoadBankAccounts()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<AssetItem>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new AssetsRepository(databasePath);
            return repository.GetBankAccounts();
        }
        catch
        {
            return Array.Empty<AssetItem>();
        }
    }

    public bool SaveBankAccount(AssetItem asset, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving bank accounts.";
            return false;
        }

        if (asset is null)
        {
            statusMessage = "No bank account was provided.";
            return false;
        }

        asset.AssetType = "Bank Account";

        if (string.IsNullOrWhiteSpace(asset.AssetName))
        {
            statusMessage = "Enter a bank account name before saving.";
            return false;
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new AssetsRepository(databasePath);
            repository.Upsert(asset);
            AppDataChangeNotifier.NotifyAssetsChanged();
            statusMessage = "Bank account saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save bank account: {ex.Message}";
            return false;
        }
    }

    public bool SaveSource(IncomeSource source, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving income sources.";
            return false;
        }

        if (source is null)
        {
            statusMessage = "No income source was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(source.SourceName))
        {
            statusMessage = "Enter a source name before saving.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            repository.Upsert(source);
            AppDataChangeNotifier.NotifyIncomeSourcesChanged();
            statusMessage = "Income source saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save income source: {ex.Message}";
            return false;
        }
    }

    public bool DeleteSource(long id, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing income sources.";
            return false;
        }

        if (id <= 0)
        {
            statusMessage = "Select an income source before removing.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            repository.Delete(id);
            AppDataChangeNotifier.NotifyIncomeSourcesChanged();
            statusMessage = "Income source removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove income source: {ex.Message}";
            return false;
        }
    }

    private static IncomeSourcesRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new IncomeSourcesRepository(databasePath);
    }

    private static IReadOnlyList<IncomeSource> SortForIncomeList(IEnumerable<IncomeSource> sources)
    {
        return sources
            .OrderBy(source => source.IsActive ? 0 : 1)
            .ThenBy(source => source.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
