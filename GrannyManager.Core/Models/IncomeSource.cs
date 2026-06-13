using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class IncomeSource
{
    public long Id { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string IncomeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool TaxesWithheld { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public string ExpectedDayOrDate { get; set; } = string.Empty;
    public string DepositedToAccount { get; set; } = string.Empty;
    public string DepositMethod { get; set; } = "Cash";
    public long LinkedBankAssetId { get; set; }
    public string LinkedBankAssetName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public decimal MonthlyEquivalent => CalculateMonthlyEquivalent(Amount, Frequency, IsActive);

    public string MonthlyEquivalentText => MonthlyEquivalent.ToString("C2", CultureInfo.CurrentCulture);

    public string AmountLabel => TaxesWithheld ? "Payment Amount (After Taxes)" : "Gross Pay";

    public string TaxHandlingText => TaxesWithheld ? "Taxes withheld" : "Taxes not withheld / unknown";

    public string DepositDisplayText
    {
        get
        {
            var method = string.IsNullOrWhiteSpace(DepositMethod) ? DepositedToAccount : DepositMethod;

            if (string.Equals(method, "Select Bank Account", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(LinkedBankAssetName) ? "Bank account not selected" : LinkedBankAssetName;

            if (string.Equals(method, "Add Bank Account", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(LinkedBankAssetName) ? "Bank account not selected" : LinkedBankAssetName;

            if (string.Equals(method, "Bank Account", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(LinkedBankAssetName) ? "Bank account not selected" : LinkedBankAssetName;

            if (!string.IsNullOrWhiteSpace(method))
                return method;

            return string.IsNullOrWhiteSpace(DepositedToAccount) ? "Not specified" : DepositedToAccount;
        }
    }

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
