using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class BillsViewModel : ViewModelBase
{
    private readonly BillsService _billsService;

    public BillsViewModel(ActiveCaseState activeCaseState, BillsService billsService)
    {
        _billsService = billsService ?? throw new ArgumentNullException(nameof(billsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadBills();

        LoadBills();
    }

    public ObservableCollection<BillListItemViewModel> Bills { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedBill))]
    [NotifyPropertyChangedFor(nameof(SelectedBillName))]
    [NotifyPropertyChangedFor(nameof(SelectedCategory))]
    [NotifyPropertyChangedFor(nameof(SelectedPriority))]
    [NotifyPropertyChangedFor(nameof(SelectedActive))]
    [NotifyPropertyChangedFor(nameof(SelectedAmount))]
    [NotifyPropertyChangedFor(nameof(SelectedFrequency))]
    [NotifyPropertyChangedFor(nameof(SelectedMonthlyEquivalent))]
    [NotifyPropertyChangedFor(nameof(SelectedDueDate))]
    [NotifyPropertyChangedFor(nameof(SelectedPaymentMethod))]
    [NotifyPropertyChangedFor(nameof(SelectedPastDueAmount))]
    [NotifyPropertyChangedFor(nameof(SelectedPaidBy))]
    [NotifyPropertyChangedFor(nameof(SelectedResponsibilityOwner))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private BillListItemViewModel? _selectedBill;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _fuelSummary = "Fuel average calculator";

    [ObservableProperty]
    private string _grocerySummary = "Grocery average calculator";

    public bool HasSelectedBill => SelectedBill is not null;

    public string SelectedBillName => SelectedBill?.Bill.BillName ?? "No bill selected";
    public string SelectedCategory => Clean(SelectedBill?.Bill.Category);
    public string SelectedPriority => Clean(SelectedBill?.Bill.Priority);
    public string SelectedActive => YesNo(SelectedBill?.Bill.IsActive);
    public string SelectedAmount => FormatMoney(SelectedBill?.Bill.Amount);
    public string SelectedFrequency => Clean(SelectedBill?.Bill.Frequency);
    public string SelectedMonthlyEquivalent => FormatMoney(SelectedBill?.Bill.MonthlyEquivalent);
    public string SelectedDueDate => Clean(SelectedBill?.Bill.DueDate);
    public string SelectedPaymentMethod => SelectedBill?.Bill.AutopayText ?? "Manual payment";
    public string SelectedPastDueAmount => FormatMoney(SelectedBill?.Bill.PastDueAmount);
    public string SelectedPaidBy => Clean(SelectedBill?.Bill.PaidBy);
    public string SelectedResponsibilityOwner => Clean(SelectedBill?.Bill.ResponsibilityOwner);
    public string SelectedNotes => Clean(SelectedBill?.Bill.Notes);
    public string SelectedCreatedUtc => FormatUtc(SelectedBill?.Bill.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(SelectedBill?.Bill.UpdatedUtc);

    partial void OnSelectedBillChanged(BillListItemViewModel? oldValue, BillListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    [RelayCommand]
    private void OpenFuelCalculator()
    {
        StatusMessage = HasActiveCase
            ? "Fuel calculator window will be wired in the next Bills pass."
            : "Open or create a case before using the fuel calculator.";
    }

    [RelayCommand]
    private void OpenGroceryCalculator()
    {
        StatusMessage = HasActiveCase
            ? "Grocery calculator window will be wired in the next Bills pass."
            : "Open or create a case before using the grocery calculator.";
    }

    private void LoadBills()
    {
        var result = _billsService.LoadBills();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Bills.Clear();

        var index = 0;
        foreach (var bill in result.Bills)
        {
            Bills.Add(new BillListItemViewModel(bill, index));
            index++;
        }

        SelectedBill = Bills.FirstOrDefault();

        UpdateReceiptSummaries();
    }

    private void UpdateReceiptSummaries()
    {
        if (!HasActiveCase)
        {
            FuelSummary = "Fuel";
            GrocerySummary = "Grocery";
            return;
        }

        try
        {
            FuelSummary = FormatReceiptSummary(_billsService.LoadReceiptAverage("Fuel"));
            GrocerySummary = FormatReceiptSummary(_billsService.LoadReceiptAverage("Grocery"));
        }
        catch
        {
            FuelSummary = "Fuel";
            GrocerySummary = "Grocery";
        }
    }

    private static string FormatReceiptSummary(ReceiptAverageSummary summary)
    {
        return summary.RoundedMonthlyEstimate > 0m
            ? $"{summary.ReceiptType}: {summary.RoundedMonthlyEstimate:C0}/mo"
            : summary.ReceiptType;
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

public sealed partial class BillListItemViewModel : ObservableObject
{
    public BillListItemViewModel(Bill bill, int index)
    {
        Bill = bill ?? throw new ArgumentNullException(nameof(bill));
        Index = index;
    }

    public Bill Bill { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(MutedForeground))]
    private bool _isSelected;

    public string BillName => string.IsNullOrWhiteSpace(Bill.BillName) ? "Unnamed Bill" : Bill.BillName.Trim();
    public string Category => string.IsNullOrWhiteSpace(Bill.Category) ? "None" : Bill.Category.Trim();
    public string Amount => Bill.Amount.ToString("C2");
    public string MonthlyEquivalent => Bill.MonthlyEquivalent.ToString("C2");

    public bool IsInactive => !Bill.IsActive;

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
