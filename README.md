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

The ADO.NET adapter defaults to the `ConfigurationEntries` table with `ConfigKey`, `ConfigValue`, and `IsEncrypted` columns. `ConfigKey` is a non-empty, case-insensitive unique key; `ConfigValue` can be null; set the database column default for `IsEncrypted` to `false`. EF Core applications map the equivalent fields in their own entity. Keys use the normal colon-separated configuration format:

| ConfigKey | ConfigValue | IsEncrypted |
| --- | --- | --- |
| `Logging:LogLevel:Default` | `Information` | `false` |
| `Secrets:ApiToken` | encrypted payload | `true` |

Rows marked `IsEncrypted = true` are passed to the configured decryptor before they enter the configuration snapshot. Plain rows do not use the decryptor. If no decryptor is configured or it throws, the error is logged and the stored ciphertext is used. Configure `DatabaseConfigurationOptions.Logger` to use the application's logger. The EF Core adapter lets the application map this marker through its own entity; ADO.NET can configure or disable the marker column.

Keep the configuration database connection string in the application's normal bootstrap configuration, usually under `ConnectionStrings`. Read it before adding this provider. The database source uses the connection string captured at registration time; values loaded later do not change its active connection.

```json
{
  "ConnectionStrings": {
    "ConfigurationDatabase": "Data Source=configuration.db"
  }
}
```

## ADO.NET

```csharp
using Ling.Configuration.Database.AdoNet;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var connectionString = builder.Configuration.GetConnectionString("ConfigurationDatabase")!;
builder.Configuration.AddAdoNetDatabaseConfiguration(new()
{
    ProviderFactory = SqliteFactory.Instance,
    ConnectionString = connectionString,
    TableName = "ConfigurationEntries"
});
```

Identifiers use letters, digits, and underscores by default. Set `IdentifierQuoter` when a provider needs quoted identifiers or when a configured name is reserved by that database.

Use the database provider's `DbProviderFactory` and driver package for SQL Server, PostgreSQL, or another ADO.NET database.

## Entity Framework Core

```csharp
using Ling.Configuration.Database;
using Ling.Configuration.Database.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var connectionString = builder.Configuration.GetConnectionString("ConfigurationDatabase")!;
IDbContextFactory<SettingsContext> contextFactory = CreateSettingsContextFactory(connectionString);
builder.Configuration.AddEntityFrameworkCoreDatabaseConfiguration<SettingsContext, Setting>(
    contextFactory,
    context => context.Settings.AsNoTracking(),
    setting => new ConfigurationEntry(setting.ConfigKey, setting.ConfigValue, setting.IsEncrypted),
    options => options.Decryptor = entry => myDecryptor.Decrypt(entry.Key, entry.Value!));
```

The adapter creates and disposes a context for the initial load and every poll. Pass an existing `IDbContextFactory<TContext>` when available, or construct one from the same `DbContextOptions` setup used by the application. Do not pass a scoped `DbContext` instance: configuration outlives a request scope, and EF Core contexts are not thread safe. The factory must use bootstrap settings directly and cannot depend on the application service provider that is built after configuration.

## Reload behavior

The provider loads one complete snapshot at startup, then polls every 30 seconds by default. Change the interval in code with `options => options.PollingInterval = TimeSpan.FromMinutes(1)`, or read a setting from an application-owned key if needed. It replaces the snapshot and raises the standard configuration reload token only when a key is added, removed, or changed. If a poll fails, the last successful snapshot remains active and polling continues.

Environment variables and command-line arguments retain their usual higher priority. Add the database source after JSON configuration so database values replace ordinary JSON values.

Services that use `IOptionsMonitor<T>` receive updated options. A value read once while registering or constructing a singleton remains a startup snapshot; a service can also read the injected `IConfiguration` when it needs the current value.

The source is application scoped. Applications can select a tenant's rows in their query or connection factory; tenant context is not built into `IConfiguration`.

## Samples

Each adapter has a runnable sample:

- **[ADO.NET with SQLite](samples/Ling.Configuration.Database.AdoNet.Sample)** — Creates a local SQLite table, loads a sample value, and observes options reloads.
  Run: `dotnet run --project samples/Ling.Configuration.Database.AdoNet.Sample`
- **[EF Core with SQLite](samples/Ling.Configuration.Database.EntityFrameworkCore.Sample)** — Uses `IDbContextFactory<TContext>` and an EF entity with the `IsEncrypted` marker.
  Run: `dotnet run --project samples/Ling.Configuration.Database.EntityFrameworkCore.Sample`

## Security

The provider can decrypt explicitly marked rows through an application-supplied callback, but it does not choose an encryption algorithm or manage keys. Protect the database with appropriate access controls, TLS, and at-rest encryption. Obtain decryption keys independently of this database.

## License

This project is licensed under the [MIT License](LICENSE).
