using System;
using System.Linq;
using GrannyManager.Application.State;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class FinanceTotalsService
{
    private readonly ActiveCaseState _activeCaseState;

    public FinanceTotalsService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public FinanceTotalsSnapshot LoadTotals()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return FinanceTotalsSnapshot.Empty;

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);

            SavingsBankAccountSyncService.Sync(databasePath);

            var income = new IncomeSourcesRepository(databasePath)
                .GetAll()
                .Where(source => source.IsActive)
                .Sum(source => source.MonthlyEquivalent);

            var bills = new BillsRepository(databasePath)
                .GetAll()
                .Where(bill => bill.IsActive)
                .Sum(bill => bill.MonthlyEquivalent);

            var allowanceSavingsItems = new AllowanceSavingsRepository(databasePath)
                .GetAll()
                .Where(item => item.IsActive)
                .ToList();

            var allowance = allowanceSavingsItems
                .Where(item => item.IsAllowance)
                .Sum(item => item.MonthlyEquivalent);

            var savings = allowanceSavingsItems
                .Where(item => item.IsSavings)
                .Sum(item => item.MonthlyEquivalent);

            // Debt minimums count as monthly obligations only when they are active and not already represented by a linked bill.
            var unlinkedDebtMinimums = new DebtsRepository(databasePath)
                .GetAll()
                .Where(debt => debt.IsActive)
                .Where(debt => debt.LinkedBillId <= 0)
                .Sum(debt => debt.MonthlyEquivalent);

            bills += unlinkedDebtMinimums;

            return new FinanceTotalsSnapshot(
                HasActiveCase: true,
                MonthlyIncome: income,
                MonthlyBills: bills,
                MonthlyAllowance: allowance,
                MonthlySavings: savings);
        }
        catch
        {
            return FinanceTotalsSnapshot.Empty;
        }
    }
}
