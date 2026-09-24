using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;

namespace Ling.Configuration.Database;

/// <summary>
/// Provides extension methods for adding database configuration sources.
/// </summary>
public static class DatabaseConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds an ADO.NET database source after JSON sources and before
    /// environment-variable and command-line sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder to add the source to.
    /// </param>
    /// <param name="connectionString">
    /// The connection string used to read configuration values.
    /// </param>
    /// <param name="dbProviderFactory">
    /// The provider factory used to create database connections.
    /// </param>
    /// <param name="setupAction">
    /// An optional callback for configuring polling, schema, and value handling.
    /// </param>
    /// <returns>
    /// The configuration builder.
    /// </returns>
    public static IConfigurationBuilder AddDatabase(
        this IConfigurationBuilder builder,
        string connectionString,
        DbProviderFactory dbProviderFactory,
        Action<DatabaseConfigurationOptions>? setupAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(dbProviderFactory);

        var options = new DatabaseConfigurationOptions();
        setupAction?.Invoke(options);
        ValidateOptions(options);
        var loader = new AdoNetDatabaseConfigurationLoader(connectionString, dbProviderFactory, options);
        var source = new DatabaseConfigurationSource { Loader = loader, Options = options };
        var insertAt = FindInsertionIndex(builder.Sources);
        builder.Sources.Insert(insertAt, source);
        return builder;
    }

    /// <summary>
    /// Adds a database source that uses a custom configuration loader after
    /// JSON sources and before environment-variable and command-line sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder to add the source to.
    /// </param>
    /// <param name="loader">
    /// The loader that reads complete configuration snapshots.
    /// </param>
    /// <param name="setupAction">
    /// An optional callback for configuring polling and value handling.
    /// </param>
    /// <returns>
    /// The configuration builder.
    /// </returns>
    public static IConfigurationBuilder AddDatabase(
        this IConfigurationBuilder builder,
        IDatabaseConfigurationLoader loader,
        Action<DatabaseConfigurationOptions>? setupAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loader);

        var options = new DatabaseConfigurationOptions();
        setupAction?.Invoke(options);
        ValidateOptions(options);

        var source = new DatabaseConfigurationSource { Loader = loader, Options = options };
        var insertAt = FindInsertionIndex(builder.Sources);
        builder.Sources.Insert(insertAt, source);
        return builder;
    }

    private static void ValidateOptions(DatabaseConfigurationOptions options)
    {
        if (options.PollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options.PollingInterval), "Polling interval must be greater than zero.");
        }
    }

    private static int FindInsertionIndex(IList<IConfigurationSource> sources)
    {
        var lastJson = -1;
        var firstEnvironmentOrCommandLine = sources.Count;
        for (var index = 0; index < sources.Count; index++)
        {
            if (sources[index] is JsonConfigurationSource or JsonStreamConfigurationSource)
            {
                lastJson = index;
            }

            if (sources[index] is EnvironmentVariablesConfigurationSource or CommandLineConfigurationSource)
            {
                firstEnvironmentOrCommandLine = Math.Min(firstEnvironmentOrCommandLine, index);
            }
        }

        return Math.Min(lastJson + 1, firstEnvironmentOrCommandLine);
    }
}
