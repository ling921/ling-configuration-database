# Ling.Configuration.Database.EntityFrameworkCore

English | [简体中文](README.zh-CN.md)

[![Build](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Ling.Configuration.Database.EntityFrameworkCore/)

Entity Framework Core adapter for [`Ling.Configuration.Database`](https://www.nuget.org/packages/Ling.Configuration.Database/). Query a configuration entity using an `IDbContextFactory<TContext>` or a context factory delegate.

## Install

```shell
dotnet add package Ling.Configuration.Database.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.*
```

Choose the EF Core database provider and major version used by your application. This adapter targets .NET 8, 9, and 10 and references the corresponding EF Core major version.

## Configuration entity and migrations

The adapter does not own a `DbContext`, table, or migration. The application maps the configuration entity in its own context and manages its schema with its normal EF Core migrations. A typical row contains:

```csharp
public sealed class ConfigurationRow
{
    public string ConfigKey { get; set; } = "";
    public string? ConfigValue { get; set; }
    public bool IsEncrypted { get; set; }
}
```

Map the entity and add a migration in the application:

```csharp
modelBuilder.Entity<ConfigurationRow>(entity =>
{
    entity.ToTable("ConfigurationEntries");
    entity.HasKey(row => row.ConfigKey);
    entity.Property(row => row.ConfigValue);
    entity.Property(row => row.IsEncrypted).HasDefaultValue(false);
});
```

```shell
dotnet ef migrations add AddConfigurationEntries
dotnet ef database update
```

Apply the migration before the database configuration source performs its first load. Production deployments can generate and review a SQL migration script or use a migration bundle. Do not call `EnsureCreated` on a database managed by EF migrations; it bypasses the migration history.

## Configure the source

Read the bootstrap connection string from the application's own configuration before registering the database source. Pass a factory that creates a fresh context for startup loading and each poll:

```csharp
using Ling.Configuration.Database;
using Ling.Configuration.Database.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var connectionString = builder.Configuration.GetConnectionString("ConfigurationDatabase")!;
IDbContextFactory<SettingsContext> contextFactory = CreateSettingsContextFactory(connectionString);

builder.Configuration.AddEntityFrameworkCoreDatabaseConfiguration<SettingsContext, ConfigurationRow>(
    contextFactory,
    context => context.ConfigurationEntries.AsNoTracking(),
    row => new ConfigurationEntry(row.ConfigKey, row.ConfigValue, row.IsEncrypted));
```

Each context is disposed by the adapter. Reuse an existing `IDbContextFactory<TContext>` or the same `DbContextOptions` setup as the application. Do not pass one scoped `DbContext` instance: configuration lives for the application lifetime and EF Core contexts are not thread safe. The factory must not depend on the application service provider that is built after configuration.

## Decrypt encrypted rows

Set `IsEncrypted` for ciphertext rows and configure the core provider's decryptor:

```csharp
builder.Configuration.AddEntityFrameworkCoreDatabaseConfiguration<SettingsContext, ConfigurationRow>(
    contextFactory,
    context => context.ConfigurationEntries.AsNoTracking(),
    row => new ConfigurationEntry(row.ConfigKey, row.ConfigValue, row.IsEncrypted),
    options => options.Decryptor = entry => decryptor.Decrypt(entry.Key, entry.Value!));
```

The callback receives the key and stored value, and runs only for marked rows. If no decryptor is configured or it throws, the exception is logged and the stored ciphertext is used as the configuration value. Set `DatabaseConfigurationOptions.Logger` to use the application's logger; otherwise the error is written through `System.Diagnostics.Trace`. Key storage, encryption algorithms, and key rotation remain application responsibilities.

## Reload

The default polling interval is 30 seconds. Override it through `DatabaseConfigurationOptions.PollingInterval`. The adapter projects rows to a full configuration snapshot; the core provider only signals reload when the snapshot changes.

## Sample

[Runnable sample](https://github.com/ling921/ling-configuration-database/tree/master/samples/Ling.Configuration.Database.EntityFrameworkCore.Sample)

## License

[MIT](https://github.com/ling921/ling-configuration-database/blob/master/LICENSE)
