using GrannyManager.Core.Enums;

namespace GrannyManager.Core.Models;

public sealed class FinanceSnapshot
{
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal MonthlyAllowance { get; set; }
    public decimal MonthlySavingsReserve { get; set; }
    public decimal Remaining => MonthlyIncome - MonthlyExpenses - MonthlyAllowance - MonthlySavingsReserve;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Unknown;
}
