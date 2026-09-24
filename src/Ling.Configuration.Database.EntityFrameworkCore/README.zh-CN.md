# Ling.Configuration.Database.EntityFrameworkCore

[English](README.md) | 简体中文

[![构建](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml/badge.svg)](https://github.com/ling921/ling-configuration-database/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Configuration.Database.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Ling.Configuration.Database.EntityFrameworkCore/)

[`Ling.Configuration.Database`](https://www.nuget.org/packages/Ling.Configuration.Database/) 的 Entity Framework Core 适配器。通过 `IDbContextFactory<TContext>` 或上下文工厂委托查询应用的配置实体。

## 安装

```shell
dotnet add package Ling.Configuration.Database.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.*
```

数据库 provider 应与应用所使用的 EF Core 主版本一致。本适配器面向 .NET 8、9、10，并引用对应主版本的 EF Core。

## 配置实体和迁移

适配器不拥有 `DbContext`、数据表或迁移。应用在自己的上下文中映射配置实体，并使用应用已有的 EF Core migrations 管理表结构。记录通常包含：

```csharp
public sealed class ConfigurationRow
{
    public string ConfigKey { get; set; } = "";
    public string? ConfigValue { get; set; }
    public bool IsEncrypted { get; set; }
}
```

在应用中映射实体并添加迁移：

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

数据库配置源首次读取之前必须先应用迁移。生产部署可生成并审核 SQL 迁移脚本，或使用 migration bundle。由 EF migrations 管理的数据库不要调用 `EnsureCreated`；该方法会绕过迁移历史。

## 添加配置源

在注册数据库配置源之前，从应用自己的配置读取引导连接字符串。传入的工厂需要为启动加载和每次轮询创建新上下文：

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

适配器会释放每次创建的上下文。可以复用应用的 `IDbContextFactory<TContext>` 或相同的 `DbContextOptions` 配置。不要传入某个请求作用域中的 `DbContext` 实例：配置源会在应用生命周期内持续工作，而 EF Core 上下文不支持并发使用。工厂不能依赖配置完成后才构建的应用服务容器。

## 解密加密记录

密文记录将 `IsEncrypted` 设为 `true`，并配置核心提供程序的解密器：

```csharp
builder.Configuration.AddEntityFrameworkCoreDatabaseConfiguration<SettingsContext, ConfigurationRow>(
    contextFactory,
    context => context.ConfigurationEntries.AsNoTracking(),
    row => new ConfigurationEntry(row.ConfigKey, row.ConfigValue, row.IsEncrypted),
    options => options.Decryptor = entry => decryptor.Decrypt(entry.Key, entry.Value!));
```

回调收到配置键和数据库中的值，只会对标记为加密的记录调用。未配置解密器或解密抛出异常时，会记录异常并将数据库密文作为配置值。设置 `DatabaseConfigurationOptions.Logger` 可使用应用的日志记录器；未设置时会写入 `System.Diagnostics.Trace`。密钥存储、加密算法和轮换由应用负责。

## 重载

默认轮询间隔为 30 秒，可通过 `DatabaseConfigurationOptions.PollingInterval` 调整。适配器将记录投影为完整配置快照；只有快照变化时，核心提供程序才触发重载。

## 示例

[可运行示例](https://github.com/ling921/ling-configuration-database/tree/master/samples/Ling.Configuration.Database.EntityFrameworkCore.Sample)

## 许可证

[MIT](https://github.com/ling921/ling-configuration-database/blob/master/LICENSE)
