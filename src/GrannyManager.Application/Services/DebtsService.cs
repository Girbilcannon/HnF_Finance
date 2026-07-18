using System;
using System.Collections.Generic;
using System.Linq;
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
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var debts = repository.GetAll()
                .OrderBy(debt => debt.IsActive ? 0 : 1)
                .ThenBy(debt => debt.Priority, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(debt => debt.DebtName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new DebtsLoadResult(
                HasActiveCase: true,
                StatusMessage: debts.Count == 0 ? "No debts have been added to this case yet." : $"{debts.Count} debt record(s) loaded.",
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

    public IReadOnlyList<Debt> LoadCreditCards()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<Debt>();

        try
        {
            return CreateRepository(activeCase.CaseFolderPath).GetCreditCards();
        }
        catch
        {
            return Array.Empty<Debt>();
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

    public bool SaveDebt(Debt debt, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving debts.";
            return false;
        }

        if (debt is null)
        {
            statusMessage = "No debt was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(debt.DebtName))
        {
            statusMessage = "Enter a debt name before saving.";
            return false;
        }

        try
        {
            CreateRepository(activeCase.CaseFolderPath).Upsert(debt);
            AppDataChangeNotifier.NotifyDebtsChanged();
            statusMessage = "Debt saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save debt: {ex.Message}";
            return false;
        }
    }

    public bool DeleteDebt(long id, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing debts.";
            return false;
        }

        if (id <= 0)
        {
            statusMessage = "Select a debt before removing.";
            return false;
        }

        try
        {
            CreateRepository(activeCase.CaseFolderPath).Delete(id);
            AppDataChangeNotifier.NotifyDebtsChanged();
            statusMessage = "Debt removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove debt: {ex.Message}";
            return false;
        }
    }

    private static DebtsRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new DebtsRepository(databasePath);
    }
}
