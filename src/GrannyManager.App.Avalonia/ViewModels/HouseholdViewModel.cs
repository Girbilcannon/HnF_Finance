using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class HouseholdViewModel : ViewModelBase
{
    private readonly HouseholdService _householdService;

    public HouseholdViewModel(ActiveCaseState activeCaseState, HouseholdService householdService)
    {
        _householdService = householdService ?? throw new ArgumentNullException(nameof(householdService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadPeople();

        LoadPeople();
    }

    public ObservableCollection<HouseholdPersonListItemViewModel> People { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPerson))]
    [NotifyPropertyChangedFor(nameof(SelectedFullName))]
    [NotifyPropertyChangedFor(nameof(SelectedRelationship))]
    [NotifyPropertyChangedFor(nameof(SelectedRole))]
    [NotifyPropertyChangedFor(nameof(SelectedLivesInHousehold))]
    [NotifyPropertyChangedFor(nameof(SelectedPaysRent))]
    [NotifyPropertyChangedFor(nameof(SelectedUsesHouseholdVehicle))]
    [NotifyPropertyChangedFor(nameof(SelectedReceivesRides))]
    [NotifyPropertyChangedFor(nameof(SelectedMonthlyContribution))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private HouseholdPersonListItemViewModel? _selectedPerson;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    public bool HasSelectedPerson => SelectedPerson is not null;

    public string SelectedFullName => SelectedPerson?.Person.FullName ?? "No person selected";
    public string SelectedRelationship => Clean(SelectedPerson?.Person.Relationship);
    public string SelectedRole => Clean(SelectedPerson?.Person.Role);
    public string SelectedLivesInHousehold => YesNo(SelectedPerson?.Person.LivesInHousehold);
    public string SelectedPaysRent => YesNo(SelectedPerson?.Person.PaysRent);
    public string SelectedUsesHouseholdVehicle => YesNo(SelectedPerson?.Person.UsesHouseholdVehicle);
    public string SelectedReceivesRides => YesNo(SelectedPerson?.Person.ReceivesRides);
    public string SelectedNotes => Clean(SelectedPerson?.Person.Notes);
    public string SelectedCreatedUtc => FormatUtc(SelectedPerson?.Person.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(SelectedPerson?.Person.UpdatedUtc);

    public string SelectedMonthlyContribution
    {
        get
        {
            var person = SelectedPerson?.Person;
            if (person is null)
                return "None";

            if (person.LinkedIncomeSourceId > 0)
                return string.IsNullOrWhiteSpace(person.LinkedIncomeSourceName)
                    ? "Linked income source"
                    : person.LinkedIncomeSourceName;

            if (person.MonthlyContribution > 0)
                return person.MonthlyContribution.ToString("C2");

            return "None";
        }
    }

    partial void OnSelectedPersonChanged(HouseholdPersonListItemViewModel? oldValue, HouseholdPersonListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadPeople();
    }

    private void LoadPeople()
    {
        var result = _householdService.LoadPeople();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        People.Clear();

        var index = 0;
        foreach (var person in result.People)
        {
            People.Add(new HouseholdPersonListItemViewModel(person, index));
            index++;
        }

        SelectedPerson = People.FirstOrDefault();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string YesNo(bool? value)
    {
        return value == true ? "Yes" : "No";
    }

    private static string FormatUtc(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "Not saved";

        return value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

public sealed partial class HouseholdPersonListItemViewModel : ObservableObject
{
    public HouseholdPersonListItemViewModel(HouseholdPerson person, int index)
    {
        Person = person ?? throw new ArgumentNullException(nameof(person));
        Index = index;
    }

    public HouseholdPerson Person { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(RelationshipForeground))]
    [NotifyPropertyChangedFor(nameof(RoleForeground))]
    private bool _isSelected;

    public string FullName => string.IsNullOrWhiteSpace(Person.FullName) ? "Unnamed Person" : Person.FullName.Trim();
    public string Relationship => string.IsNullOrWhiteSpace(Person.Relationship) ? "None" : Person.Relationship.Trim();
    public string Role => string.IsNullOrWhiteSpace(Person.Role) ? "None" : Person.Role.Trim();

    public bool IsInactive => !Person.LivesInHousehold && !IsPrimaryPerson;

    public bool IsPrimaryPerson =>
        Person.Relationship.Equals("Self", StringComparison.OrdinalIgnoreCase)
        || Person.Role.Contains("Primary", StringComparison.OrdinalIgnoreCase);

    public string RowBackground
    {
        get
        {
            if (IsSelected)
                return "#2A6FA8";

            if (IsInactive)
                return "#1A1F29";

            return Index % 2 == 0 ? "#122238" : "#0F1B2A";
        }
    }

    public string RowForeground
    {
        get
        {
            if (IsSelected)
                return "White";

            return IsInactive ? "#7D8795" : "#DDE7F3";
        }
    }

    public string RelationshipForeground => IsSelected ? "White" : IsInactive ? "#707A88" : "#C8D4E2";
    public string RoleForeground => IsSelected ? "White" : IsInactive ? "#707A88" : "#C8D4E2";
}
