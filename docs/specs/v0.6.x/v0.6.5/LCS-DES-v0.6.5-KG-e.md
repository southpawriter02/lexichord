# LCS-DES-065-KG-e: Validation Orchestrator

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-065-KG-e |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Validation Orchestrator (CKVS Phase 3a) |
| **Estimated Hours** | 6 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Validation Orchestrator** is the central coordination point for all CKVS validation. It receives documents, claims, or content, dispatches work to appropriate validators, and collects results into a unified validation response.

### 1.2 Key Responsibilities

- Receive validation requests (document, claims, streaming)
- Determine which validators to invoke based on options and license
- Dispatch validation work to validators (parallel or sequential)
- Handle validator timeouts and failures gracefully
- Collect and merge results from all validators
- Support streaming validation for real-time feedback

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Validation/
      Orchestration/
        IValidationEngine.cs
        ValidationEngine.cs
        ValidationPipeline.cs
        ValidatorRegistry.cs
```

---

## 2. Interface Definitions

### 2.1 Core Validation Engine

```csharp
namespace Lexichord.KnowledgeGraph.Validation.Orchestration;

/// <summary>
/// Core validation engine for CKVS.
/// Orchestrates all validators to produce unified validation results.
/// </summary>
public interface IValidationEngine
{
    /// <summary>
    /// Validates a document against all CKVS rules.
    /// </summary>
    /// <param name="document">Document to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with all findings.</returns>
    Task<ValidationResult> ValidateDocumentAsync(
        Document document,
        ValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates extracted claims against knowledge base.
    /// </summary>
    /// <param name="claims">Claims to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with claim-specific findings.</returns>
    Task<ValidationResult> ValidateClaimsAsync(
        IReadOnlyList<Claim> claims,
        ValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates generated content before insertion.
    /// </summary>
    /// <param name="content">Generated content to validate.</param>
    /// <param name="context">Generation context for additional info.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result for generated content.</returns>
    Task<ValidationResult> ValidateGeneratedContentAsync(
        string content,
        GenerationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Streams validation results as they're computed.
    /// Useful for real-time validation during typing.
    /// </summary>
    /// <param name="content">Content to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async stream of validation findings.</returns>
    IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        string content,
        ValidationOptions options,
        CancellationToken ct = default);
}
```

### 2.2 Individual Validator Interface

```csharp
/// <summary>
/// Interface for individual validators.
/// Validators are specialized components that check specific rule types.
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Unique validator name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Validator priority (lower = runs first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Required license tier.
    /// </summary>
    LicenseTier RequiredTier { get; }

    /// <summary>
    /// Validates content and produces findings.
    /// </summary>
    /// <param name="context">Validation context with all inputs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation findings from this validator.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Whether this validator supports streaming.
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Streaming validation (optional).
    /// </summary>
    IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        ValidationContext context,
        CancellationToken ct = default);
}
```

### 2.3 Validation Context

```csharp
/// <summary>
/// Context passed to validators containing all validation inputs.
/// </summary>
public record ValidationContext
{
    /// <summary>
    /// Document being validated (if document validation).
    /// </summary>
    public Document? Document { get; init; }

    /// <summary>
    /// Raw content being validated.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Claims to validate.
    /// </summary>
    public IReadOnlyList<Claim> Claims { get; init; } = [];

    /// <summary>
    /// Linked entities in the content.
    /// </summary>
    public IReadOnlyList<LinkedEntity> LinkedEntities { get; init; } = [];

    /// <summary>
    /// Validation options.
    /// </summary>
    public required ValidationOptions Options { get; init; }

    /// <summary>
    /// Generation context (if validating generated content).
    /// </summary>
    public GenerationContext? GenerationContext { get; init; }

    /// <summary>
    /// Workspace for context-specific rules.
    /// </summary>
    public Guid? WorkspaceId { get; init; }
}
```

### 2.4 Validation Options

```csharp
/// <summary>
/// Options controlling validation behavior.
/// </summary>
public record ValidationOptions
{
    /// <summary>
    /// Validation mode (determines validators and timeouts).
    /// </summary>
    public ValidationMode Mode { get; init; } = ValidationMode.Full;

    /// <summary>
    /// Minimum severity to include in results.
    /// </summary>
    public ValidationSeverity MinSeverity { get; init; } = ValidationSeverity.Info;

    /// <summary>
    /// Maximum time for entire validation.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Validators to skip.
    /// </summary>
    public IReadOnlySet<string> SkipValidators { get; init; } = new HashSet<string>();

    /// <summary>
    /// Whether to include fix suggestions.
    /// </summary>
    public bool IncludeFixes { get; init; } = true;

    /// <summary>
    /// Whether to run validators in parallel.
    /// </summary>
    public bool ParallelExecution { get; init; } = true;

