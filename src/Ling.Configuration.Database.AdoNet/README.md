# Ling.Configuration.Database.AdoNet

English | [简体中文](README.zh-CN.md)

[![Build](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.AdoNet.svg)](https://www.nuget.org/packages/Ling.Configuration.Database.AdoNet/)

ADO.NET adapter for [`Ling.Configuration.Database`](https://www.nuget.org/packages/Ling.Configuration.Database/). It reads a configuration snapshot through a `DbProviderFactory`; install the driver for your database separately.

## Install

```shell
dotnet add package Ling.Configuration.Database.AdoNet
dotnet add package Microsoft.Data.Sqlite
```

Replace `Microsoft.Data.Sqlite` with the driver package for your database, such as `Microsoft.Data.SqlClient` or `Npgsql`.

## Table schema

By default, the adapter selects these columns from `ConfigurationEntries`:

| Column | Meaning |
| --- | --- |
| `ConfigKey` | Non-null, case-insensitive unique configuration key. |
| `ConfigValue` | Nullable string value. |
| `IsEncrypted` | Boolean marker; use a database default of false for plain values. |

SQLite example:

```sql
CREATE TABLE ConfigurationEntries (
    ConfigKey TEXT NOT NULL PRIMARY KEY,
    ConfigValue TEXT NULL,
    IsEncrypted INTEGER NOT NULL DEFAULT 0
);
```

For an existing key/value table without an encryption marker column, set `EncryptionColumnName = null`; those rows are treated as plain text. The library does not create or migrate tables.

## Configure the provider

Store the bootstrap connection string in the application's normal location, such as `ConnectionStrings`:

```json
{
  "ConnectionStrings": {
    "ConfigurationDatabase": "Data Source=configuration.db"
  }
}
```

Read it before adding the database source. The connection string is captured when the loader is configured.

```csharp
using Ling.Configuration.Database.AdoNet;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var connectionString = builder.Configuration.GetConnectionString("ConfigurationDatabase")!;
builder.Configuration.AddAdoNetDatabaseConfiguration(new AdoNetDatabaseConfigurationOptions
{
    ProviderFactory = SqliteFactory.Instance,
    ConnectionString = connectionString,
    TableName = "ConfigurationEntries"
});
```

The source is inserted after JSON configuration and before environment variables and command-line arguments. The default polling interval is 30 seconds. Override it through the second argument when registering the provider:

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(databaseOptions,
    options => options.PollingInterval = TimeSpan.FromMinutes(1));
```

## Encrypted values

The adapter reads `IsEncrypted` and returns it as `ConfigurationEntry.IsEncrypted`. Configure the core provider to decrypt marked values:

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(databaseOptions, options =>
{
    options.Decryptor = entry => decryptor.Decrypt(entry.Key, entry.Value!);
});
```

The callback runs only for rows where `IsEncrypted` is true. The library does not provide an encryption algorithm or key storage; obtain keys from an independent secret manager.

If no decryptor is configured or decryption throws, the exception is logged and the stored ciphertext is used as the configuration value. Configure `DatabaseConfigurationOptions.Logger` to use the application's logger.

## Table and column names

Set `TableName`, `KeyColumnName`, and `ValueColumnName` when your schema uses different names. Set `EncryptionColumnName` to a custom marker column or to `null` to disable marker reads. Identifiers are restricted to letters, digits, and underscores by default; supply `IdentifierQuoter` for provider-specific quoting.

## Sample

[Runnable sample](https://github.com/ling921/ling-configuration-database/tree/master/samples/Ling.Configuration.Database.AdoNet.Sample)

## License

[MIT](https://github.com/ling921/ling-configuration-database/blob/master/LICENSE)
