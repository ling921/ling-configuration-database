using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Ling.Configuration.Database;

namespace Ling.Configuration.Database.AdoNet;

public sealed class AdoNetDatabaseConfigurationLoader : IDatabaseConfigurationLoader
{
    private readonly AdoNetDatabaseConfigurationOptions _options;

    public AdoNetDatabaseConfigurationLoader(AdoNetDatabaseConfigurationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(_options.ProviderFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.TableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.KeyColumnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ValueColumnName);
        if (_options.EncryptionColumnName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(_options.EncryptionColumnName);
        }
    }

    public IReadOnlyCollection<ConfigurationEntry> Load()
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = CreateCommand(connection);
        using var reader = command.ExecuteReader();
        return ReadEntries(reader);
    }

    public async ValueTask<IReadOnlyCollection<ConfigurationEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var entries = new List<ConfigurationEntry>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture);
            if (key is null)
            {
                throw new InvalidOperationException("The database configuration key column returned NULL.");
            }

            var value = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture);
            var isEncrypted = reader.FieldCount > 2 && !reader.IsDBNull(2)
                && Convert.ToBoolean(reader.GetValue(2), CultureInfo.InvariantCulture);
            entries.Add(new ConfigurationEntry(key, value, isEncrypted));
        }

        return entries;
    }

    private DbConnection CreateConnection()
    {
        var connection = _options.ProviderFactory.CreateConnection()
            ?? throw new InvalidOperationException("The configured provider factory could not create a connection.");
        connection.ConnectionString = _options.ConnectionString;
        return connection;
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        string QuoteIdentifier(string identifier)
        {
            if (!Regex.IsMatch(identifier, @"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant))
            {
                throw new ArgumentException($"Database identifier '{identifier}' is invalid. Use letters, digits, and underscores, or provide a provider-specific IdentifierQuoter.");
            }

            return _options.IdentifierQuoter?.Invoke(identifier) ?? identifier;
        }

        var table = string.Join(".", _options.TableName.Split('.').Select(QuoteIdentifier));
        var key = QuoteIdentifier(_options.KeyColumnName);
        var value = QuoteIdentifier(_options.ValueColumnName);
        var encryption = _options.EncryptionColumnName is null
            ? string.Empty
            : ", " + QuoteIdentifier(_options.EncryptionColumnName);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {key}, {value}{encryption} FROM {table}";
        return command;
    }

    private static IReadOnlyCollection<ConfigurationEntry> ReadEntries(DbDataReader reader)
    {
        var entries = new List<ConfigurationEntry>();
        while (reader.Read())
        {
            var key = Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture);
            if (key is null)
            {
                throw new InvalidOperationException("The database configuration key column returned NULL.");
            }

            var value = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture);
            var isEncrypted = reader.FieldCount > 2 && !reader.IsDBNull(2)
                && Convert.ToBoolean(reader.GetValue(2), CultureInfo.InvariantCulture);
            entries.Add(new ConfigurationEntry(key, value, isEncrypted));
        }

        return entries;
    }
}
