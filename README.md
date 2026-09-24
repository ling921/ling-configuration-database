# Ling.Configuration.Database

English | [简体中文](README.zh-CN.md)

[![Build](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.svg)](https://www.nuget.org/packages/Ling.Configuration.Database/)

Database-backed `Microsoft.Extensions.Configuration` with automatic polling and reload notifications.

## Packages

| Package | Purpose |
| --- | --- |
| `Ling.Configuration.Database` | Provider contracts, snapshot validation, reload and change detection. |
| `Ling.Configuration.Database.AdoNet` | ADO.NET provider-factory adapter. |
| `Ling.Configuration.Database.EntityFrameworkCore` | EF Core query adapter. |

Install the core package and one adapter. Install the database driver's package separately, such as `Microsoft.Data.Sqlite`, `Microsoft.Data.SqlClient`, or `Npgsql`.

The EF Core adapter targets .NET 8, 9, and 10 with the corresponding EF Core major version.

## Storage

The table has `ConfigKey` and `ConfigValue` columns by default. Keys use the normal colon-separated configuration format:

| ConfigKey | ConfigValue |
| --- | --- |
| `Logging:LogLevel:Default` | `Information` |
| `Features:NewCheckout` | `true` |

The key namespace `Ling:Configuration:Database` is reserved for bootstrap settings. A database snapshot containing that key or one of its descendants is rejected.

Example bootstrap settings:

```json
{
  "Ling": {
    "Configuration": {
      "Database": {
        "ConnectionString": "Data Source=configuration.db",
        "PollingInterval": "00:00:30"
      }
    }
  }
}
```

## ADO.NET

```csharp
using Ling.Configuration.Database.AdoNet;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var connectionString = builder.Configuration["Ling:Configuration:Database:ConnectionString"]!;
var pollingInterval = builder.Configuration
    .GetSection("Ling:Configuration:Database:PollingInterval")
    .Get<TimeSpan?>() ?? TimeSpan.FromSeconds(30);
builder.Configuration.AddAdoNetDatabaseConfiguration(new()
{
    ProviderFactory = SqliteFactory.Instance,
    ConnectionString = connectionString,
    TableName = "ConfigurationEntries"
}, options => options.PollingInterval = pollingInterval);
```

Identifiers use letters, digits, and underscores by default. Set `IdentifierQuoter` when a provider needs quoted identifiers or when a configured name is reserved by that database.

Use the database provider's `DbProviderFactory` and driver package for SQL Server, PostgreSQL, or another ADO.NET database.

## Entity Framework Core

```csharp
using Ling.Configuration.Database;
using Ling.Configuration.Database.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var connectionString = builder.Configuration["Ling:Configuration:Database:ConnectionString"]!;
var pollingInterval = builder.Configuration
    .GetSection("Ling:Configuration:Database:PollingInterval")
    .Get<TimeSpan?>() ?? TimeSpan.FromSeconds(30);
IDbContextFactory<SettingsContext> contextFactory = CreateSettingsContextFactory(connectionString);
builder.Configuration.AddEntityFrameworkCoreDatabaseConfiguration<SettingsContext, Setting>(
    contextFactory,
    context => context.Settings.AsNoTracking(),
    setting => new ConfigurationEntry(setting.Key, setting.Value),
    options => options.PollingInterval = pollingInterval);
```

The adapter creates and disposes a context for the initial load and every poll. Pass an existing `IDbContextFactory<TContext>` when available, or construct one from the same `DbContextOptions` setup used by the application. Do not pass a scoped `DbContext` instance: configuration outlives a request scope, and EF Core contexts are not thread safe. The factory must use bootstrap settings directly and cannot depend on the application service provider that is built after configuration.

## Reload behavior

The provider loads one complete snapshot at startup, then polls every 30 seconds by default. It replaces the snapshot and raises the standard configuration reload token only when a key is added, removed, or changed. If a poll fails, the last successful snapshot remains active and polling continues.

Environment variables and command-line arguments retain their usual higher priority. Add the database source after JSON configuration so database values replace ordinary JSON values.

Services that use `IOptionsMonitor<T>` receive updated options. A value read once while registering or constructing a singleton remains a startup snapshot; a service can also read the injected `IConfiguration` when it needs the current value.

The source is application scoped. Applications can select a tenant's rows in their query or connection factory; tenant context is not built into `IConfiguration`.

## License

This project is licensed under the [MIT License](LICENSE).
