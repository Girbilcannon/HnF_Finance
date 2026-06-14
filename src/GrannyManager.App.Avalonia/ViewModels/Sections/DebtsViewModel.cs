using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class DebtsViewModel : ViewModelBase
{
    private readonly DebtsService _debtsService;

    public DebtsViewModel(ActiveCaseState activeCaseState, DebtsService debtsService)
    {
        _debtsService = debtsService ?? throw new ArgumentNullException(nameof(debtsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadDebts();

        LoadDebts();
    }

    public ObservableCollection<DebtListItemViewModel> Debts { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDebt))]
    [NotifyPropertyChangedFor(nameof(SelectedDebtName))]
    [NotifyPropertyChangedFor(nameof(SelectedDebtType))]
    [NotifyPropertyChangedFor(nameof(SelectedCreditorCollector))]
    [NotifyPropertyChangedFor(nameof(SelectedStatus))]
    [NotifyPropertyChangedFor(nameof(SelectedPriority))]
    [NotifyPropertyChangedFor(nameof(SelectedActive))]
    [NotifyPropertyChangedFor(nameof(SelectedCurrentBalance))]
    [NotifyPropertyChangedFor(nameof(SelectedMinimumPayment))]
    [NotifyPropertyChangedFor(nameof(SelectedPaymentFrequency))]
    [NotifyPropertyChangedFor(nameof(SelectedMonthlyEquivalent))]
    [NotifyPropertyChangedFor(nameof(SelectedDueDate))]
    [NotifyPropertyChangedFor(nameof(SelectedResponsibilityOwner))]
    [NotifyPropertyChangedFor(nameof(SelectedPaidBy))]
    [NotifyPropertyChangedFor(nameof(SelectedPaymentTracking))]
    [NotifyPropertyChangedFor(nameof(SelectedLinkedBill))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private DebtListItemViewModel? _selectedDebt;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    public bool HasSelectedDebt => SelectedDebt is not null;

    private Debt? Debt => SelectedDebt?.Debt;

    public string SelectedDebtName => Debt?.DebtName ?? "No debt selected";
    public string SelectedDebtType => Clean(Debt?.DebtType);
    public string SelectedCreditorCollector => Clean(Debt?.CreditorCollector);
    public string SelectedStatus => Clean(Debt?.Status);
    public string SelectedPriority => Clean(Debt?.Priority);
    public string SelectedActive => YesNo(Debt?.IsActive);

    public string SelectedCurrentBalance => FormatMoney(Debt?.CurrentBalance);
    public string SelectedMinimumPayment => FormatMoney(Debt?.MinimumPayment);
    public string SelectedPaymentFrequency => Clean(Debt?.PaymentFrequency);
    public string SelectedMonthlyEquivalent => FormatMoney(Debt?.MonthlyEquivalent);
    public string SelectedDueDate => Clean(Debt?.DueDate);

    public string SelectedResponsibilityOwner => Clean(Debt?.ResponsibilityOwner);
    public string SelectedPaidBy => Clean(Debt?.PaidBy);

    public string SelectedPaymentTracking => Clean(Debt?.PaymentTracking);
    public string SelectedLinkedBill => string.IsNullOrWhiteSpace(Debt?.LinkedBillName) ? "None" : Debt.LinkedBillName.Trim();

    public string SelectedNotes => Clean(Debt?.Notes);
    public string SelectedCreatedUtc => FormatUtc(Debt?.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(Debt?.UpdatedUtc);

    partial void OnSelectedDebtChanged(DebtListItemViewModel? oldValue, DebtListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    private void LoadDebts()
    {
        var result = _debtsService.LoadDebts();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Debts.Clear();

        var index = 0;
        foreach (var debt in result.Debts)
        {
            Debts.Add(new DebtListItemViewModel(debt, index));
            index++;
        }

        SelectedDebt = Debts.FirstOrDefault();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string YesNo(bool? value)
    {
        return value == true ? "Yes" : "No";
    }

    private static string FormatMoney(decimal? value)
    {
        return (value ?? 0m).ToString("C2");
    }

    private static string FormatUtc(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "Not saved";

        return value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

public sealed partial class DebtListItemViewModel : ObservableObject
{
    public DebtListItemViewModel(Debt debt, int index)
    {
        Debt = debt ?? throw new ArgumentNullException(nameof(debt));
        Index = index;
    }

    public Debt Debt { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(MutedForeground))]
    private bool _isSelected;

    public string DebtName => string.IsNullOrWhiteSpace(Debt.DebtName) ? "Unnamed Debt" : Debt.DebtName.Trim();
    public string DebtType => string.IsNullOrWhiteSpace(Debt.DebtType) ? "None" : Debt.DebtType.Trim();
    public string CurrentBalance => Debt.BalanceText;
    public string MonthlyEquivalent => Debt.MonthlyEquivalentText;

    public bool IsInactive => !Debt.IsActive;

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
