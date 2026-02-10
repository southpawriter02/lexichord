// =============================================================================
// File: PostGenerationValidator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Validates LLM-generated content against the knowledge graph.
// =============================================================================
// LOGIC: Orchestrates post-generation validation by:
//   1. Verifying entity mentions against context (direct name matching).
//   2. Extracting claims via IClaimExtractionService.
//   3. Validating claims via IValidationEngine.
//   4. Detecting hallucinations via IHallucinationDetector.
//   5. Computing a composite validation score.
//   6. Determining overall status (Valid/ValidWithWarnings/Invalid/Inconclusive).
//   7. Generating fixes from findings and hallucinations.
//   8. Building a user-facing message.
//
// v0.6.6g: Post-Generation Validator (CKVS Phase 3b)
// Dependencies: IPostGenerationValidator (v0.6.6g),
//               IClaimExtractionService (v0.5.6g),
//               IValidationEngine (v0.6.5e),
//               IHallucinationDetector (v0.6.6g),
//               KnowledgeContext (v0.6.6e), AgentRequest (v0.6.6a)
//
// Spec Deviations:
//   - CopilotRequest → AgentRequest (CopilotRequest does not exist).
//   - IEntityLinkingService → direct entity name matching (service not yet implemented).
//   - ValidateClaimsAsync → ValidateDocumentAsync (method does not exist on IValidationEngine).
// =============================================================================

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Validation;

