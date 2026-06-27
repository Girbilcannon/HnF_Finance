using System;
using System.Collections.Generic;
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
        EnsureTable();
    }

    public IReadOnlyList<BillReceipt> GetByType(string receiptType)
    {
        var items = new List<BillReceipt>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ReceiptType, ReceiptDate, Amount, CreatedUtc
FROM BillReceipts
WHERE ReceiptType = $ReceiptType
ORDER BY ReceiptDate DESC, Id DESC;";
        command.Parameters.AddWithValue("$ReceiptType", receiptType.Trim());

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadReceipt(reader));

        return items;
    }

    public long Add(BillReceipt receipt)
    {
        if (receipt is null)
            throw new ArgumentNullException(nameof(receipt));

        if (receipt.CreatedUtc == default)
            receipt.CreatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO BillReceipts (ReceiptType, ReceiptDate, Amount, CreatedUtc)
VALUES ($ReceiptType, $ReceiptDate, $Amount, $CreatedUtc);
SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("$ReceiptType", receipt.ReceiptType.Trim());
        command.Parameters.AddWithValue("$ReceiptDate", receipt.ReceiptDate.Date.ToString("O"));
        command.Parameters.AddWithValue("$Amount", receipt.Amount);
        command.Parameters.AddWithValue("$CreatedUtc", receipt.CreatedUtc.ToUniversalTime().ToString("O"));

        var result = command.ExecuteScalar();
        receipt.Id = Convert.ToInt64(result);
        return receipt.Id;
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

    private void EnsureTable()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS BillReceipts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReceiptType TEXT NOT NULL DEFAULT '',
    ReceiptDate TEXT NOT NULL DEFAULT '',
    Amount REAL NOT NULL DEFAULT 0,
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
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

    private static BillReceipt ReadReceipt(SqliteDataReader reader)
    {
        return new BillReceipt
        {
            Id = reader.GetInt64(0),
            ReceiptType = GetString(reader, 1),
            ReceiptDate = GetDateTime(reader, 2),
            Amount = GetDecimal(reader, 3),
            CreatedUtc = GetDateTime(reader, 4)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static decimal GetDecimal(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed : DateTime.UtcNow;
}
