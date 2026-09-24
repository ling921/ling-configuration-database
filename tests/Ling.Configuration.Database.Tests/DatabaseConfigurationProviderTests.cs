using System.Security.Cryptography;
using Ling.Configuration.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ling.Configuration.Database.Tests;

public sealed class DatabaseConfigurationProviderTests
{
    [Fact]
    public void LoadsHierarchicalKeysAndDatabaseValuesOverrideJson()
    {
        var root = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream("{\"Logging\":{\"LogLevel\":{\"Default\":\"Warning\"}}}"u8.ToArray()))
            .AddDatabase(new MutableLoader([new("Logging:LogLevel:Default", "Information"), new("Feature:Enabled", "true")]))
            .Build();
        using var lifetime = (IDisposable)root;

        Assert.Equal("Information", root["Logging:LogLevel:Default"]);
        Assert.Equal("true", root["Feature:Enabled"]);
    }

    [Fact]
    public void DecryptsOnlyEntriesMarkedAsEncrypted()
    {
        var root = new ConfigurationBuilder()
            .AddDatabase(
                new MutableLoader([
                    new("Secrets:Token", "cipher-text", IsEncrypted: true),
                    new("Feature:Enabled", "true")]),
                options => options.Decryptor = entry => $"plain:{entry.Value}")
            .Build();
        using ((IDisposable)root)
        {
            Assert.Equal("plain:cipher-text", root["Secrets:Token"]);
            Assert.Equal("true", root["Feature:Enabled"]);
        }
    }

    [Fact]
    public void UsesCiphertextWhenNoDecryptorIsConfigured()
    {
        var logger = new RecordingLogger();
        var root = new ConfigurationBuilder()
            .AddDatabase(
                new MutableLoader([new("Secrets:Token", "cipher-text", IsEncrypted: true)]),
                options => options.Logger = logger)
            .Build();
        using ((IDisposable)root)
        {
            Assert.Equal("cipher-text", root["Secrets:Token"]);
            Assert.IsType<InvalidOperationException>(logger.LastException);
        }
    }

    [Fact]
    public void UsesCiphertextWhenDecryptorThrows()
    {
        var root = new ConfigurationBuilder()
            .AddDatabase(
                new MutableLoader([new("Secrets:Token", "cipher-text", IsEncrypted: true)]),
                options => options.Decryptor = _ => throw new CryptographicException("Bad key."))
            .Build();
        using ((IDisposable)root)
        {
            Assert.Equal("cipher-text", root["Secrets:Token"]);
        }
    }

    [Fact]
    public void AllowsConnectionStringKeysAndRejectsDuplicateKeysDuringInitialLoad()
    {
        var root = new ConfigurationBuilder()
            .AddDatabase(new MutableLoader([new("ConnectionStrings:ConfigurationDatabase", "value")]))
            .Build();
        using ((IDisposable)root)
        {
            Assert.Equal("value", root["ConnectionStrings:ConfigurationDatabase"]);
        }

        Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddDatabase(new MutableLoader([new("Case:Key", "one"), new("case:key", "two")]))
            .Build());
    }

    [Fact]
    public async Task ReloadsOnlyWhenSnapshotChangesAndRetainsSnapshotOnFailure()
    {
        var loader = new MutableLoader([new("Feature:Enabled", "false")]);
        var root = new ConfigurationBuilder()
            .AddDatabase(loader, options => options.PollingInterval = TimeSpan.FromMilliseconds(50))
            .Build();
        using var lifetime = (IDisposable)root;

        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = root.GetReloadToken().RegisterChangeCallback(_ => changed.TrySetResult(), null);
        await Task.Delay(130);
        Assert.False(changed.Task.IsCompleted);

        loader.Values = [new("Feature:Enabled", "true")];
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("true", root["Feature:Enabled"]);

        loader.ThrowOnLoad = true;
        await Task.Delay(100);
        Assert.Equal("true", root["Feature:Enabled"]);

        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var recoveryRegistration = root.GetReloadToken().RegisterChangeCallback(_ => recovered.TrySetResult(), null);
        loader.Values = [new("Feature:Enabled", "false")];
        loader.ThrowOnLoad = false;
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("false", root["Feature:Enabled"]);
    }

    [Fact]
    public async Task OptionsMonitorReceivesDatabaseChanges()
    {
        var loader = new MutableLoader([new("Sample:Message", "first")]);
        var root = new ConfigurationBuilder()
            .AddDatabase(loader, options => options.PollingInterval = TimeSpan.FromMilliseconds(40))
            .Build();
        using var lifetime = (IDisposable)root;
        var services = new ServiceCollection();
        services.Configure<MessageOptions>(root.GetSection("Sample"));
        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<MessageOptions>>();
        Assert.Equal("first", monitor.CurrentValue.Message);

        var changed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = monitor.OnChange(options => changed.TrySetResult(options.Message));
        loader.Values = [new("Sample:Message", "second")];

        Assert.Equal("second", await changed.Task.WaitAsync(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public async Task PollingDoesNotOverlapAndStopsWhenConfigurationIsDisposed()
    {
        var loader = new SlowLoader();
        var root = new ConfigurationBuilder()
            .AddDatabase(loader, options => options.PollingInterval = TimeSpan.FromMilliseconds(15))
            .Build();
        var lifetime = (IDisposable)root;
        await Task.Delay(160);
        Assert.Equal(1, loader.MaximumConcurrentLoads);

        lifetime.Dispose();
        var completedAtDispose = loader.CompletedLoads;
        await Task.Delay(100);
        Assert.Equal(completedAtDispose, loader.CompletedLoads);
    }

    [Fact]
    public void ReloadSourceIsInsertedBeforeEnvironmentAndCommandLineSources()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream("{}"u8.ToArray()))
            .AddEnvironmentVariables()
            .AddCommandLine([]);

        builder.AddDatabase(new MutableLoader([]));

        Assert.IsType<DatabaseConfigurationSource>(builder.Sources[1]);
    }

    [Fact]
    public void EnvironmentAndCommandLineValuesOverrideDatabase()
    {
        const string variableName = "LING_CONFIG_TEST_Feature__Enabled";
        var previous = Environment.GetEnvironmentVariable(variableName);
        Environment.SetEnvironmentVariable(variableName, "environment");
        try
        {
            var environmentRoot = new ConfigurationBuilder()
                .AddDatabase(new MutableLoader([new("Feature:Enabled", "database")]))
                .AddEnvironmentVariables("LING_CONFIG_TEST_")
                .Build();
            using ((IDisposable)environmentRoot)
            {
                Assert.Equal("environment", environmentRoot["Feature:Enabled"]);
            }

            var commandLineRoot = new ConfigurationBuilder()
                .AddDatabase(new MutableLoader([new("Feature:Enabled", "database")]))
                .AddCommandLine(["Feature:Enabled=command"])
                .Build();
            using ((IDisposable)commandLineRoot)
            {
                Assert.Equal("command", commandLineRoot["Feature:Enabled"]);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }

    private sealed class MutableLoader(IReadOnlyCollection<ConfigurationEntry> initial) : IDatabaseConfigurationLoader
    {
        public IReadOnlyCollection<ConfigurationEntry> Values { get; set; } = initial;
        public bool ThrowOnLoad { get; set; }

        public IReadOnlyCollection<ConfigurationEntry> Load() => GetValues();

        public ValueTask<IReadOnlyCollection<ConfigurationEntry>> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(GetValues());
        }

        private IReadOnlyCollection<ConfigurationEntry> GetValues()
        {
            if (ThrowOnLoad) throw new InvalidOperationException("Database unavailable.");
            return Values.ToArray();
        }
    }

    private sealed class MessageOptions
    {
        public string? Message { get; set; }
    }

    private sealed class RecordingLogger : ILogger
    {
        public Exception? LastException { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => LastException = exception;
    }

    private sealed class SlowLoader : IDatabaseConfigurationLoader
    {
        private int _active;
        private int _maximumConcurrentLoads;
        private int _completedLoads;

        public int MaximumConcurrentLoads => Volatile.Read(ref _maximumConcurrentLoads);
        public int CompletedLoads => Volatile.Read(ref _completedLoads);

        public IReadOnlyCollection<ConfigurationEntry> Load() => [];

        public async ValueTask<IReadOnlyCollection<ConfigurationEntry>> LoadAsync(CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(ref _active);
            UpdateMaximum(active);
            try
            {
                await Task.Delay(60, cancellationToken);
                Interlocked.Increment(ref _completedLoads);
                return [];
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        private void UpdateMaximum(int active)
        {
            var observed = Volatile.Read(ref _maximumConcurrentLoads);
            while (active > observed)
            {
                var previous = Interlocked.CompareExchange(ref _maximumConcurrentLoads, active, observed);
                if (previous == observed) return;
                observed = previous;
            }
        }
    }
}
