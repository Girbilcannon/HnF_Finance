using System.Globalization;

namespace GrannyManager.Core.Models;

public sealed class AssetItem
{
    public long Id { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = "Vehicle";
    public string Owner { get; set; } = string.Empty;
    public decimal EstimatedValue { get; set; }
    public string Status { get; set; } = "Active / In Use";
    public string LocationOrInstitution { get; set; } = string.Empty;

    // Vehicle fields
    public string VehicleYear { get; set; } = string.Empty;
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleVin { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = string.Empty;
    public string RegistrationDueDate { get; set; } = string.Empty;
    public decimal Mileage { get; set; }
    public decimal Mpg { get; set; }
    public string PrimaryDriver { get; set; } = string.Empty;

    // Property fields
    public string PropertyType { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string Occupants { get; set; } = string.Empty;
    public string HoaOrManagement { get; set; } = string.Empty;

    // Bank / cash account fields
    public string InstitutionName { get; set; } = string.Empty;
    public string AccountNickname { get; set; } = string.Empty;
    public decimal CurrentBalanceValue { get; set; }

    // Valuable item fields
    public string ValuableDescription { get; set; } = string.Empty;
    public string SerialOrIdentifier { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;

    // Other fields
    public string OtherDetails { get; set; } = string.Empty;

    // Compatibility fields from earlier asset builds. These are no longer used by the current UI,
    // but keeping them avoids breaking existing databases or older saved rows.
    public string HoldingSymbol { get; set; } = string.Empty;
    public decimal HoldingQuantity { get; set; }

    // True linked records
    public string RecurringCostHandling { get; set; } = "Not Applicable";
    public long LinkedBillId { get; set; }
    public string LinkedBillName { get; set; } = string.Empty;
    public string DatePaidOff { get; set; } = string.Empty;
    public string IncomeHandling { get; set; } = "No Income";
    public long LinkedIncomeSourceId { get; set; }
    public string LinkedIncomeSourceName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string ValueDisplay => EstimatedValue <= 0 ? "Unknown" : EstimatedValue.ToString("C2", CultureInfo.CurrentCulture);

    public string CurrentBalanceDisplay => CurrentBalanceValue <= 0 ? "Not entered" : CurrentBalanceValue.ToString("C2", CultureInfo.CurrentCulture);

    public string MileageDisplay => Mileage <= 0 ? "Not entered" : Mileage.ToString("N0", CultureInfo.CurrentCulture);

    public string MpgDisplay => Mpg <= 0 ? "Not entered" : Mpg.ToString("0.##", CultureInfo.CurrentCulture);

    public string HoldingQuantityDisplay => HoldingQuantity <= 0 ? "Not entered" : HoldingQuantity.ToString("0.########", CultureInfo.CurrentCulture);
}
