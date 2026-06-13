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
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new HouseholdPeopleRepository(databasePath);
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
