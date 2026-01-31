# LCS-DES-066-KG-g: Post-Generation Validator

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-066-KG-g |
| **System Breakdown** | LCS-SBD-066-KG |
| **Version** | v0.6.6 |
| **Codename** | Post-Generation Validator (CKVS Phase 3b) |
| **Estimated Hours** | 5 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Post-Generation Validator** validates LLM-generated content against the Knowledge Graph before presenting to users. It catches hallucinations, inconsistencies, and axiom violations in generated output.

### 1.2 Key Responsibilities

- Extract claims from generated content
- Validate claims against knowledge graph
- Detect hallucinations (claims not supported by context)
- Check axiom compliance of generated content
- Suggest fixes for invalid content
- Provide validation status to user

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Copilot/
      Validation/
        IPostGenerationValidator.cs
        PostGenerationValidator.cs
        HallucinationDetector.cs
```

---

## 2. Interface Definitions

### 2.1 Post-Generation Validator Interface

```csharp
namespace Lexichord.KnowledgeGraph.Copilot.Validation;

/// <summary>
/// Validates LLM-generated content against knowledge graph.
/// </summary>
public interface IPostGenerationValidator
{
    /// <summary>
    /// Validates generated content.
    /// </summary>
    Task<PostValidationResult> ValidateAsync(
        string generatedContent,
        KnowledgeContext context,
        CopilotRequest originalRequest,
        CancellationToken ct = default);

    /// <summary>
    /// Validates and optionally fixes generated content.
    /// </summary>
    Task<PostValidationResult> ValidateAndFixAsync(
        string generatedContent,
        KnowledgeContext context,
        CopilotRequest originalRequest,
        CancellationToken ct = default);
}
```

### 2.2 Hallucination Detector Interface

```csharp
/// <summary>
/// Detects potential hallucinations in generated content.
/// </summary>
public interface IHallucinationDetector
{
    /// <summary>
    /// Detects claims not supported by context.
    /// </summary>
    Task<IReadOnlyList<HallucinationFinding>> DetectAsync(
        string content,
        KnowledgeContext context,
        CancellationToken ct = default);
}

/// <summary>
/// A potential hallucination in generated content.
/// </summary>
public record HallucinationFinding
{
    /// <summary>The unsupported claim.</summary>
    public required string ClaimText { get; init; }

    /// <summary>Location in content.</summary>
    public TextSpan? Location { get; init; }

    /// <summary>Confidence this is a hallucination (0-1).</summary>
    public float Confidence { get; init; }

    /// <summary>Type of hallucination.</summary>
    public HallucinationType Type { get; init; }

    /// <summary>Suggested correction.</summary>
    public string? SuggestedCorrection { get; init; }
}

public enum HallucinationType
{
    /// <summary>Entity mentioned but not in context.</summary>
    UnknownEntity,

    /// <summary>Property value contradicts context.</summary>
    ContradictoryValue,

    /// <summary>Relationship not in context.</summary>
    UnsupportedRelationship,

    /// <summary>Fact not verifiable from context.</summary>
    UnverifiableFact
}
```

---

## 3. Data Types

### 3.1 Post-Validation Result

```csharp
/// <summary>
/// Result of post-generation validation.
/// </summary>
public record PostValidationResult
{
    /// <summary>Whether content passed validation.</summary>
    public bool IsValid { get; init; }

    /// <summary>Validation status.</summary>
    public PostValidationStatus Status { get; init; }

    /// <summary>Validation findings.</summary>
    public required IReadOnlyList<ValidationFinding> Findings { get; init; }

    /// <summary>Detected hallucinations.</summary>
    public IReadOnlyList<HallucinationFinding> Hallucinations { get; init; } = [];

    /// <summary>Suggested fixes.</summary>
    public IReadOnlyList<ValidationFix> SuggestedFixes { get; init; } = [];

    /// <summary>Corrected content (if auto-fix applied).</summary>
    public string? CorrectedContent { get; init; }

    /// <summary>Entities verified from context.</summary>
    public IReadOnlyList<KnowledgeEntity> VerifiedEntities { get; init; } = [];

    /// <summary>Claims extracted from content.</summary>
    public IReadOnlyList<Claim> ExtractedClaims { get; init; } = [];

    /// <summary>Validation score (0-1).</summary>
    public float ValidationScore { get; init; }

    /// <summary>Message for user.</summary>
    public string? UserMessage { get; init; }
}

