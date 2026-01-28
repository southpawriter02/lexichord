namespace Lexichord.Abstractions.Events;

/// <summary>
/// Specifies the type of content that was created, modified, or deleted.
/// </summary>
/// <remarks>
/// LOGIC: This enumeration allows handlers to filter events based on
/// content type. Not all handlers care about all content types.
///
/// Example: RAG indexing might handle Documents but skip Templates.
/// </remarks>
public enum ContentType
{
    /// <summary>
    /// A standalone document (manuscript, article, story).
    /// </summary>
    Document = 0,

    /// <summary>
    /// A chapter within a project.
    /// </summary>
    Chapter = 1,

    /// <summary>
    /// A project containing multiple chapters/documents.
    /// </summary>
    Project = 2,

    /// <summary>
    /// A quick note or snippet.
    /// </summary>
    Note = 3,

    /// <summary>
    /// A reusable document template.
    /// </summary>
    Template = 4,

    /// <summary>
    /// A style guide or writing guidelines document.
    /// </summary>
    StyleGuide = 5,

    /// <summary>
    /// Character profile or worldbuilding element.
    /// </summary>
    WorldbuildingElement = 6,

    /// <summary>
    /// Research material or reference document.
    /// </summary>
    Reference = 7
}
