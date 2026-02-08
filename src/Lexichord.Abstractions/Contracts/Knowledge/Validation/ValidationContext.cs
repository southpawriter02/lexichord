// =============================================================================
// File: ValidationContext.cs
// Project: Lexichord.Abstractions
// Description: Encapsulates the document and configuration for a validation pass.
// =============================================================================
// LOGIC: Bundles the document being validated (ID, type, content, metadata)
//   with the validation options. Passed to each IValidator so it has full
//   context to perform its checks.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Encapsulates the document and configuration for a validation pass.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ValidationContext"/> is the primary input to each
/// <see cref="IValidator"/>. It bundles the document being validated
/// with the <see cref="ValidationOptions"/> so validators have full
/// context without needing additional service dependencies.
/// </para>
/// <para>
/// <b>Immutability:</b> This type is an immutable record. The
/// <see cref="Metadata"/> dictionary is exposed as read-only to prevent
/// mutation during parallel validator execution.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5e as part of the Validation Orchestrator.
/// </para>
/// </remarks>
/// <param name="DocumentId">
/// The unique identifier of the document being validated.
/// Used for logging and finding attribution.
/// </param>
/// <param name="DocumentType">
/// The type/format of the document (e.g., "markdown", "json", "yaml").
/// Validators may use this to determine applicability.
/// </param>
/// <param name="Content">
/// The raw content of the document being validated.
/// </param>
/// <param name="Metadata">
/// Optional key-value metadata about the document (e.g., author, tags, schema version).
/// Exposed as read-only to prevent mutation during parallel validation.
/// </param>
/// <param name="Options">
/// The validation options controlling this pass (mode, timeout, license tier).
/// </param>
public record ValidationContext(
    string DocumentId,
    string DocumentType,
    string Content,
    IReadOnlyDictionary<string, object> Metadata,
    ValidationOptions Options
)
{
    /// <summary>
    /// Creates a validation context with default options and empty metadata.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="documentType">The document type.</param>
    /// <param name="content">The document content.</param>
    /// <returns>A new <see cref="ValidationContext"/> with default options.</returns>
    public static ValidationContext Create(
        string documentId,
        string documentType,
        string content) =>
        new(
            documentId,
            documentType,
            content,
            new Dictionary<string, object>(),
            ValidationOptions.Default()
        );

    /// <summary>
    /// Creates a validation context with the specified options and empty metadata.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="documentType">The document type.</param>
    /// <param name="content">The document content.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>A new <see cref="ValidationContext"/> with the specified options.</returns>
    public static ValidationContext Create(
        string documentId,
        string documentType,
        string content,
        ValidationOptions options) =>
        new(
            documentId,
            documentType,
            content,
            new Dictionary<string, object>(),
            options
        );
}
