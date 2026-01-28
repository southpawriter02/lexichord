using System;

namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Validates a secret key according to naming rules.
/// </summary>
/// <remarks>
/// LOGIC: Keys must be:
/// <list type="bullet">
///   <item>Non-null and non-empty</item>
///   <item>Max 256 characters</item>
///   <item>Printable ASCII only (0x20-0x7E)</item>
///   <item>Colon (:) allowed for namespacing</item>
/// </list>
/// </remarks>
public static class KeyValidator
{
    /// <summary>
    /// Maximum allowed key length in characters.
    /// </summary>
    public const int MaxKeyLength = 256;

    /// <summary>
    /// Validates a secret key according to naming rules.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty, exceeds max length, or contains invalid characters.
    /// </exception>
    public static void ValidateKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty or whitespace.", nameof(key));
        }

        if (key.Length > MaxKeyLength)
        {
            throw new ArgumentException($"Key exceeds maximum length of {MaxKeyLength} characters.", nameof(key));
        }

        foreach (var c in key)
        {
            if (c < 0x20 || c > 0x7E)
            {
                throw new ArgumentException(
                    $"Key contains invalid character at position {key.IndexOf(c)}. Only printable ASCII (0x20-0x7E) is allowed.",
                    nameof(key));
            }
        }
    }
}
