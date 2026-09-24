namespace Ling.Configuration.Database;

/// <summary>A single configuration key and value returned by a database source.</summary>
public sealed record ConfigurationEntry(string Key, string? Value);
