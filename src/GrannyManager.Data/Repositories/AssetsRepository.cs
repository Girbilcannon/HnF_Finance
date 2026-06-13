using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class AssetsRepository
{
    private readonly string _databasePath;

    public AssetsRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
    }

    public IReadOnlyList<AssetItem> GetAll()
    {
        var items = new List<AssetItem>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, AssetName, AssetType, Owner, EstimatedValue, Status, LocationOrInstitution,
       VehicleYear, VehicleMake, VehicleModel, VehicleVin, VehiclePlate, RegistrationStatus, RegistrationDueDate,
       Mileage, Mpg, PrimaryDriver,
       PropertyType, PropertyAddress, Occupants, HoaOrManagement,
       InstitutionName, AccountNickname, CurrentBalanceValue,
       ValuableDescription, SerialOrIdentifier, StorageLocation,
       OtherDetails, HoldingSymbol, HoldingQuantity,
       RecurringCostHandling, LinkedBillId, LinkedBillName, DatePaidOff,
       IncomeHandling, LinkedIncomeSourceId, LinkedIncomeSourceName,
       IsActive, Notes, CreatedUtc, UpdatedUtc
FROM Assets
ORDER BY IsActive DESC, AssetType COLLATE NOCASE, AssetName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadAsset(reader));

        return items;
    }

    public long Upsert(AssetItem item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        item.UpdatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        if (item.Id <= 0)
        {
            item.CreatedUtc = DateTime.UtcNow;
            command.CommandText = @"
INSERT INTO Assets
(AssetName, AssetType, Owner, EstimatedValue, Status, LocationOrInstitution,
 VehicleYear, VehicleMake, VehicleModel, VehicleVin, VehiclePlate, RegistrationStatus, RegistrationDueDate,
 Mileage, Mpg, PrimaryDriver,
 PropertyType, PropertyAddress, Occupants, HoaOrManagement,
 InstitutionName, AccountNickname, CurrentBalanceValue,
 ValuableDescription, SerialOrIdentifier, StorageLocation,
 OtherDetails, HoldingSymbol, HoldingQuantity,
 RecurringCostHandling, LinkedBillId, LinkedBillName, DatePaidOff,
 IncomeHandling, LinkedIncomeSourceId, LinkedIncomeSourceName,
 IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($AssetName, $AssetType, $Owner, $EstimatedValue, $Status, $LocationOrInstitution,
 $VehicleYear, $VehicleMake, $VehicleModel, $VehicleVin, $VehiclePlate, $RegistrationStatus, $RegistrationDueDate,
 $Mileage, $Mpg, $PrimaryDriver,
 $PropertyType, $PropertyAddress, $Occupants, $HoaOrManagement,
 $InstitutionName, $AccountNickname, $CurrentBalanceValue,
 $ValuableDescription, $SerialOrIdentifier, $StorageLocation,
 $OtherDetails, $HoldingSymbol, $HoldingQuantity,
 $RecurringCostHandling, $LinkedBillId, $LinkedBillName, $DatePaidOff,
 $IncomeHandling, $LinkedIncomeSourceId, $LinkedIncomeSourceName,
 $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Assets
SET AssetName = $AssetName,
    AssetType = $AssetType,
    Owner = $Owner,
    EstimatedValue = $EstimatedValue,
    Status = $Status,
    LocationOrInstitution = $LocationOrInstitution,
    VehicleYear = $VehicleYear,
    VehicleMake = $VehicleMake,
    VehicleModel = $VehicleModel,
    VehicleVin = $VehicleVin,
    VehiclePlate = $VehiclePlate,
    RegistrationStatus = $RegistrationStatus,
    RegistrationDueDate = $RegistrationDueDate,
    Mileage = $Mileage,
    Mpg = $Mpg,
    PrimaryDriver = $PrimaryDriver,
    PropertyType = $PropertyType,
    PropertyAddress = $PropertyAddress,
    Occupants = $Occupants,
    HoaOrManagement = $HoaOrManagement,
    InstitutionName = $InstitutionName,
    AccountNickname = $AccountNickname,
    CurrentBalanceValue = $CurrentBalanceValue,
    ValuableDescription = $ValuableDescription,
    SerialOrIdentifier = $SerialOrIdentifier,
    StorageLocation = $StorageLocation,
    OtherDetails = $OtherDetails,
    HoldingSymbol = $HoldingSymbol,
    HoldingQuantity = $HoldingQuantity,
    RecurringCostHandling = $RecurringCostHandling,
    LinkedBillId = $LinkedBillId,
    LinkedBillName = $LinkedBillName,
    DatePaidOff = $DatePaidOff,
    IncomeHandling = $IncomeHandling,
    LinkedIncomeSourceId = $LinkedIncomeSourceId,
    LinkedIncomeSourceName = $LinkedIncomeSourceName,
    IsActive = $IsActive,
    Notes = $Notes,
    UpdatedUtc = $UpdatedUtc
WHERE Id = $Id;
SELECT $Id;";
            command.Parameters.AddWithValue("$Id", item.Id);
        }

        AddParameters(command, item);
        var result = command.ExecuteScalar();
        item.Id = Convert.ToInt64(result);
        return item.Id;
    }

    public void Delete(long id)
    {
        if (id <= 0)
            return;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Assets WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, AssetItem item)
    {
        command.Parameters.AddWithValue("$AssetName", item.AssetName.Trim());
        command.Parameters.AddWithValue("$AssetType", item.AssetType.Trim());
        command.Parameters.AddWithValue("$Owner", item.Owner.Trim());
        command.Parameters.AddWithValue("$EstimatedValue", item.EstimatedValue);
        command.Parameters.AddWithValue("$Status", item.Status.Trim());
        command.Parameters.AddWithValue("$LocationOrInstitution", item.LocationOrInstitution.Trim());
        command.Parameters.AddWithValue("$VehicleYear", item.VehicleYear.Trim());
        command.Parameters.AddWithValue("$VehicleMake", item.VehicleMake.Trim());
        command.Parameters.AddWithValue("$VehicleModel", item.VehicleModel.Trim());
        command.Parameters.AddWithValue("$VehicleVin", item.VehicleVin.Trim());
        command.Parameters.AddWithValue("$VehiclePlate", item.VehiclePlate.Trim());
        command.Parameters.AddWithValue("$RegistrationStatus", item.RegistrationStatus.Trim());
        command.Parameters.AddWithValue("$RegistrationDueDate", item.RegistrationDueDate.Trim());
        command.Parameters.AddWithValue("$Mileage", item.Mileage);
        command.Parameters.AddWithValue("$Mpg", item.Mpg);
        command.Parameters.AddWithValue("$PrimaryDriver", item.PrimaryDriver.Trim());
        command.Parameters.AddWithValue("$PropertyType", item.PropertyType.Trim());
        command.Parameters.AddWithValue("$PropertyAddress", item.PropertyAddress.Trim());
        command.Parameters.AddWithValue("$Occupants", item.Occupants.Trim());
        command.Parameters.AddWithValue("$HoaOrManagement", item.HoaOrManagement.Trim());
        command.Parameters.AddWithValue("$InstitutionName", item.InstitutionName.Trim());
        command.Parameters.AddWithValue("$AccountNickname", item.AccountNickname.Trim());
        command.Parameters.AddWithValue("$CurrentBalanceValue", item.CurrentBalanceValue);
        command.Parameters.AddWithValue("$ValuableDescription", item.ValuableDescription.Trim());
        command.Parameters.AddWithValue("$SerialOrIdentifier", item.SerialOrIdentifier.Trim());
        command.Parameters.AddWithValue("$StorageLocation", item.StorageLocation.Trim());
        command.Parameters.AddWithValue("$OtherDetails", item.OtherDetails.Trim());
        command.Parameters.AddWithValue("$HoldingSymbol", item.HoldingSymbol.Trim());
        command.Parameters.AddWithValue("$HoldingQuantity", item.HoldingQuantity);
        command.Parameters.AddWithValue("$RecurringCostHandling", item.RecurringCostHandling.Trim());
        command.Parameters.AddWithValue("$LinkedBillId", item.LinkedBillId);
        command.Parameters.AddWithValue("$LinkedBillName", item.LinkedBillName.Trim());
        command.Parameters.AddWithValue("$DatePaidOff", item.DatePaidOff.Trim());
        command.Parameters.AddWithValue("$IncomeHandling", item.IncomeHandling.Trim());
        command.Parameters.AddWithValue("$LinkedIncomeSourceId", item.LinkedIncomeSourceId);
        command.Parameters.AddWithValue("$LinkedIncomeSourceName", item.LinkedIncomeSourceName.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToString("O"));
    }

    private static AssetItem ReadAsset(SqliteDataReader reader)
    {
        return new AssetItem
        {
            Id = reader.GetInt64(0),
            AssetName = reader.GetString(1),
            AssetType = reader.GetString(2),
            Owner = reader.GetString(3),
            EstimatedValue = ReadDecimal(reader, 4),
            Status = reader.GetString(5),
            LocationOrInstitution = reader.GetString(6),
            VehicleYear = reader.GetString(7),
            VehicleMake = reader.GetString(8),
            VehicleModel = reader.GetString(9),
            VehicleVin = reader.GetString(10),
            VehiclePlate = reader.GetString(11),
            RegistrationStatus = reader.GetString(12),
            RegistrationDueDate = reader.GetString(13),
            Mileage = ReadDecimal(reader, 14),
            Mpg = ReadDecimal(reader, 15),
            PrimaryDriver = reader.GetString(16),
            PropertyType = reader.GetString(17),
            PropertyAddress = reader.GetString(18),
            Occupants = reader.GetString(19),
            HoaOrManagement = reader.GetString(20),
            InstitutionName = reader.GetString(21),
            AccountNickname = reader.GetString(22),
            CurrentBalanceValue = ReadDecimal(reader, 23),
            ValuableDescription = reader.GetString(24),
            SerialOrIdentifier = reader.GetString(25),
            StorageLocation = reader.GetString(26),
            OtherDetails = reader.GetString(27),
            HoldingSymbol = reader.GetString(28),
            HoldingQuantity = ReadDecimal(reader, 29),
            RecurringCostHandling = reader.GetString(30),
            LinkedBillId = reader.GetInt64(31),
            LinkedBillName = reader.GetString(32),
            DatePaidOff = reader.GetString(33),
            IncomeHandling = reader.GetString(34),
            LinkedIncomeSourceId = reader.GetInt64(35),
            LinkedIncomeSourceName = reader.GetString(36),
            IsActive = reader.GetInt32(37) == 1,
            Notes = reader.GetString(38),
            CreatedUtc = DateTime.TryParse(reader.GetString(39), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(40), out var updated) ? updated : DateTime.UtcNow
        };
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return 0m;

        return Convert.ToDecimal(reader.GetDouble(ordinal));
    }
}
