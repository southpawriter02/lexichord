namespace Lexichord.Abstractions.Constants;

/// <summary>
/// Constants for vault key names used throughout Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: Keys follow a namespace-qualified pattern to prevent collisions:
/// - Format: {module}:{provider}:{purpose}
/// - Example: "lexichord:test-api-key" for the test key used in v0.0.8c
///
/// This pattern mirrors the ISecureVault key naming convention established in v0.0.6.
/// </remarks>
public static class VaultKeys
{
    /// <summary>
    /// Prefix for all Lexichord vault keys.
    /// </summary>
    public const string KeyPrefix = "lexichord:";

    /// <summary>
    /// The test API key for validating vault functionality (v0.0.8c).
    /// </summary>
    /// <remarks>
    /// LOGIC: This key is used by IVaultStatusService to verify vault accessibility.
    /// The value stored is for testing purposes only and is not used for real API calls.
    /// </remarks>
    public const string TestApiKey = "lexichord:test-api-key";
}
