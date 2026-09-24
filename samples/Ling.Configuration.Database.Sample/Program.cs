using Ling.Configuration.Database;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ConfigurationDatabase")
    ?? throw new InvalidOperationException("Set the bootstrap database connection string.");

await using (var connection = new SqliteConnection(connectionString))
{
    await connection.OpenAsync();
    await using (var create = connection.CreateCommand())
    {
        create.CommandText = "CREATE TABLE IF NOT EXISTS ConfigurationEntries (ConfigKey TEXT NOT NULL PRIMARY KEY, ConfigValue TEXT NULL, IsEncrypted INTEGER NOT NULL DEFAULT 0);";
        await create.ExecuteNonQueryAsync();
    }

    await using (var seed = connection.CreateCommand())
    {
        seed.CommandText = "INSERT INTO ConfigurationEntries (ConfigKey, ConfigValue, IsEncrypted) SELECT $key, $value, 0 WHERE NOT EXISTS (SELECT 1 FROM ConfigurationEntries WHERE ConfigKey = $key);";
        seed.Parameters.AddWithValue("$key", "Sample:Message");
        seed.Parameters.AddWithValue("$value", "Loaded from the ADO.NET database adapter");
        await seed.ExecuteNonQueryAsync();
    }
}

builder.Configuration.AddDatabase(connectionString, SqliteFactory.Instance, options =>
{
    options.TableName = "ConfigurationEntries";
});
builder.Services.Configure<SampleOptions>(builder.Configuration.GetSection("Sample"));

using var host = builder.Build();
var monitor = host.Services.GetRequiredService<IOptionsMonitor<SampleOptions>>();
Console.WriteLine(monitor.CurrentValue.Message);
monitor.OnChange(options => Console.WriteLine($"Updated: {options.Message}"));
await host.RunAsync();

sealed class SampleOptions
{
    public string? Message { get; set; }
}
