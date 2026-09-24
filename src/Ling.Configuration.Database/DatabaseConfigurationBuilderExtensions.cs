using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;

namespace Ling.Configuration.Database;

public static class DatabaseConfigurationBuilderExtensions
{
    /// <summary>Adds a database source after JSON sources and before environment and command-line sources.</summary>
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

    /// <summary>Adds a database source after JSON sources and before environment and command-line sources.</summary>
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
