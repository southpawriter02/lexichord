// -----------------------------------------------------------------------
// <copyright file="CircuitState.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Represents the current state of a circuit breaker.
/// </summary>
/// <remarks>
/// <para>
/// The circuit breaker pattern prevents an application from repeatedly trying
/// to execute an operation that is likely to fail. It implements a state machine
/// with the following transitions:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Closed"/> → <see cref="Open"/>: After threshold failures within sampling duration</description></item>
///   <item><description><see cref="Open"/> → <see cref="HalfOpen"/>: After break duration elapses</description></item>
///   <item><description><see cref="HalfOpen"/> → <see cref="Closed"/>: After a successful test request</description></item>
///   <item><description><see cref="HalfOpen"/> → <see cref="Open"/>: After a failed test request</description></item>
///   <item><description>Any state → <see cref="Isolated"/>: Manual isolation via policy</description></item>
/// </list>
/// </remarks>
public enum CircuitState
{
    /// <summary>
    /// The circuit is closed and requests flow normally.
    /// </summary>
    /// <remarks>
    /// This is the healthy state where all requests are processed.
    /// The circuit monitors for failures and will transition to
    /// <see cref="Open"/> if the failure threshold is exceeded.
    /// </remarks>
    Closed = 0,

    /// <summary>
    /// The circuit is open and requests are immediately rejected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When open, the circuit breaker rejects all requests immediately
    /// with a <see cref="Polly.CircuitBreaker.BrokenCircuitException"/>
    /// without attempting the underlying operation.
    /// </para>
    /// <para>
    /// After the configured break duration elapses, the circuit
    /// transitions to <see cref="HalfOpen"/> to test if the
    /// underlying service has recovered.
    /// </para>
    /// </remarks>
    Open = 1,

    /// <summary>
    /// The circuit is half-open and testing with the next request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In this state, the circuit allows a limited number of test
    /// requests through to determine if the underlying service has recovered.
    /// </para>
    /// <para>
    /// If the test request succeeds, the circuit transitions back to
    /// <see cref="Closed"/>. If it fails, the circuit transitions back to
    /// <see cref="Open"/> for another break duration.
    /// </para>
    /// </remarks>
    HalfOpen = 2,

    /// <summary>
    /// The circuit has been manually isolated and rejects all requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This state is entered through manual intervention (calling
    /// <c>Isolate()</c> on the policy). Unlike <see cref="Open"/>,
    /// the circuit will not automatically transition to <see cref="HalfOpen"/>.
    /// </para>
    /// <para>
    /// To return to normal operation, <c>Reset()</c> must be called
    /// explicitly on the policy.
    /// </para>
    /// </remarks>
    Isolated = 3
}
