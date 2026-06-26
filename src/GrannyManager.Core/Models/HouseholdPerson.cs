using System;

namespace GrannyManager.Core.Models;

public sealed class HouseholdPerson
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool LivesInHousehold { get; set; } = true;
    public bool PaysRent { get; set; }
    public bool UsesHouseholdVehicle { get; set; }
    public bool ReceivesRides { get; set; }
    public decimal MonthlyContribution { get; set; }

    // Compatibility field for the legacy WinForms fallback.
    // Avalonia uses MonthlyContribution and linked income fields, but legacy pages still reference this.
    public string ContributionHandling { get; set; } = "No Contribution";

    public long LinkedIncomeSourceId { get; set; }
    public string LinkedIncomeSourceName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
