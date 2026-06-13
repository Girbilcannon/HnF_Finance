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
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new BillsRepository(databasePath);
            var bills = repository.GetAll();

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

    public ReceiptAverageSummary LoadReceiptAverage(string receiptType)
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
            return new ReceiptAverageSummary(receiptType, 0, 0, 0m, 0m);

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
        var repository = new BillReceiptsRepository(databasePath);
        var receipts = repository.GetByType(receiptType);

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
}
