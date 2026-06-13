using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class IncomeViewModel : ViewModelBase
{
    private readonly IncomeService _incomeService;

    public IncomeViewModel(ActiveCaseState activeCaseState, IncomeService incomeService)
    {
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadSources();

        LoadSources();
    }

    public ObservableCollection<IncomeSourceListItemViewModel> Sources { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSource))]
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
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private IncomeSourceListItemViewModel? _selectedSource;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    public bool HasSelectedSource => SelectedSource is not null;

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
    public string SelectedNotes => Clean(SelectedSource?.Source.Notes);
    public string SelectedCreatedUtc => FormatUtc(SelectedSource?.Source.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(SelectedSource?.Source.UpdatedUtc);

    partial void OnSelectedSourceChanged(IncomeSourceListItemViewModel? oldValue, IncomeSourceListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadSources();
    }

    private void LoadSources()
    {
        var result = _incomeService.LoadSources();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Sources.Clear();

        var index = 0;
        foreach (var source in result.Sources)
        {
            Sources.Add(new IncomeSourceListItemViewModel(source, index));
            index++;
        }

        SelectedSource = Sources.FirstOrDefault();
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

public sealed partial class IncomeSourceListItemViewModel : ObservableObject
{
    public IncomeSourceListItemViewModel(IncomeSource source, int index)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Index = index;
    }

    public IncomeSource Source { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(MutedForeground))]
    private bool _isSelected;

    public string SourceName => string.IsNullOrWhiteSpace(Source.SourceName) ? "Unnamed Source" : Source.SourceName.Trim();
    public string IncomeType => string.IsNullOrWhiteSpace(Source.IncomeType) ? "Other" : Source.IncomeType.Trim();
    public string Frequency => string.IsNullOrWhiteSpace(Source.Frequency) ? "Monthly" : Source.Frequency.Trim();
    public string MonthlyEquivalent => Source.MonthlyEquivalentText;

    public bool IsInactive => !Source.IsActive;

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

    public string MutedForeground => IsSelected ? "White" : IsInactive ? "#707A88" : "#C8D4E2";
}
