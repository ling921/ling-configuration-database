using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Ling.Configuration.Database;
using Microsoft.Extensions.Configuration;

var entries = new List<(string Key, string? Value, bool IsEncrypted)>
{
    ("Feature:Enabled", "true", false),
    ("Secrets:Token", "cipher", true),
};

var factory = new InMemoryProviderFactory(entries);
var configuration = new ConfigurationBuilder()
    .AddDatabase(
        "InMemory",
        factory,
        options =>
        {
            options.PollingInterval = Timeout.InfiniteTimeSpan;
            options.Decryptor = entry => $"decrypted:{entry.Value}";
        })
    .Build();

try
{
    if (configuration["Feature:Enabled"] != "true"
        || configuration["Secrets:Token"] != "decrypted:cipher")
    {
        return 1;
    }

    Console.WriteLine("Ling.Configuration.Database Native AOT smoke passed.");
    return 0;
}
finally
{
    ((IDisposable)configuration).Dispose();
}

file sealed class InMemoryProviderFactory(IReadOnlyList<(string Key, string? Value, bool IsEncrypted)> entries) : DbProviderFactory
{
    public override DbConnection CreateConnection() => new InMemoryConnection(entries);
}

file sealed class InMemoryConnection(IReadOnlyList<(string Key, string? Value, bool IsEncrypted)> entries) : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "InMemory";
    public override string DataSource => "InMemory";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() => new InMemoryCommand(entries);
}

file sealed class InMemoryCommand(IReadOnlyList<(string Key, string? Value, bool IsEncrypted)> entries) : DbCommand
{
    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
    protected override DbTransaction? DbTransaction { get; set; }
    public override void Cancel() { }
    public override int ExecuteNonQuery() => 0;
    public override object? ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new InMemoryDataReader(entries);
}

file sealed class InMemoryDataReader(IReadOnlyList<(string Key, string? Value, bool IsEncrypted)> entries) : DbDataReader
{
    private int _index = -1;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => throw new NotSupportedException();
    public override int Depth => 0;
    public override int FieldCount => 3;
    public override bool HasRows => entries.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;

    public override bool Read() => ++_index < entries.Count;
    public override bool IsDBNull(int ordinal) => ordinal == 1 && entries[_index].Value is null;

    public override object GetValue(int ordinal) => ordinal switch
    {
        0 => entries[_index].Key,
        1 => entries[_index].Value ?? (object)DBNull.Value,
        2 => entries[_index].IsEncrypted,
        _ => throw new ArgumentOutOfRangeException(nameof(ordinal)),
    };

    public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
    public override byte GetByte(int ordinal) => throw new NotSupportedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override char GetChar(int ordinal) => throw new NotSupportedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override string GetDataTypeName(int ordinal) => string.Empty;
    public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
    public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
    public override double GetDouble(int ordinal) => throw new NotSupportedException();
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
    public override Type GetFieldType(int ordinal) => typeof(object);
    public override float GetFloat(int ordinal) => throw new NotSupportedException();
    public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
    public override short GetInt16(int ordinal) => throw new NotSupportedException();
    public override int GetInt32(int ordinal) => throw new NotSupportedException();
    public override long GetInt64(int ordinal) => throw new NotSupportedException();
    public override string GetName(int ordinal) => string.Empty;
    public override int GetOrdinal(string name) => throw new NotSupportedException();
    public override string GetString(int ordinal) => (string)GetValue(ordinal);
    public override int GetValues(object[] values) => throw new NotSupportedException();
    public override bool NextResult() => false;
    public override IEnumerator GetEnumerator() => throw new NotSupportedException();
}
