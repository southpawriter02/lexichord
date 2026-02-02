// =============================================================================
// File: SourceDocumentItemViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model representing a source document in the Entity Detail View.
// =============================================================================
// LOGIC: Provides a display-ready representation of a source document that
//   mentions an entity, with navigation support to open the document in the editor.
//
// v0.4.7f: Entity Detail View (Knowledge Graph Browser)
// Dependencies: None (pure view model record)
// =============================================================================

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model representing a source document in the Entity Detail View.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SourceDocumentItemViewModel"/> wraps a source document reference
/// with display-ready values for the Source Documents section of the Entity Detail View.
/// Includes navigation support to open the document in the editor.
/// </para>
/// <para>
/// <b>Provenance Tracking:</b> This view model represents the link between a
/// <see cref="KnowledgeEntity"/> and the indexed documents that reference it.
/// The <see cref="MentionCount"/> indicates how many times the entity is mentioned.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
/// </para>
/// </remarks>
public sealed record SourceDocumentItemViewModel
{
    /// <summary>
    /// Gets the document's unique identifier.
    /// </summary>
    /// <value>
    /// The GUID of the source document in the RAG index.
    /// Used for navigation when the user clicks on the document.
    /// </value>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Gets the document title.
    /// </summary>
    /// <value>
    /// The document's display title, typically derived from the filename
    /// or document metadata.
    /// </value>
    public string Title { get; init; } = "";

    /// <summary>
    /// Gets the document's file path.
    /// </summary>
    /// <value>
    /// The relative path to the document within the workspace.
    /// Used for opening the document in the editor.
    /// </value>
    public string Path { get; init; } = "";

    /// <summary>
    /// Gets the number of times this entity is mentioned in the document.
    /// </summary>
    /// <value>
    /// The mention count. A higher count indicates the entity is more
    /// prominently featured in this document.
    /// </value>
    public int MentionCount { get; init; }
}
