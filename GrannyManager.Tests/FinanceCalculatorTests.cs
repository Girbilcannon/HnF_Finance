using GrannyManager.Core.Models;
using GrannyManager.Core.Services;
using Xunit;

namespace GrannyManager.Tests;

public class FinanceCalculatorTests
{
    [Fact]
    public void CalculateMonthlyIncome_WithMonthlyIncome_ReturnsSameAmount()
    {
        var income = new IncomeSource
        {
            SourceName = "Test Income",
            Amount = 1000m,
            Frequency = "Monthly",
            IsActive = true
        };

        var monthly = FinanceCalculator.CalculateMonthlyIncome(new[] { income });

        Assert.Equal(1000m, monthly);
    }

    [Fact]
    public void CalculateMonthlyIncome_WithWeeklyIncome_ConvertsToMonthlyEquivalent()
    {
        var income = new IncomeSource
        {
            SourceName = "Weekly Income",
            Amount = 100m,
            Frequency = "Weekly",
            IsActive = true
        };

        var monthly = FinanceCalculator.CalculateMonthlyIncome(new[] { income });

        Assert.Equal(Math.Round(100m * 52m / 12m, 2), monthly);
    }

    [Fact]
    public void CalculateMonthlyIncome_IgnoresInactiveIncome()
    {
        var income = new IncomeSource
        {
            SourceName = "Inactive Income",
            Amount = 1000m,
            Frequency = "Monthly",
            IsActive = false
        };

        var monthly = FinanceCalculator.CalculateMonthlyIncome(new[] { income });

        Assert.Equal(0m, monthly);
    }
}
