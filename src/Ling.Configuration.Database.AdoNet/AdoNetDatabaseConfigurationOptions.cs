using System.Data.Common;

namespace Ling.Configuration.Database.AdoNet;

public sealed class AdoNetDatabaseConfigurationOptions
{
    public required DbProviderFactory ProviderFactory { get; init; }
    public required string ConnectionString { get; init; }
    public string TableName { get; init; } = "ConfigurationEntries";
    public string KeyColumnName { get; init; } = "ConfigKey";
    public string ValueColumnName { get; init; } = "ConfigValue";
    public string? EncryptionColumnName { get; init; } = "IsEncrypted";
    public Func<string, string>? IdentifierQuoter { get; init; }
}
