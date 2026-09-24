using System.Linq.Expressions;
using Ling.Configuration.Database;
using Microsoft.EntityFrameworkCore;

namespace Ling.Configuration.Database.EntityFrameworkCore;

public sealed class EntityFrameworkCoreDatabaseConfigurationLoader<TContext, TEntity> : IDatabaseConfigurationLoader
    where TContext : DbContext
{
    private readonly Func<TContext> _contextFactory;
    private readonly Func<TContext, IQueryable<TEntity>> _queryFactory;
    private readonly Expression<Func<TEntity, ConfigurationEntry>> _projection;

    public EntityFrameworkCoreDatabaseConfigurationLoader(
        Func<TContext> contextFactory,
        Func<TContext, IQueryable<TEntity>> queryFactory,
        Expression<Func<TEntity, ConfigurationEntry>> projection)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
        _projection = projection ?? throw new ArgumentNullException(nameof(projection));
    }

    public EntityFrameworkCoreDatabaseConfigurationLoader(
        IDbContextFactory<TContext> contextFactory,
        Func<TContext, IQueryable<TEntity>> queryFactory,
        Expression<Func<TEntity, ConfigurationEntry>> projection)
        : this(
            (contextFactory ?? throw new ArgumentNullException(nameof(contextFactory))).CreateDbContext,
            queryFactory,
            projection)
    {
    }

    public IReadOnlyCollection<ConfigurationEntry> Load()
    {
        using var context = _contextFactory();
        return _queryFactory(context).Select(_projection).ToList();
    }

    public async ValueTask<IReadOnlyCollection<ConfigurationEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory();
        return await _queryFactory(context).Select(_projection).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
