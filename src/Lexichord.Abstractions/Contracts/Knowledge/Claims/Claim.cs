// =============================================================================
// File: Claim.cs
// Project: Lexichord.Abstractions
// Description: Main claim record representing a subject-predicate-object assertion.
// =============================================================================
// LOGIC: The core record type for the Claim Extraction pipeline. A claim is
//   a machine-readable assertion extracted from prose, following a triple
//   structure (subject-predicate-object) that can be validated against
//   axioms and stored in the knowledge graph.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: ClaimEntity, ClaimObject, ClaimEvidence, ClaimValidationStatus,
//               ClaimValidationMessage, ClaimRelation (all v0.5.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// A claim extracted from text representing a subject-predicate-object assertion.
/// </summary>
/// <remarks>
/// <para>
/// A claim is the fundamental unit of structured knowledge extracted from prose.
/// It follows a triple structure:
/// </para>
/// <list type="bullet">
///   <item><b>Subject:</b> The entity performing or having the relationship
///     (see <see cref="Subject"/>).</item>
///   <item><b>Predicate:</b> The relationship type
///     (see <see cref="Predicate"/> and <see cref="ClaimPredicate"/>).</item>
///   <item><b>Object:</b> The entity or value the relationship points to
///     (see <see cref="Object"/>).</item>
/// </list>
/// <para>
/// <b>Example Claims:</b>
/// <list type="bullet">
///   <item>"GET /users ACCEPTS limit" — Endpoint accepts parameter.</item>
///   <item>"limit HAS_DEFAULT 10" — Parameter has default value.</item>
///   <item>"AuthService REQUIRES DatabaseService" — Service dependency.</item>
/// </list>
/// </para>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item>Extraction: Claim is created with <see cref="ClaimValidationStatus.Pending"/>.</item>
///   <item>Entity Linking: Subject and object are linked to graph entities.</item>
///   <item>Validation: Claim is validated against axioms.</item>
///   <item>Storage: Valid claims are persisted to the claim store.</item>
/// </list>
/// </para>
/// <para>
/// <b>License:</b> WriterPro tier required for type access; Teams tier for full functionality.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var claim = new Claim
/// {
///     Subject = ClaimEntity.Unresolved("/api/users", "Endpoint", 4, 14),
///     Predicate = ClaimPredicate.ACCEPTS,
///     Object = ClaimObject.FromEntity(
///         ClaimEntity.Unresolved("limit", "Parameter", 23, 28)),
///     Confidence = 0.92f,
///     DocumentId = documentGuid,
///     Evidence = new ClaimEvidence
///     {
///         Sentence = "The /api/users endpoint accepts a limit parameter.",
///         StartOffset = 0,
///         EndOffset = 50,
///         LineNumber = 15,
///         ExtractionMethod = ClaimExtractionMethod.PatternRule,
///         PatternId = "accepts-parameter-v1"
///     }
/// };
///
/// var canonical = claim.ToCanonicalForm();
/// // Returns: "endpoint:/api/users ACCEPTS parameter:limit"
/// </code>
/// </example>
[RequiresLicense(LicenseTier.WriterPro, FeatureCode = "CLM-01")]
public record Claim
{
    /// <summary>
    /// Unique identifier for the claim.
    /// </summary>
    /// <value>A globally unique identifier. Defaults to a new GUID on creation.</value>
    /// <remarks>
    /// LOGIC: Auto-generated on creation. Used as primary key in the
    /// claim store (PostgreSQL). Referenced by <see cref="ClaimRelation"/>.
    /// </remarks>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The subject entity (who/what the claim is about).
    /// </summary>
    /// <value>An entity reference representing the claim's subject.</value>
    /// <remarks>
    /// LOGIC: Typically the actor or primary entity in the assertion.
    /// May be unresolved initially and linked later by the entity linker.
    /// </remarks>
    public required ClaimEntity Subject { get; init; }

    /// <summary>
    /// The predicate (relationship type).
    /// </summary>
    /// <value>
    /// A predicate string, typically from <see cref="ClaimPredicate"/> constants.
    /// Example: "ACCEPTS", "RETURNS", "HAS_PROPERTY".
    /// </value>
    /// <remarks>
    /// LOGIC: Should match a standard predicate for consistency. Custom
    /// predicates are allowed but may not have inverse mappings.
    /// </remarks>
    public required string Predicate { get; init; }

