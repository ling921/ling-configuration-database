namespace Ling.Configuration.Database;

/// <summary>
/// Represents one configuration key and value returned by a database source.
/// </summary>
/// <param name="Key">
/// The colon-separated configuration key.
/// </param>
/// <param name="Value">
/// The stored value, or <see langword="null"/>.
/// </param>
/// <param name="IsEncrypted">
/// Indicates whether <paramref name="Value"/> must be decrypted before it
/// enters the configuration snapshot.
/// </param>
public sealed record ConfigurationEntry(string Key, string? Value, bool IsEncrypted = false);
