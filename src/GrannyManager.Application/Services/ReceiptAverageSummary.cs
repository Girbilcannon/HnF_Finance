namespace GrannyManager.Application.Services;

public sealed record ReceiptAverageSummary(
    string ReceiptType,
    int ReceiptCount,
    int MonthsTracked,
    decimal TotalEntered,
    decimal RoundedMonthlyEstimate);
