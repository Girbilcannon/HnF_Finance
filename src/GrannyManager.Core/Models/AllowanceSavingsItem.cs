namespace GrannyManager.Core.Models;

public sealed class AllowanceSavingsItem
{
    public int Id { get; set; }
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

    public decimal GetMonthlyEquivalent()
    {
        return Frequency.Trim().ToLowerInvariant() switch
        {
            "weekly" => Amount * 52m / 12m,
            "every 2 weeks" => Amount * 26m / 12m,
            "every two weeks" => Amount * 26m / 12m,
            "biweekly" => Amount * 26m / 12m,
            "twice monthly" => Amount * 2m,
            "monthly" => Amount,
            "quarterly" => Amount / 3m,
            "yearly" => Amount / 12m,
            "annually" => Amount / 12m,
            "annual" => Amount / 12m,
            "one-time / irregular" => 0m,
            "one-time" => 0m,
            "one time" => 0m,
            "irregular" => 0m,
            _ => Amount
        };
    }

    public bool IsSavings => string.Equals(ItemType, "Savings", StringComparison.OrdinalIgnoreCase);
    public bool IsAllowance => string.Equals(ItemType, "Allowance", StringComparison.OrdinalIgnoreCase);
}
