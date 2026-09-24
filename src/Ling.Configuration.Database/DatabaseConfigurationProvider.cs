using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ling.Configuration.Database;

/// <summary>
/// Loads configuration snapshots from a database and reloads them when they
/// change.
/// </summary>
public sealed class DatabaseConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IDatabaseConfigurationLoader _loader;
    private readonly DatabaseConfigurationOptions _options;
    private readonly CancellationTokenSource _stop = new();
    private Task? _pollingTask;
    private int _started;
    private int _disposed;

    /// <summary>
    /// Initializes a new database configuration provider.
    /// </summary>
    /// <param name="loader">
    /// The loader used to read configuration snapshots.
    /// </param>
    /// <param name="options">
    /// The options used to poll and process values.
    /// </param>
    public DatabaseConfigurationProvider(IDatabaseConfigurationLoader loader, DatabaseConfigurationOptions options)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.PollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Polling interval must be greater than zero.");
        }
    }

    /// <summary>
    /// Loads the initial database snapshot and starts background polling.
    /// </summary>
    public override void Load()
    {
        var initial = ValidateAndCreateSnapshot(_loader.Load());
        Data = initial;
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _pollingTask = Task.Run(PollAsync);
        }
    }

    /// <summary>
    /// Stops background polling and releases the resources used by this
    /// provider.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _stop.Cancel();
        _stop.Dispose();
    }

    private async Task PollAsync()
    {
        using var timer = new PeriodicTimer(_options.PollingInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false))
            {
                try
                {
                    var next = ValidateAndCreateSnapshot(await _loader.LoadAsync(_stop.Token).ConfigureAwait(false));
                    if (!SnapshotsEqual(Data, next))
                    {
                        Data = next;
                        OnReload();
                    }
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    try { _options.ReloadErrorHandler?.Invoke(exception); }
                    catch { /* Error reporting must not stop future polling. */ }
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private Dictionary<string, string?> ValidateAndCreateSnapshot(IReadOnlyCollection<ConfigurationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var snapshot = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new InvalidOperationException("Database configuration keys must not be empty.");
            }

            var value = entry.Value;
            if (entry.IsEncrypted)
            {
                try
                {
                    if (value is null)
                    {
                        throw new InvalidOperationException("The encrypted database value is NULL.");
                    }

                    var decryptor = _options.Decryptor
                        ?? throw new InvalidOperationException("No decryptor was configured.");
                    value = decryptor(entry);
                }
                catch (Exception exception)
                {
                    LogDecryptionFallback(exception, entry);
                    value = entry.Value;
                }
            }

            if (!snapshot.TryAdd(entry.Key, value))
            {
                throw new InvalidOperationException($"Database configuration contains duplicate key '{entry.Key}'.");
            }
        }

        return snapshot;
    }

    private void LogDecryptionFallback(Exception exception, ConfigurationEntry entry)
    {
        if (_options.Logger is { } logger)
        {
            try
            {
                logger.LogError(exception, "Could not decrypt configuration key '{ConfigurationKey}'; using the stored value.", entry.Key);
                return;
            }
            catch
            {
                // Logging must not prevent loading the original configuration value.
            }
        }

        try
        {
            System.Diagnostics.Trace.TraceError(
                "Could not decrypt configuration key '{0}'; using the stored value. {1}",
                entry.Key,
                exception);
        }
        catch
        {
            // Logging must not prevent loading the original configuration value.
        }
    }

    private static bool SnapshotsEqual(IDictionary<string, string?> current, IDictionary<string, string?> next)
    {
        if (current.Count != next.Count)
        {
            return false;
        }

        foreach (var pair in current)
        {
            if (!next.TryGetValue(pair.Key, out var value) || !StringComparer.Ordinal.Equals(pair.Value, value))
            {
                return false;
            }
        }

        return true;
    }
}
