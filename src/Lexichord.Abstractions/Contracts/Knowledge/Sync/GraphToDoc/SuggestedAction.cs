// =============================================================================
// File: SuggestedAction.cs
// Project: Lexichord.Abstractions
// Description: Suggested action for reviewing a flagged document.
// =============================================================================
// LOGIC: Provides actionable suggestions to help users quickly understand
//   what changes may be needed when reviewing flagged documents.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: ActionType
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// A suggested action for document review.
/// </summary>
/// <remarks>
/// <para>
/// When a document is flagged due to graph changes, the system may suggest
/// specific actions to help the user quickly resolve the flag:
/// </para>
/// <list type="bullet">
///   <item><b>ActionType:</b> Category of suggested action.</item>
///   <item><b>Description:</b> Human-readable explanation.</item>
///   <item><b>SuggestedText:</b> Optional replacement text.</item>
///   <item><b>Confidence:</b> How confident the system is in the suggestion.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// if (affectedDocument.SuggestedAction is { Confidence: > 0.8f } action)
/// {
///     Console.WriteLine($"Suggested: {action.Description}");
///     if (action.SuggestedText is not null)
///     {
///         Console.WriteLine($"Replace with: {action.SuggestedText}");
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record SuggestedAction
{
    /// <summary>
    /// Type of suggested action.
    /// </summary>
    /// <value>The category of action suggested.</value>
    /// <remarks>
    /// LOGIC: Determines UI presentation and available quick-actions.
    /// UpdateReferences enables find-replace, AddInformation shows an
    /// insert panel, etc.
    /// </remarks>
    public required ActionType ActionType { get; init; }

    /// <summary>
    /// Human-readable description of the suggestion.
    /// </summary>
    /// <value>Explanation of what action should be taken.</value>
    /// <remarks>
    /// LOGIC: Provides context for the user. Should be concise but
    /// informative enough to understand without viewing full details.
    /// Example: "Update references from 'UserService' to 'AccountService'".
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Optional suggested replacement text.
    /// </summary>
    /// <value>Text to replace existing content, or null if not applicable.</value>
    /// <remarks>
    /// LOGIC: For UpdateReferences actions, this contains the new value
    /// to use. For complex changes, this may be null and the user must
    /// determine the appropriate replacement.
    /// </remarks>
    public string? SuggestedText { get; init; }

    /// <summary>
    /// Confidence score for this suggestion.
    /// </summary>
    /// <value>Value between 0.0 and 1.0 indicating confidence.</value>
    /// <remarks>
    /// LOGIC: Higher values indicate more reliable suggestions.
    /// Below <see cref="GraphToDocSyncOptions.MinActionConfidence"/>,
    /// suggestions may be filtered from display. Default is 0.5 (medium).
    /// </remarks>
    public float Confidence { get; init; } = 0.5f;
}