    /// <summary>
    /// Maximum findings to return (for performance).
    /// </summary>
    public int MaxFindings { get; init; } = 100;
}

/// <summary>
/// Validation mode controlling which validators run.
/// </summary>
public enum ValidationMode
{
    /// <summary>Real-time as user types (schema only).</summary>
    RealTime,

    /// <summary>On document save (schema + axiom).</summary>
    OnSave,

    /// <summary>Full pre-publish validation (all validators).</summary>
    Full,

    /// <summary>Streaming validation for LLM generation.</summary>
    Streaming
}
```

---

## 3. Data Types

### 3.1 Validation Result

```csharp
/// <summary>
/// Result of a validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Overall validation status.
    /// </summary>
    public ValidationStatus Status { get; init; }

    /// <summary>
    /// All validation findings.
    /// </summary>
    public required IReadOnlyList<ValidationFinding> Findings { get; init; }

    /// <summary>
    /// Validation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of claims validated.
    /// </summary>
    public int ClaimsValidated { get; init; }

    /// <summary>
    /// Number of entities validated.
    /// </summary>
    public int EntitiesValidated { get; init; }

    /// <summary>
    /// Validators that ran.
    /// </summary>
    public IReadOnlyList<string> ValidatorsRun { get; init; } = [];

    /// <summary>
    /// Validators that timed out or failed.
    /// </summary>
    public IReadOnlyList<ValidatorFailure> ValidatorFailures { get; init; } = [];

    /// <summary>
    /// Whether document passed validation.
    /// </summary>
    public bool IsValid => Status == ValidationStatus.Valid;

    /// <summary>
    /// Error count.
    /// </summary>
    public int ErrorCount => Findings.Count(f => f.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Warning count.
    /// </summary>
    public int WarningCount => Findings.Count(f => f.Severity == ValidationSeverity.Warning);
}

/// <summary>
/// Overall validation status.
/// </summary>
public enum ValidationStatus
{
    /// <summary>All validators passed with no errors.</summary>
    Valid,

    /// <summary>Passed but with warnings.</summary>
    ValidWithWarnings,

    /// <summary>Failed with errors.</summary>
    Invalid,

    /// <summary>Validation was cancelled or timed out.</summary>
    Incomplete
}

/// <summary>
/// Records a validator failure.
/// </summary>
public record ValidatorFailure
{
    /// <summary>Validator name.</summary>
    public required string ValidatorName { get; init; }

    /// <summary>Failure reason.</summary>
    public required string Reason { get; init; }

    /// <summary>Exception if any.</summary>
    public Exception? Exception { get; init; }
}
```

### 3.2 Validation Finding

```csharp
/// <summary>
/// A single validation finding.
/// </summary>
public record ValidationFinding
{
    /// <summary>Unique finding ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Validator that produced this finding.</summary>
    public required string ValidatorName { get; init; }

    /// <summary>Finding severity.</summary>
    public ValidationSeverity Severity { get; init; }

    /// <summary>Finding code (e.g., "AXIOM_VIOLATION").</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable message.</summary>
    public required string Message { get; init; }

    /// <summary>Source location in document.</summary>
    public TextSpan? Location { get; init; }

    /// <summary>Related claim (if claim-based).</summary>
    public Claim? RelatedClaim { get; init; }

    /// <summary>Related entity (if entity-based).</summary>
    public KnowledgeEntity? RelatedEntity { get; init; }

    /// <summary>Suggested fix (if available).</summary>
    public ValidationFix? SuggestedFix { get; init; }

    /// <summary>Related axiom (if axiom violation).</summary>
    public Axiom? ViolatedAxiom { get; init; }

    /// <summary>Timestamp when finding was detected.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Additional metadata.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Finding severity levels.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Critical error blocking publication.</summary>
    Error,

    /// <summary>Warning that should be addressed.</summary>
    Warning,

    /// <summary>Informational note.</summary>
    Info,

    /// <summary>Hint or suggestion.</summary>
    Hint
}
```

---

## 4. Implementation

### 4.1 Validator Registry

```csharp
/// <summary>
/// Registry of all available validators.
/// </summary>
public class ValidatorRegistry : IValidatorRegistry
{
    private readonly Dictionary<string, IValidator> _validators = new();
    private readonly ILicenseService _licenseService;
    private readonly ILogger<ValidatorRegistry> _logger;

    public ValidatorRegistry(
        IEnumerable<IValidator> validators,
        ILicenseService licenseService,
        ILogger<ValidatorRegistry> logger)
    {
        _licenseService = licenseService;
        _logger = logger;

        foreach (var validator in validators)
        {
            _validators[validator.Name] = validator;
            _logger.LogDebug("Registered validator: {Name}", validator.Name);
        }
    }

