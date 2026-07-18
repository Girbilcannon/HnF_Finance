using System;
using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class Bill
{
    public long Id { get; set; }
    public string BillName { get; set; } = string.Empty;
    public string Category { get; set; } = "Utilities";
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public string DueDate { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Cash/Check";
    public long LinkedBankAssetId { get; set; }
    public string LinkedBankAssetName { get; set; } = string.Empty;
    public long LinkedDebtId { get; set; }
    public string LinkedDebtName { get; set; } = string.Empty;
    public bool IsAutopay { get; set; }
    public decimal PastDueAmount { get; set; }
    public string PaidBy { get; set; } = "Self (Primary Person)";
    public long PaidByHouseholdPersonId { get; set; }
    public string ResponsibilityOwner { get; set; } = "Self (Primary Person)";
    public long ResponsibilityOwnerHouseholdPersonId { get; set; }
    public string Priority { get; set; } = "Normal";
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public decimal MonthlyEquivalent => CalculateMonthlyEquivalent(Amount, Frequency, IsActive);
    public string AmountText => Amount.ToString("C2", CultureInfo.CurrentCulture);
    public string MonthlyEquivalentText => MonthlyEquivalent.ToString("C2", CultureInfo.CurrentCulture);
    public string PastDueAmountText => PastDueAmount <= 0 ? "$0.00" : PastDueAmount.ToString("C2", CultureInfo.CurrentCulture);
    public string PaymentDisplayText
    {
        get
        {
            if (LinkedBankAssetId > 0 && LinkedDebtId > 0)
                return $"{PaymentMethod} - {LinkedBankAssetName} + {LinkedDebtName}";

            if (LinkedBankAssetId > 0 && !string.IsNullOrWhiteSpace(LinkedBankAssetName))
                return $"{PaymentMethod} - {LinkedBankAssetName}";

            if (LinkedDebtId > 0 && !string.IsNullOrWhiteSpace(LinkedDebtName))
                return $"{PaymentMethod} - {LinkedDebtName}";

            return PaymentMethod;
        }
    }
    public string AutopayText => IsAutopay ? $"Autopay - {PaymentDisplayText}" : PaymentDisplayText;

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
