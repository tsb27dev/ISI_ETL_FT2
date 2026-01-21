using Microsoft.Data.Sqlite;
using SmartGardenApi.Models;

namespace SmartGardenApi.Data;

public sealed class SqliteGardenRepository : IGardenRepository
{
    private readonly SqliteGardenDb _db;

    public SqliteGardenRepository(SqliteGardenDb db)
    {
        _db = db;
    }

    // -----------------------
    // Users
    // -----------------------

    public async Task<bool> UsernameExistsAsync(string username)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Users WHERE Username = $username LIMIT 1;";
        cmd.Parameters.AddWithValue("$username", username);

        var result = await cmd.ExecuteScalarAsync();
        return result != null && result != DBNull.Value;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT Id, Username, PasswordHash
                          FROM Users
                          WHERE Username = $username
                          LIMIT 1;
                          """;
        cmd.Parameters.AddWithValue("$username", username);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2)
        };
    }

    public async Task<int> CreateUserAsync(User user)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO Users (Username, PasswordHash)
                          VALUES ($username, $passwordHash);
                          SELECT last_insert_rowid();
                          """;
        cmd.Parameters.AddWithValue("$username", user.Username);
        cmd.Parameters.AddWithValue("$passwordHash", user.PasswordHash);

        var newIdObj = await cmd.ExecuteScalarAsync();
        var newId = Convert.ToInt32(newIdObj);
        user.Id = newId;
        return newId;
    }

    public async Task<int> UpdateUserPasswordHashAsync(int userId, string newPasswordHash)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Users SET PasswordHash = $hash WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$hash", newPasswordHash);
        cmd.Parameters.AddWithValue("$id", userId);

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteUserAsync(int userId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Users WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", userId);

        return await cmd.ExecuteNonQueryAsync();
    }

    // -----------------------
    // Plants
    // -----------------------

    public async Task<List<Plant>> GetPlantsAsync()
    {
        var plants = new List<Plant>();

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT Id, Name, Location, RequiredHumidity, LastWatered
                          FROM Plants
                          ORDER BY Id;
                          """;

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plants.Add(new Plant
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Location = reader.GetString(2),
                RequiredHumidity = reader.GetDouble(3),
                LastWatered = DateTime.Parse(reader.GetString(4))
            });
        }

        return plants;
    }

    public async Task<Plant?> GetPlantByIdAsync(int id)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT Id, Name, Location, RequiredHumidity, LastWatered
                          FROM Plants
                          WHERE Id = $id
                          LIMIT 1;
                          """;
        cmd.Parameters.AddWithValue("$id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new Plant
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Location = reader.GetString(2),
            RequiredHumidity = reader.GetDouble(3),
            LastWatered = DateTime.Parse(reader.GetString(4))
        };
    }

    public async Task<int> CreatePlantAsync(Plant plant)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO Plants (Name, Location, RequiredHumidity, LastWatered)
                          VALUES ($name, $location, $requiredHumidity, $lastWatered);
                          SELECT last_insert_rowid();
                          """;
        cmd.Parameters.AddWithValue("$name", plant.Name);
        cmd.Parameters.AddWithValue("$location", plant.Location);
        cmd.Parameters.AddWithValue("$requiredHumidity", plant.RequiredHumidity);
        cmd.Parameters.AddWithValue("$lastWatered", plant.LastWatered.ToString("O"));

        var newIdObj = await cmd.ExecuteScalarAsync();
        var newId = Convert.ToInt32(newIdObj);
        plant.Id = newId;
        return newId;
    }

    public async Task<int> UpdatePlantAsync(Plant plant)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          UPDATE Plants
                          SET Name = $name,
                              Location = $location,
                              RequiredHumidity = $requiredHumidity
                          WHERE Id = $id;
                          """;
        cmd.Parameters.AddWithValue("$name", plant.Name);
        cmd.Parameters.AddWithValue("$location", plant.Location);
        cmd.Parameters.AddWithValue("$requiredHumidity", plant.RequiredHumidity);
        cmd.Parameters.AddWithValue("$id", plant.Id);

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeletePlantAsync(int id)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Plants WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(int Updated, int Created, int Deleted)> SyncPlantsFromExcelAsync(
        IReadOnlyList<(int? Id, string Name, string Location, double RequiredHumidity)> rows)
    {
        // Match existing behavior: delete items not present by ID in the Excel (only considering IDs explicitly provided),
        // then apply updates/inserts.
        var idsInExcel = rows.Where(r => r.Id is > 0).Select(r => r.Id!.Value).Distinct().ToList();

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        var deleted = await DeletePlantsNotInIdsAsync(conn, tx, idsInExcel);

        var updated = 0;
        var created = 0;

        foreach (var row in rows)
        {
            if (row.Id is > 0)
            {
                var affected = await UpdatePlantByIdAsync(conn, tx, row.Id.Value, row.Name, row.Location, row.RequiredHumidity);
                if (affected > 0)
                {
                    updated++;
                    continue;
                }

                // If the ID doesn't exist, behave like the old code: insert as new (auto-ID).
                await InsertPlantAsync(conn, tx, row.Name, row.Location, row.RequiredHumidity, DateTime.Now);
                created++;
            }
            else
            {
                await InsertPlantAsync(conn, tx, row.Name, row.Location, row.RequiredHumidity, DateTime.Now);
                created++;
            }
        }

        await tx.CommitAsync();
        return (updated, created, deleted);
    }

    private static async Task<int> UpdatePlantByIdAsync(
        SqliteConnection conn,
        SqliteTransaction tx,
        int id,
        string name,
        string location,
        double requiredHumidity)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
                          UPDATE Plants
                          SET Name = $name,
                              Location = $location,
                              RequiredHumidity = $requiredHumidity
                          WHERE Id = $id;
                          """;
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$location", location);
        cmd.Parameters.AddWithValue("$requiredHumidity", requiredHumidity);
        cmd.Parameters.AddWithValue("$id", id);

        return await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> InsertPlantAsync(
        SqliteConnection conn,
        SqliteTransaction tx,
        string name,
        string location,
        double requiredHumidity,
        DateTime lastWatered)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
                          INSERT INTO Plants (Name, Location, RequiredHumidity, LastWatered)
                          VALUES ($name, $location, $requiredHumidity, $lastWatered);
                          """;
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$location", location);
        cmd.Parameters.AddWithValue("$requiredHumidity", requiredHumidity);
        cmd.Parameters.AddWithValue("$lastWatered", lastWatered.ToString("O"));

        return await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> DeletePlantsNotInIdsAsync(
        SqliteConnection conn,
        SqliteTransaction tx,
        IReadOnlyList<int> idsInExcel)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;

        if (idsInExcel.Count == 0)
        {
            cmd.CommandText = "DELETE FROM Plants;";
            return await cmd.ExecuteNonQueryAsync();
        }

        var paramNames = new List<string>(idsInExcel.Count);
        for (var i = 0; i < idsInExcel.Count; i++)
        {
            var p = $"$id{i}";
            paramNames.Add(p);
            cmd.Parameters.AddWithValue(p, idsInExcel[i]);
        }

        cmd.CommandText = $"DELETE FROM Plants WHERE Id NOT IN ({string.Join(", ", paramNames)});";
        return await cmd.ExecuteNonQueryAsync();
    }
}
