using Microsoft.Extensions.Logging;

namespace Ling.Configuration.Database;

/// <summary>Options for polling a database configuration source.</summary>
public sealed class DatabaseConfigurationOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    public string TableName { get; set; } = "ConfigurationEntries";

    public string KeyColumnName { get; set; } = "ConfigKey";

    public string ValueColumnName { get; set; } = "ConfigValue";

    public string? EncryptionColumnName { get; set; } = "IsEncrypted";

    public Func<string, string>? IdentifierQuoter { get; set; }

    /// <summary>Synchronously decrypts values marked as encrypted in the database.</summary>
    public Func<ConfigurationEntry, string?>? Decryptor { get; set; }

    /// <summary>Optional logger for decryption failures and plaintext fallback.</summary>
    public ILogger? Logger { get; set; }

    /// <summary>Called when a background reload fails. The current snapshot remains active.</summary>
    public Action<Exception>? ReloadErrorHandler { get; set; }
}
