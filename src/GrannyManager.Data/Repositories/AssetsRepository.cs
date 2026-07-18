using System;
using System.Collections.Generic;
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
        EnsureTable();
    }

    public IReadOnlyList<AssetItem> GetAll()
    {
        var items = new List<AssetItem>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, AssetName, AssetType, EstimatedValue, InstitutionName, AccountType, AccountLastFour,
       LinkedIncomeSourceName, LinkedBillName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM Assets
ORDER BY IsActive DESC, AssetType COLLATE NOCASE, AssetName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadAsset(reader));

        return items;
    }

    public IReadOnlyList<AssetItem> GetBankAccounts()
    {
        var items = new List<AssetItem>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, AssetName, AssetType, EstimatedValue, InstitutionName, AccountType, AccountLastFour,
       LinkedIncomeSourceName, LinkedBillName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM Assets
WHERE IsActive = 1 AND AssetType = 'Bank Account'
ORDER BY AssetName COLLATE NOCASE;";

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
(AssetName, AssetType, EstimatedValue, InstitutionName, AccountType, AccountLastFour,
 LinkedIncomeSourceName, LinkedBillName, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($AssetName, $AssetType, $EstimatedValue, $InstitutionName, $AccountType, $AccountLastFour,
 $LinkedIncomeSourceName, $LinkedBillName, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Assets
SET AssetName = $AssetName,
    AssetType = $AssetType,
    EstimatedValue = $EstimatedValue,
    InstitutionName = $InstitutionName,
    AccountType = $AccountType,
    AccountLastFour = $AccountLastFour,
    LinkedIncomeSourceName = $LinkedIncomeSourceName,
    LinkedBillName = $LinkedBillName,
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

    private void EnsureTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS Assets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AssetName TEXT NOT NULL DEFAULT '',
    AssetType TEXT NOT NULL DEFAULT 'Bank Account',
    EstimatedValue REAL NOT NULL DEFAULT 0,
    InstitutionName TEXT NOT NULL DEFAULT '',
    AccountType TEXT NOT NULL DEFAULT '',
    AccountLastFour TEXT NOT NULL DEFAULT '',
    LinkedIncomeSourceName TEXT NOT NULL DEFAULT '',
    LinkedBillName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            command.ExecuteNonQuery();
        }

        EnsureColumn(connection, "Assets", "AssetName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "AssetType", "TEXT NOT NULL DEFAULT 'Bank Account'");
        EnsureColumn(connection, "Assets", "EstimatedValue", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Assets", "InstitutionName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "AccountType", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "AccountLastFour", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "LinkedIncomeSourceName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "LinkedBillName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "Assets", "Notes", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Assets", "CreatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "Assets", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_Assets_AssetType ON Assets(AssetType);
CREATE INDEX IF NOT EXISTS IX_Assets_AssetName ON Assets(AssetName);";
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

    private static void AddParameters(SqliteCommand command, AssetItem item)
    {
        command.Parameters.AddWithValue("$AssetName", item.AssetName.Trim());
        command.Parameters.AddWithValue("$AssetType", string.IsNullOrWhiteSpace(item.AssetType) ? "Other" : item.AssetType.Trim());
        command.Parameters.AddWithValue("$EstimatedValue", item.EstimatedValue);
        command.Parameters.AddWithValue("$InstitutionName", item.InstitutionName.Trim());
        command.Parameters.AddWithValue("$AccountType", item.AccountType.Trim());
        command.Parameters.AddWithValue("$AccountLastFour", item.AccountLastFour.Trim());
        command.Parameters.AddWithValue("$LinkedIncomeSourceName", item.LinkedIncomeSourceName.Trim());
        command.Parameters.AddWithValue("$LinkedBillName", item.LinkedBillName.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static AssetItem ReadAsset(SqliteDataReader reader)
    {
        return new AssetItem
        {
            Id = reader.GetInt64(0),
            AssetName = GetString(reader, 1),
            AssetType = GetString(reader, 2),
            EstimatedValue = GetDecimal(reader, 3),
            InstitutionName = GetString(reader, 4),
            AccountType = GetString(reader, 5),
            AccountLastFour = GetString(reader, 6),
            LinkedIncomeSourceName = GetString(reader, 7),
            LinkedBillName = GetString(reader, 8),
            IsActive = GetBool(reader, 9),
            Notes = GetString(reader, 10),
            CreatedUtc = GetDateTime(reader, 11),
            UpdatedUtc = GetDateTime(reader, 12)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static bool GetBool(SqliteDataReader reader, int index) => !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    private static decimal GetDecimal(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
}
