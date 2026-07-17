using System;
using System.Collections.Generic;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class AllowanceSavingsRepository
{
    private readonly string _databasePath;

    public AllowanceSavingsRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureTable();
    }

    public IReadOnlyList<AllowanceSavingsItem> GetAll()
    {
        var items = new List<AllowanceSavingsItem>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ItemName, ItemType, Amount, Frequency, WhereStored, StorageMethod,
       LinkedBankAssetId, LinkedBankAssetName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM AllowanceSavingsItems
ORDER BY IsActive DESC, ItemType COLLATE NOCASE, ItemName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadItem(reader));

        return items;
    }

    public AllowanceSavingsItem? GetById(long id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ItemName, ItemType, Amount, Frequency, WhereStored, StorageMethod,
       LinkedBankAssetId, LinkedBankAssetName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM AllowanceSavingsItems
WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadItem(reader) : null;
    }

    public long Upsert(AllowanceSavingsItem item)
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
INSERT INTO AllowanceSavingsItems
(ItemName, ItemType, Amount, Frequency, WhereStored, StorageMethod,
 LinkedBankAssetId, LinkedBankAssetName, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($ItemName, $ItemType, $Amount, $Frequency, $WhereStored, $StorageMethod,
 $LinkedBankAssetId, $LinkedBankAssetName, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE AllowanceSavingsItems
SET ItemName = $ItemName,
    ItemType = $ItemType,
    Amount = $Amount,
    Frequency = $Frequency,
    WhereStored = $WhereStored,
    StorageMethod = $StorageMethod,
    LinkedBankAssetId = $LinkedBankAssetId,
    LinkedBankAssetName = $LinkedBankAssetName,
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
        command.CommandText = "DELETE FROM AllowanceSavingsItems WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
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
);";
            command.ExecuteNonQuery();
        }

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
        EnsureColumn(connection, "AllowanceSavingsItems", "CreatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "AllowanceSavingsItems", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_AllowanceSavingsItems_ItemName ON AllowanceSavingsItems(ItemName);
CREATE INDEX IF NOT EXISTS IX_AllowanceSavingsItems_ItemType ON AllowanceSavingsItems(ItemType);";
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

    private static void AddParameters(SqliteCommand command, AllowanceSavingsItem item)
    {
        command.Parameters.AddWithValue("$ItemName", item.ItemName.Trim());
        command.Parameters.AddWithValue("$ItemType", string.IsNullOrWhiteSpace(item.ItemType) ? "Allowance" : item.ItemType.Trim());
        command.Parameters.AddWithValue("$Amount", item.Amount);
        command.Parameters.AddWithValue("$Frequency", string.IsNullOrWhiteSpace(item.Frequency) ? "Monthly" : item.Frequency.Trim());
        command.Parameters.AddWithValue("$WhereStored", item.WhereStored.Trim());
        command.Parameters.AddWithValue("$StorageMethod", string.IsNullOrWhiteSpace(item.StorageMethod) ? "Cash / Envelope" : item.StorageMethod.Trim());
        command.Parameters.AddWithValue("$LinkedBankAssetId", item.LinkedBankAssetId);
        command.Parameters.AddWithValue("$LinkedBankAssetName", item.LinkedBankAssetName.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static AllowanceSavingsItem ReadItem(SqliteDataReader reader)
    {
        return new AllowanceSavingsItem
        {
            Id = reader.GetInt64(0),
            ItemName = GetString(reader, 1),
            ItemType = GetString(reader, 2),
            Amount = GetDecimal(reader, 3),
            Frequency = GetString(reader, 4),
            WhereStored = GetString(reader, 5),
            StorageMethod = GetString(reader, 6),
            LinkedBankAssetId = GetLong(reader, 7),
            LinkedBankAssetName = GetString(reader, 8),
            IsActive = GetBool(reader, 9),
            Notes = GetString(reader, 10),
            CreatedUtc = GetDateTime(reader, 11),
            UpdatedUtc = GetDateTime(reader, 12)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static bool GetBool(SqliteDataReader reader, int index) => !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    private static long GetLong(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
    private static decimal GetDecimal(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
}
