using System;
using System.Collections.Generic;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class DebtsRepository
{
    private readonly string _databasePath;

    public DebtsRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureTable();
    }

    public IReadOnlyList<Debt> GetAll()
    {
        var items = new List<Debt>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, DebtName, DebtType, CreditorCollector, CurrentBalance, MinimumPayment, PaymentFrequency, DueDate,
       ResponsibilityOwner, PaidBy, PaymentTracking, LinkedBillId, LinkedBillName, Status, Priority,
       IsActive, Notes, CreatedUtc, UpdatedUtc
FROM Debts
ORDER BY IsActive DESC, Priority COLLATE NOCASE, DebtName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadDebt(reader));

        return items;
    }

    public IReadOnlyList<Debt> GetCreditCards()
    {
        var items = new List<Debt>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, DebtName, DebtType, CreditorCollector, CurrentBalance, MinimumPayment, PaymentFrequency, DueDate,
       ResponsibilityOwner, PaidBy, PaymentTracking, LinkedBillId, LinkedBillName, Status, Priority,
       IsActive, Notes, CreatedUtc, UpdatedUtc
FROM Debts
WHERE IsActive = 1 AND DebtType = 'Credit Card'
ORDER BY DebtName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadDebt(reader));

        return items;
    }

    public long Upsert(Debt item)
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
INSERT INTO Debts
(DebtName, DebtType, CreditorCollector, CurrentBalance, MinimumPayment, PaymentFrequency, DueDate,
 ResponsibilityOwner, PaidBy, PaymentTracking, LinkedBillId, LinkedBillName, Status, Priority, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($DebtName, $DebtType, $CreditorCollector, $CurrentBalance, $MinimumPayment, $PaymentFrequency, $DueDate,
 $ResponsibilityOwner, $PaidBy, $PaymentTracking, $LinkedBillId, $LinkedBillName, $Status, $Priority, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Debts
SET DebtName = $DebtName,
    DebtType = $DebtType,
    CreditorCollector = $CreditorCollector,
    CurrentBalance = $CurrentBalance,
    MinimumPayment = $MinimumPayment,
    PaymentFrequency = $PaymentFrequency,
    DueDate = $DueDate,
    ResponsibilityOwner = $ResponsibilityOwner,
    PaidBy = $PaidBy,
    PaymentTracking = $PaymentTracking,
    LinkedBillId = $LinkedBillId,
    LinkedBillName = $LinkedBillName,
    Status = $Status,
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
        command.CommandText = "DELETE FROM Debts WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS Debts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DebtName TEXT NOT NULL DEFAULT '',
    DebtType TEXT NOT NULL DEFAULT 'Credit Card',
    CreditorCollector TEXT NOT NULL DEFAULT '',
    CurrentBalance REAL NOT NULL DEFAULT 0,
    MinimumPayment REAL NOT NULL DEFAULT 0,
    PaymentFrequency TEXT NOT NULL DEFAULT 'Monthly',
    DueDate TEXT NOT NULL DEFAULT '',
    ResponsibilityOwner TEXT NOT NULL DEFAULT '',
    PaidBy TEXT NOT NULL DEFAULT '',
    PaymentTracking TEXT NOT NULL DEFAULT 'Not Linked',
    LinkedBillId INTEGER NOT NULL DEFAULT 0,
    LinkedBillName TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT 'Current',
    Priority TEXT NOT NULL DEFAULT 'Normal',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            command.ExecuteNonQuery();
        }

        EnsureColumn(connection, "Debts", "DebtName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "DebtType", "TEXT NOT NULL DEFAULT 'Credit Card'");
        EnsureColumn(connection, "Debts", "CreditorCollector", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "CurrentBalance", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Debts", "MinimumPayment", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Debts", "PaymentFrequency", "TEXT NOT NULL DEFAULT 'Monthly'");
        EnsureColumn(connection, "Debts", "DueDate", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "ResponsibilityOwner", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "PaidBy", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "PaymentTracking", "TEXT NOT NULL DEFAULT 'Not Linked'");
        EnsureColumn(connection, "Debts", "LinkedBillId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Debts", "LinkedBillName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "Status", "TEXT NOT NULL DEFAULT 'Current'");
        EnsureColumn(connection, "Debts", "Priority", "TEXT NOT NULL DEFAULT 'Normal'");
        EnsureColumn(connection, "Debts", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "Debts", "Notes", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Debts", "CreatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "Debts", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_Debts_DebtName ON Debts(DebtName);
CREATE INDEX IF NOT EXISTS IX_Debts_DebtType ON Debts(DebtType);
CREATE INDEX IF NOT EXISTS IX_Debts_IsActive ON Debts(IsActive);";
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

    private static void AddParameters(SqliteCommand command, Debt item)
    {
        command.Parameters.AddWithValue("$DebtName", item.DebtName.Trim());
        command.Parameters.AddWithValue("$DebtType", string.IsNullOrWhiteSpace(item.DebtType) ? "Credit Card" : item.DebtType.Trim());
        command.Parameters.AddWithValue("$CreditorCollector", item.CreditorCollector.Trim());
        command.Parameters.AddWithValue("$CurrentBalance", item.CurrentBalance);
        command.Parameters.AddWithValue("$MinimumPayment", item.MinimumPayment);
        command.Parameters.AddWithValue("$PaymentFrequency", string.IsNullOrWhiteSpace(item.PaymentFrequency) ? "Monthly" : item.PaymentFrequency.Trim());
        command.Parameters.AddWithValue("$DueDate", item.DueDate.Trim());
        command.Parameters.AddWithValue("$ResponsibilityOwner", item.ResponsibilityOwner.Trim());
        command.Parameters.AddWithValue("$PaidBy", item.PaidBy.Trim());
        command.Parameters.AddWithValue("$PaymentTracking", string.IsNullOrWhiteSpace(item.PaymentTracking) ? "Not Linked" : item.PaymentTracking.Trim());
        command.Parameters.AddWithValue("$LinkedBillId", item.LinkedBillId);
        command.Parameters.AddWithValue("$LinkedBillName", item.LinkedBillName.Trim());
        command.Parameters.AddWithValue("$Status", string.IsNullOrWhiteSpace(item.Status) ? "Current" : item.Status.Trim());
        command.Parameters.AddWithValue("$Priority", string.IsNullOrWhiteSpace(item.Priority) ? "Normal" : item.Priority.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static Debt ReadDebt(SqliteDataReader reader)
    {
        return new Debt
        {
            Id = reader.GetInt64(0),
            DebtName = GetString(reader, 1),
            DebtType = GetString(reader, 2),
            CreditorCollector = GetString(reader, 3),
            CurrentBalance = GetDecimal(reader, 4),
            MinimumPayment = GetDecimal(reader, 5),
            PaymentFrequency = GetString(reader, 6),
            DueDate = GetString(reader, 7),
            ResponsibilityOwner = GetString(reader, 8),
            PaidBy = GetString(reader, 9),
            PaymentTracking = GetString(reader, 10),
            LinkedBillId = GetLong(reader, 11),
            LinkedBillName = GetString(reader, 12),
            Status = GetString(reader, 13),
            Priority = GetString(reader, 14),
            IsActive = GetBool(reader, 15),
            Notes = GetString(reader, 16),
            CreatedUtc = GetDateTime(reader, 17),
            UpdatedUtc = GetDateTime(reader, 18)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static bool GetBool(SqliteDataReader reader, int index) => !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    private static long GetLong(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
    private static decimal GetDecimal(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
}
