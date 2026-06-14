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
        EnsureDocumentRecordsTable();
    }

    public IReadOnlyList<DocumentRecord> GetAll()
    {
        var items = new List<DocumentRecord>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, Title, Category, Tags, OriginalFileName, StoredFilePath, SourceFilePath,
       LinkedRecordType, LinkedRecordId, LinkedRecordName, IsActive, IsImportant, Notes, CreatedUtc, UpdatedUtc
FROM DocumentRecords
ORDER BY IsActive DESC, IsImportant DESC, Category COLLATE NOCASE, Title COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadDocument(reader));
        return items;
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
            item.CreatedUtc = DateTime.UtcNow;
            command.CommandText = @"
INSERT INTO DocumentRecords
(Title, Category, Tags, OriginalFileName, StoredFilePath, SourceFilePath,
 LinkedRecordType, LinkedRecordId, LinkedRecordName, IsActive, IsImportant, Notes, CreatedUtc, UpdatedUtc)
VALUES
($Title, $Category, $Tags, $OriginalFileName, $StoredFilePath, $SourceFilePath,
 $LinkedRecordType, $LinkedRecordId, $LinkedRecordName, $IsActive, $IsImportant, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE DocumentRecords
SET Title = $Title,
    Category = $Category,
    Tags = $Tags,
    OriginalFileName = $OriginalFileName,
    StoredFilePath = $StoredFilePath,
    SourceFilePath = $SourceFilePath,
    LinkedRecordType = $LinkedRecordType,
    LinkedRecordId = $LinkedRecordId,
    LinkedRecordName = $LinkedRecordName,
    IsActive = $IsActive,
    IsImportant = $IsImportant,
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
        command.CommandText = "DELETE FROM DocumentRecords WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureDocumentRecordsTable()
    {
        using var connection = OpenConnection();
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
CREATE TABLE IF NOT EXISTS DocumentRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL DEFAULT '',
    Category TEXT NOT NULL DEFAULT '',
    Tags TEXT NOT NULL DEFAULT '',
    OriginalFileName TEXT NOT NULL DEFAULT '',
    StoredFilePath TEXT NOT NULL DEFAULT '',
    SourceFilePath TEXT NOT NULL DEFAULT '',
    LinkedRecordType TEXT NOT NULL DEFAULT 'None',
    LinkedRecordId INTEGER NOT NULL DEFAULT 0,
    LinkedRecordName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsImportant INTEGER NOT NULL DEFAULT 0,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
        createCommand.ExecuteNonQuery();

        EnsureColumn(connection, "DocumentRecords", "IsImportant", "INTEGER NOT NULL DEFAULT 0");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_DocumentRecords_Title ON DocumentRecords(Title);
CREATE INDEX IF NOT EXISTS IX_DocumentRecords_Category ON DocumentRecords(Category);
CREATE INDEX IF NOT EXISTS IX_DocumentRecords_IsImportant ON DocumentRecords(IsImportant);";
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
        command.Parameters.AddWithValue("$Title", item.Title.Trim());
        command.Parameters.AddWithValue("$Category", item.Category.Trim());
        command.Parameters.AddWithValue("$Tags", item.Tags.Trim());
        command.Parameters.AddWithValue("$OriginalFileName", item.OriginalFileName.Trim());
        command.Parameters.AddWithValue("$StoredFilePath", item.StoredFilePath.Trim());
        command.Parameters.AddWithValue("$SourceFilePath", item.SourceFilePath.Trim());
        command.Parameters.AddWithValue("$LinkedRecordType", item.LinkedRecordType.Trim());
        command.Parameters.AddWithValue("$LinkedRecordId", item.LinkedRecordId);
        command.Parameters.AddWithValue("$LinkedRecordName", item.LinkedRecordName.Trim());
        command.Parameters.AddWithValue("$IsActive", item.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$IsImportant", item.IsImportant ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", item.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", item.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", item.UpdatedUtc.ToString("O"));
    }

    private static DocumentRecord ReadDocument(SqliteDataReader reader)
    {
        return new DocumentRecord
        {
            Id = reader.GetInt64(0),
            Title = reader.GetString(1),
            Category = reader.GetString(2),
            Tags = reader.GetString(3),
            OriginalFileName = reader.GetString(4),
            StoredFilePath = reader.GetString(5),
            SourceFilePath = reader.GetString(6),
            LinkedRecordType = reader.GetString(7),
            LinkedRecordId = reader.GetInt64(8),
            LinkedRecordName = reader.GetString(9),
            IsActive = reader.GetInt32(10) == 1,
            IsImportant = reader.GetInt32(11) == 1,
            Notes = reader.GetString(12),
            CreatedUtc = DateTime.TryParse(reader.GetString(13), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(14), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
