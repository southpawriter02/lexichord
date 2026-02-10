// -----------------------------------------------------------------------
// <copyright file="UsageTracker.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Events;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Chat.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Service for tracking and accumulating agent usage metrics.
/// </summary>
/// <remarks>
/// <para>
/// The UsageTracker maintains running totals of token consumption and costs
/// for the current conversation and session. It provides:
/// </para>
/// <list type="bullet">
///   <item>Real-time usage accumulation</item>
///   <item>Event publication for telemetry</item>
///   <item>Monthly summary access (Teams only)</item>
///   <item>Usage persistence for historical tracking</item>
/// </list>
/// <para>
/// This service has scoped lifetime to maintain per-conversation state.
/// Session totals are tracked via a singleton <see cref="SessionUsageCoordinator"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Recording usage after agent invocation
/// _usageTracker.RecordUsageAsync(response.Usage, new InvocationContext(
///     AgentId: agent.AgentId,
///     Model: modelId,
///     Duration: stopwatch.Elapsed,
///     Streamed: wasStreamed
/// ));
///
/// // Getting current totals
/// var conversationTotal = _usageTracker.ConversationUsage;
/// var sessionTotal = _usageTracker.SessionUsage;
/// </code>
/// </example>
public sealed class UsageTracker : IDisposable
{
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly UsageRepository _usageRepository;
    private readonly SessionUsageCoordinator _sessionCoordinator;
    private readonly ILogger<UsageTracker> _logger;

    private readonly object _lock = new();
    private UsageMetrics _conversationUsage = UsageMetrics.Zero;
    private Guid _currentConversationId;
    private string _currentModel = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="UsageTracker"/>.
    /// </summary>
    /// <param name="mediator">MediatR mediator for event publishing.</param>
    /// <param name="licenseContext">License context for tier checks.</param>
    /// <param name="usageRepository">Repository for usage persistence.</param>
    /// <param name="sessionCoordinator">Singleton session coordinator.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public UsageTracker(
        IMediator mediator,
        ILicenseContext licenseContext,
        UsageRepository usageRepository,
        SessionUsageCoordinator sessionCoordinator,
        ILogger<UsageTracker> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _usageRepository = usageRepository ?? throw new ArgumentNullException(nameof(usageRepository));
        _sessionCoordinator = sessionCoordinator ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the accumulated usage for the current conversation.
    /// </summary>
    /// <value>
    /// Thread-safe snapshot of the current conversation's usage metrics.
    /// </value>
    public UsageMetrics ConversationUsage
    {
        get
        {
            lock (_lock)
            {
                return _conversationUsage;
            }
        }
    }

    /// <summary>
    /// Gets the accumulated usage for the current session.
    /// </summary>
    /// <value>
    /// Delegates to <see cref="SessionUsageCoordinator.TotalUsage"/>.
    /// </value>
    public UsageMetrics SessionUsage => _sessionCoordinator.TotalUsage;

    /// <summary>
    /// Gets the current conversation ID.
    /// </summary>
    public Guid ConversationId => _currentConversationId;

    /// <summary>
    /// Event raised when usage is recorded.
    /// </summary>
    public event EventHandler<UsageRecordedEventArgs>? UsageRecorded;

    /// <summary>
    /// Initializes tracking for a new conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <remarks>
    /// LOGIC: Resets conversation-level usage while preserving
    /// session totals via <see cref="SessionUsageCoordinator"/>.
    /// </remarks>
    public void StartConversation(Guid conversationId)
    {
        lock (_lock)
        {
            _currentConversationId = conversationId;
            _conversationUsage = UsageMetrics.Zero;
        }

        _logger.LogDebug(
            "Started tracking for conversation: {ConversationId}",
            conversationId);
    }