    /// <summary>
    /// The object of the claim (entity or literal).
    /// </summary>
    /// <value>An entity reference or literal value.</value>
    /// <remarks>
    /// LOGIC: Can be an entity (linking to the knowledge graph) or a
    /// literal value (string, number, boolean). Use <see cref="ClaimObject.Type"/>
    /// to discriminate.
    /// </remarks>
    public required ClaimObject Object { get; init; }

    /// <summary>
    /// Confidence score in the claim (0.0-1.0).
    /// </summary>
    /// <value>
    /// A score from 0.0 (low confidence) to 1.0 (certain).
    /// Defaults to 0.0 for unscored claims.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed by the extraction method. Pattern matches typically
    /// have higher confidence than SRL extractions. Used for filtering
    /// and prioritization.
    /// </remarks>
    public float Confidence { get; init; }

    /// <summary>
    /// ID of the project containing this claim.
    /// </summary>
    /// <value>
    /// The project GUID for multi-tenant claim isolation.
    /// May be null for global claims.
    /// </value>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// ID of the source document.
    /// </summary>
    /// <value>The GUID of the document from which the claim was extracted.</value>
    /// <remarks>
    /// LOGIC: Links to the RAG document store. When a document is re-indexed,
    /// its claims are updated.
    /// </remarks>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Evidence linking the claim to its source text.
    /// </summary>
    /// <value>Provenance information including sentence, offset, and extraction method.</value>
    public ClaimEvidence? Evidence { get; init; }

    /// <summary>
    /// Current validation status.
    /// </summary>
    /// <value>The outcome of axiom validation.</value>
    public ClaimValidationStatus ValidationStatus { get; init; } = ClaimValidationStatus.Pending;

    /// <summary>
    /// Validation messages (errors, warnings, info).
    /// </summary>
    /// <value>A list of validation messages from the axiom engine.</value>
    public IReadOnlyList<ClaimValidationMessage> ValidationMessages { get; init; }
        = Array.Empty<ClaimValidationMessage>();

    /// <summary>
    /// Whether the claim has been reviewed by a human.
    /// </summary>
    /// <value>True if a human has verified or corrected this claim.</value>
    public bool IsReviewed { get; init; }

    /// <summary>
    /// Notes from human review.
    /// </summary>
    /// <value>Optional reviewer notes explaining corrections or context.</value>
    public string? ReviewNotes { get; init; }

    /// <summary>
    /// Related claims (derivation, support, contradiction).
    /// </summary>
    /// <value>Relationships to other claims in the store.</value>
    public IReadOnlyList<ClaimRelation> RelatedClaims { get; init; }
        = Array.Empty<ClaimRelation>();

    /// <summary>
    /// Additional metadata as key-value pairs.
    /// </summary>
    /// <value>
    /// Extensible metadata for custom properties. Common keys include
    /// "source_version", "extraction_pipeline_version", "reviewer_id".
    /// </value>
    public IReadOnlyDictionary<string, object> Metadata { get; init; }
        = new Dictionary<string, object>();

    /// <summary>
    /// When the claim was extracted.
    /// </summary>
    /// <value>UTC timestamp of initial extraction.</value>
    public DateTimeOffset ExtractedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the claim was last updated.
    /// </summary>
    /// <value>UTC timestamp of last modification (validation, review, etc.).</value>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the claim is active (not superseded or deleted).
    /// </summary>
    /// <value>True for current claims, false for superseded or deleted claims.</value>
    /// <remarks>
    /// LOGIC: Soft delete pattern. Inactive claims are retained for history
    /// but excluded from queries by default.
    /// </remarks>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Version number for optimistic concurrency.
    /// </summary>
    /// <value>Incremented on each update. Starts at 1.</value>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Converts the claim to a canonical string form.
    /// </summary>
    /// <returns>
    /// A normalized string representation: "type:subject PREDICATE type:object"
    /// </returns>
    /// <remarks>
    /// LOGIC: Used for claim deduplication and comparison. Two claims with
    /// the same canonical form are considered duplicates.
    /// </remarks>
    /// <example>
    /// <code>
    /// var canonical = claim.ToCanonicalForm();
    /// // Returns: "endpoint:/api/users ACCEPTS parameter:limit"
    /// </code>
    /// </example>
    public string ToCanonicalForm()
    {
        var subjectCanonical = $"{Subject.EntityType.ToLowerInvariant()}:{Subject.NormalizedForm}";
        var objectCanonical = Object.ToCanonicalForm();

        if (Object.Type == ClaimObjectType.Entity && Object.Entity is not null)
        {
            objectCanonical = $"{Object.Entity.EntityType.ToLowerInvariant()}:{objectCanonical}";
        }

        return $"{subjectCanonical} {Predicate} {objectCanonical}";
    }
}
