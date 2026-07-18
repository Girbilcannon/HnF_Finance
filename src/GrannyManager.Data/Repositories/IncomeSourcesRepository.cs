using System;
using System.Collections.Generic;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class IncomeSourcesRepository
{
    private readonly string _databasePath;

    public IncomeSourcesRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureTable();
    }

    public IReadOnlyList<IncomeSource> GetAll()
    {
        var items = new List<IncomeSource>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, SourceName, IncomeType, Amount, Frequency, TaxesWithheld, ExpectedDayOrDate, DepositDestination, LinkedBankAssetId, LinkedBankAssetName,
       LinkedHouseholdPersonId, LinkedHouseholdPersonName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM IncomeSources
ORDER BY IsActive DESC, SourceName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadIncomeSource(reader));

        return items;
    }

    public long Upsert(IncomeSource item)
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
INSERT INTO IncomeSources
(SourceName, IncomeType, Amount, Frequency, TaxesWithheld, ExpectedDayOrDate, DepositDestination, LinkedBankAssetId, LinkedBankAssetName,
 LinkedHouseholdPersonId, LinkedHouseholdPersonName, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($SourceName, $IncomeType, $Amount, $Frequency, $TaxesWithheld, $ExpectedDayOrDate, $DepositDestination, $LinkedBankAssetId, $LinkedBankAssetName,
 $LinkedHouseholdPersonId, $LinkedHouseholdPersonName, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE IncomeSources
SET SourceName = $SourceName,
    IncomeType = $IncomeType,
    Amount = $Amount,
    Frequency = $Frequency,
    TaxesWithheld = $TaxesWithheld,
    ExpectedDayOrDate = $ExpectedDayOrDate,
    DepositDestination = $DepositDestination,
    LinkedBankAssetId = $LinkedBankAssetId,
    LinkedBankAssetName = $LinkedBankAssetName,
    LinkedHouseholdPersonId = $LinkedHouseholdPersonId,
    LinkedHouseholdPersonName = $LinkedHouseholdPersonName,
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
        command.CommandText = "DELETE FROM IncomeSources WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS IncomeSources (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SourceName TEXT NOT NULL DEFAULT '',
    IncomeType TEXT NOT NULL DEFAULT '',
    Amount REAL NOT NULL DEFAULT 0,
    Frequency TEXT NOT NULL DEFAULT 'Monthly',
    TaxesWithheld INTEGER NOT NULL DEFAULT 0,
    ExpectedDayOrDate TEXT NOT NULL DEFAULT '',
    DepositDestination TEXT NOT NULL DEFAULT '',
    LinkedHouseholdPersonId INTEGER NOT NULL DEFAULT 0,
    LinkedHouseholdPersonName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            command.ExecuteNonQuery();
        }

        EnsureColumn(connection, "IncomeSources", "SourceName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "IncomeType", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "Amount", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomeSources", "Frequency", "TEXT NOT NULL DEFAULT 'Monthly'");
        EnsureColumn(connection, "IncomeSources", "TaxesWithheld", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomeSources", "ExpectedDayOrDate", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "DepositDestination", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "LinkedBankAssetId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomeSources", "LinkedBankAssetName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "LinkedHouseholdPersonId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomeSources", "LinkedHouseholdPersonName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "IncomeSources", "Notes", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "IncomeSources", "CreatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "IncomeSources", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = "CREATE INDEX IF NOT EXISTS IX_IncomeSources_SourceName ON IncomeSources(SourceName);";
        indexCommand.ExecuteNonQuery();
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

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, IncomeSource item)
    {
        command.Parameters.AddWithValue("$SourceName", item.SourceName.Trim());
        command.Parameters.AddWithValue("$IncomeType", item.IncomeType.Trim());
        command.Parameters.AddWithValue("$Amount", item.Amount);
        command.Parameters.AddWithValue("$Frequency", item.Frequency.Trim());
        command.Parameters.AddWithValue("$TaxesWithheld", item.TaxesWithheld ? 1 : 0);
        command.Parameters.AddWithValue("$ExpectedDayOrDate", item.ExpectedDayOrDate.Trim());
        command.Parameters.AddWithValue("$DepositDestination", item.DepositDestination.Trim());
        command.Parameters.AddWithValue("$LinkedBankAssetId", item.LinkedBankAssetId);
        command.Parameters.AddWithValue("$LinkedBankAssetName", item.LinkedBankAssetName.Trim());
        command.Parameters.AddWithValue("$LinkedHouseholdPersonId", item.LinkedHouseholdPersonId);
        command.Parameters.AddWithValue("$LinkedHouseholdPersonName", item.LinkedHouseholdPersonName.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static IncomeSource ReadIncomeSource(SqliteDataReader reader)
    {
        return new IncomeSource
        {
            Id = reader.GetInt64(0),
            SourceName = GetString(reader, 1),
            IncomeType = GetString(reader, 2),
            Amount = GetDecimal(reader, 3),
            Frequency = GetString(reader, 4),
            TaxesWithheld = GetBool(reader, 5),
            ExpectedDayOrDate = GetString(reader, 6),
            DepositDestination = GetString(reader, 7),
            LinkedBankAssetId = GetLong(reader, 8),
            LinkedBankAssetName = GetString(reader, 9),
            LinkedHouseholdPersonId = GetLong(reader, 10),
            LinkedHouseholdPersonName = GetString(reader, 11),
            IsActive = GetBool(reader, 12),
            Notes = GetString(reader, 13),
            CreatedUtc = GetDateTime(reader, 14),
            UpdatedUtc = GetDateTime(reader, 15)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static bool GetBool(SqliteDataReader reader, int index) => !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    private static long GetLong(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
    private static decimal GetDecimal(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
}
