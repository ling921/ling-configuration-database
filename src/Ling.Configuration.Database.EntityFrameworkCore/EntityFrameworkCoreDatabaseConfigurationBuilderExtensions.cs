using System.Linq.Expressions;
using Ling.Configuration.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ling.Configuration.Database.EntityFrameworkCore;

public static class EntityFrameworkCoreDatabaseConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEntityFrameworkCoreDatabaseConfiguration<TContext, TEntity>(
        this IConfigurationBuilder builder,
        Func<TContext> contextFactory,
        Func<TContext, IQueryable<TEntity>> queryFactory,
        Expression<Func<TEntity, ConfigurationEntry>> projection,
        Action<DatabaseConfigurationOptions>? configure = null)
        where TContext : DbContext
    {
        var loader = new EntityFrameworkCoreDatabaseConfigurationLoader<TContext, TEntity>(contextFactory, queryFactory, projection);
        return builder.AddDatabaseConfiguration(loader, configure);
    }
}
