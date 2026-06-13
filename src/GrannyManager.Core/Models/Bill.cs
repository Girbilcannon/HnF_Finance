using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class Bill
{
    public long Id { get; set; }
    public string BillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public string DueDate { get; set; } = string.Empty;
    public bool IsAutopay { get; set; }
    public string PaidBy { get; set; } = string.Empty;
    public string ResponsibilityOwner { get; set; } = string.Empty;
    public decimal PastDueAmount { get; set; }
    public string Priority { get; set; } = "Normal";
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public decimal MonthlyEquivalent => CalculateMonthlyEquivalent(Amount, Frequency, IsActive);

    public string MonthlyEquivalentText => MonthlyEquivalent.ToString("C2", CultureInfo.CurrentCulture);

    public string StatusText => IsActive ? "Active" : "Inactive";

    public string AutopayText => IsAutopay ? "Autopay" : "Manual payment";

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
