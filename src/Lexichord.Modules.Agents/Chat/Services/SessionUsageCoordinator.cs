// -----------------------------------------------------------------------
// <copyright file="SessionUsageCoordinator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Singleton coordinator for session-level usage tracking.
/// </summary>
/// <remarks>
/// <para>
/// This singleton maintains the accumulated usage across all conversations
/// in the current application session. It is thread-safe and resets when
/// the application restarts.
/// </para>
/// <para>
/// The <see cref="UsageTracker"/> (scoped) delegates session-level accumulation
/// to this coordinator to ensure totals survive conversation changes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
public sealed class SessionUsageCoordinator
{
    private readonly object _lock = new();
    private UsageMetrics _totalUsage = UsageMetrics.Zero;

    /// <summary>
    /// Gets the total usage for the current session.
    /// </summary>
    /// <value>
    /// Accumulated <see cref="UsageMetrics"/> across all conversations
    /// since application start.
    /// </value>
    public UsageMetrics TotalUsage
    {
        get
        {
            lock (_lock)
            {
                return _totalUsage;
            }
        }
    }

    /// <summary>
    /// Adds usage to the session total.
    /// </summary>
    /// <param name="usage">Usage metrics to add.</param>
    /// <remarks>
    /// LOGIC: Thread-safe accumulation using lock to prevent
    /// race conditions when multiple conversations record usage
    /// concurrently.
    /// </remarks>
    public void AddUsage(UsageMetrics usage)
    {
        ArgumentNullException.ThrowIfNull(usage);

        lock (_lock)
        {
            _totalUsage = _totalUsage.Add(usage);
        }
    }

    /// <summary>
    /// Resets the session total.
    /// </summary>
    /// <remarks>
    /// LOGIC: Only called during testing or explicit session reset.
    /// Normal session reset occurs via application restart.
    /// </remarks>
    public void Reset()
    {
        lock (_lock)
        {
            _totalUsage = UsageMetrics.Zero;
        }
    }
}