    /// <summary>
    /// Gets validators applicable for the given mode and license.
    /// </summary>
    public IReadOnlyList<IValidator> GetValidatorsForMode(
        ValidationMode mode,
        LicenseTier licenseTier,
        IReadOnlySet<string> skipValidators)
    {
        var applicableValidators = _validators.Values
            .Where(v => !skipValidators.Contains(v.Name))
            .Where(v => v.RequiredTier <= licenseTier)
            .Where(v => IsValidatorApplicableForMode(v, mode))
            .OrderBy(v => v.Priority)
            .ToList();

        return applicableValidators;
    }

    private bool IsValidatorApplicableForMode(IValidator validator, ValidationMode mode)
    {
        return mode switch
        {
            ValidationMode.RealTime => validator.Name == "SchemaValidator",
            ValidationMode.OnSave => validator.Name is "SchemaValidator" or "AxiomValidator",
            ValidationMode.Full => true,
            ValidationMode.Streaming => validator.SupportsStreaming,
            _ => true
        };
    }

    public IValidator? GetValidator(string name)
    {
        return _validators.TryGetValue(name, out var v) ? v : null;
    }
}
```

### 4.2 Validation Engine Implementation

```csharp
/// <summary>
/// Core validation engine implementation.
/// </summary>
public class ValidationEngine : IValidationEngine
{
    private readonly IValidatorRegistry _registry;
    private readonly IResultAggregator _aggregator;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<ValidationEngine> _logger;

    public ValidationEngine(
        IValidatorRegistry registry,
        IResultAggregator aggregator,
        ILicenseService licenseService,
        ILogger<ValidationEngine> logger)
    {
        _registry = registry;
        _aggregator = aggregator;
        _licenseService = licenseService;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateDocumentAsync(
        Document document,
        ValidationOptions options,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var license = await _licenseService.GetCurrentLicenseAsync(ct);

        var context = new ValidationContext
        {
            Document = document,
            Content = document.Content,
            Options = options,
            WorkspaceId = document.WorkspaceId
        };

        var findings = await RunValidatorsAsync(context, license.Tier, ct);
        sw.Stop();

        return _aggregator.Aggregate(findings, sw.Elapsed, options);
    }

    public async Task<ValidationResult> ValidateClaimsAsync(
        IReadOnlyList<Claim> claims,
        ValidationOptions options,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var license = await _licenseService.GetCurrentLicenseAsync(ct);

        var context = new ValidationContext
        {
            Claims = claims,
            Options = options
        };

        var findings = await RunValidatorsAsync(context, license.Tier, ct);
        sw.Stop();

        return _aggregator.Aggregate(findings, sw.Elapsed, options, claimsValidated: claims.Count);
    }

    public async Task<ValidationResult> ValidateGeneratedContentAsync(
        string content,
        GenerationContext genContext,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var license = await _licenseService.GetCurrentLicenseAsync(ct);

        var context = new ValidationContext
        {
            Content = content,
            GenerationContext = genContext,
            Options = new ValidationOptions { Mode = ValidationMode.Streaming }
        };

        var findings = await RunValidatorsAsync(context, license.Tier, ct);
        sw.Stop();

        return _aggregator.Aggregate(findings, sw.Elapsed, context.Options);
    }

    public async IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        string content,
        ValidationOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var license = await _licenseService.GetCurrentLicenseAsync(ct);

        var context = new ValidationContext
        {
            Content = content,
            Options = options with { Mode = ValidationMode.Streaming }
        };

        var validators = _registry.GetValidatorsForMode(
            ValidationMode.Streaming,
            license.Tier,
            options.SkipValidators);

        foreach (var validator in validators.Where(v => v.SupportsStreaming))
        {
            await foreach (var finding in validator.ValidateStreamingAsync(context, ct))
            {
                yield return finding;
            }
        }
    }

    private async Task<List<ValidationFinding>> RunValidatorsAsync(
        ValidationContext context,
        LicenseTier licenseTier,
        CancellationToken ct)
    {
        var validators = _registry.GetValidatorsForMode(
            context.Options.Mode,
            licenseTier,
            context.Options.SkipValidators);

        _logger.LogDebug(
            "Running {Count} validators for mode {Mode}",
            validators.Count,
            context.Options.Mode);

        var findings = new List<ValidationFinding>();
        var failures = new List<ValidatorFailure>();

        if (context.Options.ParallelExecution)
        {
            var tasks = validators.Select(v => RunValidatorWithTimeoutAsync(v, context, ct));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result.Findings != null)
                    findings.AddRange(result.Findings);
                if (result.Failure != null)
                    failures.Add(result.Failure);
            }
        }
        else
        {
            foreach (var validator in validators)
            {
                var result = await RunValidatorWithTimeoutAsync(validator, context, ct);
                if (result.Findings != null)
                    findings.AddRange(result.Findings);
                if (result.Failure != null)
                    failures.Add(result.Failure);
            }
        }

