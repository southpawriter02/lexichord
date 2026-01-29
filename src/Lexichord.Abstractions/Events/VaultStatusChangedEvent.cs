using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Vault status levels for status change events.
/// </summary>
public enum VaultStatusLevel
{
    /// <summary>Vault is accessible and contains at least one key.</summary>
    Ready,

    /// <summary>Vault is accessible but contains no keys.</summary>
    Empty,

    /// <summary>Vault encountered an error during access.</summary>
    Error,

    /// <summary>Vault is not available on this platform.</summary>
    Unavailable
}

/// <summary>
/// Published when the secure vault status changes.
/// </summary>
/// <remarks>
/// LOGIC: This event enables cross-module notification of vault availability.
/// Modules that depend on stored secrets can subscribe to know when:
/// - A key is added (Empty → Ready)
/// - All keys are deleted (Ready → Empty)
/// - Vault becomes unavailable (any → Error/Unavailable)
///
/// Handlers should NOT perform long-running operations; use this for UI updates
/// and lightweight state synchronization only.
/// </remarks>
/// <param name="Status">The new vault status level.</param>
/// <param name="Reason">Human-readable description of the status change.</param>
/// <param name="Timestamp">When the status change occurred (UTC).</param>
public sealed record VaultStatusChangedEvent(
    VaultStatusLevel Status,
    string Reason,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new VaultStatusChangedEvent with the current UTC time.
    /// </summary>
    public static VaultStatusChangedEvent Create(VaultStatusLevel status, string reason)
        => new(status, reason, DateTime.UtcNow);
}
