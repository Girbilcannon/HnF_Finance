namespace GrannyManager.Core.Models;

public sealed class HouseholdPerson
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool LivesInHousehold { get; set; }
    public bool PaysRent { get; set; }
    // Legacy field kept for existing cases; new contributions should link to Income Sources instead.
    public decimal MonthlyContribution { get; set; }
    public string ContributionHandling { get; set; } = "No Contribution";
    public long LinkedIncomeSourceId { get; set; }
    public string LinkedIncomeSourceName { get; set; } = string.Empty;
    public bool UsesHouseholdVehicle { get; set; }
    public bool ReceivesRides { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
