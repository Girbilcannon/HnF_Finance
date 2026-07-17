using System;
using System.Collections.Generic;
using System.Linq;
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
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var items = repository.GetAll()
                .OrderBy(item => item.IsActive ? 0 : 1)
                .ThenBy(item => item.ItemType, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.ItemName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

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

    public bool SaveItem(AllowanceSavingsItem item, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving allowance or savings items.";
            return false;
        }

        if (item is null)
        {
            statusMessage = "No allowance/savings item was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.ItemName))
        {
            statusMessage = "Enter a name before saving.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            repository.Upsert(item);
            AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
            statusMessage = "Allowance/savings item saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save allowance/savings item: {ex.Message}";
            return false;
        }
    }

    public bool DeleteItem(long id, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing allowance or savings items.";
            return false;
        }

        if (id <= 0)
        {
            statusMessage = "Select an allowance/savings item before removing.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            repository.Delete(id);
            AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
            statusMessage = "Allowance/savings item removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove allowance/savings item: {ex.Message}";
            return false;
        }
    }

    private static AllowanceSavingsRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new AllowanceSavingsRepository(databasePath);
    }
}
