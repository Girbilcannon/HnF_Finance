using System;
using System.Collections.Generic;
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

        AppDataChangeNotifier.DebtsChanged += (_, _) => LoadDebts();

        LoadDebts();
    }

    public ObservableCollection<DebtRowViewModel> Debts { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDebt))]
    [NotifyPropertyChangedFor(nameof(CanEditDebt))]
    [NotifyPropertyChangedFor(nameof(CanRemoveDebt))]
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
    private DebtRowViewModel? _selectedDebt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddDebt))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedDebt => SelectedDebt is not null;
    public bool CanAddDebt => HasActiveCase;
    public bool CanEditDebt => HasActiveCase && HasSelectedDebt;
    public bool CanRemoveDebt => HasActiveCase && HasSelectedDebt;

    private Debt? Debt => SelectedDebt?.Debt;

    public string SelectedDebtName => Debt?.DebtName ?? "No debt selected";
    public string SelectedDebtType => Clean(Debt?.DebtType);
    public string SelectedCreditorCollector => Clean(Debt?.CreditorCollector);
    public string SelectedStatus => Clean(Debt?.Status);
    public string SelectedPriority => Clean(Debt?.Priority);
    public string SelectedActive => YesNo(Debt?.IsActive);
    public string SelectedCurrentBalance => Debt?.BalanceText ?? "$0.00";
    public string SelectedMinimumPayment => Debt?.MinimumPaymentText ?? "$0.00";
    public string SelectedPaymentFrequency => Clean(Debt?.PaymentFrequency);
    public string SelectedMonthlyEquivalent => Debt?.MonthlyEquivalentText ?? "$0.00";
    public string SelectedDueDate => Clean(Debt?.DueDate);
    public string SelectedResponsibilityOwner => Clean(Debt?.ResponsibilityOwner);
    public string SelectedPaidBy => Clean(Debt?.PaidBy);
    public string SelectedPaymentTracking => Clean(Debt?.PaymentTracking);
    public string SelectedLinkedBill => string.IsNullOrWhiteSpace(Debt?.LinkedBillName) ? "None" : Debt.LinkedBillName.Trim();
    public string SelectedNotes => Clean(Debt?.Notes);
    public string SelectedCreatedUtc => FormatUtc(Debt?.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(Debt?.UpdatedUtc);

    partial void OnSelectedDebtChanged(DebtRowViewModel? oldValue, DebtRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public Debt CreateBlankDebt()
    {
        return new Debt
        {
            DebtType = "Credit Card",
            PaymentFrequency = "Monthly",
            Status = "Current",
            Priority = "Normal",
            IsActive = true,
            PaymentTracking = "Not Linked"
        };
    }

    public Debt? CreateEditableCopyOfSelectedDebt()
    {
        var debt = Debt;
        if (debt is null)
            return null;

        return new Debt
        {
            Id = debt.Id,
            DebtName = debt.DebtName,
            DebtType = debt.DebtType,
            CreditorCollector = debt.CreditorCollector,
            CurrentBalance = debt.CurrentBalance,
            MinimumPayment = debt.MinimumPayment,
            PaymentFrequency = debt.PaymentFrequency,
            DueDate = debt.DueDate,
            ResponsibilityOwner = debt.ResponsibilityOwner,
            PaidBy = debt.PaidBy,
            PaymentTracking = debt.PaymentTracking,
            LinkedBillId = debt.LinkedBillId,
            LinkedBillName = debt.LinkedBillName,
            Status = debt.Status,
            Priority = debt.Priority,
            IsActive = debt.IsActive,
            Notes = debt.Notes,
            CreatedUtc = debt.CreatedUtc,
            UpdatedUtc = debt.UpdatedUtc
        };
    }

    public IReadOnlyList<HouseholdPerson> GetHouseholdPeople()
    {
        return _debtsService.LoadHouseholdPeople();
    }

    public IReadOnlyList<Bill> GetBills()
    {
        return _debtsService.LoadBills();
    }

    public IReadOnlyList<AssetItem> GetBankAccounts()
    {
        return _debtsService.LoadBankAccounts();
    }

    public IReadOnlyList<Debt> GetCreditCardDebts()
    {
        return _debtsService.LoadCreditCards();
    }

    public Bill CreateBlankBillForDebt(Debt debt)
    {
        return new Bill
        {
            BillName = string.IsNullOrWhiteSpace(debt?.DebtName) ? "Debt Payment" : $"{debt.DebtName} Payment",
            Category = "Debt Payment",
            Amount = debt?.MinimumPayment ?? 0m,
            Frequency = string.IsNullOrWhiteSpace(debt?.PaymentFrequency) ? "Monthly" : debt.PaymentFrequency,
            DueDate = debt?.DueDate ?? string.Empty,
            PaymentMethod = "Cash/Check",
            LinkedDebtId = debt?.Id ?? 0,
            LinkedDebtName = debt?.DebtName ?? string.Empty,
            PaidBy = string.IsNullOrWhiteSpace(debt?.PaidBy) ? "Self (Primary Person)" : debt.PaidBy,
            ResponsibilityOwner = string.IsNullOrWhiteSpace(debt?.ResponsibilityOwner) ? "Self (Primary Person)" : debt.ResponsibilityOwner,
            Priority = string.IsNullOrWhiteSpace(debt?.Priority) ? "Normal" : debt.Priority,
            IsActive = debt?.IsActive ?? true,
            Notes = string.IsNullOrWhiteSpace(debt?.DebtName) ? "Created from Debt dialog." : $"Created from Debt dialog for '{debt.DebtName}'."
        };
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

    public Debt CreateBlankCreditCardDebt()
    {
        return new Debt
        {
            DebtType = "Credit Card",
            PaymentFrequency = "Monthly",
            Status = "Current",
            Priority = "Normal",
            IsActive = true,
            PaymentTracking = "Not Linked"
        };
    }

    public bool SaveBill(Bill bill)
    {
        if (!_debtsService.SaveBill(bill, out var message))
        {
            StatusMessage = message;
            return false;
        }

        RefreshAfterCrossSectionSave();
        StatusMessage = message;
        return true;
    }

    public void RefreshAfterCrossSectionSave()
    {
        AppDataChangeNotifier.NotifyAllFinanceChanged();
        LoadDebts();
    }

    public bool SaveBankAccount(AssetItem bankAccount)
    {
        if (!_debtsService.SaveBankAccount(bankAccount, out var message))
        {
            StatusMessage = message;
            return false;
        }

        StatusMessage = message;
        return true;
    }

    public bool SaveCreditCardDebt(Debt debt)
    {
        return SaveDebt(debt);
    }

    public bool SaveDebt(Debt debt)
    {
        if (!_debtsService.SaveDebt(debt, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadDebts();
        SelectedDebt = Debts.FirstOrDefault(row => row.Debt.Id == debt.Id) ?? Debts.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedDebt()
    {
        var selectedId = Debt?.Id ?? 0;
        if (!_debtsService.DeleteDebt(selectedId, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadDebts();
        StatusMessage = message;
        return true;
    }

    private void LoadDebts()
    {
        var selectedId = Debt?.Id ?? 0;
        var result = _debtsService.LoadDebts();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Debts.Clear();

        var index = 0;
        foreach (var debt in result.Debts)
        {
            Debts.Add(new DebtRowViewModel(debt, index));
            index++;
        }

        SelectedDebt = Debts.FirstOrDefault(row => row.Debt.Id == selectedId) ?? Debts.FirstOrDefault();
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string YesNo(bool? value) => value == true ? "Yes" : "No";
    private static string FormatUtc(DateTime? value) => value is null || value.Value == default ? "Not saved" : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}

public sealed partial class DebtRowViewModel : ObservableObject
{
    public DebtRowViewModel(Debt debt, int index)
    {
        Debt = debt ?? throw new ArgumentNullException(nameof(debt));
        Index = index;
    }

    public Debt Debt { get; }
    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string DebtName => Debt.DisplayName;
    public string DebtType => string.IsNullOrWhiteSpace(Debt.DebtType) ? "Debt" : Debt.DebtType.Trim();
    public string CurrentBalance => Debt.BalanceText;
    public string MinimumPayment => Debt.MinimumPaymentText;
    public bool IsInactive => !Debt.IsActive;
    public string NameForeground => IsInactive ? "#7D8795" : "White";
    public string DetailForeground => IsInactive ? "#707A88" : "#C8D4E2";
}
