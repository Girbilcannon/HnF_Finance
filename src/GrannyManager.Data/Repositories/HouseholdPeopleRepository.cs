using System;
using System.Collections.Generic;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using Microsoft.Data.Sqlite;

namespace GrannyManager.Data.Repositories;

public sealed class HouseholdPeopleRepository
{
    private readonly string _databasePath;

    public HouseholdPeopleRepository(string databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        DatabaseInitializer.EnsureCreated(_databasePath);
        EnsureHouseholdPeopleTable();
    }

    public IReadOnlyList<HouseholdPerson> GetAll()
    {
        var people = new List<HouseholdPerson>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FullName, Relationship, Role, LivesInHousehold, PaysRent, UsesHouseholdVehicle, ReceivesRides,
       MonthlyContribution, ContributionHandling, LinkedIncomeSourceId, LinkedIncomeSourceName, IsActive, Notes, CreatedUtc, UpdatedUtc
FROM HouseholdPeople
ORDER BY IsActive DESC, LivesInHousehold DESC, FullName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            people.Add(ReadPerson(reader));

        return people;
    }

    public long Upsert(HouseholdPerson person)
    {
        if (person is null)
            throw new ArgumentNullException(nameof(person));

        person.UpdatedUtc = DateTime.UtcNow;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        if (person.Id <= 0)
        {
            person.CreatedUtc = DateTime.UtcNow;
            command.CommandText = @"
INSERT INTO HouseholdPeople
(FullName, Relationship, Role, LivesInHousehold, PaysRent, UsesHouseholdVehicle, ReceivesRides,
 MonthlyContribution, ContributionHandling, LinkedIncomeSourceId, LinkedIncomeSourceName, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($FullName, $Relationship, $Role, $LivesInHousehold, $PaysRent, $UsesHouseholdVehicle, $ReceivesRides,
 $MonthlyContribution, $ContributionHandling, $LinkedIncomeSourceId, $LinkedIncomeSourceName, $IsActive, $Notes, $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE HouseholdPeople
SET FullName = $FullName,
    Relationship = $Relationship,
    Role = $Role,
    LivesInHousehold = $LivesInHousehold,
    PaysRent = $PaysRent,
    UsesHouseholdVehicle = $UsesHouseholdVehicle,
    ReceivesRides = $ReceivesRides,
    MonthlyContribution = $MonthlyContribution,
    ContributionHandling = $ContributionHandling,
    LinkedIncomeSourceId = $LinkedIncomeSourceId,
    LinkedIncomeSourceName = $LinkedIncomeSourceName,
    IsActive = $IsActive,
    Notes = $Notes,
    UpdatedUtc = $UpdatedUtc
WHERE Id = $Id;
SELECT $Id;";
            command.Parameters.AddWithValue("$Id", person.Id);
        }

        AddParameters(command, person);
        var result = command.ExecuteScalar();
        person.Id = Convert.ToInt64(result);
        return person.Id;
    }

    public void Delete(long id)
    {
        if (id <= 0)
            return;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM HouseholdPeople WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureHouseholdPeopleTable()
    {
        using var connection = OpenConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS HouseholdPeople (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL DEFAULT '',
    Relationship TEXT NOT NULL DEFAULT '',
    Role TEXT NOT NULL DEFAULT '',
    LivesInHousehold INTEGER NOT NULL DEFAULT 1,
    PaysRent INTEGER NOT NULL DEFAULT 0,
    UsesHouseholdVehicle INTEGER NOT NULL DEFAULT 0,
    ReceivesRides INTEGER NOT NULL DEFAULT 0,
    MonthlyContribution REAL NOT NULL DEFAULT 0,
    ContributionHandling TEXT NOT NULL DEFAULT 'No Contribution',
    LinkedIncomeSourceId INTEGER NOT NULL DEFAULT 0,
    LinkedIncomeSourceName TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            command.ExecuteNonQuery();
        }

        EnsureColumn(connection, "HouseholdPeople", "FullName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "HouseholdPeople", "Relationship", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "HouseholdPeople", "Role", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "HouseholdPeople", "LivesInHousehold", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "HouseholdPeople", "PaysRent", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "HouseholdPeople", "UsesHouseholdVehicle", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "HouseholdPeople", "ReceivesRides", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "HouseholdPeople", "MonthlyContribution", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "HouseholdPeople", "ContributionHandling", "TEXT NOT NULL DEFAULT 'No Contribution'");
        EnsureColumn(connection, "HouseholdPeople", "LinkedIncomeSourceId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "HouseholdPeople", "LinkedIncomeSourceName", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "HouseholdPeople", "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "HouseholdPeople", "Notes", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "HouseholdPeople", "CreatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "HouseholdPeople", "UpdatedUtc", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_HouseholdPeople_FullName ON HouseholdPeople(FullName);
CREATE INDEX IF NOT EXISTS IX_HouseholdPeople_IsActive ON HouseholdPeople(IsActive);";
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

    private static void AddParameters(SqliteCommand command, HouseholdPerson person)
    {
        command.Parameters.AddWithValue("$FullName", person.FullName.Trim());
        command.Parameters.AddWithValue("$Relationship", person.Relationship.Trim());
        command.Parameters.AddWithValue("$Role", person.Role.Trim());
        command.Parameters.AddWithValue("$LivesInHousehold", person.LivesInHousehold ? 1 : 0);
        command.Parameters.AddWithValue("$PaysRent", person.PaysRent ? 1 : 0);
        command.Parameters.AddWithValue("$UsesHouseholdVehicle", person.UsesHouseholdVehicle ? 1 : 0);
        command.Parameters.AddWithValue("$ReceivesRides", person.ReceivesRides ? 1 : 0);
        command.Parameters.AddWithValue("$MonthlyContribution", person.MonthlyContribution);
        command.Parameters.AddWithValue("$ContributionHandling", string.IsNullOrWhiteSpace(person.ContributionHandling) ? "No Contribution" : person.ContributionHandling.Trim());
        command.Parameters.AddWithValue("$LinkedIncomeSourceId", person.LinkedIncomeSourceId);
        command.Parameters.AddWithValue("$LinkedIncomeSourceName", person.LinkedIncomeSourceName.Trim());
        command.Parameters.AddWithValue("$IsActive", person.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", person.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", person.CreatedUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", person.UpdatedUtc.ToUniversalTime().ToString("O"));
    }

    private static HouseholdPerson ReadPerson(SqliteDataReader reader)
    {
        return new HouseholdPerson
        {
            Id = reader.GetInt64(0),
            FullName = GetString(reader, 1),
            Relationship = GetString(reader, 2),
            Role = GetString(reader, 3),
            LivesInHousehold = GetBool(reader, 4),
            PaysRent = GetBool(reader, 5),
            UsesHouseholdVehicle = GetBool(reader, 6),
            ReceivesRides = GetBool(reader, 7),
            MonthlyContribution = GetDecimal(reader, 8),
            ContributionHandling = GetString(reader, 9),
            LinkedIncomeSourceId = GetLong(reader, 10),
            LinkedIncomeSourceName = GetString(reader, 11),
            IsActive = GetBool(reader, 12),
            Notes = GetString(reader, 13),
            CreatedUtc = GetDateTime(reader, 14),
            UpdatedUtc = GetDateTime(reader, 15)
        };
    }

    private static string GetString(SqliteDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    }

    private static bool GetBool(SqliteDataReader reader, int index)
    {
        return !reader.IsDBNull(index) && reader.GetInt64(index) != 0;
    }

    private static long GetLong(SqliteDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
    }

    private static decimal GetDecimal(SqliteDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetDouble(index));
    }

    private static DateTime GetDateTime(SqliteDataReader reader, int index)
    {
        var value = GetString(reader, index);
        return DateTime.TryParse(value, out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;
    }
}
