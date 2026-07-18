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

            if (!debt.IsActive)
                DeactivateBillsLinkedToDebt(activeCase.CaseFolderPath, debt);

            AppDataChangeNotifier.NotifyDebtsChanged();
            AppDataChangeNotifier.NotifyBillsChanged();
            statusMessage = debt.IsActive
                ? "Debt saved."
                : "Debt saved. Linked bill/payment records were deactivated.";
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

    private static void DeactivateBillsLinkedToDebt(string caseFolderPath, Debt debt)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        var billsRepository = new BillsRepository(databasePath);

        foreach (var bill in billsRepository.GetAll())
        {
            var linkedById = debt.Id > 0 && bill.LinkedDebtId == debt.Id;
            var linkedByName = !string.IsNullOrWhiteSpace(debt.DebtName) &&
                string.Equals(bill.LinkedDebtName, debt.DebtName, StringComparison.OrdinalIgnoreCase);

            if (!linkedById && !linkedByName)
                continue;

            if (!bill.IsActive)
                continue;

            bill.IsActive = false;
            bill.Notes = AppendSystemNote(bill.Notes, $"Deactivated because linked debt '{debt.DebtName}' was marked inactive.");
            billsRepository.Upsert(bill);
        }
    }

    private static string AppendSystemNote(string existingNotes, string note)
    {
        if (string.IsNullOrWhiteSpace(existingNotes))
            return note;

        if (existingNotes.Contains(note, StringComparison.OrdinalIgnoreCase))
            return existingNotes;

        return $"{existingNotes.Trim()}{Environment.NewLine}{note}";
    }

    private static DebtsRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new DebtsRepository(databasePath);
    }
}
