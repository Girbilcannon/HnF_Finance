using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.App.Services;

public sealed class FinanceSummaryService
{
    public CaseMoneySummary BuildSummary(CaseProfile? activeCase)
    {
        if (activeCase is null || string.IsNullOrWhiteSpace(activeCase.CaseFolderPath))
            return CaseMoneySummary.Empty;

        try
        {
            string databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            DatabaseInitializer.EnsureCreated(databasePath);

            var incomeSources = new IncomeSourcesRepository(databasePath).GetAll();
            var bills = new BillsRepository(databasePath).GetAll();
            var allowanceSavings = new AllowanceSavingsRepository(databasePath).GetAll();
            var householdPeople = new HouseholdPeopleRepository(databasePath).GetAll();

            var activeIncome = incomeSources.Where(source => source.IsActive).ToList();
            var activeBills = bills.Where(bill => bill.IsActive).ToList();
            var selfPaidBills = activeBills
                .Where(bill => IsPaidByPrimaryPerson(bill.PaidBy, activeCase))
                .ToList();
            var otherPaidBills = activeBills
                .Where(bill => !IsPaidByPrimaryPerson(bill.PaidBy, activeCase))
                .ToList();
            var activeReserveItems = allowanceSavings.Where(item => item.IsActive).ToList();
            var contributingPeople = householdPeople
                .Where(person => person.MonthlyContribution > 0m || person.PaysRent)
                .ToList();

            decimal incomeSourceMonthly = activeIncome.Sum(source => source.MonthlyEquivalent);
            decimal householdContributionMonthly = contributingPeople.Sum(person => person.MonthlyContribution);
            decimal monthlyIncome = incomeSourceMonthly + householdContributionMonthly;

            decimal monthlyExpenses = selfPaidBills.Sum(bill => bill.MonthlyEquivalent);
            decimal monthlyAllowance = activeReserveItems
                .Where(item => item.IsAllowance)
                .Sum(item => item.GetMonthlyEquivalent());
            decimal monthlySavings = activeReserveItems
                .Where(item => item.IsSavings)
                .Sum(item => item.GetMonthlyEquivalent());
            decimal pastDue = selfPaidBills.Sum(bill => bill.PastDueAmount);
            decimal otherPaidMonthly = otherPaidBills.Sum(bill => bill.MonthlyEquivalent);
            decimal otherPaidPastDue = otherPaidBills.Sum(bill => bill.PastDueAmount);

            return new CaseMoneySummary
            {
                IncomeSourceMonthly = decimal.Round(incomeSourceMonthly, 2),
                HouseholdContributionMonthly = decimal.Round(householdContributionMonthly, 2),
                MonthlyIncome = decimal.Round(monthlyIncome, 2),
                MonthlyExpenses = decimal.Round(monthlyExpenses, 2),
                MonthlyAllowance = decimal.Round(monthlyAllowance, 2),
                MonthlySavings = decimal.Round(monthlySavings, 2),
                PastDue = decimal.Round(pastDue, 2),
                OtherPaidMonthlyExpenses = decimal.Round(otherPaidMonthly, 2),
                OtherPaidPastDue = decimal.Round(otherPaidPastDue, 2),
                ActiveIncomeCount = activeIncome.Count,
                ActiveBillCount = selfPaidBills.Count,
                OtherPaidBillCount = otherPaidBills.Count,
                ActiveReserveCount = activeReserveItems.Count,
                AllowanceCount = activeReserveItems.Count(item => item.IsAllowance),
                SavingsCount = activeReserveItems.Count(item => item.IsSavings),
                HouseholdMemberCount = householdPeople.Count(person => person.LivesInHousehold),
                ContributorCount = contributingPeople.Count
            };
        }
        catch
        {
            return CaseMoneySummary.Empty;
        }
    }

    private static bool IsPaidByPrimaryPerson(string? paidBy, CaseProfile activeCase)
    {
        var payer = (paidBy ?? string.Empty).Trim();

        // Legacy/blank bills are treated as paid by the primary case person so old data
        // does not disappear from the money picture.
        if (string.IsNullOrWhiteSpace(payer))
            return true;

        var primaryName = activeCase.PrimaryPersonName?.Trim();
        if (string.IsNullOrWhiteSpace(primaryName))
            primaryName = activeCase.DisplayName?.Trim();

        if (payer.StartsWith("Self (", StringComparison.OrdinalIgnoreCase) &&
            payer.EndsWith(")", StringComparison.Ordinal))
        {
            var nameInsideSelf = payer[6..^1].Trim();
            return string.IsNullOrWhiteSpace(primaryName) ||
                   string.Equals(nameInsideSelf, primaryName, StringComparison.OrdinalIgnoreCase);
        }

        // If older data stored the primary person's plain name instead of Self (...),
        // still count it as a primary-person expense.
        return !string.IsNullOrWhiteSpace(primaryName) &&
               string.Equals(payer, primaryName, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class CaseMoneySummary
{
    public static CaseMoneySummary Empty { get; } = new();

    public decimal IncomeSourceMonthly { get; init; }
    public decimal HouseholdContributionMonthly { get; init; }
    public decimal MonthlyIncome { get; init; }
    public decimal MonthlyExpenses { get; init; }
    public decimal MonthlyAllowance { get; init; }
    public decimal MonthlySavings { get; init; }
    public decimal PastDue { get; init; }
    public decimal OtherPaidMonthlyExpenses { get; init; }
    public decimal OtherPaidPastDue { get; init; }

    public int ActiveIncomeCount { get; init; }
    public int ActiveBillCount { get; init; }
    public int OtherPaidBillCount { get; init; }
    public int ActiveReserveCount { get; init; }
    public int AllowanceCount { get; init; }
    public int SavingsCount { get; init; }
    public int HouseholdMemberCount { get; init; }
    public int ContributorCount { get; init; }

    public decimal Remaining => MonthlyIncome - MonthlyExpenses - MonthlyAllowance - MonthlySavings;
}
