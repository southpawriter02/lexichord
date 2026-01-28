using System;
using System.Security.Cryptography;
using System.Text;

namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Computes the storage file name for a secret key.
/// </summary>
/// <remarks>
/// LOGIC: Using SHA256 hash avoids issues with:
/// <list type="bullet">
///   <item>Special characters in keys</item>
///   <item>Case sensitivity differences between filesystems</item>
///   <item>Path length limits on Windows</item>
/// </list>
/// </remarks>
internal static class KeyHasher
{
    /// <summary>
    /// Computes a filesystem-safe file name for a secret key.
    /// </summary>
    /// <param name="key">The secret key to hash.</param>
    /// <returns>A 32-character lowercase hex string suitable for use as a filename.</returns>
    /// <remarks>
    /// LOGIC: Uses first 16 bytes of SHA256 hash (32 hex chars) for reasonable uniqueness
    /// while keeping filenames manageable.
    /// </remarks>
    public static string ComputeFileName(string key)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        // Use first 16 bytes (32 hex chars) for reasonable uniqueness
        return Convert.ToHexString(hashBytes.AsSpan(0, 16)).ToLowerInvariant();
    }
}
