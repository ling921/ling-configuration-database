using Microsoft.Extensions.Configuration;

namespace Ling.Configuration.Database;

public sealed class DatabaseConfigurationSource : IConfigurationSource
{
    public required IDatabaseConfigurationLoader Loader { get; init; }

    public DatabaseConfigurationOptions Options { get; init; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new DatabaseConfigurationProvider(Loader, Options);
}