    /// <summary>
    /// Records usage from an agent invocation.
    /// </summary>
    /// <param name="usage">The usage metrics to record.</param>
    /// <param name="context">Context about the invocation.</param>
    /// <remarks>
    /// LOGIC: Accumulates conversation and session totals, publishes
    /// a MediatR event for telemetry, persists for Teams users, and
    /// notifies UI via the UsageRecorded event.
    /// </remarks>
    public async Task RecordUsageAsync(UsageMetrics usage, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(context);

        // LOGIC: Update conversation total under lock.
        lock (_lock)
        {
            _conversationUsage = _conversationUsage.Add(usage);
            _currentModel = context.Model;
        }

        // LOGIC: Update session totals via singleton coordinator.
        _sessionCoordinator.AddUsage(usage);

        _logger.LogDebug(
            "Recorded usage: {PromptTokens}+{CompletionTokens} tokens, {Cost:C4}",
            usage.PromptTokens, usage.CompletionTokens, usage.EstimatedCost);

        // LOGIC: Publish telemetry event (fire-and-forget, non-blocking).
        var invocationEvent = new AgentInvocationEvent(
            context.AgentId,
            context.Model,
            usage.PromptTokens,
            usage.CompletionTokens,
            context.Duration,
            context.Streamed)
        {
            ConversationId = _currentConversationId
        };

        try
        {
            await _mediator.Publish(invocationEvent);
        }
        catch (Exception ex)
        {
            // LOGIC: Event publishing failures are non-fatal.
            _logger.LogWarning(ex, "Failed to publish AgentInvocationEvent");
        }

        // LOGIC: Persist for monthly summary (Teams only).
        if (_licenseContext.GetCurrentTier() >= LicenseTier.Teams)
        {
            await PersistUsageAsync(usage, context);
        }

        // LOGIC: Notify UI subscribers.
        OnUsageRecorded(usage);
    }

    /// <summary>
    /// Resets the conversation usage totals.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears conversation-level accumulation without
    /// affecting session totals. Notifies UI with zero metrics.
    /// </remarks>
    public void ResetConversation()
    {
        lock (_lock)
        {
            _conversationUsage = UsageMetrics.Zero;
        }

        _logger.LogDebug("Reset conversation usage");
        OnUsageRecorded(UsageMetrics.Zero);
    }

    /// <summary>
    /// Gets the monthly usage summary (Teams only).
    /// </summary>
    /// <param name="month">The month to get summary for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Monthly usage summary.</returns>
    /// <exception cref="LicenseTierException">
    /// Thrown if user lacks Teams license.
    /// </exception>
    public async Task<MonthlyUsageSummary> GetMonthlySummaryAsync(
        DateOnly month,
        CancellationToken ct = default)
    {
        // LOGIC: License enforcement for Teams-only feature.
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            throw new LicenseTierException(
                "Monthly usage summary requires Teams license",
                LicenseTier.Teams);
        }

        return await _usageRepository.GetMonthlySummaryAsync(month, ct);
    }

    /// <summary>
    /// Exports usage history (Teams only).
    /// </summary>
    /// <param name="startDate">Start of export period.</param>
    /// <param name="endDate">End of export period.</param>
    /// <param name="format">Export format (CSV or JSON).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export data stream.</returns>
    /// <exception cref="LicenseTierException">
    /// Thrown if user lacks Teams license.
    /// </exception>
    public async Task<Stream> ExportUsageAsync(
        DateOnly startDate,
        DateOnly endDate,
        ExportFormat format,
        CancellationToken ct = default)
    {
        // LOGIC: License enforcement for Teams-only feature.
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            throw new LicenseTierException(
                "Usage export requires Teams license",
                LicenseTier.Teams);
        }

        return await _usageRepository.ExportAsync(startDate, endDate, format, ct);
    }

    /// <summary>
    /// Persists a usage record for Teams users.
    /// </summary>
    private async Task PersistUsageAsync(UsageMetrics usage, InvocationContext context)
    {
        try
        {
            await _usageRepository.RecordAsync(new UsageRecord
            {
                Timestamp = DateTimeOffset.UtcNow,
                ConversationId = _currentConversationId,
                AgentId = context.AgentId,
                Model = context.Model,
                PromptTokens = usage.PromptTokens,
                CompletionTokens = usage.CompletionTokens,
                EstimatedCost = usage.EstimatedCost,
                Duration = context.Duration,
                Streamed = context.Streamed
            });
        }
        catch (Exception ex)
        {
            // LOGIC: Persistence failures are non-fatal.
            _logger.LogWarning(ex, "Failed to persist usage record");
        }
    }

    /// <summary>
    /// Raises the <see cref="UsageRecorded"/> event.
    /// </summary>
    private void OnUsageRecorded(UsageMetrics usage)
    {
        UsageRecorded?.Invoke(this, new UsageRecordedEventArgs(
            usage,
            ConversationUsage,
            SessionUsage));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // LOGIC: No unmanaged resources to dispose, but implementing
        // IDisposable for proper pattern compliance with scoped lifetime.
    }
}
