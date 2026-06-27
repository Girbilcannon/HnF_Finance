using System;
using System.Collections.Generic;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class BillsRepository
{
    private readonly string _databasePath;

    public BillsRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureTable();
    }

    public IReadOnlyList<Bill> GetAll()
    {
        var items = new List<Bill>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, BillName, Category, Amount, Frequency, DueDate, PaymentMethod, IsAutopay, PastDueAmount,
       PaidBy, PaidByHouseholdPersonId, ResponsibilityOwner, ResponsibilityOwnerHouseholdPersonId,
       Priority, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM Bills
ORDER BY IsActive DESC, Priority COLLATE NOCASE, BillName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadBill(reader));

        return items;
    }

    public long Upsert(Bill item)
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
INSERT INTO Bills
(BillName, Category, Amount, Frequency, DueDate, PaymentMethod, IsAutopay, PastDueAmount,
 PaidBy, PaidByHouseholdPersonId, ResponsibilityOwner, ResponsibilityOwnerHouseholdPersonId,
 Priority, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($BillName, $Category, $Amount, $Frequency, $DueDate, $PaymentMethod, $IsAutopay, $PastDueAmount,
 $PaidBy, $PaidByHouseholdPersonId, $ResponsibilityOwner, $ResponsibilityOwnerHouseholdPersonId,
 $Priority, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Bills
SET BillName = $BillName,
    Category = $Category,
    Amount = $Amount,
    Frequency = $Frequency,
    DueDate = $DueDate,
    PaymentMethod = $PaymentMethod,
    IsAutopay = $IsAutopay,
    PastDueAmount = $PastDueAmount,
    PaidBy = $PaidBy,
    PaidByHouseholdPersonId = $PaidByHouseholdPersonId,
    ResponsibilityOwner = $ResponsibilityOwner,
    ResponsibilityOwnerHouseholdPersonId = $ResponsibilityOwnerHouseholdPersonId,
    Priority = $Priority,
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
        command.CommandText = "DELETE FROM Bills WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS Bills (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BillName TEXT NOT NULL DEFAULT '',
    Category TEXT NOT NULL DEFAULT '',
    Amount REAL NOT NULL DEFAULT 0,
    Frequency TEXT NOT NULL DEFAULT 'Monthly',
    DueDate TEXT NOT NULL DEFAULT '',
    PaymentMethod TEXT NOT NULL DEFAULT '',
    IsAutopay INTEGER NOT NULL DEFAULT 0,
    PastDueAmount REAL NOT NULL DEFAULT 0,
    PaidBy TEXT NOT NULL DEFAULT 'Self (Primary Person)',
    PaidByHouseholdPersonId INTEGER NOT NULL DEFAULT 0,
    ResponsibilityOwner TEXT NOT NULL DEFAULT 'Self (Primary Person)',
    ResponsibilityOwnerHouseholdPersonId INTEGER NOT NULL DEFAULT 0,
    Priority TEXT NOT NULL DEFAULT 'Normal',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            command.ExecuteNonQuery();
        }

        EnsureColumn(connection, "Bills", "BillName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Bills", "Category", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Bills", "Amount", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Bills", "Frequency", "TEXT NOT NULL DEFAULT 'Monthly'");
        EnsureColumn(connection, "Bills", "DueDate", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Bills", "PaymentMethod", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Bills", "IsAutopay", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Bills", "PastDueAmount", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Bills", "PaidBy", "TEXT NOT NULL DEFAULT 'Self (Primary Person)'");
        EnsureColumn(connection, "Bills", "PaidByHouseholdPersonId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Bills", "ResponsibilityOwner", "TEXT NOT NULL DEFAULT 'Self (Primary Person)'");
        EnsureColumn(connection, "Bills", "ResponsibilityOwnerHouseholdPersonId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Bills", "Priority", "TEXT NOT NULL DEFAULT 'Normal'");
        EnsureColumn(connection, "Bills", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "Bills", "Notes", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Bills", "CreatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "Bills", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = "CREATE INDEX IF NOT EXISTS IX_Bills_BillName ON Bills(BillName);";
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

    private static void AddParameters(SqliteCommand command, Bill item)
    {
        command.Parameters.AddWithValue("$BillName", item.BillName.Trim());
        command.Parameters.AddWithValue("$Category", item.Category.Trim());
        command.Parameters.AddWithValue("$Amount", item.Amount);
        command.Parameters.AddWithValue("$Frequency", item.Frequency.Trim());
        command.Parameters.AddWithValue("$DueDate", item.DueDate.Trim());
        command.Parameters.AddWithValue("$PaymentMethod", item.PaymentMethod.Trim());
        command.Parameters.AddWithValue("$IsAutopay", item.IsAutopay ? 1 : 0);
        command.Parameters.AddWithValue("$PastDueAmount", item.PastDueAmount);
        command.Parameters.AddWithValue("$PaidBy", item.PaidBy.Trim());
        command.Parameters.AddWithValue("$PaidByHouseholdPersonId", item.PaidByHouseholdPersonId);
        command.Parameters.AddWithValue("$ResponsibilityOwner", item.ResponsibilityOwner.Trim());
        command.Parameters.AddWithValue("$ResponsibilityOwnerHouseholdPersonId", item.ResponsibilityOwnerHouseholdPersonId);
        command.Parameters.AddWithValue("$Priority", item.Priority.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static Bill ReadBill(SqliteDataReader reader)
    {
        return new Bill
        {
            Id = reader.GetInt64(0),
            BillName = GetString(reader, 1),
            Category = GetString(reader, 2),
            Amount = GetDecimal(reader, 3),
            Frequency = GetString(reader, 4),
            DueDate = GetString(reader, 5),
            PaymentMethod = GetString(reader, 6),
            IsAutopay = GetBool(reader, 7),
            PastDueAmount = GetDecimal(reader, 8),
            PaidBy = GetString(reader, 9),
            PaidByHouseholdPersonId = GetLong(reader, 10),
            ResponsibilityOwner = GetString(reader, 11),
            ResponsibilityOwnerHouseholdPersonId = GetLong(reader, 12),
            Priority = GetString(reader, 13),
            IsActive = GetBool(reader, 14),
            Notes = GetString(reader, 15),
            CreatedUtc = GetDateTime(reader, 16),
            UpdatedUtc = GetDateTime(reader, 17)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static bool GetBool(SqliteDataReader reader, int index) => !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    private static long GetLong(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
    private static decimal GetDecimal(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
}
