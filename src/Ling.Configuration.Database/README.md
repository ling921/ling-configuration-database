# Ling.Configuration.Database

English | [简体中文](README.zh-CN.md)

[![Build](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.svg)](https://www.nuget.org/packages/Ling.Configuration.Database/)

Database-backed `Microsoft.Extensions.Configuration` provider with polling, snapshot change detection, reload tokens, and optional value decryption.

## Install

```shell
dotnet add package Ling.Configuration.Database
```

Install an adapter package to read from a database:

- [ADO.NET adapter](https://www.nuget.org/packages/Ling.Configuration.Database.AdoNet/)
- [Entity Framework Core adapter](https://www.nuget.org/packages/Ling.Configuration.Database.EntityFrameworkCore/)

The core package targets .NET 8 and has no database driver dependency.

## Stored values

Each database row represents one configuration key. ADO.NET defaults to a `ConfigurationEntries` table with the columns below; EF Core applications map equivalent fields in their own entity. Keys use the standard colon-separated format:

| ConfigKey | ConfigValue | IsEncrypted |
| --- | --- | --- |
| `Logging:LogLevel:Default` | `Information` | `false` |
| `Secrets:ApiToken` | encrypted payload | `true` |

Keys must be non-empty and unique without regard to case. Values can be null. The ADO.NET adapter reads the `IsEncrypted` column by default; with EF Core, map the marker from your own entity.

## Add a source

Adapters add the database source after JSON sources and before environment variables and command-line arguments:

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(databaseOptions);
```

The database snapshot overrides earlier JSON values. Environment variables and command-line arguments keep their normal higher priority.

## Polling and reload

The provider loads a complete snapshot during configuration startup and polls every 30 seconds by default. Change the interval in code:

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(
    databaseOptions,
    options => options.PollingInterval = TimeSpan.FromMinutes(1));
```

The provider compares keys without regard to case and values ordinally. It replaces the snapshot and signals a reload only when a key is added, removed, or changed. A polling failure leaves the last successful snapshot in place and polling continues. The first load must succeed.

Use `IOptionsMonitor<T>` or read the current injected `IConfiguration` to observe reloaded values. Values copied into a service during registration or construction remain a startup snapshot.

## Decrypt encrypted values

Mark encrypted rows with `IsEncrypted = true` and configure a decryptor:

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(
    databaseOptions,
    options => options.Decryptor = entry =>
        myDecryptor.Decrypt(entry.Key, entry.Value!));
```

The synchronous decryptor receives the key and stored ciphertext. It runs for marked rows only. If no decryptor is configured, or decryption throws, the provider logs the exception and uses the stored ciphertext as the configuration value. Set `DatabaseConfigurationOptions.Logger` to use the application's logger; otherwise the error is written through `System.Diagnostics.Trace`.

The package does not select an encryption algorithm or manage key storage and rotation. Obtain decryption keys from an independent secret-management system. See the adapter README for how to read the encryption marker from each database.

## Custom loaders

Implement `IDatabaseConfigurationLoader` when configuration rows need custom retrieval or transformation. Return a `ConfigurationEntry` for each row and set `IsEncrypted` when its value needs decryption.

## License

[MIT](https://github.com/ling921/ling-configuration-database/blob/master/LICENSE)
