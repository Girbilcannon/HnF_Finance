using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class Debt
{
    public long Id { get; set; }
    public string DebtName { get; set; } = string.Empty;
    public string DebtType { get; set; } = string.Empty;
    public string CreditorCollector { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal MinimumPayment { get; set; }
    public string PaymentFrequency { get; set; } = "Monthly";
    public string DueDate { get; set; } = string.Empty;
    public string ResponsibilityOwner { get; set; } = string.Empty;
    public string PaidBy { get; set; } = string.Empty;
    public string PaymentTracking { get; set; } = "Not Linked";
    public long LinkedBillId { get; set; }
    public string LinkedBillName { get; set; } = string.Empty;
    public string Status { get; set; } = "Current";
    public string Priority { get; set; } = "Normal";
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public decimal MonthlyEquivalent => CalculateMonthlyEquivalent(MinimumPayment, PaymentFrequency, IsActive);

    public string BalanceText => CurrentBalance <= 0m ? "Unknown" : CurrentBalance.ToString("C2", CultureInfo.CurrentCulture);
    public string MonthlyEquivalentText => MonthlyEquivalent.ToString("C2", CultureInfo.CurrentCulture);
    public string StatusText => IsActive ? "Active" : "Inactive";

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
