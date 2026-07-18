using System;
using System.Collections.Generic;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class DocumentsRepository
{
    private readonly string _databasePath;

    public DocumentsRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureTable();
    }

    public IReadOnlyList<DocumentRecord> GetAll()
    {
        var items = new List<DocumentRecord>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, DisplayName, OriginalFileName, StoredFileName, RelativePath, FullPath, PersonName, Category,
       LinkedSection, LinkedRecordId, LinkedRecordName, CustomFolder, Tags, Notes, IsMergedFile,
       PasswordProtectedRequested, ImportBatchId, IsActive, ImportedUtc, UpdatedUtc
FROM Documents
ORDER BY IsActive DESC, ImportedUtc DESC, DisplayName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadDocument(reader));

        return items;
    }

    public DocumentRecord? GetById(long id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, DisplayName, OriginalFileName, StoredFileName, RelativePath, FullPath, PersonName, Category,
       LinkedSection, LinkedRecordId, LinkedRecordName, CustomFolder, Tags, Notes, IsMergedFile,
       PasswordProtectedRequested, ImportBatchId, IsActive, ImportedUtc, UpdatedUtc
FROM Documents
WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadDocument(reader) : null;
    }

    public long Upsert(DocumentRecord item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        item.UpdatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        if (item.Id <= 0)
        {
            item.ImportedUtc = DateTime.UtcNow;
            command.CommandText = @"
INSERT INTO Documents
(DisplayName, OriginalFileName, StoredFileName, RelativePath, FullPath, PersonName, Category,
 LinkedSection, LinkedRecordId, LinkedRecordName, CustomFolder, Tags, Notes, IsMergedFile,
 PasswordProtectedRequested, ImportBatchId, IsActive, ImportedUtc, UpdatedUtc)
VALUES
($DisplayName, $OriginalFileName, $StoredFileName, $RelativePath, $FullPath, $PersonName, $Category,
 $LinkedSection, $LinkedRecordId, $LinkedRecordName, $CustomFolder, $Tags, $Notes, $IsMergedFile,
 $PasswordProtectedRequested, $ImportBatchId, $IsActive, $ImportedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Documents
SET DisplayName = $DisplayName,
    OriginalFileName = $OriginalFileName,
    StoredFileName = $StoredFileName,
    RelativePath = $RelativePath,
    FullPath = $FullPath,
    PersonName = $PersonName,
    Category = $Category,
    LinkedSection = $LinkedSection,
    LinkedRecordId = $LinkedRecordId,
    LinkedRecordName = $LinkedRecordName,
    CustomFolder = $CustomFolder,
    Tags = $Tags,
    Notes = $Notes,
    IsMergedFile = $IsMergedFile,
    PasswordProtectedRequested = $PasswordProtectedRequested,
    ImportBatchId = $ImportBatchId,
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
        command.CommandText = "DELETE FROM Documents WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS Documents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DisplayName TEXT NOT NULL DEFAULT '',
    OriginalFileName TEXT NOT NULL DEFAULT '',
    StoredFileName TEXT NOT NULL DEFAULT '',
    RelativePath TEXT NOT NULL DEFAULT '',
    FullPath TEXT NOT NULL DEFAULT '',
    PersonName TEXT NOT NULL DEFAULT 'General',
    Category TEXT NOT NULL DEFAULT 'Other',
    LinkedSection TEXT NOT NULL DEFAULT 'Other',
    LinkedRecordId INTEGER NOT NULL DEFAULT 0,
    LinkedRecordName TEXT NOT NULL DEFAULT '',
    CustomFolder TEXT NOT NULL DEFAULT '',
    Tags TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    IsMergedFile INTEGER NOT NULL DEFAULT 0,
    PasswordProtectedRequested INTEGER NOT NULL DEFAULT 0,
    ImportBatchId TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    ImportedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            command.ExecuteNonQuery();
        }

        EnsureColumn(connection, "Documents", "DisplayName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "OriginalFileName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "StoredFileName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "RelativePath", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "FullPath", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "PersonName", "TEXT NOT NULL DEFAULT 'General'");
        EnsureColumn(connection, "Documents", "Category", "TEXT NOT NULL DEFAULT 'Other'");
        EnsureColumn(connection, "Documents", "LinkedSection", "TEXT NOT NULL DEFAULT 'Other'");
        EnsureColumn(connection, "Documents", "LinkedRecordId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Documents", "LinkedRecordName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "CustomFolder", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "Tags", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "Notes", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "IsMergedFile", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Documents", "PasswordProtectedRequested", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Documents", "ImportBatchId", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Documents", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "Documents", "ImportedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "Documents", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_Documents_DisplayName ON Documents(DisplayName);
CREATE INDEX IF NOT EXISTS IX_Documents_PersonCategory ON Documents(PersonName, Category);
CREATE INDEX IF NOT EXISTS IX_Documents_Tags ON Documents(Tags);
CREATE INDEX IF NOT EXISTS IX_Documents_LinkedSection ON Documents(LinkedSection);";
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

    private static void AddParameters(SqliteCommand command, DocumentRecord item)
    {
        command.Parameters.AddWithValue("$DisplayName", item.DisplayName.Trim());
        command.Parameters.AddWithValue("$OriginalFileName", item.OriginalFileName.Trim());
        command.Parameters.AddWithValue("$StoredFileName", item.StoredFileName.Trim());
        command.Parameters.AddWithValue("$RelativePath", item.RelativePath.Trim());
        command.Parameters.AddWithValue("$FullPath", item.FullPath.Trim());
        command.Parameters.AddWithValue("$PersonName", string.IsNullOrWhiteSpace(item.PersonName) ? "General" : item.PersonName.Trim());
        command.Parameters.AddWithValue("$Category", string.IsNullOrWhiteSpace(item.Category) ? "Other" : item.Category.Trim());
        command.Parameters.AddWithValue("$LinkedSection", string.IsNullOrWhiteSpace(item.LinkedSection) ? "Other" : item.LinkedSection.Trim());
        command.Parameters.AddWithValue("$LinkedRecordId", item.LinkedRecordId);
        command.Parameters.AddWithValue("$LinkedRecordName", item.LinkedRecordName.Trim());
        command.Parameters.AddWithValue("$CustomFolder", item.CustomFolder.Trim());
        command.Parameters.AddWithValue("$Tags", item.Tags.Trim());
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$IsMergedFile", item.IsMergedFile ? 1 : 0);
        command.Parameters.AddWithValue("$PasswordProtectedRequested", item.PasswordProtectedRequested ? 1 : 0);
        command.Parameters.AddWithValue("$ImportBatchId", item.ImportBatchId.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$ImportedUtc", item.ImportedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static DocumentRecord ReadDocument(SqliteDataReader reader)
    {
        return new DocumentRecord
        {
            Id = reader.GetInt64(0),
            DisplayName = GetString(reader, 1),
            OriginalFileName = GetString(reader, 2),
            StoredFileName = GetString(reader, 3),
            RelativePath = GetString(reader, 4),
            FullPath = GetString(reader, 5),
            PersonName = GetString(reader, 6),
            Category = GetString(reader, 7),
            LinkedSection = GetString(reader, 8),
            LinkedRecordId = GetLong(reader, 9),
            LinkedRecordName = GetString(reader, 10),
            CustomFolder = GetString(reader, 11),
            Tags = GetString(reader, 12),
            Notes = GetString(reader, 13),
            IsMergedFile = GetBool(reader, 14),
            PasswordProtectedRequested = GetBool(reader, 15),
            ImportBatchId = GetString(reader, 16),
            IsActive = GetBool(reader, 17),
            ImportedUtc = GetDateTime(reader, 18),
            UpdatedUtc = GetDateTime(reader, 19)
        };
    }

    private static string GetString(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static bool GetBool(SqliteDataReader reader, int index) => !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    private static long GetLong(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
    private static DateTime GetDateTime(SqliteDataReader reader, int index) => DateTime.TryParse(GetString(reader, index), out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
}
