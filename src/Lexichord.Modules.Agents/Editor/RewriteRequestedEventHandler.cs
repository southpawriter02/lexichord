// -----------------------------------------------------------------------
// <copyright file="RewriteRequestedEventHandler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification handler bridging v0.7.3a events to the
//   v0.7.3b command pipeline (v0.7.3b). When the user triggers a rewrite
//   from the context menu, EditorAgentContextMenuProvider publishes a
//   RewriteRequestedEvent. This handler receives it, maps the fields to
//   a RewriteRequest, and delegates to IRewriteCommandHandler.ExecuteAsync.
//
//   Error handling:
//     - MediatR notification handlers should not throw — exceptions from
//       the handler are caught, logged, and swallowed to prevent affecting
//       other notification subscribers.
//
//   Field mapping:
//     RewriteRequestedEvent         →  RewriteRequest
//     ─────────────────────         ─  ──────────────
//     Intent                        →  Intent
//     SelectedText                  →  SelectedText
//     SelectionSpan                 →  SelectionSpan
//     DocumentPath                  →  DocumentPath
//     CustomInstruction             →  CustomInstruction
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Handles <see cref="RewriteRequestedEvent"/> notifications by delegating
/// to the <see cref="IRewriteCommandHandler"/> pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This handler bridges the v0.7.3a event-driven context menu integration
/// with the v0.7.3b command pipeline. When the user selects a rewrite option
/// from the context menu, <see cref="EditorAgentContextMenuProvider"/> publishes
/// a <see cref="RewriteRequestedEvent"/>. This handler maps the event fields
/// to a <see cref="RewriteRequest"/> and invokes the full pipeline.
/// </para>
/// <para>
/// <b>Error Safety:</b> As a MediatR notification handler, this class catches
/// and logs all exceptions rather than rethrowing. This prevents a failure in
/// the rewrite pipeline from affecting other notification subscribers.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public sealed class RewriteRequestedEventHandler : INotificationHandler<RewriteRequestedEvent>
{
    private readonly IRewriteCommandHandler _commandHandler;
    private readonly ILogger<RewriteRequestedEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RewriteRequestedEventHandler"/>.
    /// </summary>
    /// <param name="commandHandler">The rewrite command handler pipeline.</param>
    /// <param name="logger">Logger for diagnostics and error reporting.</param>
    public RewriteRequestedEventHandler(
        IRewriteCommandHandler commandHandler,
        ILogger<RewriteRequestedEventHandler> logger)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a <see cref="RewriteRequestedEvent"/> by executing the rewrite pipeline.
    /// </summary>
    /// <param name="notification">The rewrite requested event from the context menu.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: Maps the event fields to a <see cref="RewriteRequest"/> and delegates
    /// to <see cref="IRewriteCommandHandler.ExecuteAsync"/>. The result is logged but
    /// not returned (MediatR notification handlers are fire-and-forget).
    /// </para>
    /// <para>
    /// Field mapping:
    /// <list type="bullet">
    ///   <item><description><c>Intent</c> → <c>RewriteRequest.Intent</c></description></item>
    ///   <item><description><c>SelectedText</c> → <c>RewriteRequest.SelectedText</c></description></item>
    ///   <item><description><c>SelectionSpan</c> → <c>RewriteRequest.SelectionSpan</c></description></item>
    ///   <item><description><c>DocumentPath</c> → <c>RewriteRequest.DocumentPath</c></description></item>
    ///   <item><description><c>CustomInstruction</c> → <c>RewriteRequest.CustomInstruction</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task Handle(
        RewriteRequestedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling RewriteRequestedEvent: {Intent} for {CharCount} chars at {DocumentPath}",
            notification.Intent,
            notification.SelectedText.Length,
            notification.DocumentPath ?? "(untitled)");

        try
        {
            // LOGIC: Map event fields to RewriteRequest.
            var request = new RewriteRequest
            {
                SelectedText = notification.SelectedText,
                SelectionSpan = notification.SelectionSpan,
                Intent = notification.Intent,
                CustomInstruction = notification.CustomInstruction,
                DocumentPath = notification.DocumentPath
            };

            // LOGIC: Execute the full rewrite pipeline.
            var result = await _commandHandler.ExecuteAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Rewrite completed successfully: {Intent}, {OriginalLength} -> {RewrittenLength} chars in {DurationMs}ms",
                    result.Intent,
                    result.OriginalText.Length,
                    result.RewrittenText.Length,
                    result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Rewrite completed with failure: {Intent}, Error={ErrorMessage}",
                    result.Intent,
                    result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Cancellation is expected (user cancelled or shutdown).
            // Log and return without rethrowing.
            _logger.LogInformation(
                "RewriteRequestedEvent handling cancelled for {Intent}",
                notification.Intent);
        }
        catch (Exception ex)
        {
            // LOGIC: MediatR notification handlers should not throw.
            // Log the error and swallow to prevent affecting other subscribers.
            _logger.LogError(
                ex,
                "Failed to handle RewriteRequestedEvent for {Intent}: {ErrorMessage}",
                notification.Intent,
                ex.Message);
        }
    }
}
