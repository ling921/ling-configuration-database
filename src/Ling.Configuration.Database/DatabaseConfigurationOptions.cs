namespace Ling.Configuration.Database;

/// <summary>Options for polling a database configuration source.</summary>
public sealed class DatabaseConfigurationOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Called when a background reload fails. The current snapshot remains active.</summary>
    public Action<Exception>? ReloadErrorHandler { get; set; }
}
