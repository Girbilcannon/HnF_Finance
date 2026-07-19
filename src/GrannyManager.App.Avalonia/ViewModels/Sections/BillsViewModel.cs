using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class BillsViewModel : ViewModelBase
{
    private readonly BillsService _billsService;

    public BillsViewModel(ActiveCaseState activeCaseState, BillsService billsService)
    {
        _billsService = billsService ?? throw new ArgumentNullException(nameof(billsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadBills();

        AppDataChangeNotifier.BillsChanged += (_, _) => LoadBills();

        LoadBills();
    }

    public ObservableCollection<BillRowViewModel> Bills { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedBill))]
    [NotifyPropertyChangedFor(nameof(CanEditBill))]
    [NotifyPropertyChangedFor(nameof(CanRemoveBill))]
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
    private BillRowViewModel? _selectedBill;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddBill))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedBill => SelectedBill is not null;
    public bool CanAddBill => HasActiveCase;
    public bool CanEditBill => HasActiveCase && HasSelectedBill && (SelectedBill?.Bill.Id ?? 0) > 0;
    public bool CanRemoveBill => HasActiveCase && HasSelectedBill && (SelectedBill?.Bill.Id ?? 0) > 0;

    public string SelectedBillName => SelectedBill?.Bill.BillName ?? "No bill selected";
    public string SelectedCategory => Clean(SelectedBill?.Bill.Category);
    public string SelectedPriority => Clean(SelectedBill?.Bill.Priority);
    public string SelectedActive => YesNo(SelectedBill?.Bill.IsActive);
    public string SelectedAmount => SelectedBill?.Bill.AmountText ?? "$0.00";
    public string SelectedFrequency => Clean(SelectedBill?.Bill.Frequency);
    public string SelectedMonthlyEquivalent => SelectedBill?.Bill.MonthlyEquivalentText ?? "$0.00";
    public string SelectedDueDate => Clean(SelectedBill?.Bill.DueDate);
    public string SelectedPaymentMethod => SelectedBill?.Bill.AutopayText ?? "Manual payment";
    public string SelectedPastDueAmount => SelectedBill?.Bill.PastDueAmountText ?? "$0.00";
    public string SelectedPaidBy => Clean(SelectedBill?.Bill.PaidBy);
    public string SelectedResponsibilityOwner => Clean(SelectedBill?.Bill.ResponsibilityOwner);
    public string SelectedNotes => Clean(SelectedBill?.Bill.Notes);
    public string SelectedCreatedUtc => FormatDate(SelectedBill?.Bill.CreatedUtc);
    public string SelectedUpdatedUtc => FormatDate(SelectedBill?.Bill.UpdatedUtc);

    partial void OnSelectedBillChanged(BillRowViewModel? oldValue, BillRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public Bill CreateBlankBill()
    {
        return new Bill
        {
            Category = "Utilities",
            Frequency = "Monthly",
            PaymentMethod = "Cash/Check",
            PaidBy = "Self (Primary Person)",
            ResponsibilityOwner = "Self (Primary Person)",
            Priority = "Normal",
            IsActive = true
        };
    }

    public Bill? CreateEditableCopyOfSelectedBill()
    {
        var bill = SelectedBill?.Bill;
        if (bill is null)
            return null;

        return new Bill
        {
            Id = bill.Id,
            BillName = bill.BillName,
            Category = bill.Category,
            Amount = bill.Amount,
            Frequency = bill.Frequency,
            DueDate = bill.DueDate,
            PaymentMethod = bill.PaymentMethod,
            LinkedBankAssetId = bill.LinkedBankAssetId,
            LinkedBankAssetName = bill.LinkedBankAssetName,
            LinkedDebtId = bill.LinkedDebtId,
            LinkedDebtName = bill.LinkedDebtName,
            IsAutopay = bill.IsAutopay,
            PastDueAmount = bill.PastDueAmount,
            PaidBy = bill.PaidBy,
            PaidByHouseholdPersonId = bill.PaidByHouseholdPersonId,
            ResponsibilityOwner = bill.ResponsibilityOwner,
            ResponsibilityOwnerHouseholdPersonId = bill.ResponsibilityOwnerHouseholdPersonId,
            Priority = bill.Priority,
            IsActive = bill.IsActive,
            Notes = bill.Notes,
            CreatedUtc = bill.CreatedUtc,
            UpdatedUtc = bill.UpdatedUtc
        };
    }

    public IReadOnlyList<HouseholdPerson> GetHouseholdPeople()
    {
        return _billsService.LoadHouseholdPeople();
    }

    public IReadOnlyList<AssetItem> GetBankAccounts()
    {
        return _billsService.LoadBankAccounts();
    }

    public IReadOnlyList<Debt> GetCreditCardDebts()
    {
        return _billsService.LoadCreditCardDebts();
    }

    public Debt CreateBlankCreditCardDebt()
    {
        return new Debt
        {
            DebtType = "Credit Card",
            PaymentFrequency = "Monthly",
            Status = "Current",
            Priority = "Normal",
            IsActive = true
        };
    }

    public bool SaveCreditCardDebt(Debt debt)
    {
        return _billsService.SaveCreditCardDebt(debt, out _);
    }

    public AssetItem CreateBlankBankAccount()
    {
        return new AssetItem
        {
            AssetType = "Bank Account",
            AccountType = "Checking",
            IsActive = true
        };
    }

    public bool SaveBankAccount(AssetItem bankAccount)
    {
        return _billsService.SaveBankAccount(bankAccount, out _);
    }

    public IReadOnlyList<BillReceipt> LoadReceipts(string receiptType)
    {
        return _billsService.LoadReceipts(receiptType);
    }

    public ReceiptAverageSummary LoadReceiptAverage(string receiptType)
    {
        return _billsService.LoadReceiptAverage(receiptType);
    }

    public bool AddReceipt(string receiptType, DateTime receiptDate, decimal amount, out string message)
    {
        var result = _billsService.AddReceipt(receiptType, receiptDate, amount, out message);
        if (result)
            LoadBills();
        else
            StatusMessage = message;

        return result;
    }

    public bool DeleteReceipt(long receiptId, out string message)
    {
        var result = _billsService.DeleteReceipt(receiptId, out message);
        if (result)
            LoadBills();
        else
            StatusMessage = message;

        return result;
    }


    public bool SaveBill(Bill bill)
    {
        if (!_billsService.SaveBill(bill, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadBills();
        SelectedBill = Bills.FirstOrDefault(item => item.Bill.Id == bill.Id) ?? Bills.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedBill()
    {
        var selectedId = SelectedBill?.Bill.Id ?? 0;
        if (!_billsService.DeleteBill(selectedId, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadBills();
        StatusMessage = message;
        return true;
    }

    public void RefreshFromNavigation()
    {
        LoadBills();
    }

    private void LoadBills()
    {
        var selectedId = SelectedBill?.Bill.Id ?? 0;
        var result = _billsService.LoadBills();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Bills.Clear();

        var index = 0;
        foreach (var bill in result.Bills)
        {
            Bills.Add(new BillRowViewModel(bill, index));
            index++;
        }

        SelectedBill = Bills.FirstOrDefault(item => item.Bill.Id == selectedId) ?? Bills.FirstOrDefault();
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string YesNo(bool? value) => value == true ? "Yes" : "No";
    private static string FormatDate(DateTime? value) => value is null || value.Value == default ? "Not saved" : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}

public sealed partial class BillRowViewModel : ObservableObject
{
    public BillRowViewModel(Bill bill, int index)
    {
        Bill = bill ?? throw new ArgumentNullException(nameof(bill));
        Index = index;
    }

    public Bill Bill { get; }
    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string BillName => string.IsNullOrWhiteSpace(Bill.BillName) ? "Unnamed Bill" : Bill.BillName.Trim();
    public string Category => string.IsNullOrWhiteSpace(Bill.Category) ? "Other" : Bill.Category.Trim();
    public string Amount => Bill.AmountText;
    public string Frequency => string.IsNullOrWhiteSpace(Bill.Frequency) ? "Monthly" : Bill.Frequency.Trim();
    public bool IsInactive => !Bill.IsActive;
    public string NameForeground => IsInactive ? "#7D8795" : "White";
    public string DetailForeground => IsInactive ? "#707A88" : "#C8D4E2";
}
