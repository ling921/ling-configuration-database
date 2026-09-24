# Ling.Configuration.Database

[English](README.md) | 简体中文

[![构建](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.svg)](https://www.nuget.org/packages/Ling.Configuration.Database/)

为 `Microsoft.Extensions.Configuration` 提供数据库配置源，支持定时轮询和变更通知。

## NuGet 包

| 包 | 用途 |
| --- | --- |
| `Ling.Configuration.Database` | 配置提供程序接口、快照校验、重载和变化检测。 |
| `Ling.Configuration.Database.AdoNet` | ADO.NET 工厂适配器。 |
| `Ling.Configuration.Database.EntityFrameworkCore` | EF Core 查询适配器。 |

安装核心包和需要的适配器。数据库驱动由应用单独安装，例如 `Microsoft.Data.Sqlite`、`Microsoft.Data.SqlClient` 或 `Npgsql`。

EF Core 适配器分别面向 .NET 8、9、10，并引用对应主版本的 EF Core。

## 数据格式

默认表包含 `ConfigKey` 和 `ConfigValue` 两列。配置键使用冒号分隔的标准层级格式：

| ConfigKey | ConfigValue |
| --- | --- |
| `Logging:LogLevel:Default` | `Information` |
| `Features:NewCheckout` | `true` |

`Ling:Configuration:Database` 是保留的引导配置键空间。若数据库快照含有该键或其子键，整份快照会被拒绝。

示例 `appsettings.json`：

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

## ADO.NET 接入

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

默认标识符仅允许字母、数字和下划线。数据库需要引用标识符或配置了保留字时，可设置 `IdentifierQuoter`。

SQL Server、PostgreSQL 等数据库使用各自驱动包提供的 `DbProviderFactory`。

## EF Core 接入

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

适配器会在启动加载和每次轮询时创建并释放一个上下文。若应用已有 `IDbContextFactory<TContext>`，可直接传入；否则可使用与应用相同的 `DbContextOptions` 配置创建工厂。不要传入某个请求作用域中的 `DbContext` 实例：配置源的生命周期长于请求作用域，而且 EF Core 上下文不支持并发使用。工厂应直接使用引导配置，不能依赖配置构建完成后才创建的应用服务容器。

## 重载语义

启动时读取完整快照，之后默认每 30 秒轮询一次。只有配置键新增、删除或值变化时才替换快照并触发标准重载通知。轮询失败时保留上次成功的快照并继续重试。

数据库源应添加在 JSON 配置之后；环境变量和命令行参数仍具有更高优先级。

使用 `IOptionsMonitor<T>` 的服务会收到更新后的选项。在注册服务或构造单例时读取并保存的普通值只代表启动时快照；也可以在需要当前值时读取注入的 `IConfiguration`。

配置源按应用级工作。调用方可以通过查询或连接工厂选择租户配置；本库不在 `IConfiguration` 中内建租户上下文。

## 许可证

本项目采用 [MIT 许可证](LICENSE)。
