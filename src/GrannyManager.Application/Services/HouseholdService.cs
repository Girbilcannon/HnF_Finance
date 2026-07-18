using System;
using System.Collections.Generic;
using System.Linq;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class HouseholdService
{
    private readonly ActiveCaseState _activeCaseState;

    public HouseholdService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public HouseholdLoadResult LoadPeople()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new HouseholdLoadResult(
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding household members.",
                People: Array.Empty<HouseholdPerson>());
        }

        try
        {
            var repository = CreateHouseholdRepository(activeCase.CaseFolderPath);
            var people = SortForHouseholdList(repository.GetAll());

            return new HouseholdLoadResult(
                HasActiveCase: true,
                StatusMessage: people.Count == 0
                    ? "No household members have been added to this case yet."
                    : $"{people.Count} household record(s) loaded.",
                People: people);
        }
        catch (Exception ex)
        {
            return new HouseholdLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load household records: {ex.Message}",
                People: Array.Empty<HouseholdPerson>());
        }
    }

    public IReadOnlyList<IncomeSource> LoadIncomeSources()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<IncomeSource>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new IncomeSourcesRepository(databasePath);
            return repository.GetAll().Where(source => source.IsActive).OrderBy(source => source.SourceName).ToList();
        }
        catch
        {
            return Array.Empty<IncomeSource>();
        }
    }

    public IReadOnlyList<Bill> LoadBillsPaidByPerson(long householdPersonId, string householdPersonName)
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<Bill>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new BillsRepository(databasePath);
            return repository.GetAll()
                .Where(bill => bill.IsActive)
                .Where(bill =>
                    (householdPersonId > 0 && bill.PaidByHouseholdPersonId == householdPersonId) ||
                    (!string.IsNullOrWhiteSpace(householdPersonName) && string.Equals(bill.PaidBy, householdPersonName, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(bill => bill.BillName)
                .ToList();
        }
        catch
        {
            return Array.Empty<Bill>();
        }
    }

    public HouseholdInactiveImpactPreview GetInactiveImpactPreview(HouseholdPerson person)
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid || person is null || person.Id <= 0 || person.IsActive)
            return HouseholdInactiveImpactPreview.Empty;

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);

        var incomes = new IncomeSourcesRepository(databasePath)
            .GetAll()
            .Where(source => source.IsActive)
            .Where(source =>
                source.LinkedHouseholdPersonId == person.Id ||
                (!string.IsNullOrWhiteSpace(person.FullName) &&
                 string.Equals(source.LinkedHouseholdPersonName, person.FullName, StringComparison.OrdinalIgnoreCase)))
            .Select(source => source.SourceName)
            .ToList();

        var bills = new BillsRepository(databasePath)
            .GetAll()
            .Where(bill => bill.IsActive)
            .Where(bill =>
                bill.PaidByHouseholdPersonId == person.Id ||
                bill.ResponsibilityOwnerHouseholdPersonId == person.Id ||
                (!string.IsNullOrWhiteSpace(person.FullName) &&
                 (string.Equals(bill.PaidBy, person.FullName, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(bill.ResponsibilityOwner, person.FullName, StringComparison.OrdinalIgnoreCase))))
            .Select(bill => bill.BillName)
            .ToList();

        var debts = new DebtsRepository(databasePath)
            .GetAll()
            .Where(debt => debt.IsActive)
            .Where(debt =>
                !string.IsNullOrWhiteSpace(person.FullName) &&
                (string.Equals(debt.PaidBy, person.FullName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(debt.ResponsibilityOwner, person.FullName, StringComparison.OrdinalIgnoreCase)))
            .Select(debt => debt.DebtName)
            .ToList();

        return new HouseholdInactiveImpactPreview(incomes, bills, debts);
    }

    public IReadOnlyList<HouseholdPerson> LoadTransferTargets(long personBeingDeactivatedId)
    {
        var result = LoadPeople();
        return result.People
            .Where(person => person.IsActive)
            .Where(person => person.Id != personBeingDeactivatedId)
            .ToList();
    }

    public bool SaveIncomeSource(IncomeSource source, out string statusMessage)
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
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new IncomeSourcesRepository(databasePath);
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

    public bool SavePerson(HouseholdPerson person, out string statusMessage)
    {
        return SavePerson(person, null, out statusMessage);
    }

    public bool SavePerson(HouseholdPerson person, HouseholdPerson? transferTarget, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving household members.";
            return false;
        }

        if (person is null)
        {
            statusMessage = "No household member was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(person.FullName))
        {
            statusMessage = "Enter a name before saving this household member.";
            return false;
        }

        try
        {
            var repository = CreateHouseholdRepository(activeCase.CaseFolderPath);
            repository.Upsert(person);

            if (!person.IsActive)
                ApplyInactivePersonDependencies(activeCase.CaseFolderPath, person, transferTarget);

            AppDataChangeNotifier.NotifyHouseholdChanged();
            AppDataChangeNotifier.NotifyIncomeSourcesChanged();
            AppDataChangeNotifier.NotifyBillsChanged();
            AppDataChangeNotifier.NotifyDebtsChanged();

            statusMessage = person.IsActive
                ? "Household member saved."
                : "Household member saved. Linked income was deactivated and payment responsibility was transferred where needed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save household member: {ex.Message}";
            return false;
        }
    }

    public bool DeletePerson(long id, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing household members.";
            return false;
        }

        if (id <= 0)
        {
            statusMessage = "Select a household member before removing.";
            return false;
        }

        try
        {
            var repository = CreateHouseholdRepository(activeCase.CaseFolderPath);
            repository.Delete(id);
            AppDataChangeNotifier.NotifyHouseholdChanged();
            statusMessage = "Household member removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove household member: {ex.Message}";
            return false;
        }
    }

    private static void ApplyInactivePersonDependencies(string caseFolderPath, HouseholdPerson inactivePerson, HouseholdPerson? transferTarget)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);

        var incomeRepository = new IncomeSourcesRepository(databasePath);
        foreach (var source in incomeRepository.GetAll())
        {
            var linked = source.LinkedHouseholdPersonId == inactivePerson.Id ||
                string.Equals(source.LinkedHouseholdPersonName, inactivePerson.FullName, StringComparison.OrdinalIgnoreCase);

            if (!linked || !source.IsActive)
                continue;

            source.IsActive = false;
            incomeRepository.Upsert(source);
        }

        if (transferTarget is null)
            return;

        var billsRepository = new BillsRepository(databasePath);
        foreach (var bill in billsRepository.GetAll())
        {
            var changed = false;

            if (bill.PaidByHouseholdPersonId == inactivePerson.Id ||
                string.Equals(bill.PaidBy, inactivePerson.FullName, StringComparison.OrdinalIgnoreCase))
            {
                bill.PaidByHouseholdPersonId = transferTarget.Id;
                bill.PaidBy = transferTarget.Id > 0 ? transferTarget.FullName : "Self (Primary Person)";
                changed = true;
            }

            if (bill.ResponsibilityOwnerHouseholdPersonId == inactivePerson.Id ||
                string.Equals(bill.ResponsibilityOwner, inactivePerson.FullName, StringComparison.OrdinalIgnoreCase))
            {
                bill.ResponsibilityOwnerHouseholdPersonId = transferTarget.Id;
                bill.ResponsibilityOwner = transferTarget.Id > 0 ? transferTarget.FullName : "Self (Primary Person)";
                changed = true;
            }

            if (changed)
                billsRepository.Upsert(bill);
        }

        var debtsRepository = new DebtsRepository(databasePath);
        foreach (var debt in debtsRepository.GetAll())
        {
            var changed = false;
            var transferName = transferTarget.Id > 0 ? transferTarget.FullName : "Self (Primary Person)";

            if (string.Equals(debt.PaidBy, inactivePerson.FullName, StringComparison.OrdinalIgnoreCase))
            {
                debt.PaidBy = transferName;
                changed = true;
            }

            if (string.Equals(debt.ResponsibilityOwner, inactivePerson.FullName, StringComparison.OrdinalIgnoreCase))
            {
                debt.ResponsibilityOwner = transferName;
                changed = true;
            }

            if (changed)
                debtsRepository.Upsert(debt);
        }
    }

    private static HouseholdPeopleRepository CreateHouseholdRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new HouseholdPeopleRepository(databasePath);
    }

    private static IReadOnlyList<HouseholdPerson> SortForHouseholdList(IEnumerable<HouseholdPerson> people)
    {
        return people
            .OrderBy(p => GetSortGroup(p))
            .ThenBy(p => p.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static int GetSortGroup(HouseholdPerson person)
    {
        if (IsPrimaryPerson(person))
            return 0;

        if (person.LivesInHousehold)
            return 1;

        return 2;
    }

    private static bool IsPrimaryPerson(HouseholdPerson person)
    {
        return person.Relationship.Equals("Self", StringComparison.OrdinalIgnoreCase)
            || person.Role.Contains("Primary", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record HouseholdInactiveImpactPreview(
    IReadOnlyList<string> IncomeSources,
    IReadOnlyList<string> Bills,
    IReadOnlyList<string> Debts)
{
    public bool HasAnyImpact => IncomeSources.Count > 0 || Bills.Count > 0 || Debts.Count > 0;

    public static HouseholdInactiveImpactPreview Empty { get; } = new(
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>());
}
