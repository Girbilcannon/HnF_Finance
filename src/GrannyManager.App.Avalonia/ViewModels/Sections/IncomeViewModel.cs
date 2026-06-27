using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class IncomeViewModel : ViewModelBase
{
    private readonly IncomeService _incomeService;

    public IncomeViewModel(ActiveCaseState activeCaseState, IncomeService incomeService)
    {
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadSources();

        AppDataChangeNotifier.IncomeSourcesChanged += (_, _) => LoadSources();

        LoadSources();
    }

    public ObservableCollection<IncomeRowViewModel> Sources { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSource))]
    [NotifyPropertyChangedFor(nameof(CanEditSource))]
    [NotifyPropertyChangedFor(nameof(CanRemoveSource))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceName))]
    [NotifyPropertyChangedFor(nameof(SelectedIncomeType))]
    [NotifyPropertyChangedFor(nameof(SelectedActiveText))]
    [NotifyPropertyChangedFor(nameof(SelectedTaxHandling))]
    [NotifyPropertyChangedFor(nameof(SelectedAmountLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedAmount))]
    [NotifyPropertyChangedFor(nameof(SelectedFrequency))]
    [NotifyPropertyChangedFor(nameof(SelectedMonthlyEquivalent))]
    [NotifyPropertyChangedFor(nameof(SelectedExpectedDayOrDate))]
    [NotifyPropertyChangedFor(nameof(SelectedDepositDestination))]
    [NotifyPropertyChangedFor(nameof(SelectedHouseholdLink))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private IncomeRowViewModel? _selectedSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddSource))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedSource => SelectedSource is not null;
    public bool CanAddSource => HasActiveCase;
    public bool CanEditSource => HasActiveCase && HasSelectedSource;
    public bool CanRemoveSource => HasActiveCase && HasSelectedSource;

    public string SelectedSourceName => SelectedSource?.Source.SourceName ?? "No income source selected";
    public string SelectedIncomeType => Clean(SelectedSource?.Source.IncomeType);
    public string SelectedActiveText => YesNo(SelectedSource?.Source.IsActive);
    public string SelectedTaxHandling => SelectedSource?.Source.TaxHandlingText ?? "None";
    public string SelectedAmountLabel => SelectedSource?.Source.AmountLabel ?? "Amount";
    public string SelectedAmount => SelectedSource?.Source.Amount.ToString("C2") ?? "$0.00";
    public string SelectedFrequency => Clean(SelectedSource?.Source.Frequency);
    public string SelectedMonthlyEquivalent => SelectedSource?.Source.MonthlyEquivalentText ?? "$0.00";
    public string SelectedExpectedDayOrDate => Clean(SelectedSource?.Source.ExpectedDayOrDate);
    public string SelectedDepositDestination => SelectedSource?.Source.DepositDisplayText ?? "Not specified";
    public string SelectedHouseholdLink => Clean(SelectedSource?.Source.LinkedHouseholdPersonName);
    public string SelectedNotes => Clean(SelectedSource?.Source.Notes);
    public string SelectedCreatedUtc => FormatDate(SelectedSource?.Source.CreatedUtc);
    public string SelectedUpdatedUtc => FormatDate(SelectedSource?.Source.UpdatedUtc);

    partial void OnSelectedSourceChanged(IncomeRowViewModel? oldValue, IncomeRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public IncomeSource CreateBlankSource()
    {
        return new IncomeSource
        {
            IncomeType = "Social Security",
            Frequency = "Monthly",
            IsActive = true
        };
    }

    public IncomeSource? CreateEditableCopyOfSelectedSource()
    {
        var source = SelectedSource?.Source;
        if (source is null)
            return null;

        return new IncomeSource
        {
            Id = source.Id,
            SourceName = source.SourceName,
            IncomeType = source.IncomeType,
            Amount = source.Amount,
            Frequency = source.Frequency,
            TaxesWithheld = source.TaxesWithheld,
            ExpectedDayOrDate = source.ExpectedDayOrDate,
            DepositDestination = source.DepositDestination,
            LinkedHouseholdPersonId = source.LinkedHouseholdPersonId,
            LinkedHouseholdPersonName = source.LinkedHouseholdPersonName,
            IsActive = source.IsActive,
            Notes = source.Notes,
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc
        };
    }

    public IReadOnlyList<HouseholdPerson> GetHouseholdPeople()
    {
        return _incomeService.LoadHouseholdPeople();
    }

    public bool SaveSource(IncomeSource source)
    {
        if (!_incomeService.SaveSource(source, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadSources();
        SelectedSource = Sources.FirstOrDefault(item => item.Source.Id == source.Id) ?? Sources.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedSource()
    {
        var selectedId = SelectedSource?.Source.Id ?? 0;
        if (!_incomeService.DeleteSource(selectedId, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadSources();
        StatusMessage = message;
        return true;
    }

    public void RefreshFromNavigation()
    {
        LoadSources();
    }

    private void LoadSources()
    {
        var selectedId = SelectedSource?.Source.Id ?? 0;
        var result = _incomeService.LoadSources();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Sources.Clear();

        var index = 0;
        foreach (var source in result.Sources)
        {
            Sources.Add(new IncomeRowViewModel(source, index));
            index++;
        }

        SelectedSource = Sources.FirstOrDefault(item => item.Source.Id == selectedId) ?? Sources.FirstOrDefault();
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string YesNo(bool? value) => value == true ? "Yes" : "No";
    private static string FormatDate(DateTime? value) => value is null || value.Value == default ? "Not saved" : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}

public sealed partial class IncomeRowViewModel : ObservableObject
{
    public IncomeRowViewModel(IncomeSource source, int index)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Index = index;
    }

    public IncomeSource Source { get; }
    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string SourceName => string.IsNullOrWhiteSpace(Source.SourceName) ? "Unnamed Source" : Source.SourceName.Trim();
    public string IncomeType => string.IsNullOrWhiteSpace(Source.IncomeType) ? "Other" : Source.IncomeType.Trim();
    public string Frequency => string.IsNullOrWhiteSpace(Source.Frequency) ? "Monthly" : Source.Frequency.Trim();
    public string MonthlyEquivalent => Source.MonthlyEquivalentText;
    public bool IsInactive => !Source.IsActive;
    public string NameForeground => IsInactive ? "#7D8795" : "White";
    public string DetailForeground => IsInactive ? "#707A88" : "#C8D4E2";
}
