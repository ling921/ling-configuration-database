# Ling.Configuration.Database

[English](README.md) | 简体中文

[![构建](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.svg)](https://www.nuget.org/packages/Ling.Configuration.Database/)

基于 ADO.NET 的 `Microsoft.Extensions.Configuration` 配置提供程序，支持定时轮询和重载通知。项目只发布一个 NuGet 包；数据库驱动由使用方单独安装。

## 安装

```shell
dotnet add package Ling.Configuration.Database
dotnet add package Microsoft.Data.Sqlite
```

## 使用

添加数据库配置源之前，先从引导配置读取连接字符串。调用 `AddDatabase` 时会捕获该连接字符串，数据库加载的值不会改变此配置源使用的连接。

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

`setupAction` 是可选的。默认查询从 `ConfigurationEntries` 表读取 `ConfigKey`、`ConfigValue` 和 `IsEncrypted`。`ConfigKey` 不能为空，且不区分大小写唯一；`ConfigValue` 可以为 null。如果表没有加密标记列，将 `EncryptionColumnName` 设为 `null`。默认标识符只允许字母、数字和下划线；可通过 `IdentifierQuoter` 配置数据库特定的引用方式。

数据库驱动包提供 `DbProviderFactory`。使用 SQL Server、PostgreSQL 等数据库时，安装对应驱动并传入其工厂与连接字符串。

如果需要自定义查询、租户筛选或特殊表结构，可以实现 `IDatabaseConfigurationLoader`，并传给 `AddDatabase(loader, setupAction)` 重载。此方式仍使用相同的快照校验、解密、轮询和重载逻辑。

## 加密值

加密存储的记录将 `IsEncrypted` 设为 `true`。可选的 `Decryptor` 只会处理这类记录。若未配置解密器或解密器抛出异常，会记录异常并使用数据库中的原始值。设置 `DatabaseConfigurationOptions.Logger` 可使用应用的日志记录器；未设置时会写入 `System.Diagnostics.Trace`。

## 重载行为

构建配置时会读取完整快照，之后默认每 30 秒轮询一次。键名比较不区分大小写，值使用序数比较。只有键新增、删除或值变化时才替换快照并触发标准重载令牌。轮询失败时保留上次成功的快照，并在后续继续轮询。轮询按顺序执行，不会重叠。

请将数据库配置源添加在 JSON 源之后。环境变量和命令行参数仍保持原有的更高优先级。`IOptionsMonitor<T>` 和再次读取注入的 `IConfiguration` 的服务可以获得更新；在服务注册时读取并保存的普通值是一次性快照。

## 表结构示例

```sql
CREATE TABLE ConfigurationEntries (
    ConfigKey TEXT NOT NULL PRIMARY KEY,
    ConfigValue TEXT NULL,
    IsEncrypted INTEGER NOT NULL DEFAULT 0
);
```

请根据所用数据库调整列类型。本库不会创建或迁移该表；请使用数据库项目已有的迁移方式管理表结构。

## 示例

[SQLite 示例](samples/Ling.Configuration.Database.Sample) 会创建本地数据库表、读取示例值并演示选项重载。

```shell
dotnet run --project samples/Ling.Configuration.Database.Sample
```

## 许可证

本项目采用 [MIT 许可证](LICENSE)。
