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
            command.CommandText = "CREATE TABLE ConfigurationEntries (ConfigKey TEXT NOT NULL, ConfigValue TEXT NULL, IsEncrypted INTEGER NOT NULL DEFAULT 0); INSERT INTO ConfigurationEntries (ConfigKey, ConfigValue, IsEncrypted) VALUES ('Feature:Enabled', 'true', 0), ('Optional', NULL, 0), ('Secrets:Token', 'cipher-text', 1);";
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
        Assert.Contains(entries, entry => entry.Key == "Secrets:Token" && entry.Value == "cipher-text" && entry.IsEncrypted);
        Assert.Equal(3, initialEntries.Count);
    }
}
