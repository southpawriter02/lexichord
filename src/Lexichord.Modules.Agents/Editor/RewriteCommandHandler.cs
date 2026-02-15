// -----------------------------------------------------------------------
// <copyright file="RewriteCommandHandler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Orchestrates the complete rewrite command pipeline (v0.7.3b).
//   This handler is the central coordinator between the context menu
//   (v0.7.3a), the EditorAgent (LLM invocation), and the applicator
//   (v0.7.3d — document update).
//
//   Pipeline steps:
//     1. Verify license (FeatureCodes.EditorAgent)
//     2. Set IsExecuting = true
//     3. Create linked CancellationTokenSource for Cancel() support
//     4. Publish RewriteStartedEvent via MediatR
//     5. Delegate to IEditorAgent.RewriteAsync / RewriteStreamingAsync
//     6. Delegate to IRewriteApplicator (if registered, v0.7.3d)
//     7. Publish RewriteCompletedEvent via MediatR
//     8. Return result; reset IsExecuting in finally block
//
//   Thread safety:
//     - IsExecuting is not thread-safe (single UI thread expected)
//     - _currentCts is managed exclusively in Execute/Cancel methods
//
//   Forward dependency (v0.7.3d):
//     - IRewriteApplicator is nullable (injected as IRewriteApplicator?)
//     - When null, the handler skips document application and returns
//       the rewrite result without applying it
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Orchestrates the complete rewrite command pipeline from user action to document update.
/// </summary>
/// <remarks>
/// <para>
/// The command handler is the central orchestrator of the Editor Agent pipeline.
/// It enforces license requirements, manages execution state, publishes MediatR
/// events for observability, and coordinates between the <see cref="IEditorAgent"/>
/// and <see cref="IRewriteApplicator"/>.
/// </para>
/// <para>
/// <b>License Gating:</b> The handler checks <see cref="ILicenseContext.IsFeatureEnabled"/>
/// with <see cref="FeatureCodes.EditorAgent"/> before executing. If the feature is not
/// enabled, a failed <see cref="RewriteResult"/> is returned immediately.
/// </para>
/// <para>
/// <b>Applicator (v0.7.3d):</b> The <see cref="IRewriteApplicator"/> is injected as
/// a nullable dependency. Until v0.7.3d provides the concrete implementation, the handler
/// skips document application and returns the rewrite result without applying it.
/// </para>
/// <para>
/// <b>Cancellation:</b> The <see cref="Cancel"/> method cancels the internal
/// <see cref="CancellationTokenSource"/> created during execution. This triggers
/// an <see cref="OperationCanceledException"/> in the agent, which returns a failed
/// <see cref="RewriteResult"/> with "Rewrite cancelled" message.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public sealed class RewriteCommandHandler : IRewriteCommandHandler
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IEditorAgent _editorAgent;
    private readonly IRewriteApplicator? _applicator;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<RewriteCommandHandler> _logger;

    // ── State ───────────────────────────────────────────────────────────
    private CancellationTokenSource? _currentCts;

    /// <summary>
    /// Initializes a new instance of <see cref="RewriteCommandHandler"/>.
    /// </summary>
    /// <param name="editorAgent">The Editor Agent for LLM rewrite operations.</param>
    /// <param name="applicator">
    /// Optional rewrite applicator for document updates (v0.7.3d).
    /// Null until v0.7.3d provides the concrete implementation.
    /// </param>
    /// <param name="mediator">MediatR mediator for publishing lifecycle events.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public RewriteCommandHandler(
        IEditorAgent editorAgent,
        IRewriteApplicator? applicator,
        IMediator mediator,
        ILicenseContext licenseContext,
        ILogger<RewriteCommandHandler> logger)
    {
        _editorAgent = editorAgent ?? throw new ArgumentNullException(nameof(editorAgent));
        _applicator = applicator; // Nullable — provided by v0.7.3d
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsExecuting { get; private set; }

    /// <inheritdoc />
    public async Task<RewriteResult> ExecuteAsync(
        RewriteRequest request,
        CancellationToken ct = default)
    {
        // LOGIC: Step 1 — Verify license before any work.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.EditorAgent))
        {
            _logger.LogWarning(
                "Rewrite attempted without valid license for feature {FeatureCode}",
                FeatureCodes.EditorAgent);

            return RewriteResult.Failed(
                request.SelectedText,
                request.Intent,
                "Writer Pro license required for AI rewriting.",
                TimeSpan.Zero);
        }

        // LOGIC: Step 2 — Track execution state for UI binding.
        IsExecuting = true;
        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            // LOGIC: Step 3 — Publish start event for observability and UI state.
            await _mediator.Publish(
                RewriteStartedEvent.Create(
                    request.Intent,
                    request.SelectedText.Length,
                    request.DocumentPath),
                _currentCts.Token);

            _logger.LogDebug(
                "Published RewriteStartedEvent for {Intent} ({CharCount} chars)",
                request.Intent,
                request.SelectedText.Length);

            // LOGIC: Step 4 — Delegate to EditorAgent for LLM invocation.
            var result = await _editorAgent.RewriteAsync(request, _currentCts.Token);

            // LOGIC: Step 5 — Apply to document if applicator is available (v0.7.3d).
            if (result.Success && _applicator is not null && request.DocumentPath is not null)
            {
                _logger.LogDebug(
                    "Applying rewrite result to document: {DocumentPath}",
                    request.DocumentPath);

                var applied = await _applicator.ApplyRewriteAsync(
                    request.DocumentPath,
                    request.SelectionSpan,
                    result,
                    _currentCts.Token);

                if (!applied)
                {
                    _logger.LogWarning("Failed to apply rewrite to document");

                    result = result with
                    {
                        Success = false,
                        ErrorMessage = "Failed to apply rewrite to document."
                    };
                }
            }
            else if (result.Success && _applicator is null)
            {
                _logger.LogDebug(
                    "IRewriteApplicator not registered (v0.7.3d). Skipping document application.");
            }

            // LOGIC: Step 6 — Publish completion event (always, regardless of success).
            await _mediator.Publish(
                RewriteCompletedEvent.Create(
                    result.Intent,
                    result.Success,
                    result.Usage,
                    result.Duration,
                    result.ErrorMessage),
                CancellationToken.None); // Don't let cancellation prevent completion event

            _logger.LogDebug(
                "Published RewriteCompletedEvent: Success={Success}, Duration={DurationMs}ms",
                result.Success,
                result.Duration.TotalMilliseconds);

            return result;
        }
        finally
        {
            // LOGIC: Always reset execution state in finally block.
            IsExecuting = false;
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RewriteProgressUpdate> ExecuteStreamingAsync(
        RewriteRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // LOGIC: Verify license before streaming.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.EditorAgent))
        {
            _logger.LogWarning(
                "Streaming rewrite attempted without valid license for feature {FeatureCode}",
                FeatureCodes.EditorAgent);

            yield return new RewriteProgressUpdate
            {
                PartialText = string.Empty,
                ProgressPercentage = 0,
                State = RewriteProgressState.Failed,
                StatusMessage = "Writer Pro license required for AI rewriting."
            };
            yield break;
        }

        IsExecuting = true;
        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        RewriteResult? finalResult = null;

        try
        {
            // LOGIC: Publish start event before streaming begins.
            await _mediator.Publish(
                RewriteStartedEvent.Create(
                    request.Intent,
                    request.SelectedText.Length,
                    request.DocumentPath),
                _currentCts.Token);

            await foreach (var update in _editorAgent.RewriteStreamingAsync(
                request, _currentCts.Token))
            {
                yield return update;

                // LOGIC: Capture the final result text when streaming completes.
                if (update.State == RewriteProgressState.Completed)
                {
                    finalResult = new RewriteResult
                    {
                        OriginalText = request.SelectedText,
                        RewrittenText = update.PartialText,
                        Intent = request.Intent,
                        Success = true,
                        ErrorMessage = null,
                        Usage = UsageMetrics.Zero, // Not available in streaming mode
                        Duration = TimeSpan.Zero   // Not tracked in streaming mode
                    };
                }
            }

            // LOGIC: Apply result to document if applicator is available (v0.7.3d).
            if (finalResult?.Success == true && _applicator is not null && request.DocumentPath is not null)
            {
                var applied = await _applicator.ApplyRewriteAsync(
                    request.DocumentPath,
                    request.SelectionSpan,
                    finalResult,
                    _currentCts.Token);

                if (!applied)
                {
                    yield return new RewriteProgressUpdate
                    {
                        PartialText = finalResult.RewrittenText,
                        ProgressPercentage = 100,
                        State = RewriteProgressState.Failed,
                        StatusMessage = "Failed to apply rewrite to document."
                    };
                }
            }

            // LOGIC: Publish completion event after streaming finishes.
            if (finalResult is not null)
            {
                await _mediator.Publish(
                    RewriteCompletedEvent.Create(
                        finalResult.Intent,
                        finalResult.Success,
                        finalResult.Usage,
                        finalResult.Duration,
                        finalResult.ErrorMessage),
                    CancellationToken.None);
            }
        }
        finally
        {
            IsExecuting = false;
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }

    /// <inheritdoc />
    public void Cancel()
    {
        if (_currentCts is not null && !_currentCts.IsCancellationRequested)
        {
            _logger.LogInformation("Cancelling in-progress rewrite");
            _currentCts.Cancel();
        }
    }
}
