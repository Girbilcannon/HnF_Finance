using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class IncomeSourcesRepository
{
    private readonly string _databasePath;

    public IncomeSourcesRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
    }

    public IReadOnlyList<IncomeSource> GetAll()
    {
        var items = new List<IncomeSource>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, SourceName, IncomeType, Amount, TaxesWithheld, Frequency, ExpectedDayOrDate, DepositedToAccount,
       DepositMethod, LinkedBankAssetId, LinkedBankAssetName,
       IsActive, Notes, CreatedUtc, UpdatedUtc
FROM IncomeSources
ORDER BY IsActive DESC, SourceName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(ReadIncomeSource(reader));

        return items;
    }

    public long Upsert(IncomeSource source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        source.UpdatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        if (source.Id <= 0)
        {
            source.CreatedUtc = DateTime.UtcNow;
            command.CommandText = @"
INSERT INTO IncomeSources
(SourceName, IncomeType, Amount, TaxesWithheld, Frequency, ExpectedDayOrDate, DepositedToAccount,
 DepositMethod, LinkedBankAssetId, LinkedBankAssetName,
 IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($SourceName, $IncomeType, $Amount, $TaxesWithheld, $Frequency, $ExpectedDayOrDate, $DepositedToAccount,
 $DepositMethod, $LinkedBankAssetId, $LinkedBankAssetName,
 $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE IncomeSources
SET SourceName = $SourceName,
    IncomeType = $IncomeType,
    Amount = $Amount,
    TaxesWithheld = $TaxesWithheld,
    Frequency = $Frequency,
    ExpectedDayOrDate = $ExpectedDayOrDate,
    DepositedToAccount = $DepositedToAccount,
    DepositMethod = $DepositMethod,
    LinkedBankAssetId = $LinkedBankAssetId,
    LinkedBankAssetName = $LinkedBankAssetName,
    IsActive = $IsActive,
    Notes = $Notes,
    UpdatedUtc = $UpdatedUtc
WHERE Id = $Id;
SELECT $Id;";
            command.Parameters.AddWithValue("$Id", source.Id);
        }

        AddParameters(command, source);
        var result = command.ExecuteScalar();
        source.Id = Convert.ToInt64(result);
        return source.Id;
    }

    public void Delete(long id)
    {
        if (id <= 0)
            return;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM IncomeSources WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, IncomeSource source)
    {
        command.Parameters.AddWithValue("$SourceName", source.SourceName.Trim());
        command.Parameters.AddWithValue("$IncomeType", source.IncomeType.Trim());
        command.Parameters.AddWithValue("$Amount", source.Amount);
        command.Parameters.AddWithValue("$TaxesWithheld", source.TaxesWithheld ? 1 : 0);
        command.Parameters.AddWithValue("$Frequency", source.Frequency.Trim());
        command.Parameters.AddWithValue("$ExpectedDayOrDate", source.ExpectedDayOrDate.Trim());
        command.Parameters.AddWithValue("$DepositedToAccount", source.DepositedToAccount.Trim());
        command.Parameters.AddWithValue("$DepositMethod", string.IsNullOrWhiteSpace(source.DepositMethod) ? "Cash" : source.DepositMethod.Trim());
        command.Parameters.AddWithValue("$LinkedBankAssetId", source.LinkedBankAssetId);
        command.Parameters.AddWithValue("$LinkedBankAssetName", source.LinkedBankAssetName.Trim());
        command.Parameters.AddWithValue("$IsActive", source.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", source.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", source.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", source.UpdatedUtc.ToString("O"));
    }

    private static IncomeSource ReadIncomeSource(SqliteDataReader reader)
    {
        return new IncomeSource
        {
            Id = reader.GetInt64(0),
            SourceName = reader.GetString(1),
            IncomeType = reader.GetString(2),
            Amount = Convert.ToDecimal(reader.GetDouble(3)),
            TaxesWithheld = reader.GetInt32(4) == 1,
            Frequency = reader.GetString(5),
            ExpectedDayOrDate = reader.GetString(6),
            DepositedToAccount = reader.GetString(7),
            DepositMethod = reader.GetString(8),
            LinkedBankAssetId = reader.GetInt64(9),
            LinkedBankAssetName = reader.GetString(10),
            IsActive = reader.GetInt32(11) == 1,
            Notes = reader.GetString(12),
            CreatedUtc = DateTime.TryParse(reader.GetString(13), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(14), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
