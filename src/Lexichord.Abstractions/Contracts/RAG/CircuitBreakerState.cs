// =============================================================================
// File: CircuitBreakerState.cs
// Project: Lexichord.Abstractions
// Description: Mirrors Polly circuit breaker states for external visibility.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the state of the circuit breaker protecting external dependencies.
/// </summary>
/// <remarks>
/// <para>
/// This enum mirrors the Polly <c>CircuitState</c> for use in the abstractions layer
/// without requiring a direct dependency on Polly. The circuit breaker pattern prevents
/// cascading failures by temporarily blocking requests to a failing service.
/// </para>
/// <para>
/// State transitions:
/// <list type="bullet">
///   <item><description><see cref="Closed"/> → <see cref="Open"/>: After threshold failures</description></item>
///   <item><description><see cref="Open"/> → <see cref="HalfOpen"/>: After break duration expires</description></item>
///   <item><description><see cref="HalfOpen"/> → <see cref="Closed"/>: On successful probe</description></item>
///   <item><description><see cref="HalfOpen"/> → <see cref="Open"/>: On probe failure</description></item>
/// </list>
/// </para>
/// </remarks>
public enum CircuitBreakerState
{
    /// <summary>
    /// The circuit is closed and requests flow normally.
    /// This is the healthy state where all operations are permitted.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// The circuit is open and requests are blocked.
    /// This occurs after the failure threshold is exceeded.
    /// Requests will fail fast without attempting the operation.
    /// </summary>
    Open = 1,

    /// <summary>
    /// The circuit is half-open and testing recovery.
    /// A limited number of probe requests are allowed through
    /// to determine if the underlying service has recovered.
    /// </summary>
    HalfOpen = 2
}
