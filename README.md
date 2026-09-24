# Ling.Configuration.Database

English | [简体中文](README.zh-CN.md)

[![Build](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.svg)](https://www.nuget.org/packages/Ling.Configuration.Database/)

An ADO.NET backed `Microsoft.Extensions.Configuration` provider with polling reload support. It is distributed as one package; install the ADO.NET driver you want to use separately.

## Install

```shell
dotnet add package Ling.Configuration.Database
dotnet add package Microsoft.Data.Sqlite
```

## Usage

Read the bootstrap connection string before adding the database source. It is captured when `AddDatabase` is called, so a value loaded from the database cannot change the connection used by this provider.

```json
{
  "ConnectionStrings": {
    "ConfigurationDatabase": "Data Source=configuration.db"
  }
}
```

```csharp
using Ling.Configuration.Database;
using Microsoft.Data.Sqlite;

var connectionString = builder.Configuration.GetConnectionString("ConfigurationDatabase")!;
builder.Configuration.AddDatabase(connectionString, SqliteFactory.Instance, options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(30);
    options.TableName = "ConfigurationEntries";
    options.Decryptor = entry => Decrypt(entry.Value!);
});
```

`setupAction` is optional. The default query reads `ConfigKey`, `ConfigValue`, and `IsEncrypted` from `ConfigurationEntries`. `ConfigKey` must be non-empty and unique without regard to case. `ConfigValue` may be null. Set `EncryptionColumnName = null` when the table has no encryption marker column. Identifiers allow letters, digits, and underscores by default; configure `IdentifierQuoter` for provider-specific quoting.

The driver package supplies the `DbProviderFactory`. For SQL Server or PostgreSQL, install the corresponding driver and pass its factory with the connection string.

For a custom query, tenant filter, or nonstandard schema, implement `IDatabaseConfigurationLoader` and pass it to the `AddDatabase(loader, setupAction)` overload. The same snapshot validation, decryption, polling, and reload behavior applies.

## Encrypted values

Set `IsEncrypted` to `true` for rows that contain ciphertext. The optional `Decryptor` runs only for those rows. If it is missing or throws, the error is logged and the original stored value is used. Set `DatabaseConfigurationOptions.Logger` to use the application's logger; otherwise the error is written through `System.Diagnostics.Trace`.

## Reload behavior

The provider loads a complete snapshot when configuration is built, then polls every 30 seconds by default. It compares keys without regard to case and compares values ordinally. It updates the snapshot and triggers the standard reload token only when a key is added, removed, or changed. A failed poll keeps the last successful snapshot and later polls continue. Polls run serially.

Add the provider after JSON sources. Environment variables and command-line arguments keep their normal higher priority. `IOptionsMonitor<T>` and services that read the injected `IConfiguration` again can observe updates. A regular value read once during service registration is a one-time snapshot.

## Schema example

```sql
CREATE TABLE ConfigurationEntries (
    ConfigKey TEXT NOT NULL PRIMARY KEY,
    ConfigValue TEXT NULL,
    IsEncrypted INTEGER NOT NULL DEFAULT 0
);
```

Adapt the types to the selected database. The library does not create or migrate this table; manage its schema with your database's usual migration mechanism.

## Sample

[SQLite sample](samples/Ling.Configuration.Database.Sample) creates a local database table, loads a sample value, and demonstrates options reload.

```shell
dotnet run --project samples/Ling.Configuration.Database.Sample
```

## License

This project is licensed under the [MIT License](LICENSE).
