using System;
using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class IncomeSource
{
    public long Id { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string IncomeType { get; set; } = "Social Security";
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public bool TaxesWithheld { get; set; }
    public string ExpectedDayOrDate { get; set; } = string.Empty;
    public string DepositDestination { get; set; } = string.Empty;
    public long LinkedHouseholdPersonId { get; set; }
    public string LinkedHouseholdPersonName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string TaxHandlingText => TaxesWithheld ? "Taxes withheld" : "No taxes withheld";
    public string AmountLabel => TaxesWithheld ? "After Taxes" : "Gross Pay";
    public string DepositDisplayText => string.IsNullOrWhiteSpace(DepositDestination) ? "Not specified" : DepositDestination.Trim();
    public decimal MonthlyEquivalent => CalculateMonthlyEquivalent(Amount, Frequency, IsActive);
    public string MonthlyEquivalentText => MonthlyEquivalent.ToString("C2", CultureInfo.CurrentCulture);

    public static decimal CalculateMonthlyEquivalent(decimal amount, string? frequency, bool isActive = true)
    {
        if (!isActive)
            return 0m;

        return (frequency ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "weekly" => amount * 52m / 12m,
            "every 2 weeks" => amount * 26m / 12m,
            "biweekly" => amount * 26m / 12m,
            "twice monthly" => amount * 2m,
            "monthly" => amount,
            "quarterly" => amount / 3m,
            "yearly" => amount / 12m,
            "annually" => amount / 12m,
            "one-time / irregular" => 0m,
            "one-time" => 0m,
            "irregular" => 0m,
            _ => amount
        };
    }
}
