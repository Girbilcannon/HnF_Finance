using System;
using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class AssetItem
{
    public long Id { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = "Bank Account";
    public decimal EstimatedValue { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountLastFour { get; set; } = string.Empty;
    public string LinkedIncomeSourceName { get; set; } = string.Empty;
    public string LinkedBillName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public bool IsBankAccount => string.Equals(AssetType, "Bank Account", StringComparison.OrdinalIgnoreCase);
    public string EstimatedValueText => EstimatedValue.ToString("C2", CultureInfo.CurrentCulture);

    public string DisplayName
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(AssetName) ? "Unnamed Asset" : AssetName.Trim();
            if (!IsBankAccount || string.IsNullOrWhiteSpace(AccountLastFour))
                return name;

            return $"{name} ••••{AccountLastFour.Trim()}";
        }
    }
}