public enum PostValidationStatus
{
    /// <summary>All content verified.</summary>
    Valid,

    /// <summary>Minor issues, still usable.</summary>
    ValidWithWarnings,

    /// <summary>Significant issues found.</summary>
    Invalid,

    /// <summary>Validation could not complete.</summary>
    Inconclusive
}
```

---

## 4. Implementation

### 4.1 Post-Generation Validator

```csharp
public class PostGenerationValidator : IPostGenerationValidator
{
    private readonly IClaimExtractionService _claimExtractor;
    private readonly IEntityLinkingService _entityLinker;
    private readonly IValidationEngine _validationEngine;
    private readonly IHallucinationDetector _hallucinationDetector;
    private readonly ILogger<PostGenerationValidator> _logger;

    public async Task<PostValidationResult> ValidateAsync(
        string generatedContent,
        KnowledgeContext context,
        CopilotRequest originalRequest,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();
        var verifiedEntities = new List<KnowledgeEntity>();

        // 1. Extract entity mentions from generated content
        var linkingResult = await _entityLinker.LinkEntitiesAsync(
            generatedContent,
            new LinkingContext { AvailableEntities = context.Entities },
            ct);

        // 2. Check which entities are verified by context
        foreach (var linkedEntity in linkingResult.LinkedEntities)
        {
            if (linkedEntity.EntityId.HasValue)
            {
                var contextEntity = context.Entities
                    .FirstOrDefault(e => e.Id == linkedEntity.EntityId);

                if (contextEntity != null)
                {
                    verifiedEntities.Add(contextEntity);
                }
            }
        }

        // 3. Extract claims from generated content
        var claimResult = await _claimExtractor.ExtractClaimsAsync(
            generatedContent,
            linkingResult.LinkedEntities,
            new ClaimExtractionContext(),
            ct);

        // 4. Validate claims against context
        var validationResult = await _validationEngine.ValidateClaimsAsync(
            claimResult.Claims,
            new ValidationOptions { Mode = ValidationMode.Full },
            ct);

        findings.AddRange(validationResult.Findings);

        // 5. Detect hallucinations
        var hallucinations = await _hallucinationDetector.DetectAsync(
            generatedContent, context, ct);

        // 6. Compute validation score
        var score = ComputeValidationScore(
            claimResult.Claims.Count,
            findings.Count,
            hallucinations.Count,
            verifiedEntities.Count);

        // 7. Determine status
        var status = DetermineStatus(findings, hallucinations, score);

        return new PostValidationResult
        {
            IsValid = status == PostValidationStatus.Valid,
            Status = status,
            Findings = findings,
            Hallucinations = hallucinations,
            SuggestedFixes = GenerateFixes(findings, hallucinations),
            VerifiedEntities = verifiedEntities,
            ExtractedClaims = claimResult.Claims,
            ValidationScore = score,
            UserMessage = GenerateUserMessage(status, findings.Count, hallucinations.Count)
        };
    }

    public async Task<PostValidationResult> ValidateAndFixAsync(
        string generatedContent,
        KnowledgeContext context,
        CopilotRequest originalRequest,
        CancellationToken ct = default)
    {
        var result = await ValidateAsync(generatedContent, context, originalRequest, ct);

        if (result.IsValid || result.SuggestedFixes.Count == 0)
        {
            return result;
        }

        // Apply auto-fixable corrections
        var corrected = generatedContent;
        foreach (var fix in result.SuggestedFixes.Where(f => f.CanAutoApply))
        {
            if (fix.ReplaceSpan != null && fix.ReplacementText != null)
            {
                corrected = ApplyFix(corrected, fix);
            }
        }

        return result with { CorrectedContent = corrected };
    }

