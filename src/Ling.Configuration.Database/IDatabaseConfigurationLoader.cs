namespace Ling.Configuration.Database;

/// <summary>
/// Loads complete snapshots of configuration values.
/// </summary>
public interface IDatabaseConfigurationLoader
{
    /// <summary>
    /// Loads a complete configuration snapshot synchronously.
    /// </summary>
    /// <returns>
    /// The configuration entries read from the data source.
    /// </returns>
    IReadOnlyCollection<ConfigurationEntry> Load();

    /// <summary>
    /// Loads a complete configuration snapshot asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the load operation.
    /// </param>
    /// <returns>
    /// A task that contains the configuration entries read from the data
    /// source.
    /// </returns>
    ValueTask<IReadOnlyCollection<ConfigurationEntry>> LoadAsync(CancellationToken cancellationToken = default);
}
