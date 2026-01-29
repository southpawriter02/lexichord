using Lexichord.Abstractions.Messaging;

namespace Lexichord.Host.Commands;

/// <summary>
/// Command to create a new document in the application.
/// </summary>
/// <param name="Title">The title of the document (required, max 200 characters).</param>
/// <param name="Content">The content of the document (optional).</param>
/// <param name="Tags">Optional tags for categorization (max 10 tags, each max 50 characters).</param>
/// <param name="WordCountGoal">Optional target word count (must be positive if specified).</param>
/// <remarks>
/// DESIGN: This command demonstrates validation patterns for:
/// - Required string fields with length constraints
/// - Optional fields with conditional validation
/// - Collection validation with item-level rules
/// - Custom validation logic (e.g., word count range)
///
/// This is a sample command for v0.0.7d demonstration purposes.
/// </remarks>
public sealed record CreateDocumentCommand(
    string Title,
    string? Content,
    IReadOnlyList<string>? Tags,
    int? WordCountGoal) : ICommand<CreateDocumentResult>;

/// <summary>
/// Result of creating a document.
/// </summary>
/// <param name="DocumentId">The unique identifier of the created document.</param>
/// <param name="Title">The title of the created document.</param>
/// <param name="CreatedAt">The timestamp when the document was created.</param>
public sealed record CreateDocumentResult(
    Guid DocumentId,
    string Title,
    DateTimeOffset CreatedAt);
