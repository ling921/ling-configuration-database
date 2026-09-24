namespace Ling.Configuration.Database;

/// <summary>Loads complete snapshots of configuration values.</summary>
public interface IDatabaseConfigurationLoader
{
    IReadOnlyCollection<ConfigurationEntry> Load();

    ValueTask<IReadOnlyCollection<ConfigurationEntry>> LoadAsync(CancellationToken cancellationToken = default);
}