    private float ComputeValidationScore(
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

    private PostValidationStatus DetermineStatus(
        IReadOnlyList<ValidationFinding> findings,
        IReadOnlyList<HallucinationFinding> hallucinations)
    {
        var errorCount = findings.Count(f => f.Severity == ValidationSeverity.Error);
        var highConfidenceHallucinations = hallucinations.Count(h => h.Confidence > 0.8f);

        if (errorCount > 0 || highConfidenceHallucinations > 0)
            return PostValidationStatus.Invalid;

        if (findings.Count > 0 || hallucinations.Count > 0)
            return PostValidationStatus.ValidWithWarnings;

        return PostValidationStatus.Valid;
    }

    private IReadOnlyList<ValidationFix> GenerateFixes(
        IReadOnlyList<ValidationFinding> findings,
        IReadOnlyList<HallucinationFinding> hallucinations)
    {
        var fixes = new List<ValidationFix>();

        // Add fixes from validation findings
        fixes.AddRange(findings
            .Where(f => f.SuggestedFix != null)
            .Select(f => f.SuggestedFix!));

        // Create fixes for hallucinations
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

    private string ApplyFix(string content, ValidationFix fix)
    {
        if (fix.ReplaceSpan == null) return content;

        return content.Substring(0, fix.ReplaceSpan.Start) +
               (fix.ReplacementText ?? "") +
               content.Substring(fix.ReplaceSpan.End);
    }

    private string GenerateUserMessage(
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
```

### 4.2 Hallucination Detector

```csharp
public class HallucinationDetector : IHallucinationDetector
{
    private readonly IEntityRecognizer _entityRecognizer;

    public async Task<IReadOnlyList<HallucinationFinding>> DetectAsync(
        string content,
        KnowledgeContext context,
        CancellationToken ct = default)
    {
        var findings = new List<HallucinationFinding>();

        // Get context entity names for comparison
        var contextEntityNames = context.Entities
            .Select(e => e.Name.ToLowerInvariant())
            .ToHashSet();

        var contextPropertyValues = context.Entities
            .SelectMany(e => e.Properties.Values)
            .Where(v => v != null)
            .Select(v => v!.ToString()!.ToLowerInvariant())
            .ToHashSet();

        // Recognize entities in content
        var mentions = await _entityRecognizer.RecognizeAsync(
            content, new RecognitionContext(), ct);

        foreach (var mention in mentions.Mentions)
        {
            // Check if entity exists in context
            var mentionLower = mention.Text.ToLowerInvariant();
            var isInContext = contextEntityNames.Contains(mentionLower) ||
                              contextPropertyValues.Contains(mentionLower);

            if (!isInContext && mention.Confidence > 0.7f)
            {
                findings.Add(new HallucinationFinding
                {
                    ClaimText = mention.Text,
                    Location = mention.Span,
                    Confidence = 0.6f + (mention.Confidence - 0.7f),
                    Type = HallucinationType.UnknownEntity,
                    SuggestedCorrection = FindClosestMatch(mention.Text, contextEntityNames)
                });
            }
        }

        // Detect contradictory values
        findings.AddRange(DetectContradictions(content, context));

        return findings;
    }

    private IEnumerable<HallucinationFinding> DetectContradictions(
        string content,
        KnowledgeContext context)
    {
        // Simple pattern-based contradiction detection
        foreach (var entity in context.Entities)
        {
            foreach (var prop in entity.Properties)
            {
                if (prop.Value == null) continue;

                var pattern = $@"{entity.Name}\s+\w+\s+{prop.Key}[:\s]+(\w+)";
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    var foundValue = match.Groups[1].Value;
                    var expectedValue = prop.Value.ToString();

                    if (!string.Equals(foundValue, expectedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new HallucinationFinding
                        {
                            ClaimText = match.Value,
                            Location = new TextSpan { Start = match.Index, End = match.Index + match.Length },
                            Confidence = 0.8f,
                            Type = HallucinationType.ContradictoryValue,
                            SuggestedCorrection = match.Value.Replace(foundValue, expectedValue)
                        };
                    }
                }
            }
        }
    }

    private string? FindClosestMatch(string text, HashSet<string> candidates)
    {
        var textLower = text.ToLowerInvariant();
        var closest = candidates
            .Select(c => (Candidate: c, Distance: LevenshteinDistance(textLower, c)))
            .Where(x => x.Distance <= 3)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return closest.Candidate;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var matrix = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            matrix[i, j] = Math.Min(
                Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                matrix[i - 1, j - 1] + cost);
        }

        return matrix[a.Length, b.Length];
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Claim extraction fails | Return inconclusive |
| Validation timeout | Return partial results |
| Empty content | Return valid (nothing to check) |

---

## 6. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `Validate_CleanContent_ReturnsValid` | Valid content passes |
| `Validate_Hallucination_Detected` | Unknown entity caught |
| `Validate_Contradiction_Detected` | Value conflict caught |
| `ValidateAndFix_AppliesCorrections` | Auto-fix works |

---

## 7. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Basic validation |
| Teams | Full + hallucination detection |
| Enterprise | Full + auto-fix |

---

## 8. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
