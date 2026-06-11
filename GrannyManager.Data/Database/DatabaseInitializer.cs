using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Database;

public static class DatabaseInitializer
{
    public static void EnsureCreated(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));

        var folder = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS HouseholdPeople (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL DEFAULT '',
    Relationship TEXT NOT NULL DEFAULT '',
    Role TEXT NOT NULL DEFAULT '',
    LivesInHousehold INTEGER NOT NULL DEFAULT 0,
    PaysRent INTEGER NOT NULL DEFAULT 0,
    MonthlyContribution REAL NOT NULL DEFAULT 0,
    ContributionHandling TEXT NOT NULL DEFAULT 'No Contribution',
    LinkedIncomeSourceId INTEGER NOT NULL DEFAULT 0,
    LinkedIncomeSourceName TEXT NOT NULL DEFAULT '',
    UsesHouseholdVehicle INTEGER NOT NULL DEFAULT 0,
    ReceivesRides INTEGER NOT NULL DEFAULT 0,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_HouseholdPeople_FullName ON HouseholdPeople(FullName);

CREATE TABLE IF NOT EXISTS IncomeSources (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SourceName TEXT NOT NULL DEFAULT '',
    IncomeType TEXT NOT NULL DEFAULT '',
    Amount REAL NOT NULL DEFAULT 0,
    TaxesWithheld INTEGER NOT NULL DEFAULT 0,
    Frequency TEXT NOT NULL DEFAULT 'Monthly',
    ExpectedDayOrDate TEXT NOT NULL DEFAULT '',
    DepositedToAccount TEXT NOT NULL DEFAULT '',
    DepositMethod TEXT NOT NULL DEFAULT 'Cash',
    LinkedBankAssetId INTEGER NOT NULL DEFAULT 0,
    LinkedBankAssetName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_IncomeSources_SourceName ON IncomeSources(SourceName);

CREATE TABLE IF NOT EXISTS BillsSpending (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BillName TEXT NOT NULL DEFAULT '',
    Category TEXT NOT NULL DEFAULT '',
    Amount REAL NOT NULL DEFAULT 0,
    Frequency TEXT NOT NULL DEFAULT 'Monthly',
    DueDate TEXT NOT NULL DEFAULT '',
    IsAutopay INTEGER NOT NULL DEFAULT 0,
    PaidBy TEXT NOT NULL DEFAULT '',
    ResponsibilityOwner TEXT NOT NULL DEFAULT '',
    PastDueAmount REAL NOT NULL DEFAULT 0,
    Priority TEXT NOT NULL DEFAULT 'Normal',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_BillsSpending_BillName ON BillsSpending(BillName);

CREATE TABLE IF NOT EXISTS AllowanceSavingsItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemName TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL DEFAULT 'Allowance',
    Amount REAL NOT NULL DEFAULT 0,
    Frequency TEXT NOT NULL DEFAULT 'Monthly',
    WhereStored TEXT NOT NULL DEFAULT '',
    StorageMethod TEXT NOT NULL DEFAULT 'Cash / Envelope',
    LinkedBankAssetId INTEGER NOT NULL DEFAULT 0,
    LinkedBankAssetName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_AllowanceSavingsItems_ItemName ON AllowanceSavingsItems(ItemName);
CREATE INDEX IF NOT EXISTS IX_AllowanceSavingsItems_ItemType ON AllowanceSavingsItems(ItemType);

CREATE TABLE IF NOT EXISTS Assets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AssetName TEXT NOT NULL DEFAULT '',
    AssetType TEXT NOT NULL DEFAULT 'Vehicle',
    Owner TEXT NOT NULL DEFAULT '',
    EstimatedValue REAL NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Active / In Use',
    LocationOrInstitution TEXT NOT NULL DEFAULT '',
    VehicleYear TEXT NOT NULL DEFAULT '',
    VehicleMake TEXT NOT NULL DEFAULT '',
    VehicleModel TEXT NOT NULL DEFAULT '',
    VehicleVin TEXT NOT NULL DEFAULT '',
    VehiclePlate TEXT NOT NULL DEFAULT '',
    RegistrationStatus TEXT NOT NULL DEFAULT '',
    RegistrationDueDate TEXT NOT NULL DEFAULT '',
    Mileage REAL NOT NULL DEFAULT 0,
    Mpg REAL NOT NULL DEFAULT 0,
    PrimaryDriver TEXT NOT NULL DEFAULT '',
    PropertyType TEXT NOT NULL DEFAULT '',
    PropertyAddress TEXT NOT NULL DEFAULT '',
    Occupants TEXT NOT NULL DEFAULT '',
    HoaOrManagement TEXT NOT NULL DEFAULT '',
    InstitutionName TEXT NOT NULL DEFAULT '',
    AccountNickname TEXT NOT NULL DEFAULT '',
    CurrentBalanceValue REAL NOT NULL DEFAULT 0,
    ValuableDescription TEXT NOT NULL DEFAULT '',
    SerialOrIdentifier TEXT NOT NULL DEFAULT '',
    StorageLocation TEXT NOT NULL DEFAULT '',
    OtherDetails TEXT NOT NULL DEFAULT '',
    HoldingSymbol TEXT NOT NULL DEFAULT '',
    HoldingQuantity REAL NOT NULL DEFAULT 0,
    RecurringCostHandling TEXT NOT NULL DEFAULT 'Not Applicable',
    LinkedBillId INTEGER NOT NULL DEFAULT 0,
    LinkedBillName TEXT NOT NULL DEFAULT '',
    DatePaidOff TEXT NOT NULL DEFAULT '',
    IncomeHandling TEXT NOT NULL DEFAULT 'No Income',
    LinkedIncomeSourceId INTEGER NOT NULL DEFAULT 0,
    LinkedIncomeSourceName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_Assets_AssetName ON Assets(AssetName);
CREATE INDEX IF NOT EXISTS IX_Assets_AssetType ON Assets(AssetType);
";
        command.ExecuteNonQuery();

        EnsureColumn(connection, "IncomeSources", "TaxesWithheld", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomeSources", "DepositMethod", "TEXT NOT NULL DEFAULT 'Cash'");
        EnsureColumn(connection, "IncomeSources", "LinkedBankAssetId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomeSources", "LinkedBankAssetName", "TEXT NOT NULL DEFAULT ''");

        EnsureColumn(connection, "HouseholdPeople", "ContributionHandling", "TEXT NOT NULL DEFAULT 'No Contribution'");
        EnsureColumn(connection, "HouseholdPeople", "LinkedIncomeSourceId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "HouseholdPeople", "LinkedIncomeSourceName", "TEXT NOT NULL DEFAULT ''");

        EnsureColumn(connection, "BillsSpending", "Category", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "BillsSpending", "DueDate", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "BillsSpending", "IsAutopay", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "BillsSpending", "PaidBy", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "BillsSpending", "ResponsibilityOwner", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "BillsSpending", "PastDueAmount", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "BillsSpending", "Priority", "TEXT NOT NULL DEFAULT 'Normal'");
        EnsureColumn(connection, "BillsSpending", "IsActive", "INTEGER NOT NULL DEFAULT 1");

        EnsureColumn(connection, "AllowanceSavingsItems", "ItemName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "AllowanceSavingsItems", "ItemType", "TEXT NOT NULL DEFAULT 'Allowance'");
        EnsureColumn(connection, "AllowanceSavingsItems", "Amount", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "AllowanceSavingsItems", "Frequency", "TEXT NOT NULL DEFAULT 'Monthly'");
        EnsureColumn(connection, "AllowanceSavingsItems", "WhereStored", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "AllowanceSavingsItems", "StorageMethod", "TEXT NOT NULL DEFAULT 'Cash / Envelope'");
        EnsureColumn(connection, "AllowanceSavingsItems", "LinkedBankAssetId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "AllowanceSavingsItems", "LinkedBankAssetName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "AllowanceSavingsItems", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "AllowanceSavingsItems", "Notes", "TEXT NOT NULL DEFAULT ''");

        EnsureColumn(connection, "Assets", "AssetName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "AssetType", "TEXT NOT NULL DEFAULT 'Vehicle'");
        EnsureColumn(connection, "Assets", "Owner", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "EstimatedValue", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "Status", "TEXT NOT NULL DEFAULT 'Active / In Use'");
        EnsureColumn(connection, "Assets", "LocationOrInstitution", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "VehicleYear", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "VehicleMake", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "VehicleModel", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "VehicleVin", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "VehiclePlate", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "RegistrationStatus", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "RegistrationDueDate", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "Mileage", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "Mpg", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "PrimaryDriver", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "PropertyType", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "PropertyAddress", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "Occupants", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "HoaOrManagement", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "InstitutionName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "AccountNickname", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "CurrentBalanceValue", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "ValuableDescription", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "SerialOrIdentifier", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "StorageLocation", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "OtherDetails", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "HoldingSymbol", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "HoldingQuantity", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "RecurringCostHandling", "TEXT NOT NULL DEFAULT 'Not Applicable'");
        EnsureColumn(connection, "Assets", "LinkedBillId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "LinkedBillName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "DatePaidOff", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "IncomeHandling", "TEXT NOT NULL DEFAULT 'No Income'");
        EnsureColumn(connection, "Assets", "LinkedIncomeSourceId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "LinkedIncomeSourceName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "Assets", "Notes", "TEXT NOT NULL DEFAULT ''");
    }

    private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info({tableName});";

        var exists = false;
        using (var reader = checkCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
            return;

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alterCommand.ExecuteNonQuery();
    }
}
