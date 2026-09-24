# Ling.Configuration.Database

[English](README.md) | 简体中文

[![构建](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.svg)](https://www.nuget.org/packages/Ling.Configuration.Database/)

为 `Microsoft.Extensions.Configuration` 提供数据库配置源，支持定时轮询、快照变化检测、重载通知和可选的配置值解密。

## 安装

```shell
dotnet add package Ling.Configuration.Database
```

读取数据库还需安装一种适配器：

- [ADO.NET 适配器](https://www.nuget.org/packages/Ling.Configuration.Database.AdoNet/)
- [Entity Framework Core 适配器](https://www.nuget.org/packages/Ling.Configuration.Database.EntityFrameworkCore/)

核心包面向 .NET 8，不依赖具体数据库驱动。

## 数据格式

每条数据库记录对应一个配置键。ADO.NET 默认使用 `ConfigurationEntries` 表和以下列；EF Core 应用在自己的实体中映射对应字段。键使用标准冒号分隔格式：

| ConfigKey | ConfigValue | IsEncrypted |
| --- | --- | --- |
| `Logging:LogLevel:Default` | `Information` | `false` |
| `Secrets:ApiToken` | 加密后的内容 | `true` |

键不能为空，且不区分大小写必须唯一。值可以为空。ADO.NET 默认读取 `IsEncrypted` 列；EF Core 由应用实体映射此标记。

## 添加配置源

适配器会将数据库源放在 JSON 之后、环境变量和命令行参数之前：

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(databaseOptions);
```

数据库快照会覆盖更早的 JSON 值；环境变量和命令行参数仍保持更高优先级。

## 轮询和重载

配置启动时会读取完整快照，之后默认每 30 秒轮询一次。可以在代码中调整间隔：

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(
    databaseOptions,
    options => options.PollingInterval = TimeSpan.FromMinutes(1));
```

键比较不区分大小写，值按序比较。只有键新增、删除或值变化时才替换快照并发出重载通知。轮询失败时保留上次成功快照并继续重试；首次加载必须成功。

使用 `IOptionsMonitor<T>` 或读取当前注入的 `IConfiguration` 可获得重载后的值。服务注册或构造时复制保存的值仍是启动时快照。

## 解密加密值

将加密记录的 `IsEncrypted` 设为 `true`，并配置解密器：

```csharp
builder.Configuration.AddAdoNetDatabaseConfiguration(
    databaseOptions,
    options => options.Decryptor = entry =>
        myDecryptor.Decrypt(entry.Key, entry.Value!));
```

同步解密器会收到配置键和数据库中的密文，只对标记为加密的记录调用。未配置解密器或解密抛出异常时，提供程序会记录异常并将数据库原文密文作为配置值。设置 `DatabaseConfigurationOptions.Logger` 可使用应用的日志记录器；未设置时会写入 `System.Diagnostics.Trace`。

本包不指定加密算法，也不管理密钥存储和轮换。请从独立的密钥管理系统获取解密密钥。如何从数据库读取加密标记，见各适配器 README。

## 自定义加载器

当配置记录需要自定义读取或转换逻辑时，可实现 `IDatabaseConfigurationLoader`。每条记录返回一个 `ConfigurationEntry`，需要解密时设置 `IsEncrypted`。

## 许可证

[MIT](https://github.com/ling921/ling-configuration-database/blob/master/LICENSE)
