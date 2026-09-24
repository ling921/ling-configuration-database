using Ling.Configuration.Database;
using Ling.Configuration.Database.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration["Ling:Configuration:Database:ConnectionString"]
    ?? throw new InvalidOperationException("Set the bootstrap database connection string.");
var pollingInterval = TimeSpan.Parse(
    builder.Configuration["Ling:Configuration:Database:PollingInterval"] ?? "00:00:30",
    System.Globalization.CultureInfo.InvariantCulture);

await using (var setup = CreateContext(connectionString))
{
    await setup.Database.EnsureCreatedAsync();
    if (!await setup.Settings.AnyAsync())
    {
        setup.Settings.Add(new Setting { Key = "Sample:Message", Value = "Loaded from the database" });
        await setup.SaveChangesAsync();
    }
}

builder.Configuration.AddEntityFrameworkCoreDatabaseConfiguration<SampleContext, Setting>(
    () => CreateContext(connectionString),
    context => context.Settings.AsNoTracking(),
    setting => new ConfigurationEntry(setting.Key, setting.Value),
    options => options.PollingInterval = pollingInterval);
builder.Services.Configure<SampleOptions>(builder.Configuration.GetSection("Sample"));

using var host = builder.Build();
var monitor = host.Services.GetRequiredService<IOptionsMonitor<SampleOptions>>();
Console.WriteLine(monitor.CurrentValue.Message);
monitor.OnChange(options => Console.WriteLine($"Updated: {options.Message}"));
await host.RunAsync();

static SampleContext CreateContext(string connectionString)
    => new(new DbContextOptionsBuilder<SampleContext>().UseSqlite(connectionString).Options);

sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options)
{
    public DbSet<Setting> Settings => Set<Setting>();
}

sealed class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}

sealed class SampleOptions
{
    public string? Message { get; set; }
}
