using System;
using System.Collections.Generic;
using System.Linq;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class BillsService
{
    private readonly ActiveCaseState _activeCaseState;

    public BillsService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public BillsLoadResult LoadBills()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new BillsLoadResult(
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding bills or spending items.",
                Bills: Array.Empty<Bill>());
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var bills = repository.GetAll().ToList();

            AddReceiptAverageBillIfNeeded(bills, "Fuel");
            AddReceiptAverageBillIfNeeded(bills, "Grocery");

            return new BillsLoadResult(
                HasActiveCase: true,
                StatusMessage: bills.Count == 0
                    ? "No bills or spending items have been added to this case yet."
                    : $"{bills.Count} bill/spending record(s) loaded.",
                Bills: bills);
        }
        catch (Exception ex)
        {
            return new BillsLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load bills: {ex.Message}",
                Bills: Array.Empty<Bill>());
        }
    }

    public IReadOnlyList<HouseholdPerson> LoadHouseholdPeople()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<HouseholdPerson>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new HouseholdPeopleRepository(databasePath);
            return repository.GetAll().Where(p => p.IsActive).OrderBy(p => p.FullName).ToList();
        }
        catch
        {
            return Array.Empty<HouseholdPerson>();
        }
    }

    public IReadOnlyList<AssetItem> LoadBankAccounts()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<AssetItem>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new AssetsRepository(databasePath);
            return repository.GetBankAccounts();
        }
        catch
        {
            return Array.Empty<AssetItem>();
        }
    }

    public bool SaveBankAccount(AssetItem bankAccount, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving bank accounts.";
            return false;
        }

        if (bankAccount is null)
        {
            statusMessage = "No bank account was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(bankAccount.AssetName))
        {
            statusMessage = "Enter a bank account name before saving.";
            return false;
        }

        try
        {
            bankAccount.AssetType = "Bank Account";
            bankAccount.IsActive = true;

            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new AssetsRepository(databasePath);
            repository.Upsert(bankAccount);

            AppDataChangeNotifier.NotifyAssetsChanged();
            statusMessage = "Bank account saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save bank account: {ex.Message}";
            return false;
        }
    }

    public IReadOnlyList<Debt> LoadCreditCardDebts()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<Debt>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new DebtsRepository(databasePath);
            return repository.GetCreditCards();
        }
        catch
        {
            return Array.Empty<Debt>();
        }
    }

    public bool SaveCreditCardDebt(Debt debt, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving credit card debts.";
            return false;
        }

        if (debt is null)
        {
            statusMessage = "No credit card debt was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(debt.DebtName))
        {
            statusMessage = "Enter a credit card name before saving.";
            return false;
        }

        try
        {
            debt.DebtType = "Credit Card";
            debt.IsActive = true;

            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new DebtsRepository(databasePath);
            repository.Upsert(debt);

            AppDataChangeNotifier.NotifyDebtsChanged();
            statusMessage = "Credit card debt saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save credit card debt: {ex.Message}";
            return false;
        }
    }

    public bool SaveBill(Bill bill, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving bills.";
            return false;
        }

        if (bill is null)
        {
            statusMessage = "No bill was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(bill.BillName))
        {
            statusMessage = "Enter a bill name before saving.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            repository.Upsert(bill);
            AppDataChangeNotifier.NotifyBillsChanged();
            statusMessage = "Bill saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save bill: {ex.Message}";
            return false;
        }
    }

    public bool DeleteBill(long id, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing bills.";
            return false;
        }

        if (id <= 0)
        {
            statusMessage = "Select a saved bill before removing.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var bill = repository.GetById(id);
            repository.Delete(id);

            if (bill is not null)
                DeleteReceiptsByTypeIfAverageBill(activeCase.CaseFolderPath, bill);

            AppDataChangeNotifier.NotifyBillsChanged();
            statusMessage = "Bill removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove bill: {ex.Message}";
            return false;
        }
    }

    public IReadOnlyList<BillReceipt> LoadReceipts(string receiptType)
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<BillReceipt>();

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
        var repository = new BillReceiptsRepository(databasePath);
        return repository.GetByType(receiptType);
    }

    public bool AddReceipt(string receiptType, DateTime receiptDate, decimal amount, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before adding receipts.";
            return false;
        }

        if (amount <= 0m)
        {
            statusMessage = "Enter a receipt amount greater than zero.";
            return false;
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new BillReceiptsRepository(databasePath);
            repository.Add(new BillReceipt
            {
                ReceiptType = receiptType,
                ReceiptDate = receiptDate.Date,
                Amount = amount
            });

            statusMessage = "Receipt added.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not add receipt: {ex.Message}";
            return false;
        }
    }

    public bool DeleteReceipt(long receiptId, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before deleting receipts.";
            return false;
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new BillReceiptsRepository(databasePath);
            repository.Delete(receiptId);
            statusMessage = "Receipt deleted.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not delete receipt: {ex.Message}";
            return false;
        }
    }

    public ReceiptAverageSummary LoadReceiptAverage(string receiptType)
    {
        var receipts = LoadReceipts(receiptType);
        var total = receipts.Sum(r => r.Amount);
        var monthCount = receipts
            .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
            .Count();

        var estimate = CalculateRoundedMonthlyReceiptAverage(receipts);
        return new ReceiptAverageSummary(receiptType, receipts.Count, monthCount, total, estimate);
    }

    public static decimal CalculateRoundedMonthlyReceiptAverage(IEnumerable<BillReceipt> receipts)
    {
        var monthlyTotals = receipts
            .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
            .Select(g => g.Sum(r => r.Amount))
            .Where(total => total > 0m)
            .ToList();

        if (monthlyTotals.Count == 0)
            return 0m;

        var average = monthlyTotals.Average();
        return Math.Ceiling(average);
    }

    private void AddReceiptAverageBillIfNeeded(List<Bill> bills, string receiptType)
    {
        var summary = LoadReceiptAverage(receiptType);
        var existing = bills.FirstOrDefault(bill =>
            string.Equals(bill.BillName, $"{receiptType} Average", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(bill.Category, receiptType, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(bill.Notes, "Receipt Average Profile", StringComparison.OrdinalIgnoreCase)));

        if (summary.RoundedMonthlyEstimate <= 0m)
        {
            if (existing is not null)
                existing.Amount = 0m;

            return;
        }

        if (existing is not null)
        {
            existing.Amount = summary.RoundedMonthlyEstimate;
            existing.Frequency = "Monthly";
            existing.DueDate = "Calculated from receipts";
            existing.Category = receiptType;
            return;
        }

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            bills.Add(CreateReceiptAverageBill(receiptType, summary.RoundedMonthlyEstimate));
            return;
        }

        var repository = CreateRepository(activeCase.CaseFolderPath);
        var bill = CreateReceiptAverageBill(receiptType, summary.RoundedMonthlyEstimate);
        repository.Upsert(bill);
        bills.Add(bill);
    }

    private static Bill CreateReceiptAverageBill(string receiptType, decimal amount)
    {
        return new Bill
        {
            BillName = $"{receiptType} Average",
            Category = receiptType,
            Amount = amount,
            Frequency = "Monthly",
            DueDate = "Calculated from receipts",
            PaymentMethod = "Bank/Debit",
            PaidBy = "Self (Primary Person)",
            ResponsibilityOwner = "Self (Primary Person)",
            Priority = "Normal",
            IsActive = true,
            Notes = "Receipt Average Profile"
        };
    }

    private static bool IsReceiptAverageBill(Bill bill, out string receiptType)
    {
        receiptType = string.Empty;

        if (string.Equals(bill.BillName, "Fuel Average", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(bill.Category, "Fuel", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(bill.Notes, "Receipt Average Profile", StringComparison.OrdinalIgnoreCase))
        {
            receiptType = "Fuel";
            return true;
        }

        if (string.Equals(bill.BillName, "Grocery Average", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(bill.Category, "Grocery", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(bill.Notes, "Receipt Average Profile", StringComparison.OrdinalIgnoreCase))
        {
            receiptType = "Grocery";
            return true;
        }

        return false;
    }

    private static void DeleteReceiptsByTypeIfAverageBill(string caseFolderPath, Bill bill)
    {
        if (!IsReceiptAverageBill(bill, out var receiptType))
            return;

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        var receiptRepository = new BillReceiptsRepository(databasePath);
        receiptRepository.DeleteByType(receiptType);
    }

    private static BillsRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new BillsRepository(databasePath);
    }
}
