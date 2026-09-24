# Ling.Configuration.Database.EntityFrameworkCore

Entity Framework Core adapter for `Ling.Configuration.Database`. Pass an `IDbContextFactory<TContext>` or a context factory delegate, query, and entity projection. A fresh context is created and disposed for startup loading and each poll. Reuse the application's context factory or its `DbContextOptions` setup; do not pass a scoped `DbContext` instance because configuration outlives the scope and EF Core contexts are not thread safe.
