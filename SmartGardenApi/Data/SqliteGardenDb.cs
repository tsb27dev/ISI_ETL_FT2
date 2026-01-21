using Microsoft.Data.Sqlite;
using System.IO;

namespace SmartGardenApi.Data;

/// <summary>
/// Lightweight SQLite helper (no Entity Framework). Provides connections and ensures schema exists.
/// </summary>
public sealed class SqliteGardenDb
{
    public string ConnectionString { get; }

    public SqliteGardenDb(IConfiguration configuration)
    {
        // Tenta ler da connection string primeiro, depois de variável de ambiente, depois default
        ConnectionString = configuration.GetConnectionString("Garden") 
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:Garden")
            ?? "Data Source=garden.db";
        
        // No Azure, usa o diretório persistente se disponível
        if (ConnectionString.StartsWith("Data Source=") && 
            !ConnectionString.Contains("/home/data/") && 
            Directory.Exists("/home/data"))
        {
            var dbName = ConnectionString.Replace("Data Source=", "").Trim();
            if (dbName == "garden.db")
            {
                ConnectionString = "Data Source=/home/data/garden.db";
            }
        }
    }

    public SqliteConnection CreateConnection() => new(ConnectionString);

    public async Task EnsureCreatedAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          PRAGMA foreign_keys = ON;

                          CREATE TABLE IF NOT EXISTS Users (
                              Id INTEGER PRIMARY KEY AUTOINCREMENT,
                              Username TEXT NOT NULL UNIQUE,
                              PasswordHash TEXT NOT NULL
                          );

                          CREATE TABLE IF NOT EXISTS Plants (
                              Id INTEGER PRIMARY KEY AUTOINCREMENT,
                              Name TEXT NOT NULL,
                              Location TEXT NOT NULL,
                              RequiredHumidity REAL NOT NULL,
                              LastWatered TEXT NOT NULL
                          );
                          """;

        await cmd.ExecuteNonQueryAsync();
    }
}