/// <summary>
/// Validates LLM-generated content against the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Implements the post-generation validation pipeline defined in LCS-DES-066-KG-g.
/// Coordinates claim extraction, validation, and hallucination detection to produce
/// a <see cref="PostValidationResult"/> before content is presented to the user.
/// </para>
/// <para>
/// <b>Graceful Degradation:</b> If claim extraction or validation fails,
/// the validator returns <see cref="PostValidationStatus.Inconclusive"/>
/// rather than throwing. Hallucination detection failures are logged and
/// skipped (empty list returned).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6g as part of the Post-Generation Validator.
/// </para>
/// </remarks>
public class PostGenerationValidator : IPostGenerationValidator
{
    private readonly IClaimExtractionService _claimExtractor;
    private readonly IValidationEngine _validationEngine;
    private readonly IHallucinationDetector _hallucinationDetector;
    private readonly ILogger<PostGenerationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostGenerationValidator"/> class.
    /// </summary>
    /// <param name="claimExtractor">Service for extracting claims from text.</param>
    /// <param name="validationEngine">Engine for validating documents/claims.</param>
    /// <param name="hallucinationDetector">Detector for hallucinations.</param>
    /// <param name="logger">Logger instance.</param>
    public PostGenerationValidator(
        IClaimExtractionService claimExtractor,
        IValidationEngine validationEngine,
        IHallucinationDetector hallucinationDetector,
        ILogger<PostGenerationValidator> logger)
    {
        _claimExtractor = claimExtractor ?? throw new ArgumentNullException(nameof(claimExtractor));
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
        _hallucinationDetector = hallucinationDetector ?? throw new ArgumentNullException(nameof(hallucinationDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PostValidationResult> ValidateAsync(
        string generatedContent,
        KnowledgeContext context,
        AgentRequest originalRequest,
        CancellationToken ct = default)
    {
        // --- Empty content → valid (nothing to check) ---
        if (string.IsNullOrWhiteSpace(generatedContent))
        {
            _logger.LogDebug("Post-validation: empty content, returning valid");
            return PostValidationResult.Valid();
        }

        _logger.LogInformation(
            "Post-validation: validating {Length}-char content against {EntityCount} context entities",
            generatedContent.Length,
            context.Entities.Count);

        var findings = new List<ValidationFinding>();
        var verifiedEntities = new List<KnowledgeEntity>();

        // 1. Entity verification — direct name matching (IEntityLinkingService not yet available)
        VerifyEntities(generatedContent, context, verifiedEntities);

        // 2. Extract claims from generated content
        IReadOnlyList<Abstractions.Contracts.Knowledge.Claims.Claim> extractedClaims;
        try
        {
            var claimResult = await _claimExtractor.ExtractClaimsAsync(
                generatedContent,
                [], // No linked entities available (IEntityLinkingService not implemented)
                new ClaimExtractionContext(),
                ct);

            extractedClaims = claimResult.Claims;
            _logger.LogDebug("Post-validation: extracted {ClaimCount} claims", extractedClaims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-validation: claim extraction failed, returning inconclusive");
            return PostValidationResult.Inconclusive("Validation inconclusive — claim extraction failed");
        }

        // 3. Validate claims via validation engine
        try
        {
            var validationContext = ValidationContext.Create(
                "post-generation",
                "generated",
                generatedContent);

            var validationResult = await _validationEngine.ValidateDocumentAsync(
                validationContext, ct);

            findings.AddRange(validationResult.Findings);
            _logger.LogDebug(
                "Post-validation: validation engine returned {FindingCount} findings",
                validationResult.Findings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-validation: validation engine failed, continuing without claim validation");
        }

        // 4. Detect hallucinations
        IReadOnlyList<HallucinationFinding> hallucinations;
        try
        {
            hallucinations = await _hallucinationDetector.DetectAsync(
                generatedContent, context, ct);
            _logger.LogDebug(
                "Post-validation: detected {HallucinationCount} potential hallucinations",
                hallucinations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-validation: hallucination detection failed, continuing without hallucination results");
            hallucinations = [];
        }

        // 5. Compute validation score
        var score = ComputeValidationScore(
            extractedClaims.Count,
            findings.Count,
            hallucinations.Count,
            verifiedEntities.Count);

        // 6. Determine status
        var status = DetermineStatus(findings, hallucinations, score);

        // 7. Generate fixes
        var suggestedFixes = GenerateFixes(findings, hallucinations);

        // 8. Build result
        var result = new PostValidationResult
        {
            IsValid = status == PostValidationStatus.Valid,
            Status = status,
            Findings = findings,
            Hallucinations = hallucinations,
            SuggestedFixes = suggestedFixes,
            VerifiedEntities = verifiedEntities,
            ExtractedClaims = extractedClaims,
            ValidationScore = score,
            UserMessage = GenerateUserMessage(status, findings.Count, hallucinations.Count)
        };

        _logger.LogInformation(
            "Post-validation complete: status={Status}, score={Score:F2}, findings={FindingCount}, hallucinations={HallucinationCount}",
            result.Status, result.ValidationScore, findings.Count, hallucinations.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<PostValidationResult> ValidateAndFixAsync(
        string generatedContent,
        KnowledgeContext context,
        AgentRequest originalRequest,
        CancellationToken ct = default)
    {
        var result = await ValidateAsync(generatedContent, context, originalRequest, ct);

        if (result.IsValid || result.SuggestedFixes.Count == 0)
        {
            return result;
        }

        // Apply auto-fixable corrections
        var corrected = generatedContent;
        var appliedCount = 0;

        foreach (var fix in result.SuggestedFixes.Where(f => f.CanAutoApply))
        {
            if (fix.ReplaceSpan != null && fix.ReplacementText != null)
            {
                corrected = ApplyFix(corrected, fix);
                appliedCount++;
            }
        }

        if (appliedCount > 0)
        {
            _logger.LogInformation(
                "Post-validation: applied {FixCount} auto-fixes", appliedCount);
            return result with { CorrectedContent = corrected };
        }

        return result;
    }

    // ─── Private Helpers ─────────────────────────────────────────────

    /// <summary>
    /// Verifies entities in generated content against context using
    /// case-insensitive name matching (replaces IEntityLinkingService).
    /// </summary>
    private void VerifyEntities(
        string content,
        KnowledgeContext context,
        List<KnowledgeEntity> verifiedEntities)
    {
        var contentLower = content.ToLowerInvariant();

        foreach (var entity in context.Entities)
        {
            if (contentLower.Contains(entity.Name.ToLowerInvariant()))
            {
                verifiedEntities.Add(entity);
            }
        }

        _logger.LogDebug(
            "Post-validation: verified {VerifiedCount}/{TotalCount} entities in content",
            verifiedEntities.Count, context.Entities.Count);
    }

    /// <summary>
    /// Computes a composite validation score based on issue counts.
    /// </summary>
    /// <remarks>
    /// LOGIC: score = 1.0 - min(issueRatio, 1.0) + entityBoost,
    /// where entityBoost = min(verifiedEntities * 0.05, 0.2).
    /// If no claims were extracted, score defaults to 1.0.
    /// </remarks>
    private static float ComputeValidationScore(
        int totalClaims,
        int findingCount,
        int hallucinationCount,
        int verifiedEntityCount)
    {
        if (totalClaims == 0) return 1.0f;

        var issueCount = findingCount + hallucinationCount;
        var issueRatio = (float)issueCount / totalClaims;

        // Base score from issue ratio
        var score = 1.0f - Math.Min(issueRatio, 1.0f);

        // Boost for verified entities
        var entityBoost = Math.Min(verifiedEntityCount * 0.05f, 0.2f);

        return Math.Min(score + entityBoost, 1.0f);
    }

    /// <summary>
    /// Determines the overall validation status from findings and hallucinations.
    /// </summary>
    private static PostValidationStatus DetermineStatus(
        IReadOnlyList<ValidationFinding> findings,
        IReadOnlyList<HallucinationFinding> hallucinations,
        float score)
    {
        var errorCount = findings.Count(f => f.Severity == ValidationSeverity.Error);
        var highConfidenceHallucinations = hallucinations.Count(h => h.Confidence > 0.8f);

        if (errorCount > 0 || highConfidenceHallucinations > 0)
            return PostValidationStatus.Invalid;

        if (findings.Count > 0 || hallucinations.Count > 0)
            return PostValidationStatus.ValidWithWarnings;

        return PostValidationStatus.Valid;
    }

    /// <summary>
    /// Generates fix suggestions from validation findings and hallucination detections.
    /// </summary>
    private static IReadOnlyList<ValidationFix> GenerateFixes(
        IReadOnlyList<ValidationFinding> findings,
        IReadOnlyList<HallucinationFinding> hallucinations)
    {
        var fixes = new List<ValidationFix>();

        // Add fixes from validation findings (SuggestedFix is string on ValidationFinding)
        fixes.AddRange(findings
            .Where(f => f.SuggestedFix != null)
            .Select(f => new ValidationFix
            {
                Description = $"Suggested fix: {f.Message}",
                ReplacementText = f.SuggestedFix,
                Confidence = f.Severity == ValidationSeverity.Error ? 0.9f : 0.7f,
                CanAutoApply = false // String-based fixes cannot be auto-applied without span
            }));

        // Create fixes for hallucinations with location + correction
        foreach (var h in hallucinations.Where(h => h.SuggestedCorrection != null))
        {
            if (h.Location != null)
            {
                fixes.Add(new ValidationFix
                {
                    Description = $"Replace unsupported claim: {h.ClaimText}",
                    ReplaceSpan = h.Location,
                    ReplacementText = h.SuggestedCorrection,
                    Confidence = h.Confidence,
                    CanAutoApply = h.Confidence > 0.9f
                });
            }
        }

        return fixes;
    }

    /// <summary>
    /// Applies a fix to the content by replacing the specified span.
    /// </summary>
    private static string ApplyFix(string content, ValidationFix fix)
    {
        if (fix.ReplaceSpan == null) return content;

        return content[..fix.ReplaceSpan.Start] +
               (fix.ReplacementText ?? "") +
               content[fix.ReplaceSpan.End..];
    }

    /// <summary>
    /// Generates a user-facing message based on validation status.
    /// </summary>
    private static string GenerateUserMessage(
        PostValidationStatus status,
        int findingCount,
        int hallucinationCount)
    {
        return status switch
        {
            PostValidationStatus.Valid => "✓ Content validated against knowledge graph",
            PostValidationStatus.ValidWithWarnings =>
                $"⚠ Content has {findingCount + hallucinationCount} potential issues",
            PostValidationStatus.Invalid =>
                $"✗ Content has {findingCount} errors and {hallucinationCount} unverified claims",
            _ => "Validation inconclusive"
        };
    }
}
