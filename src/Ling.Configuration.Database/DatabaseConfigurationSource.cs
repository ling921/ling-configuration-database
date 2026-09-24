using Microsoft.Extensions.Configuration;

namespace Ling.Configuration.Database;

/// <summary>
/// Describes a database-backed configuration source.
/// </summary>
public sealed class DatabaseConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets the loader that reads configuration snapshots from the database.
    /// </summary>
    public required IDatabaseConfigurationLoader Loader { get; init; }

    /// <summary>
    /// Gets the options used to poll and process database configuration values.
    /// </summary>
    public DatabaseConfigurationOptions Options { get; init; } = new();

    /// <summary>
    /// Builds a provider for this configuration source.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <returns>
    /// The provider for this source.
    /// </returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new DatabaseConfigurationProvider(Loader, Options);
}