        return findings;
    }

    private async Task<(IReadOnlyList<ValidationFinding>? Findings, ValidatorFailure? Failure)>
        RunValidatorWithTimeoutAsync(
            IValidator validator,
            ValidationContext context,
            CancellationToken ct)
    {
        var perValidatorTimeout = context.Options.Timeout / 3; // Divide timeout among validators

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(perValidatorTimeout);

            var findings = await validator.ValidateAsync(context, cts.Token);

            _logger.LogDebug(
                "Validator {Name} produced {Count} findings",
                validator.Name,
                findings.Count);

            return (findings, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Validator {Name} timed out", validator.Name);
            return (null, new ValidatorFailure
            {
                ValidatorName = validator.Name,
                Reason = "Timeout"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validator {Name} failed", validator.Name);
            return (null, new ValidatorFailure
            {
                ValidatorName = validator.Name,
                Reason = ex.Message,
                Exception = ex
            });
        }
    }
}
```

### 4.3 Validation Pipeline

```csharp
/// <summary>
/// Configures the validation pipeline for different scenarios.
/// </summary>
public class ValidationPipeline
{
    private readonly IValidationEngine _engine;
    private readonly IEntityLinkingService _entityLinker;
    private readonly IClaimExtractionService _claimExtractor;

    public ValidationPipeline(
        IValidationEngine engine,
        IEntityLinkingService entityLinker,
        IClaimExtractionService claimExtractor)
    {
        _engine = engine;
        _entityLinker = entityLinker;
        _claimExtractor = claimExtractor;
    }

    /// <summary>
    /// Full validation pipeline: link entities → extract claims → validate.
    /// </summary>
    public async Task<ValidationResult> RunFullPipelineAsync(
        Document document,
        ValidationOptions options,
        CancellationToken ct = default)
    {
        // Step 1: Link entities
        var linkingResult = await _entityLinker.LinkEntitiesAsync(
            document.Content,
            new LinkingContext { DocumentId = document.Id },
            ct);

        // Step 2: Extract claims
        var claimResult = await _claimExtractor.ExtractClaimsAsync(
            document.Content,
            linkingResult.LinkedEntities,
            new ClaimExtractionContext { DocumentId = document.Id },
            ct);

        // Step 3: Validate everything
        return await _engine.ValidateDocumentAsync(
            document with
            {
                LinkedEntities = linkingResult.LinkedEntities,
                ExtractedClaims = claimResult.Claims
            },
            options,
            ct);
    }
}
```

---

## 5. DI Registration

```csharp
/// <summary>
/// Extension methods for registering validation services.
/// </summary>
public static class ValidationServiceExtensions
{
    public static IServiceCollection AddValidationEngine(
        this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IValidatorRegistry, ValidatorRegistry>();
        services.AddSingleton<IValidationEngine, ValidationEngine>();
        services.AddSingleton<IResultAggregator, ResultAggregator>();

        // Register all validators
        services.AddSingleton<IValidator, SchemaValidator>();
        services.AddSingleton<IValidator, AxiomValidator>();
        services.AddSingleton<IValidator, ConsistencyChecker>();

        // Pipeline
        services.AddScoped<ValidationPipeline>();

        return services;
    }
}
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Validator timeout | Record failure, continue with other validators |
| Validator exception | Log error, record failure, continue |
| Invalid input | Return early with InvalidInput finding |
| License check fails | Skip validators requiring higher tier |
| All validators fail | Return Incomplete status with failures |

---

## 7. Testing Requirements

### 7.1 Unit Tests

| Test Case | Description |
| :-------- | :---------- |
| `ValidateDocument_RunsAllApplicableValidators` | Verifies correct validators invoked |
| `ValidateDocument_RespectsMode` | Real-time only runs schema validator |
| `ValidateDocument_RespectsLicense` | Skips validators above license tier |
| `ValidateDocument_HandlesTimeout` | Records timeout as failure |
| `ValidateDocument_ParallelExecution` | Validators run in parallel |
| `ValidateDocument_SequentialExecution` | Validators run in order |
| `ValidateStreaming_YieldsFindings` | Streaming produces findings incrementally |

### 7.2 Integration Tests

| Test Case | Description |
| :-------- | :---------- |
| `FullPipeline_LinksExtractsValidates` | End-to-end pipeline test |
| `ValidationEngine_WithRealValidators` | Integration with actual validators |

---

## 8. Performance Considerations

- **Parallel Execution:** Run validators concurrently by default
- **Timeout Budgeting:** Divide overall timeout among validators
- **Early Termination:** Stop after MaxFindings reached
- **Caching:** Cache validator results for unchanged content (future)
- **Incremental Validation:** Only validate changed portions (future)

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | ValidationEngine with schema only |
| Teams | Full validation engine |
| Enterprise | Full + custom validators |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
