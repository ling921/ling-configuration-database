using Ling.Configuration.Database.AdoNet;
using Microsoft.Data.Sqlite;

namespace Ling.Configuration.Database.AdoNet.Tests;

public sealed class AdoNetDatabaseConfigurationLoaderTests
{
    [Fact]
    public async Task ReadsKeyValueRows()
    {
        var connectionString = $"Data Source=config-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await using (var command = anchor.CreateCommand())
        {
            command.CommandText = "CREATE TABLE ConfigurationEntries (ConfigKey TEXT NOT NULL, ConfigValue TEXT NULL); INSERT INTO ConfigurationEntries VALUES ('Feature:Enabled', 'true'), ('Optional', NULL);";
            await command.ExecuteNonQueryAsync();
        }

        var loader = new AdoNetDatabaseConfigurationLoader(new AdoNetDatabaseConfigurationOptions
        {
            ProviderFactory = SqliteFactory.Instance,
            ConnectionString = connectionString
        });

        var entries = await loader.LoadAsync();
        var initialEntries = loader.Load();

        Assert.Contains(entries, entry => entry.Key == "Feature:Enabled" && entry.Value == "true");
        Assert.Contains(entries, entry => entry.Key == "Optional" && entry.Value is null);
        Assert.Equal(2, initialEntries.Count);
    }
}
