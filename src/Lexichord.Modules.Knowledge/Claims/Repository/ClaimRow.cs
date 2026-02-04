// =============================================================================
// File: ClaimRow.cs
// Project: Lexichord.Modules.Knowledge
// Description: Database row entity for claims table.
// =============================================================================
// LOGIC: DTO for Dapper mapping between Claim domain objects and the
//   claims PostgreSQL table. Uses snake_case column naming.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

namespace Lexichord.Modules.Knowledge.Claims.Repository;

/// <summary>
/// Database row representation for the claims table.
/// </summary>
/// <remarks>
/// <para>
/// Maps between the <see cref="Claim"/> domain object and the PostgreSQL
/// claims table using snake_case column names.
/// </para>
/// </remarks>
internal sealed class ClaimRow
{
    /// <summary>Unique claim identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Project that owns this claim.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Document from which claim was extracted.</summary>
    public Guid DocumentId { get; init; }

    #region Subject

    /// <summary>Subject entity ID if resolved.</summary>
    public Guid? SubjectEntityId { get; init; }

    /// <summary>Subject entity type (e.g., "method", "class").</summary>
    public string SubjectEntityType { get; init; } = "";

    /// <summary>Subject surface form from source text.</summary>
    public string SubjectSurfaceForm { get; init; } = "";

    /// <summary>Subject normalized form (lowercase).</summary>
    public string SubjectNormalizedForm { get; init; } = "";

    /// <summary>Subject start offset in document.</summary>
    public int SubjectStartOffset { get; init; }

    /// <summary>Subject end offset in document.</summary>
    public int SubjectEndOffset { get; init; }

    #endregion

    #region Predicate

    /// <summary>Predicate type (e.g., ACCEPTS, RETURNS).</summary>
    public string Predicate { get; init; } = "";

    #endregion

    #region Object

    /// <summary>Object type: 'Entity' or 'Literal'.</summary>
    public string ObjectType { get; init; } = "";

    /// <summary>Object entity ID if resolved.</summary>
    public Guid? ObjectEntityId { get; init; }

    /// <summary>Object entity type (if entity).</summary>
    public string? ObjectEntityType { get; init; }

    /// <summary>Object surface form (if entity).</summary>
    public string? ObjectSurfaceForm { get; init; }

    /// <summary>Object literal value (if literal).</summary>
    public string? ObjectLiteralValue { get; init; }

    /// <summary>Object literal type (if literal): string, int, float, bool.</summary>
    public string? ObjectLiteralType { get; init; }

    /// <summary>Object unit (if literal has unit).</summary>
    public string? ObjectUnit { get; init; }

    #endregion

    #region Confidence & Validation

    /// <summary>Confidence score 0.0-1.0.</summary>
    public float Confidence { get; init; }

    /// <summary>Validation status: Pending, Valid, Invalid, etc.</summary>
    public string ValidationStatus { get; init; } = "Pending";

    /// <summary>Validation messages as JSON array.</summary>
    public string? ValidationMessagesJson { get; init; }

    /// <summary>Whether claim has been reviewed by human.</summary>
    public bool IsReviewed { get; init; }

    /// <summary>Review notes from human reviewer.</summary>
    public string? ReviewNotes { get; init; }

    #endregion

    #region Extraction

    /// <summary>Extraction method: Pattern, Dependency, SRL, Manual.</summary>
    public string ExtractionMethod { get; init; } = "";

    /// <summary>Pattern ID if extracted via pattern.</summary>
    public string? PatternId { get; init; }

    #endregion

    #region Evidence

    /// <summary>Source sentence text.</summary>
    public string EvidenceSentence { get; init; } = "";

    /// <summary>Evidence start offset in document.</summary>
    public int EvidenceStartOffset { get; init; }

    /// <summary>Evidence end offset in document.</summary>
    public int EvidenceEndOffset { get; init; }

    /// <summary>Section heading if available.</summary>
    public string? EvidenceSection { get; init; }

    #endregion

    #region Metadata

    /// <summary>Additional metadata as JSON.</summary>
    public string? MetadataJson { get; init; }

    /// <summary>Whether claim is active (not soft-deleted).</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Version number for optimistic concurrency.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Extraction timestamp.</summary>
    public DateTimeOffset ExtractedAt { get; init; }

    /// <summary>Last update timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    #endregion
}
