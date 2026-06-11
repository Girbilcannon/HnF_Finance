using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class BillReceiptsRepository
{
    private readonly string _databasePath;

    public BillReceiptsRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureReceiptTable();
    }

    public IReadOnlyList<BillReceipt> GetByType(string receiptType)
    {
        var items = new List<BillReceipt>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ReceiptType, ReceiptDate, Amount, CreatedUtc, UpdatedUtc
FROM BillReceipts
WHERE ReceiptType = $ReceiptType
ORDER BY ReceiptDate DESC, Id DESC;";
        command.Parameters.AddWithValue("$ReceiptType", receiptType.Trim());

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadReceipt(reader));

        return items;
    }

    public long Add(BillReceipt item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        item.CreatedUtc = DateTime.UtcNow;
        item.UpdatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO BillReceipts
(ReceiptType, ReceiptDate, Amount, CreatedUtc, UpdatedUtc)
VALUES
($ReceiptType, $ReceiptDate, $Amount, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
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
        command.CommandText = "DELETE FROM BillReceipts WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureReceiptTable()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS BillReceipts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReceiptType TEXT NOT NULL DEFAULT '',
    ReceiptDate TEXT NOT NULL DEFAULT '',
    Amount REAL NOT NULL DEFAULT 0,
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_BillReceipts_TypeDate ON BillReceipts(ReceiptType, ReceiptDate);";
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, BillReceipt item)
    {
        command.Parameters.AddWithValue("$ReceiptType", item.ReceiptType.Trim());
        command.Parameters.AddWithValue("$ReceiptDate", item.ReceiptDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$Amount", item.Amount);
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToString("O"));
    }

    private static BillReceipt ReadReceipt(SqliteDataReader reader)
    {
        return new BillReceipt
        {
            Id = reader.GetInt64(0),
            ReceiptType = reader.GetString(1),
            ReceiptDate = DateTime.TryParse(reader.GetString(2), out var date) ? date : DateTime.Today,
            Amount = Convert.ToDecimal(reader.GetDouble(3)),
            CreatedUtc = DateTime.TryParse(reader.GetString(4), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(5), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
