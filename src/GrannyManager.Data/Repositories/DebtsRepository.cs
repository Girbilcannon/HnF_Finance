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
        EnsureDebtsTable();
    }

    public IReadOnlyList<Debt> GetAll()
    {
        var items = new List<Debt>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, DebtName, DebtType, CreditorCollector, CurrentBalance, MinimumPayment, PaymentFrequency, DueDate,
       ResponsibilityOwner, PaidBy, PaymentTracking, LinkedBillId, LinkedBillName, Status, Priority, IsActive,
       Notes, CreatedUtc, UpdatedUtc
FROM Debts
ORDER BY IsActive DESC, Priority COLLATE NOCASE, DebtName COLLATE NOCASE;";

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
 ResponsibilityOwner, PaidBy, PaymentTracking, LinkedBillId, LinkedBillName, Status, Priority, IsActive,
 Notes, CreatedUtc, UpdatedUtc)
VALUES
($DebtName, $DebtType, $CreditorCollector, $CurrentBalance, $MinimumPayment, $PaymentFrequency, $DueDate,
 $ResponsibilityOwner, $PaidBy, $PaymentTracking, $LinkedBillId, $LinkedBillName, $Status, $Priority, $IsActive,
 $Notes, $CreatedUtc, $UpdatedUtc);
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

    private void EnsureDebtsTable()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Debts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DebtName TEXT NOT NULL DEFAULT '',
    DebtType TEXT NOT NULL DEFAULT '',
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
);

CREATE INDEX IF NOT EXISTS IX_Debts_DebtName ON Debts(DebtName);
CREATE INDEX IF NOT EXISTS IX_Debts_IsActive ON Debts(IsActive);";
        command.ExecuteNonQuery();
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
        command.Parameters.AddWithValue("$DebtType", item.DebtType.Trim());
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
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToString("O"));
    }

    private static Debt ReadDebt(SqliteDataReader reader)
    {
        return new Debt
        {
            Id = reader.GetInt64(0),
            DebtName = reader.GetString(1),
            DebtType = reader.GetString(2),
            CreditorCollector = reader.GetString(3),
            CurrentBalance = Convert.ToDecimal(reader.GetDouble(4)),
            MinimumPayment = Convert.ToDecimal(reader.GetDouble(5)),
            PaymentFrequency = reader.GetString(6),
            DueDate = reader.GetString(7),
            ResponsibilityOwner = reader.GetString(8),
            PaidBy = reader.GetString(9),
            PaymentTracking = reader.GetString(10),
            LinkedBillId = reader.GetInt64(11),
            LinkedBillName = reader.GetString(12),
            Status = reader.GetString(13),
            Priority = reader.GetString(14),
            IsActive = reader.GetInt32(15) == 1,
            Notes = reader.GetString(16),
            CreatedUtc = DateTime.TryParse(reader.GetString(17), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(18), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
