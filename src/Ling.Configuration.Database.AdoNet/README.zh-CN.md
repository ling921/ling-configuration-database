# Ling.Configuration.Database.AdoNet

[English](README.md) | 简体中文

[![构建](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.AdoNet.svg)](https://www.nuget.org/packages/Ling.Configuration.Database.AdoNet/)

[`Ling.Configuration.Database`](https://www.nuget.org/packages/Ling.Configuration.Database/) 的 ADO.NET 适配器，通过 `DbProviderFactory` 读取配置快照。数据库驱动由应用单独安装。

## 安装

```shell
dotnet add package Ling.Configuration.Database.AdoNet
dotnet add package Microsoft.Data.Sqlite
```

将 `Microsoft.Data.Sqlite` 替换为数据库对应的驱动包，例如 `Microsoft.Data.SqlClient` 或 `Npgsql`。

## 表结构

适配器默认从 `ConfigurationEntries` 表读取以下列：

| 列 | 说明 |
| --- | --- |
| `ConfigKey` | 非空配置键，建议设置为不区分大小写唯一。 |
| `ConfigValue` | 可为空的字符串值。 |
| `IsEncrypted` | 布尔标记；普通配置行建议在数据库中默认设为 `false`。 |

SQLite 示例：

```sql
CREATE TABLE ConfigurationEntries (
    ConfigKey TEXT NOT NULL PRIMARY KEY,
    ConfigValue TEXT NULL,
    IsEncrypted INTEGER NOT NULL DEFAULT 0
);
```

已有的键值表若还没有加密标记列，可设置 `EncryptionColumnName = null`，这些行会按明文处理。库不会自动创建或迁移表结构。

## 配置提供程序

引导连接字符串放在应用自己的配置中，例如 `ConnectionStrings`：

```json
{
  "ConnectionStrings": {
    "ConfigurationDatabase": "Data Source=configuration.db"
  }
}
```

请在添加数据库配置源之前读取连接字符串；加载器会固定使用注册时传入的连接字符串。

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

数据库源会放在 JSON 配置之后、环境变量和命令行参数之前。默认每 30 秒轮询一次；注册时可以调整：

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(databaseOptions,
    options => options.PollingInterval = TimeSpan.FromMinutes(1));
```

## 加密值

适配器读取 `IsEncrypted` 列，并将值传给 `ConfigurationEntry.IsEncrypted`。配置核心提供程序解密被标记的记录：

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(databaseOptions, options =>
{
    options.Decryptor = entry => decryptor.Decrypt(entry.Key, entry.Value!);
});
```

只有 `IsEncrypted` 为 `true` 的行才会调用解密器。本库不提供加密算法或密钥存储；请从独立的密钥管理服务获取密钥。

未配置解密器或解密抛出异常时，会记录异常并将数据库密文作为配置值。设置 `DatabaseConfigurationOptions.Logger` 可使用应用的日志记录器。

## 表名和列名

可通过 `TableName`、`KeyColumnName` 和 `ValueColumnName` 指定其他表名或列名。`EncryptionColumnName` 可指定加密标记列；设为 `null` 可关闭标记读取。标识符默认只允许字母、数字和下划线；数据库需要专用引用规则时可提供 `IdentifierQuoter`。

## 示例

[可运行示例](https://github.com/ling921/ling-configuration-database/tree/master/samples/Ling.Configuration.Database.AdoNet.Sample)

## 许可证

[MIT](https://github.com/ling921/ling-configuration-database/blob/master/LICENSE)
