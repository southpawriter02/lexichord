// =============================================================================
// File: PostValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Result of post-generation validation against the knowledge graph.
// =============================================================================
// LOGIC: Aggregates all outputs from the post-generation validation pipeline:
//   validation findings, hallucination detections, suggested fixes, verified
//   entities, extracted claims, a composite score, and a user-facing message.
//   Immutable record for safe passage across async boundaries.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// Dependencies: ValidationFinding (v0.6.5), HallucinationFinding (v0.6.6g),
//               ValidationFix (v0.6.6g), KnowledgeEntity (v0.4.5e),
//               Claim (v0.5.6e), PostValidationStatus (v0.6.6g)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Result of post-generation validation.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="IPostGenerationValidator.ValidateAsync"/> and
/// <see cref="IPostGenerationValidator.ValidateAndFixAsync"/>. Contains
/// all validation outputs needed for the Co-pilot to decide how to
/// present generated content to the user.
/// </para>
/// <para>
/// <b>Key Properties:</b>
/// <list type="bullet">
///   <item><see cref="IsValid"/> — quick check: <c>true</c> when
///     <see cref="Status"/> is <see cref="PostValidationStatus.Valid"/>.</item>
///   <item><see cref="ValidationScore"/> — composite 0–1 score.</item>
///   <item><see cref="CorrectedContent"/> — populated only by
///     <see cref="IPostGenerationValidator.ValidateAndFixAsync"/>
///     when auto-fixes were applied.</item>
///   <item><see cref="UserMessage"/> — human-readable summary.</item>
/// </list>
/// </para>
/// <para>
/// <b>Immutability:</b> This is an immutable record. Use the <c>with</c>
/// expression to create variants (e.g., adding <see cref="CorrectedContent"/>).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateAsync(content, context, request, ct);
/// if (!result.IsValid)
/// {
///     logger.LogWarning("Validation failed: {Message}", result.UserMessage);
///     foreach (var h in result.Hallucinations)
///     {
///         logger.LogWarning("Hallucination: {Claim}", h.ClaimText);
///     }
/// }
/// </code>
/// </example>
public record PostValidationResult
{
    /// <summary>
    /// Whether content passed validation.
    /// </summary>
    /// <value>
    /// <c>true</c> when <see cref="Status"/> is
    /// <see cref="PostValidationStatus.Valid"/>; otherwise <c>false</c>.
    /// </value>
    public bool IsValid { get; init; }

    /// <summary>
    /// Overall validation status.
    /// </summary>
    /// <value>One of the <see cref="PostValidationStatus"/> values.</value>
    public PostValidationStatus Status { get; init; }

    /// <summary>
    /// Validation findings from the validation engine.
    /// </summary>
    /// <value>
    /// All findings aggregated from claim validation. May be empty if
    /// no issues were found.
    /// </value>
    public required IReadOnlyList<ValidationFinding> Findings { get; init; }

    /// <summary>
    /// Detected hallucinations in the generated content.
    /// </summary>
    /// <value>
    /// Hallucination findings from the <see cref="IHallucinationDetector"/>.
    /// Empty list if no hallucinations were found.
    /// </value>
    public IReadOnlyList<HallucinationFinding> Hallucinations { get; init; } = [];

    /// <summary>
    /// Suggested fixes for validation issues and hallucinations.
    /// </summary>
    /// <value>
    /// Fixes generated from findings and hallucinations. Each fix
    /// describes a text replacement. Fixes with
    /// <see cref="ValidationFix.CanAutoApply"/> = <c>true</c> can be
    /// applied automatically.
    /// </value>
    public IReadOnlyList<ValidationFix> SuggestedFixes { get; init; } = [];

    /// <summary>
    /// Corrected content after auto-fixes were applied.
    /// </summary>
    /// <value>
    /// The content with auto-applicable fixes applied, or <c>null</c>
    /// if no auto-fixes were applied or validation used
    /// <see cref="IPostGenerationValidator.ValidateAsync"/>.
    /// </value>
    public string? CorrectedContent { get; init; }

    /// <summary>
    /// Entities from the content that were verified against context.
    /// </summary>
    /// <value>
    /// Knowledge entities from <see cref="KnowledgeContext.Entities"/>
    /// that were confirmed to appear in the generated content.
    /// </value>
    public IReadOnlyList<KnowledgeEntity> VerifiedEntities { get; init; } = [];

    /// <summary>
    /// Claims extracted from the generated content.
    /// </summary>
    /// <value>
    /// Claims extracted by the <see cref="IClaimExtractionService"/>
    /// from the generated content. Used for claim validation.
    /// </value>
    public IReadOnlyList<Claim> ExtractedClaims { get; init; } = [];

    /// <summary>
    /// Composite validation score (0–1).
    /// </summary>
    /// <value>
    /// A score from 0.0 (many issues) to 1.0 (fully validated).
    /// Computed from issue ratio with a boost for verified entities.
    /// </value>
    /// <remarks>
    /// LOGIC: <c>score = 1.0 - min(issueRatio, 1.0) + entityBoost</c>,
    /// where issueRatio = (findings + hallucinations) / totalClaims,
    /// and entityBoost = min(verifiedEntities * 0.05, 0.2).
    /// </remarks>
    public float ValidationScore { get; init; }

    /// <summary>
    /// User-facing validation message.
    /// </summary>
    /// <value>
    /// A human-readable summary such as "✓ Content validated against
    /// knowledge graph" or "✗ Content has 2 errors and 1 unverified claims".
    /// </value>
    public string? UserMessage { get; init; }

    /// <summary>
    /// Creates a valid result with no findings.
    /// </summary>
    /// <returns>A <see cref="PostValidationResult"/> indicating valid content.</returns>
    public static PostValidationResult Valid() => new()
    {
        IsValid = true,
        Status = PostValidationStatus.Valid,
        Findings = [],
        ValidationScore = 1.0f,
        UserMessage = "✓ Content validated against knowledge graph"
    };

    /// <summary>
    /// Creates an inconclusive result (validation could not complete).
    /// </summary>
    /// <param name="reason">Human-readable reason for the failure.</param>
    /// <returns>
    /// A <see cref="PostValidationResult"/> with
    /// <see cref="PostValidationStatus.Inconclusive"/> status.
    /// </returns>
    public static PostValidationResult Inconclusive(string reason) => new()
    {
        IsValid = false,
        Status = PostValidationStatus.Inconclusive,
        Findings = [],
        ValidationScore = 0.0f,
        UserMessage = reason
    };
}
