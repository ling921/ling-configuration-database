using Microsoft.Extensions.Logging;

namespace Ling.Configuration.Database;

/// <summary>
/// Configures how a database configuration source reads, decrypts, and reloads
/// configuration values.
/// </summary>
public sealed class DatabaseConfigurationOptions
{
    /// <summary>
    /// Gets or sets the time to wait between database polls.
    /// </summary>
    /// <value>The polling interval, which defaults to 30 seconds.</value>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the name of the table that stores configuration entries.
    /// </summary>
    /// <value>The table name, which defaults to <c>ConfigurationEntries</c>.</value>
    public string TableName { get; set; } = "ConfigurationEntries";

    /// <summary>
    /// Gets or sets the name of the column that stores configuration keys.
    /// </summary>
    /// <value>The key column name, which defaults to <c>ConfigKey</c>.</value>
    public string KeyColumnName { get; set; } = "ConfigKey";

    /// <summary>
    /// Gets or sets the name of the column that stores configuration values.
    /// </summary>
    /// <value>The value column name, which defaults to <c>ConfigValue</c>.</value>
    public string ValueColumnName { get; set; } = "ConfigValue";

    /// <summary>
    /// Gets or sets the name of the column that marks encrypted values.
    /// </summary>
    /// <value>
    /// The encryption marker column name, which defaults to
    /// <c>IsEncrypted</c>. Set this property to <see langword="null"/> when
    /// the table does not contain an encryption marker column.
    /// </value>
    public string? EncryptionColumnName { get; set; } = "IsEncrypted";

    /// <summary>
    /// Gets or sets a function that quotes a validated database identifier.
    /// </summary>
    /// <value>
    /// A provider-specific identifier quoting function, or <see langword="null"/>
    /// to use the identifier without quoting.
    /// </value>
    public Func<string, string>? IdentifierQuoter { get; set; }

    /// <summary>
    /// Gets or sets a callback that decrypts values marked as encrypted in the
    /// database.
    /// </summary>
    /// <value>
    /// A function that receives the encrypted entry and returns its plaintext
    /// value, or <see langword="null"/> when no decryptor is configured.
    /// </value>
    public Func<ConfigurationEntry, string?>? Decryptor { get; set; }

    /// <summary>
    /// Gets or sets the logger used to report decryption failures.
    /// </summary>
    /// <value>
    /// The logger to use, or <see langword="null"/> to write errors through
    /// <see cref="System.Diagnostics.Trace"/>.
    /// </value>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when a background reload fails.
    /// </summary>
    /// <value>
    /// An error handler that receives the exception. The last successful
    /// configuration snapshot remains active when a reload fails.
    /// </value>
    public Action<Exception>? ReloadErrorHandler { get; set; }
}
