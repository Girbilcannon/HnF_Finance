using GrannyManager.Core.Models;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class AllowanceSavingsRepository
{
    private readonly string _databasePath;

    public AllowanceSavingsRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public List<AllowanceSavingsItem> GetAll()
    {
        var items = new List<AllowanceSavingsItem>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ItemName, ItemType, Amount, Frequency, WhereStored, StorageMethod, LinkedBankAssetId, LinkedBankAssetName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM AllowanceSavingsItems
ORDER BY IsActive DESC, ItemType ASC, ItemName ASC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(ReadItem(reader));
        }

        return items;
    }

    public AllowanceSavingsItem? GetById(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ItemName, ItemType, Amount, Frequency, WhereStored, StorageMethod, LinkedBankAssetId, LinkedBankAssetName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM AllowanceSavingsItems
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadItem(reader) : null;
    }

    public int Save(AllowanceSavingsItem item)
    {
        if (item.Id <= 0)
            return Insert(item);

        Update(item);
        return item.Id;
    }

    public void Delete(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM AllowanceSavingsItems WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private int Insert(AllowanceSavingsItem item)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO AllowanceSavingsItems
(ItemName, ItemType, Amount, Frequency, WhereStored, StorageMethod, LinkedBankAssetId, LinkedBankAssetName, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($itemName, $itemType, $amount, $frequency, $whereStored, $storageMethod, $linkedBankAssetId, $linkedBankAssetName, $isActive, $notes, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
SELECT last_insert_rowid();";
        AddParameters(command, item);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void Update(AllowanceSavingsItem item)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE AllowanceSavingsItems
SET ItemName = $itemName,
    ItemType = $itemType,
    Amount = $amount,
    Frequency = $frequency,
    WhereStored = $whereStored,
    StorageMethod = $storageMethod,
    LinkedBankAssetId = $linkedBankAssetId,
    LinkedBankAssetName = $linkedBankAssetName,
    IsActive = $isActive,
    Notes = $notes,
    UpdatedUtc = CURRENT_TIMESTAMP
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", item.Id);
        AddParameters(command, item);
        command.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand command, AllowanceSavingsItem item)
    {
        command.Parameters.AddWithValue("$itemName", item.ItemName ?? string.Empty);
        command.Parameters.AddWithValue("$itemType", item.ItemType ?? "Allowance");
        command.Parameters.AddWithValue("$amount", item.Amount);
        command.Parameters.AddWithValue("$frequency", item.Frequency ?? "Monthly");
        command.Parameters.AddWithValue("$whereStored", item.WhereStored ?? string.Empty);
        command.Parameters.AddWithValue("$storageMethod", item.StorageMethod ?? "Cash / Envelope");
        command.Parameters.AddWithValue("$linkedBankAssetId", item.LinkedBankAssetId);
        command.Parameters.AddWithValue("$linkedBankAssetName", item.LinkedBankAssetName ?? string.Empty);
        command.Parameters.AddWithValue("$isActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$notes", item.Notes ?? string.Empty);
    }

    private static AllowanceSavingsItem ReadItem(SqliteDataReader reader)
    {
        return new AllowanceSavingsItem
        {
            Id = reader.GetInt32(0),
            ItemName = reader.GetString(1),
            ItemType = reader.GetString(2),
            Amount = Convert.ToDecimal(reader.GetDouble(3)),
            Frequency = reader.GetString(4),
            WhereStored = reader.GetString(5),
            StorageMethod = reader.GetString(6),
            LinkedBankAssetId = reader.GetInt64(7),
            LinkedBankAssetName = reader.GetString(8),
            IsActive = reader.GetInt32(9) == 1,
            Notes = reader.GetString(10),
            CreatedUtc = DateTime.TryParse(reader.GetString(11), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(12), out var updated) ? updated : DateTime.UtcNow
        };
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }
}
