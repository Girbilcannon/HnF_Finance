using System;
using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class AllowanceSavingsItem
{
    public long Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Allowance";
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public string WhereStored { get; set; } = string.Empty;
    public string StorageMethod { get; set; } = "Cash / Envelope";
    public long LinkedBankAssetId { get; set; }
    public string LinkedBankAssetName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public bool IsSavings => string.Equals(ItemType, "Savings", StringComparison.OrdinalIgnoreCase);
    public bool IsAllowance => string.Equals(ItemType, "Allowance", StringComparison.OrdinalIgnoreCase);

    public decimal MonthlyEquivalent => CalculateMonthlyEquivalent(Amount, Frequency, IsActive);
    public string AmountText => Amount.ToString("C2", CultureInfo.CurrentCulture);
    public string MonthlyEquivalentText => MonthlyEquivalent.ToString("C2", CultureInfo.CurrentCulture);

    public decimal GetMonthlyEquivalent()
    {
        return MonthlyEquivalent;
    }

    public static decimal CalculateMonthlyEquivalent(decimal amount, string? frequency, bool isActive = true)
    {
        if (!isActive)
            return 0m;

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
}
