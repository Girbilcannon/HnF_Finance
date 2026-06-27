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
            AppDataChangeNotifier.NotifyHouseholdChanged();
            statusMessage = "Household member saved.";
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
