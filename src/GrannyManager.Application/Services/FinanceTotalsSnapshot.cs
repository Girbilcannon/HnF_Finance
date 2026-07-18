using System.Globalization;

namespace GrannyManager.Application.Services;

public sealed record FinanceTotalsSnapshot(
    bool HasActiveCase,
    decimal MonthlyIncome,
    decimal MonthlyBills,
    decimal MonthlyAllowance,
    decimal MonthlySavings)
{
    public decimal RemainingDeficit => MonthlyIncome - MonthlyBills - MonthlyAllowance - MonthlySavings;

    public string MonthlyIncomeText => MonthlyIncome.ToString("C2", CultureInfo.CurrentCulture);
    public string MonthlyBillsText => MonthlyBills.ToString("C2", CultureInfo.CurrentCulture);
    public string MonthlyAllowanceSavingsText => $"{MonthlyAllowance.ToString("C2", CultureInfo.CurrentCulture)} / {MonthlySavings.ToString("C2", CultureInfo.CurrentCulture)}";
    public string RemainingDeficitText => RemainingDeficit.ToString("C2", CultureInfo.CurrentCulture);

    public string RemainingDeficitBrush
    {
        get
        {
            if (RemainingDeficit < 50m)
                return "#D9534F";

            if (RemainingDeficit < 200m)
                return "#F0C94B";

            return "#4CAF50";
        }
    }

    public static FinanceTotalsSnapshot Empty { get; } = new(false, 0m, 0m, 0m, 0m);
}
