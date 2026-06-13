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
    }

    public IReadOnlyList<HouseholdPerson> GetAll()
    {
        var people = new List<HouseholdPerson>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FullName, Relationship, Role, LivesInHousehold, PaysRent, MonthlyContribution,
       ContributionHandling, LinkedIncomeSourceId, LinkedIncomeSourceName,
       UsesHouseholdVehicle, ReceivesRides, Notes, CreatedUtc, UpdatedUtc
FROM HouseholdPeople
ORDER BY FullName COLLATE NOCASE;";

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
(FullName, Relationship, Role, LivesInHousehold, PaysRent, MonthlyContribution,
 ContributionHandling, LinkedIncomeSourceId, LinkedIncomeSourceName,
 UsesHouseholdVehicle, ReceivesRides, Notes, CreatedUtc, UpdatedUtc)
VALUES
($FullName, $Relationship, $Role, $LivesInHousehold, $PaysRent, $MonthlyContribution,
 $ContributionHandling, $LinkedIncomeSourceId, $LinkedIncomeSourceName,
 $UsesHouseholdVehicle, $ReceivesRides, $Notes, $CreatedUtc, $UpdatedUtc);
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
    MonthlyContribution = $MonthlyContribution,
    ContributionHandling = $ContributionHandling,
    LinkedIncomeSourceId = $LinkedIncomeSourceId,
    LinkedIncomeSourceName = $LinkedIncomeSourceName,
    UsesHouseholdVehicle = $UsesHouseholdVehicle,
    ReceivesRides = $ReceivesRides,
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
        command.Parameters.AddWithValue("$MonthlyContribution", person.MonthlyContribution);
        command.Parameters.AddWithValue("$ContributionHandling", person.ContributionHandling.Trim());
        command.Parameters.AddWithValue("$LinkedIncomeSourceId", person.LinkedIncomeSourceId);
        command.Parameters.AddWithValue("$LinkedIncomeSourceName", person.LinkedIncomeSourceName.Trim());
        command.Parameters.AddWithValue("$UsesHouseholdVehicle", person.UsesHouseholdVehicle ? 1 : 0);
        command.Parameters.AddWithValue("$ReceivesRides", person.ReceivesRides ? 1 : 0);
        command.Parameters.AddWithValue("$Notes", person.Notes.Trim());
        command.Parameters.AddWithValue("$CreatedUtc", person.CreatedUtc.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedUtc", person.UpdatedUtc.ToString("O"));
    }

    private static HouseholdPerson ReadPerson(SqliteDataReader reader)
    {
        return new HouseholdPerson
        {
            Id = reader.GetInt64(0),
            FullName = reader.GetString(1),
            Relationship = reader.GetString(2),
            Role = reader.GetString(3),
            LivesInHousehold = reader.GetInt32(4) == 1,
            PaysRent = reader.GetInt32(5) == 1,
            MonthlyContribution = Convert.ToDecimal(reader.GetDouble(6)),
            ContributionHandling = reader.GetString(7),
            LinkedIncomeSourceId = reader.GetInt64(8),
            LinkedIncomeSourceName = reader.GetString(9),
            UsesHouseholdVehicle = reader.GetInt32(10) == 1,
            ReceivesRides = reader.GetInt32(11) == 1,
            Notes = reader.GetString(12),
            CreatedUtc = DateTime.TryParse(reader.GetString(13), out var created) ? created : DateTime.UtcNow,
            UpdatedUtc = DateTime.TryParse(reader.GetString(14), out var updated) ? updated : DateTime.UtcNow
        };
    }
}
