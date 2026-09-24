# Ling.Configuration.Database.EntityFrameworkCore

`Ling.Configuration.Database` 的 EF Core 适配器，可传入 `IDbContextFactory<TContext>` 或上下文工厂委托、查询和实体投影。启动加载及每次轮询都会创建并释放新的上下文。可以复用应用的上下文工厂或 `DbContextOptions` 配置；不要传入请求作用域中的 `DbContext` 实例，因为配置源生命周期更长，且 EF Core 上下文不支持并发使用。
