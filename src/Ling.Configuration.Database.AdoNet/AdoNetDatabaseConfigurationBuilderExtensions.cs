using Ling.Configuration.Database;
using Microsoft.Extensions.Configuration;

namespace Ling.Configuration.Database.AdoNet;

public static class AdoNetDatabaseConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddAdoNetDatabaseConfiguration(
        this IConfigurationBuilder builder,
        AdoNetDatabaseConfigurationOptions databaseOptions,
        Action<DatabaseConfigurationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);
        return builder.AddDatabaseConfiguration(new AdoNetDatabaseConfigurationLoader(databaseOptions), configure);
    }
}
