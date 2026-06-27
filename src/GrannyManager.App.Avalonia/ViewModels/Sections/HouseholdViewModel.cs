using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class HouseholdViewModel : ViewModelBase
{
    private readonly HouseholdService _householdService;

    public HouseholdViewModel(ActiveCaseState activeCaseState, HouseholdService householdService)
    {
        _householdService = householdService ?? throw new ArgumentNullException(nameof(householdService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadPeople();

        AppDataChangeNotifier.HouseholdChanged += (_, _) => LoadPeople();

        LoadPeople();
    }

    public ObservableCollection<HouseholdRowViewModel> People { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPerson))]
    [NotifyPropertyChangedFor(nameof(CanEditPerson))]
    [NotifyPropertyChangedFor(nameof(CanRemovePerson))]
    [NotifyPropertyChangedFor(nameof(SelectedFullName))]
    [NotifyPropertyChangedFor(nameof(SelectedRelationship))]
    [NotifyPropertyChangedFor(nameof(SelectedRole))]
    [NotifyPropertyChangedFor(nameof(SelectedLivesInHousehold))]
    [NotifyPropertyChangedFor(nameof(SelectedPaysRent))]
    [NotifyPropertyChangedFor(nameof(SelectedUsesHouseholdVehicle))]
    [NotifyPropertyChangedFor(nameof(SelectedReceivesRides))]
    [NotifyPropertyChangedFor(nameof(SelectedMonthlyContribution))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedBillsPaid))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private HouseholdRowViewModel? _selectedPerson;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddPerson))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedPerson => SelectedPerson is not null;
    public bool CanAddPerson => HasActiveCase;
    public bool CanEditPerson => HasActiveCase && HasSelectedPerson;
    public bool CanRemovePerson => HasActiveCase && HasSelectedPerson;

    public string SelectedFullName => SelectedPerson?.Person.FullName ?? "No person selected";
    public string SelectedRelationship => Clean(SelectedPerson?.Person.Relationship);
    public string SelectedRole => Clean(SelectedPerson?.Person.Role);
    public string SelectedLivesInHousehold => YesNo(SelectedPerson?.Person.LivesInHousehold);
    public string SelectedPaysRent => YesNo(SelectedPerson?.Person.PaysRent);
    public string SelectedUsesHouseholdVehicle => YesNo(SelectedPerson?.Person.UsesHouseholdVehicle);
    public string SelectedReceivesRides => YesNo(SelectedPerson?.Person.ReceivesRides);
    public string SelectedNotes => Clean(SelectedPerson?.Person.Notes);
    public string SelectedCreatedUtc => FormatDate(SelectedPerson?.Person.CreatedUtc);
    public string SelectedUpdatedUtc => FormatDate(SelectedPerson?.Person.UpdatedUtc);

    public string SelectedMonthlyContribution
    {
        get
        {
            var person = SelectedPerson?.Person;
            if (person is null)
                return "No Contribution";

            var mode = string.IsNullOrWhiteSpace(person.ContributionHandling)
                ? "No Contribution"
                : person.ContributionHandling.Trim();

            if (mode == "Select Income Source")
            {
                return string.IsNullOrWhiteSpace(person.LinkedIncomeSourceName)
                    ? "Select Income Source"
                    : person.LinkedIncomeSourceName;
            }

            if (mode == "Add Income Source")
                return "Add Income Source";

            return "No Contribution";
        }
    }

    public string SelectedBillsPaid
    {
        get
        {
            var person = SelectedPerson?.Person;
            if (person is null)
                return "No bills linked.";

            var bills = _householdService.LoadBillsPaidByPerson(person.Id, person.FullName);
            if (bills.Count == 0)
                return "No bills linked.";

            return string.Join(", ", bills.Select(bill => $"{bill.BillName} ({bill.AmountText})"));
        }
    }

    partial void OnSelectedPersonChanged(HouseholdRowViewModel? oldValue, HouseholdRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public HouseholdPerson CreateBlankPerson()
    {
        return new HouseholdPerson
        {
            LivesInHousehold = true,
            ContributionHandling = "No Contribution",
            IsActive = true
        };
    }

    public HouseholdPerson? CreateEditableCopyOfSelectedPerson()
    {
        var source = SelectedPerson?.Person;
        if (source is null)
            return null;

        return new HouseholdPerson
        {
            Id = source.Id,
            FullName = source.FullName,
            Relationship = source.Relationship,
            Role = source.Role,
            LivesInHousehold = source.LivesInHousehold,
            PaysRent = source.PaysRent,
            UsesHouseholdVehicle = source.UsesHouseholdVehicle,
            ReceivesRides = source.ReceivesRides,
            MonthlyContribution = source.MonthlyContribution,
            ContributionHandling = source.ContributionHandling,
            LinkedIncomeSourceId = source.LinkedIncomeSourceId,
            LinkedIncomeSourceName = source.LinkedIncomeSourceName,
            IsActive = source.IsActive,
            Notes = source.Notes,
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc
        };
    }

    public IReadOnlyList<IncomeSource> GetIncomeSources()
    {
        return _householdService.LoadIncomeSources();
    }

    public IReadOnlyList<HouseholdPerson> GetHouseholdPeople()
    {
        return _householdService.LoadPeople().People;
    }

    public IncomeSource CreateBlankIncomeSource()
    {
        return new IncomeSource
        {
            IncomeType = "Family Contribution",
            Frequency = "Monthly",
            DepositDestination = "Cash/Check",
            IsActive = true
        };
    }

    public bool SaveIncomeSource(IncomeSource source)
    {
        if (!_householdService.SaveIncomeSource(source, out var message))
        {
            StatusMessage = message;
            return false;
        }

        StatusMessage = message;
        return true;
    }


    public bool SavePerson(HouseholdPerson person)
    {
        if (!_householdService.SavePerson(person, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadPeople();
        SelectedPerson = People.FirstOrDefault(item => item.Person.Id == person.Id) ?? People.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedPerson()
    {
        var selectedId = SelectedPerson?.Person.Id ?? 0;
        if (!_householdService.DeletePerson(selectedId, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadPeople();
        StatusMessage = message;
        return true;
    }

    public void RefreshFromNavigation()
    {
        LoadPeople();
    }

    private void LoadPeople()
    {
        var selectedId = SelectedPerson?.Person.Id ?? 0;
        var result = _householdService.LoadPeople();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        People.Clear();

        var index = 0;
        foreach (var person in result.People)
        {
            People.Add(new HouseholdRowViewModel(person, index));
            index++;
        }

        SelectedPerson = People.FirstOrDefault(item => item.Person.Id == selectedId) ?? People.FirstOrDefault();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string YesNo(bool? value)
    {
        return value == true ? "Yes" : "No";
    }

    private static string FormatDate(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "Not saved";

        return value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
    }
}

public sealed partial class HouseholdRowViewModel : ObservableObject
{
    public HouseholdRowViewModel(HouseholdPerson person, int index)
    {
        Person = person ?? throw new ArgumentNullException(nameof(person));
        Index = index;
    }

    public HouseholdPerson Person { get; }

    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string FullName => string.IsNullOrWhiteSpace(Person.FullName) ? "Unnamed Person" : Person.FullName.Trim();
    public string Relationship => string.IsNullOrWhiteSpace(Person.Relationship) ? "None" : Person.Relationship.Trim();
    public string Role => string.IsNullOrWhiteSpace(Person.Role) ? "None" : Person.Role.Trim();

    public bool IsInactive => !Person.IsActive;

    public string NameForeground => IsInactive ? "#7D8795" : "White";
    public string DetailForeground => IsInactive ? "#707A88" : "#C8D4E2";
}
