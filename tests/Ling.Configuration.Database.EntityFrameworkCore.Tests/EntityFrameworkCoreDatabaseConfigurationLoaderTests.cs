using Ling.Configuration.Database;
using Ling.Configuration.Database.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ling.Configuration.Database.EntityFrameworkCore.Tests;

public sealed class EntityFrameworkCoreDatabaseConfigurationLoaderTests
{
    [Fact]
    public async Task ReadsProjectedValuesWithContextPerLoad()
    {
        var connectionString = $"Data Source=config-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await using (var context = CreateContext(connectionString))
        {
            await context.Database.EnsureCreatedAsync();
            context.Settings.Add(new ConfigurationRow { ConfigKey = "Feature:Enabled", ConfigValue = "true", IsEncrypted = false });
            await context.SaveChangesAsync();
        }

        var loader = new EntityFrameworkCoreDatabaseConfigurationLoader<SettingsContext, ConfigurationRow>(
            new DelegateDbContextFactory<SettingsContext>(() => CreateContext(connectionString)),
            context => context.Settings.AsNoTracking(),
            row => new ConfigurationEntry(row.ConfigKey, row.ConfigValue, row.IsEncrypted));

        var entries = await loader.LoadAsync();
        var initialEntries = loader.Load();
        Assert.Contains(entries, entry => entry.Key == "Feature:Enabled" && entry.Value == "true" && !entry.IsEncrypted);
        Assert.Single(initialEntries);
    }

    private static SettingsContext CreateContext(string connectionString)
        => new(new DbContextOptionsBuilder<SettingsContext>().UseSqlite(connectionString).Options);

    private sealed class SettingsContext(DbContextOptions<SettingsContext> options) : DbContext(options)
    {
        public DbSet<ConfigurationRow> Settings => Set<ConfigurationRow>();
    }

    private sealed class DelegateDbContextFactory<TContext>(Func<TContext> create)
        : IDbContextFactory<TContext>
        where TContext : DbContext
    {
        public TContext CreateDbContext() => create();

        public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(create());
    }

    private sealed class ConfigurationRow
    {
        public int Id { get; set; }
        public string ConfigKey { get; set; } = "";
        public string? ConfigValue { get; set; }
        public bool IsEncrypted { get; set; }
    }
}
