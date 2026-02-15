// -----------------------------------------------------------------------
// <copyright file="RewriteRequest.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Encapsulates a user's request to rewrite selected text via the
//   Editor Agent (v0.7.3b). Carries the selected text, rewrite intent,
//   optional custom instruction, document context, and timeout configuration.
//
//   Validation rules (§4.1):
//     - SelectedText must be non-empty and non-whitespace
//     - SelectedText must not exceed 50,000 characters
//     - Custom intent requires a non-empty CustomInstruction
//
//   Token estimation:
//     - Uses a rough heuristic of ~4 characters per token
//     - Used by the pipeline for progress estimation, not for billing
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// A request to rewrite selected text using the Editor Agent.
/// </summary>
/// <remarks>
/// <para>
/// Created by the <see cref="RewriteRequestedEventHandler"/> when bridging
/// <see cref="Events.RewriteRequestedEvent"/> from the v0.7.3a context menu
/// to the v0.7.3b command pipeline.
/// </para>
/// <para>
/// The <see cref="Validate"/> method should be called before passing to
/// <see cref="IEditorAgent.RewriteAsync"/> to fail fast on invalid input.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
public record RewriteRequest
{
    /// <summary>
    /// Maximum allowed length for selected text in characters.
    /// </summary>
    /// <remarks>
    /// LOGIC: 50,000 characters is roughly ~12,500 tokens. Combined with context
    /// and system prompt, this keeps total prompt size well within LLM limits.
    /// </remarks>
    public const int MaxSelectedTextLength = 50_000;

    /// <summary>
    /// Default timeout for LLM response.
    /// </summary>
    /// <remarks>
    /// LOGIC: 30 seconds balances user patience with allowing complex rewrites
    /// of longer text. Can be overridden per-request.
    /// </remarks>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The text selected by the user for rewriting.
    /// </summary>
    public required string SelectedText { get; init; }

    /// <summary>
    /// The span of the selection in the document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used by <see cref="IRewriteApplicator"/> (v0.7.3d) to replace the
    /// correct range in the document after rewriting.
    /// </remarks>
    public required TextSpan SelectionSpan { get; init; }

    /// <summary>
    /// The type of rewrite transformation requested.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps to a specific prompt template via
    /// <see cref="IEditorAgent.GetTemplateId"/>:
    /// <list type="bullet">
    ///   <item><description>Formal → editor-rewrite-formal</description></item>
    ///   <item><description>Simplified → editor-rewrite-simplify</description></item>
    ///   <item><description>Expanded → editor-rewrite-expand</description></item>
    ///   <item><description>Custom → editor-rewrite-custom</description></item>
    /// </list>
    /// </remarks>
    public required RewriteIntent Intent { get; init; }

    /// <summary>
    /// Custom instruction for <see cref="RewriteIntent.Custom"/>.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required when <see cref="Intent"/> is <see cref="RewriteIntent.Custom"/>.
    /// Injected into the <c>editor-rewrite-custom</c> template as <c>{{custom_instruction}}</c>.
    /// </remarks>
    public string? CustomInstruction { get; init; }

    /// <summary>
    /// Path to the document being edited.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for context gathering (surrounding text, style rules) via
    /// <see cref="Abstractions.Agents.Context.IContextOrchestrator"/>.
    /// </remarks>
    public string? DocumentPath { get; init; }

    /// <summary>
    /// Additional context to include in the prompt.
    /// </summary>
    /// <remarks>
    /// LOGIC: Passed as hints to the context orchestrator via
    /// <see cref="Abstractions.Agents.Context.ContextGatheringRequest.Hints"/>.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? AdditionalContext { get; init; }

    /// <summary>
    /// Maximum time to wait for the LLM response.
    /// </summary>
    /// <remarks>
    /// LOGIC: Applied via a linked <see cref="CancellationTokenSource"/> in
    /// <see cref="EditorAgent.RewriteAsync"/>. Defaults to 30 seconds.
    /// </remarks>
    public TimeSpan Timeout { get; init; } = DefaultTimeout;

    /// <summary>
    /// Estimates the token count for this request's selected text.
    /// </summary>
    /// <remarks>
    /// LOGIC: Rough estimate at ~4 characters per token. Used for progress
    /// estimation in streaming mode, not for billing or budget enforcement.
    /// </remarks>
    public int EstimatedTokens => SelectedText.Length / 4 + (CustomInstruction?.Length ?? 0) / 4;

    /// <summary>
    /// Validates the request is properly formed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>SelectedText is null, empty, or whitespace</description></item>
    ///   <item><description>SelectedText exceeds <see cref="MaxSelectedTextLength"/></description></item>
    ///   <item><description>Intent is Custom but CustomInstruction is missing</description></item>
    /// </list>
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SelectedText))
        {
            throw new ArgumentException(
                "Selected text cannot be empty or whitespace.",
                nameof(SelectedText));
        }

        if (SelectedText.Length > MaxSelectedTextLength)
        {
            throw new ArgumentException(
                $"Selected text exceeds maximum length of {MaxSelectedTextLength:N0} characters.",
                nameof(SelectedText));
        }

        if (Intent == RewriteIntent.Custom && string.IsNullOrWhiteSpace(CustomInstruction))
        {
            throw new ArgumentException(
                "Custom instruction is required when intent is Custom.",
                nameof(CustomInstruction));
        }
    }
}
