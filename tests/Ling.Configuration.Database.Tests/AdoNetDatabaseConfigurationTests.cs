using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Ling.Configuration.Database.Tests;

public sealed class AdoNetDatabaseConfigurationTests
{
    [Fact]
    public async Task AddDatabaseReadsKeyValueAndEncryptionMarkerColumns()
    {
        var connectionString = $"Data Source=config-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await using (var command = anchor.CreateCommand())
        {
            command.CommandText = "CREATE TABLE ConfigurationEntries (ConfigKey TEXT NOT NULL, ConfigValue TEXT NULL, IsEncrypted INTEGER NOT NULL DEFAULT 0); INSERT INTO ConfigurationEntries (ConfigKey, ConfigValue, IsEncrypted) VALUES ('Feature:Enabled', 'true', 0), ('Optional', NULL, 0), ('Secrets:Token', 'cipher-text', 1);";
            await command.ExecuteNonQueryAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddDatabase(connectionString, SqliteFactory.Instance)
            .Build();
        try
        {
            Assert.Equal("true", configuration["Feature:Enabled"]);
            Assert.Null(configuration["Optional"]);
            Assert.Equal("cipher-text", configuration["Secrets:Token"]);
        }
        finally
        {
            ((IDisposable)configuration).Dispose();
        }
    }
}
