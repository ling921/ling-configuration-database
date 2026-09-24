using Microsoft.Extensions.Configuration;

namespace Ling.Configuration.Database;

public sealed class DatabaseConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IDatabaseConfigurationLoader _loader;
    private readonly DatabaseConfigurationOptions _options;
    private readonly CancellationTokenSource _stop = new();
    private Task? _pollingTask;
    private int _started;
    private int _disposed;

    public DatabaseConfigurationProvider(IDatabaseConfigurationLoader loader, DatabaseConfigurationOptions options)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.PollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Polling interval must be greater than zero.");
        }
    }

    public override void Load()
    {
        var initial = ValidateAndCreateSnapshot(_loader.Load());
        Data = initial;
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _pollingTask = Task.Run(PollAsync);
        }
    }

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

    private static Dictionary<string, string?> ValidateAndCreateSnapshot(IReadOnlyCollection<ConfigurationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var snapshot = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new InvalidOperationException("Database configuration keys must not be empty.");
            }

            if (!snapshot.TryAdd(entry.Key, entry.Value))
            {
                throw new InvalidOperationException($"Database configuration contains duplicate key '{entry.Key}'.");
            }
        }

        return snapshot;
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
