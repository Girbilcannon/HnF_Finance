using GrannyManager.Core.Enums;
using GrannyManager.Core.Models;

namespace GrannyManager.Core.Services;

public sealed class FinanceCalculator
{
    public decimal ToMonthlyAmount(decimal amount, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.OneTime => 0m,
            PaymentFrequency.Weekly => amount * 52m / 12m,
            PaymentFrequency.EveryTwoWeeks => amount * 26m / 12m,
            PaymentFrequency.TwiceMonthly => amount * 2m,
            PaymentFrequency.Monthly => amount,
            PaymentFrequency.Quarterly => amount / 3m,
            PaymentFrequency.Yearly => amount / 12m,
            _ => amount
        };
    }

    public decimal ToMonthlyAmount(decimal amount, string? frequency)
    {
        return (frequency ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "weekly" => amount * 52m / 12m,
            "every 2 weeks" => amount * 26m / 12m,
            "every two weeks" => amount * 26m / 12m,
            "biweekly" => amount * 26m / 12m,
            "twice monthly" => amount * 2m,
            "monthly" => amount,
            "quarterly" => amount / 3m,
            "yearly" => amount / 12m,
            "annually" => amount / 12m,
            "annual" => amount / 12m,
            "one-time / irregular" => 0m,
            "one-time" => 0m,
            "one time" => 0m,
            "irregular" => 0m,
            _ => amount
        };
    }

    public static decimal CalculateMonthlyIncome(IEnumerable<IncomeSource> incomes)
    {
        if (incomes is null)
        {
            return 0m;
        }

        var calculator = new FinanceCalculator();
        var monthlyIncome = incomes
            .Where(i => i.IsActive)
            .Sum(i => calculator.ToMonthlyAmount(i.Amount, i.Frequency));

        return decimal.Round(monthlyIncome, 2);
    }

    public FinanceSnapshot BuildSnapshot(IEnumerable<IncomeSource> incomes, IEnumerable<Bill> bills)
    {
        return BuildSnapshot(incomes, bills, 0m, 0m);
    }

    public FinanceSnapshot BuildSnapshot(
        IEnumerable<IncomeSource> incomes,
        IEnumerable<Bill> bills,
        decimal monthlyAllowance,
        decimal monthlySavingsReserve)
    {
        decimal monthlyIncome = incomes.Where(i => i.IsActive).Sum(i => ToMonthlyAmount(i.Amount, i.Frequency));
        decimal monthlyBills = bills.Sum(b => ToMonthlyAmount(b.Amount, b.Frequency));
        decimal pastDue = bills.Sum(b => b.PastDueAmount);

        var snapshot = new FinanceSnapshot
        {
            MonthlyIncome = decimal.Round(monthlyIncome, 2),
            MonthlyExpenses = decimal.Round(monthlyBills + pastDue, 2),
            MonthlyAllowance = decimal.Round(monthlyAllowance, 2),
            MonthlySavingsReserve = decimal.Round(monthlySavingsReserve, 2)
        };

        snapshot.RiskLevel = snapshot.Remaining switch
        {
            < 0m => RiskLevel.Red,
            < 250m => RiskLevel.Yellow,
            _ => RiskLevel.Green
        };

        return snapshot;
    }
}
