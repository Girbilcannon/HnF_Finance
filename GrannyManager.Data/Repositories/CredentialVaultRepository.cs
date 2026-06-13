using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class CredentialVaultRepository
{
    private readonly string _databasePath;

    public CredentialVaultRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureTable();
    }

    public IReadOnlyList<CredentialRecord> GetAll()
    {
        var items = new List<CredentialRecord>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, AccountName, WebsiteUrl, EncryptedUsername, EncryptedPassword, EncryptedRecoveryInfo,
       EncryptedSecurityNotes, LinkedRecordType, LinkedRecordId, LinkedRecordName, IsActive, CreatedUtc, UpdatedUtc
FROM CredentialRecords
ORDER BY IsActive DESC, AccountName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadCredential(reader));
        return items;
    }

    public long Upsert(CredentialRecord item)
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
INSERT INTO CredentialRecords
(AccountName, WebsiteUrl, EncryptedUsername, EncryptedPassword, EncryptedRecoveryInfo, EncryptedSecurityNotes,
 LinkedRecordType, LinkedRecordId, LinkedRecordName, IsActive, CreatedUtc, UpdatedUtc)
VALUES
($AccountName, $WebsiteUrl, $EncryptedUsername, $EncryptedPassword, $EncryptedRecoveryInfo, $EncryptedSecurityNotes,
 $LinkedRecordType, $LinkedRecordId, $LinkedRecordName, $IsActive, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE CredentialRecords
SET AccountName = $AccountName,
    WebsiteUrl = $WebsiteUrl,
    EncryptedUsername = $EncryptedUsername,
    EncryptedPassword = $EncryptedPassword,
    EncryptedRecoveryInfo = $EncryptedRecoveryInfo,
    EncryptedSecurityNotes = $EncryptedSecurityNotes,
    LinkedRecordType = $LinkedRecordType,
    LinkedRecordId = $LinkedRecordId,
    LinkedRecordName = $LinkedRecordName,
    IsActive = $IsActive,
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
        command.CommandText = "DELETE FROM CredentialRecords WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }


    private void EnsureTable()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS CredentialRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountName TEXT NOT NULL DEFAULT '',
    WebsiteUrl TEXT NOT NULL DEFAULT '',
    EncryptedUsername TEXT NOT NULL DEFAULT '',
    EncryptedPassword TEXT NOT NULL DEFAULT '',
    EncryptedRecoveryInfo TEXT NOT NULL DEFAULT '',
    EncryptedSecurityNotes TEXT NOT NULL DEFAULT '',
    LinkedRecordType TEXT NOT NULL DEFAULT '',
    LinkedRecordId INTEGER NOT NULL DEFAULT 0,
    LinkedRecordName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS IX_CredentialRecords_AccountName ON CredentialRecords(AccountName);
CREATE INDEX IF NOT EXISTS IX_CredentialRecords_LinkedRecord ON CredentialRecords(LinkedRecordType, LinkedRecordId);";
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, CredentialRecord item)
    {
        command.Parameters.AddWithValue("$AccountName", item.AccountName.Trim());
        command.Parameters.AddWithValue("$WebsiteUrl", item.WebsiteUrl.Trim());
        command.Parameters.AddWithValue("$EncryptedUsername", item.EncryptedUsername.Trim());
        command.Parameters.AddWithValue("$EncryptedPassword", item.EncryptedPassword.Trim());
        command.Parameters.AddWithValue("$EncryptedRecoveryInfo", item.EncryptedRecoveryInfo.Trim());
        command.Parameters.AddWithValue("$EncryptedSecurityNotes", item.EncryptedSecurityNotes.Trim());
        command.Parameters.AddWithValue("$LinkedRecordType", item.LinkedRecordType.Trim());
        command.Parameters.AddWithValue("$LinkedRecordId", item.LinkedRecordId);
        command.Parameters.AddWithValue("$LinkedRecordName", item.LinkedRecordName.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToString("O"));
    }

    private static CredentialRecord ReadCredential(SqliteDataReader reader)
    {
        return new CredentialRecord
        {
            Id = reader.GetInt64(0),
            AccountName = reader.GetString(1),
            WebsiteUrl = reader.GetString(2),
            EncryptedUsername = reader.GetString(3),
            EncryptedPassword = reader.GetString(4),
            EncryptedRecoveryInfo = reader.GetString(5),
            EncryptedSecurityNotes = reader.GetString(6),
            LinkedRecordType = reader.GetString(7),
            LinkedRecordId = reader.GetInt64(8),
            LinkedRecordName = reader.GetString(9),
            IsActive = reader.GetInt32(10) == 1,
            CreatedUtc = DateTime.TryParse(reader.GetString(11), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(12), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
