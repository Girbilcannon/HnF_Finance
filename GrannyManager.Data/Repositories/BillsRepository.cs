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
    }

    public IReadOnlyList<Bill> GetAll()
    {
        var items = new List<Bill>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, BillName, Category, Amount, Frequency, DueDate, IsAutopay, PaidBy,
       ResponsibilityOwner, PastDueAmount, Priority, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM BillsSpending
ORDER BY IsActive DESC, Priority COLLATE NOCASE, BillName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadBill(reader));

        return items;
    }

    public long Upsert(Bill bill)
    {
        if (bill is null)
            throw new ArgumentNullException(nameof(bill));

        bill.UpdatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        if (bill.Id <= 0)
        {
            bill.CreatedUtc = DateTime.UtcNow;
            command.CommandText = @"
INSERT INTO BillsSpending
(BillName, Category, Amount, Frequency, DueDate, IsAutopay, PaidBy, ResponsibilityOwner,
 PastDueAmount, Priority, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($BillName, $Category, $Amount, $Frequency, $DueDate, $IsAutopay, $PaidBy, $ResponsibilityOwner,
 $PastDueAmount, $Priority, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE BillsSpending
SET BillName = $BillName,
    Category = $Category,
    Amount = $Amount,
    Frequency = $Frequency,
    DueDate = $DueDate,
    IsAutopay = $IsAutopay,
    PaidBy = $PaidBy,
    ResponsibilityOwner = $ResponsibilityOwner,
    PastDueAmount = $PastDueAmount,
    Priority = $Priority,
    IsActive = $IsActive,
    Notes = $Notes,
    UpdatedUtc = $UpdatedUtc
WHERE Id = $Id;
SELECT $Id;";
            command.Parameters.AddWithValue("$Id", bill.Id);
        }

        AddParameters(command, bill);
        var result = command.ExecuteScalar();
        bill.Id = Convert.ToInt64(result);
        return bill.Id;
    }

    public void Delete(long id)
    {
        if (id <= 0)
            return;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM BillsSpending WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, Bill bill)
    {
        command.Parameters.AddWithValue("$BillName", bill.BillName.Trim());
        command.Parameters.AddWithValue("$Category", bill.Category.Trim());
        command.Parameters.AddWithValue("$Amount", bill.Amount);
        command.Parameters.AddWithValue("$Frequency", bill.Frequency.Trim());
        command.Parameters.AddWithValue("$DueDate", bill.DueDate.Trim());
        command.Parameters.AddWithValue("$IsAutopay", bill.IsAutopay ? 1 : 0);
        command.Parameters.AddWithValue("$PaidBy", bill.PaidBy.Trim());
        command.Parameters.AddWithValue("$ResponsibilityOwner", bill.ResponsibilityOwner.Trim());
        command.Parameters.AddWithValue("$PastDueAmount", bill.PastDueAmount);
        command.Parameters.AddWithValue("$Priority", bill.Priority.Trim());
        command.Parameters.AddWithValue("$IsActive", bill.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", bill.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", bill.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", bill.UpdatedUtc.ToString("O"));
    }

    private static Bill ReadBill(SqliteDataReader reader)
    {
        return new Bill
        {
            Id = reader.GetInt64(0),
            BillName = reader.GetString(1),
            Category = reader.GetString(2),
            Amount = Convert.ToDecimal(reader.GetDouble(3)),
            Frequency = reader.GetString(4),
            DueDate = reader.GetString(5),
            IsAutopay = reader.GetInt32(6) == 1,
            PaidBy = reader.GetString(7),
            ResponsibilityOwner = reader.GetString(8),
            PastDueAmount = Convert.ToDecimal(reader.GetDouble(9)),
            Priority = reader.GetString(10),
            IsActive = reader.GetInt32(11) == 1,
            Notes = reader.GetString(12),
            CreatedUtc = DateTime.TryParse(reader.GetString(13), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(14), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
