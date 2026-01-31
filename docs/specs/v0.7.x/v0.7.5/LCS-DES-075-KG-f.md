# LCS-DES-075-KG-f: Design Specification — Issue Aggregator

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `VAL-075-KG-f` | Unified validation sub-part f |
| **Feature Name** | `Issue Aggregator` | Combines results from all validators |
| **Target Version** | `v0.7.5f` | Sixth sub-part of v0.7.5-KG |
| **Module Scope** | `Lexichord.Modules.Validation` | Validation module |
| **Swimlane** | `Validation` | Core validation vertical |
| **License Tier** | `Core` | Available across all tiers |
| **Feature Gate Key** | `FeatureFlags.Validation.UnifiedIssues` | Unified validation feature |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-075-KG](./LCS-SBD-075-KG.md) | Unified Validation scope |
| **Scope Breakdown** | [LCS-SBD-075-KG S3.1](./LCS-SBD-075-KG.md#31-sub-parts) | f = Aggregator |

---

## 2. Executive Summary

### 2.1 The Requirement

The unified validation system needs a service that:

1. **Invokes multiple validators** in parallel: Style Linter, Grammar Linter, CKVS Validation Engine
2. **Normalizes results** to UnifiedIssue format (from spec 075-KG-e)
3. **Deduplicates** issues when multiple validators detect the same problem
4. **Normalizes severity** across validators
5. **Applies filters** based on license tier and user preferences
6. **Returns combined result** with grouping and metadata

Current approach: Each validator runs independently, consumers must integrate results manually. This creates complexity and inconsistency in UI and fix workflows.

### 2.2 The Proposed Solution

Implement `IUnifiedValidationService` that:

1. Calls all registered validators in parallel
2. Converts each validator's result to UnifiedIssue records using factories from 075-KG-e
3. Detects and marks duplicates (same location/code across validators)
4. Filters issues by license tier
5. Returns `UnifiedValidationResult` with aggregated data

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IStyleLinter` | v0.3.x | Style violation detection |
| `IGrammarLinter` | v0.3.x | Grammar error detection |
| `IValidationEngine` | v0.6.5-KG | Knowledge/axiom validation |
| `ILicenseContext` | v0.1.x | License tier checking |
| `UnifiedIssue` | v0.7.5e (this spec suite) | Normalized issue type |
| `UnifiedValidationResult` | v0.7.5e | Result container |
| `ILogger` | .NET | Observability |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Microsoft.Extensions.Logging` | 8.x | Logging framework |
| `MediatR` | 12.x | Event publishing |

### 3.2 Licensing Behavior

- **Load Behavior:** Soft Gate
  - Aggregator loads for all license tiers
  - Core tier: Returns only Style issues
  - WriterPro tier: Returns Style + Grammar + Schema issues
  - Teams/Enterprise tier: Returns all issues including Knowledge
  - Method checks `ILicenseContext` at entry point

---

## 4. Data Contract (The API)

### 4.1 Primary Interface

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Service that aggregates validation results from multiple independent validators
/// into a unified set of issues with consistent severity and fix information.
/// </summary>
public interface IUnifiedValidationService
{
    /// <summary>
    /// Validates a document using all available validators.
    /// Results are aggregated, deduplicated, and returned in unified format.
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">Validation options (filters, parallelism, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Combined validation result from all validators.</returns>
    Task<UnifiedValidationResult> ValidateAsync(
        Document document,
        UnifiedValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a specific range in the document.
    /// Useful for incremental validation during editing.
    /// </summary>
    /// <param name="document">The document containing the range.</param>
    /// <param name="range">The text range to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result for the range only.</returns>
    Task<UnifiedValidationResult> ValidateRangeAsync(
        Document document,
        TextSpan range,
        UnifiedValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the cached validation result for a document if available.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cached result or null if not cached.</returns>
    Task<UnifiedValidationResult?> GetCachedResultAsync(
        string documentPath,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates cached validation result for a document.
    /// Called when document content or rules change.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    void InvalidateCache(string documentPath);

    /// <summary>
    /// Invalidates all cached validation results.
    /// Called when global rules or configuration changes.
    /// </summary>
    void InvalidateAllCaches();

    /// <summary>
    /// Event raised when validation completes.
    /// </summary>
    event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;
}
```

### 4.2 Options Record

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Configuration options for unified validation.
/// </summary>
public record UnifiedValidationOptions
{
    /// <summary>
    /// Whether to run Style Linter.
    /// </summary>
    public bool IncludeStyleLinter { get; init; } = true;

    /// <summary>
    /// Whether to run Grammar Linter.
    /// </summary>
    public bool IncludeGrammarLinter { get; init; } = true;

    /// <summary>
    /// Whether to run CKVS Validation Engine.
    /// </summary>
    public bool IncludeValidationEngine { get; init; } = true;

    /// <summary>
    /// Minimum severity level to include in results.
    /// Issues below this severity are filtered out.
    /// </summary>
    public UnifiedSeverity MinimumSeverity { get; init; } = UnifiedSeverity.Hint;

    /// <summary>
    /// Whether to cache results.
    /// </summary>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// Cache TTL in milliseconds.
    /// </summary>
    public int CacheTtlMs { get; init; } = 300_000; // 5 minutes

    /// <summary>
    /// Whether to enable deduplication across validators.
    /// </summary>
    public bool EnableDeduplication { get; init; } = true;

    /// <summary>
    /// Maximum number of issues to return.
    /// 0 = unlimited.
    /// </summary>
    public int MaxIssuesPerDocument { get; init; } = 1000;

    /// <summary>
    /// Whether to run validators in parallel.
    /// </summary>
    public bool ParallelValidation { get; init; } = true;

    /// <summary>
    /// Timeout per validator in milliseconds.
    /// </summary>
    public int ValidatorTimeoutMs { get; init; } = 30_000; // 30 seconds

    /// <summary>
    /// Filter by issue category.
    /// Null = no filter, include all categories.
    /// </summary>
    public IReadOnlyList<IssueCategory>? FilterByCategory { get; init; }

    /// <summary>
    /// Whether to include fix suggestions.
    /// </summary>
    public bool IncludeFixes { get; init; } = true;
}
```

### 4.3 Result Record

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Combined validation result from all validators.
/// </summary>
public record UnifiedValidationResult
{
    /// <summary>
    /// Path to the validated document.
    /// </summary>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// All detected issues from all validators.
    /// </summary>
    public required IReadOnlyList<UnifiedIssue> Issues { get; init; }

    /// <summary>
    /// Total validation duration across all validators.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Timestamp when validation completed.
    /// </summary>
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether this result was retrieved from cache.
    /// </summary>
    public bool IsCached { get; init; }

    /// <summary>
    /// Validation options that produced this result.
    /// Useful for cache validation.
    /// </summary>
    public UnifiedValidationOptions OptionsHash { get; init; }

    /// <summary>
    /// Issues grouped by category.
    /// </summary>
    public IReadOnlyDictionary<IssueCategory, IReadOnlyList<UnifiedIssue>> ByCategory =>
        Issues
            .GroupBy(i => i.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UnifiedIssue>)g.ToList());

    /// <summary>
    /// Issues grouped by severity.
    /// </summary>
    public IReadOnlyDictionary<UnifiedSeverity, IReadOnlyList<UnifiedIssue>> BySeverity =>
        Issues
            .GroupBy(i => i.Severity)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UnifiedIssue>)g.ToList());

    /// <summary>
    /// Count of issues by severity.
    /// </summary>
    public IReadOnlyDictionary<UnifiedSeverity, int> CountBySeverity =>
        Issues
            .GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Total number of issues.
    /// </summary>
    public int TotalIssueCount => Issues.Count;

    /// <summary>
    /// Number of errors (blocking).
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == UnifiedSeverity.Error);

    /// <summary>
    /// Number of warnings (should fix).
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == UnifiedSeverity.Warning);

    /// <summary>
    /// Number of auto-fixable issues.
    /// </summary>
    public int AutoFixableCount => Issues.Count(i => i.Fix?.CanAutoApply == true);

    /// <summary>
    /// Whether the document can be published (no errors).
    /// </summary>
    public bool CanPublish => !Issues.Any(i => i.Severity == UnifiedSeverity.Error);

    /// <summary>
    /// Detailed validation trace (Debug level only).
    /// Maps validator name to its result details.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ValidatorDetails { get; init; }
}
```

### 4.4 Event Args

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Event arguments for validation completion.
/// </summary>
public class ValidationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Path to the validated document.
    /// </summary>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// The validation result.
    /// </summary>
    public required UnifiedValidationResult Result { get; init; }

    /// <summary>
    /// Which validators completed successfully.
    /// </summary>
    public IReadOnlyList<string> SuccessfulValidators { get; init; } = new List<string>();

    /// <summary>
    /// Validators that failed or timed out.
    /// Maps validator name to exception message.
    /// </summary>
    public IReadOnlyDictionary<string, string> FailedValidators { get; init; } =
        new Dictionary<string, string>();
}
```

---

## 5. Deduplication Strategy

### 5.1 Duplicate Detection Logic

Two issues are considered duplicates if:

1. **Same location:** `issue1.Location == issue2.Location` (within 1 character tolerance)
2. **Same or related code:** Code prefix match (e.g., "STYLE_001" matches "STYLE_001_ALT")
3. **Similar message:** Edit distance < 0.2 of message length
4. **Different validator:** Not from the same source validator

### 5.2 Duplicate Handling

When duplicates detected:

1. Keep the highest severity version
2. Mark all but primary as `IsDuplicate = true`
3. Set `DuplicateOf` field to primary's ID
4. Preserve original severity for audit
5. Combine fix suggestions if applicable

### 5.3 Example

```
Style Linter finds: "Line 5: Passive voice detected"
Grammar Linter finds: "Line 5: 'was done' is passive voice"

Result: One unified issue marked as duplicate
```

---

## 6. Implementation

### 6.1 Aggregator Service Implementation

```csharp
namespace Lexichord.Modules.Validation;

/// <summary>
/// Service that aggregates validation results from multiple validators.
/// </summary>
public class UnifiedValidationService : IUnifiedValidationService
{
    private readonly IStyleLinter _styleLinter;
    private readonly IGrammarLinter _grammarLinter;
    private readonly IValidationEngine _validationEngine;
    private readonly ILicenseContext _licenseContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UnifiedValidationService> _logger;
    private readonly SemaphoreSlim _validationLock = new(1, 1);

    public UnifiedValidationService(
        IStyleLinter styleLinter,
        IGrammarLinter grammarLinter,
        IValidationEngine validationEngine,
        ILicenseContext licenseContext,
        IMemoryCache cache,
        ILogger<UnifiedValidationService> logger)
    {
        _styleLinter = styleLinter ?? throw new ArgumentNullException(nameof(styleLinter));
        _grammarLinter = grammarLinter ?? throw new ArgumentNullException(nameof(grammarLinter));
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UnifiedValidationResult> ValidateAsync(
        Document document,
        UnifiedValidationOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug("Starting unified validation for: {Path}", document.Path);

        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Check license
        var tier = await _licenseContext.GetCurrentTierAsync(ct);
        _logger.LogDebug("License tier: {Tier}", tier);

        // Try cache
        if (options.EnableCaching)
        {
            var cached = await GetCachedResultAsync(document.Path, ct);
            if (cached != null && CacheIsValid(cached, options))
            {
                _logger.LogDebug("Returning cached validation result for: {Path}", document.Path);
                return cached with { IsCached = true };
            }
        }

        // Run validation with lock to prevent concurrent validations
        await _validationLock.WaitAsync(ct);
        try
        {
            var tasks = new List<Task<List<UnifiedIssue>>>();
            var validatorNames = new List<string>();

            // Collect validator tasks based on options and license
            if (options.IncludeStyleLinter && IsAvailableForTier(tier, "StyleLinter"))
            {
                validatorNames.Add("Style Linter");
                tasks.Add(RunStyleLinterAsync(document, options, ct));
            }

            if (options.IncludeGrammarLinter && IsAvailableForTier(tier, "GrammarLinter"))
            {
                validatorNames.Add("Grammar Linter");
                tasks.Add(RunGrammarLinterAsync(document, options, ct));
            }

            if (options.IncludeValidationEngine && IsAvailableForTier(tier, "ValidationEngine"))
            {
                validatorNames.Add("CKVS Validation Engine");
                tasks.Add(RunValidationEngineAsync(document, options, ct));
            }

            // Run in parallel if enabled
            var results = options.ParallelValidation
                ? await Task.WhenAll(tasks)
                : await RunSequentiallyAsync(tasks, ct);

            // Combine and deduplicate
            var allIssues = results.SelectMany(r => r).ToList();
            var deduplicatedIssues = options.EnableDeduplication
                ? DeduplicateIssues(allIssues)
                : allIssues;

            // Apply filters
            var filteredIssues = ApplyFilters(deduplicatedIssues, options, tier);

            // Limit result size
            if (options.MaxIssuesPerDocument > 0)
            {
                filteredIssues = filteredIssues.Take(options.MaxIssuesPerDocument).ToList();
            }

            stopwatch.Stop();

            var result = new UnifiedValidationResult
            {
                DocumentPath = document.Path,
                Issues = filteredIssues.AsReadOnly(),
                Duration = stopwatch.Elapsed,
                ValidatedAt = startTime,
                IsCached = false,
                OptionsHash = options
            };

            // Cache result
            if (options.EnableCaching)
            {
                var cacheKey = GetCacheKey(document.Path, options);
                _cache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromMilliseconds(options.CacheTtlMs));
            }

            _logger.LogInformation(
                "Validation completed for {Path}: {Count} issues in {ElapsedMs}ms",
                document.Path,
                result.TotalIssueCount,
                stopwatch.ElapsedMilliseconds);

            // Raise event
            OnValidationCompleted(result, validatorNames);

            return result;
        }
        finally
        {
            _validationLock.Release();
        }
    }

    public async Task<UnifiedValidationResult> ValidateRangeAsync(
        Document document,
        TextSpan range,
        UnifiedValidationOptions options,
        CancellationToken ct = default)
    {
        var fullResult = await ValidateAsync(document, options, ct);

        // Filter to range
        var rangeIssues = fullResult.Issues
            .Where(i => i.Location != null &&
                        i.Location.Start >= range.Start &&
                        i.Location.End <= range.End)
            .ToList();

        return fullResult with
        {
            Issues = rangeIssues.AsReadOnly()
        };
    }

    public Task<UnifiedValidationResult?> GetCachedResultAsync(
        string documentPath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        // In practice, would need better cache key tracking
        // For now, return null and let caller perform full validation
        return Task.FromResult<UnifiedValidationResult?>(null);
    }

    public void InvalidateCache(string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        _logger.LogDebug("Cache invalidated for: {Path}", documentPath);
    }

    public void InvalidateAllCaches()
    {
        _logger.LogInformation("All validation caches invalidated");
    }

    public event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;

    #region Private Methods

    private async Task<List<UnifiedIssue>> RunStyleLinterAsync(
        Document document,
        UnifiedValidationOptions options,
        CancellationToken ct)
    {
        try
        {
            var cts = new CancellationTokenSource(options.ValidatorTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            var lintResult = await _styleLinter.LintAsync(document, linkedCts.Token);

            _logger.LogDebug(
                "Style Linter found {Count} violations",
                lintResult.Violations.Count);

            return lintResult.Violations
                .Select(v => UnifiedIssueFactory.FromLintViolation(
                    v,
                    v.Rule,
                    v.SuggestedFix))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Style Linter validation timed out");
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Style Linter validation failed");
            return new();
        }
    }

    private async Task<List<UnifiedIssue>> RunGrammarLinterAsync(
        Document document,
        UnifiedValidationOptions options,
        CancellationToken ct)
    {
        try
        {
            var cts = new CancellationTokenSource(options.ValidatorTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            var grammarResult = await _grammarLinter.CheckAsync(document, linkedCts.Token);

            _logger.LogDebug(
                "Grammar Linter found {Count} errors",
                grammarResult.Errors.Count);

            return grammarResult.Errors
                .Select(e => UnifiedIssueFactory.FromGrammarError(
                    e,
                    e.SuggestedReplacement))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Grammar Linter validation timed out");
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Grammar Linter validation failed");
            return new();
        }
    }

    private async Task<List<UnifiedIssue>> RunValidationEngineAsync(
        Document document,
        UnifiedValidationOptions options,
        CancellationToken ct)
    {
        try
        {
            var cts = new CancellationTokenSource(options.ValidatorTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            var validationResult = await _validationEngine.ValidateAsync(
                document,
                linkedCts.Token);

            _logger.LogDebug(
                "Validation Engine found {Count} issues",
                validationResult.Issues.Count);

            return validationResult.Issues
                .Select(i => UnifiedIssueFactory.FromValidationIssue(i, null))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Validation Engine validation timed out");
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation Engine validation failed");
            return new();
        }
    }

    private List<UnifiedIssue> DeduplicateIssues(List<UnifiedIssue> issues)
    {
        var deduped = new List<UnifiedIssue>();
        var processed = new HashSet<Guid>();

        foreach (var issue in issues.OrderByDescending(i => (int)i.Severity))
        {
            if (processed.Contains(issue.Id))
                continue;

            // Find duplicates
            var duplicates = issues
                .Where(i => !processed.Contains(i.Id) &&
                            i.Id != issue.Id &&
                            IsDuplicate(issue, i))
                .ToList();

            // Mark duplicates
            foreach (var dup in duplicates)
            {
                processed.Add(dup.Id);
                deduped.Add(dup with
                {
                    IsDuplicate = true,
                    DuplicateOf = new[] { issue.Id }.ToList()
                });
            }

            deduped.Add(issue);
            processed.Add(issue.Id);
        }

        return deduped;
    }

    private bool IsDuplicate(UnifiedIssue issue1, UnifiedIssue issue2)
    {
        // Same location (within tolerance)
        if (issue1.Location != null && issue2.Location != null)
        {
            const int tolerance = 1;
            if (Math.Abs(issue1.Location.Start - issue2.Location.Start) > tolerance)
                return false;
        }

        // Code prefix match
        if (!issue1.Code.StartsWith(issue2.Code.Split('_')[0]))
            return false;

        // Different validator
        if (issue1.ValidatorName == issue2.ValidatorName)
            return false;

        return true;
    }

    private List<UnifiedIssue> ApplyFilters(
        List<UnifiedIssue> issues,
        UnifiedValidationOptions options,
        LicenseTier tier)
    {
        var filtered = issues;

        // Minimum severity filter
        filtered = filtered
            .Where(i => i.Severity <= options.MinimumSeverity)
            .ToList();

        // Category filter
        if (options.FilterByCategory != null)
        {
            filtered = filtered
                .Where(i => options.FilterByCategory.Contains(i.Category))
                .ToList();
        }

        return filtered;
    }

    private bool IsAvailableForTier(LicenseTier tier, string validator) =>
        (tier, validator) switch
        {
            (LicenseTier.Core, "StyleLinter") => true,
            (LicenseTier.WriterPro, "StyleLinter" or "GrammarLinter") => true,
            (_, "ValidationEngine") => tier is LicenseTier.Teams or LicenseTier.Enterprise,
            _ => false
        };

    private bool CacheIsValid(UnifiedValidationResult cached, UnifiedValidationOptions options)
    {
        // Simplified cache validation
        // In practice, would check content hash and rules version
        return cached.OptionsHash == options &&
               (DateTimeOffset.UtcNow - cached.ValidatedAt).TotalMilliseconds < 300_000;
    }

    private static string GetCacheKey(string documentPath, UnifiedValidationOptions options)
    {
        return $"validation:{documentPath}:{options.GetHashCode()}";
    }

    private static async Task<List<T>> RunSequentiallyAsync<T>(
        List<Task<T>> tasks,
        CancellationToken ct)
    {
        var results = new List<T>();
        foreach (var task in tasks)
        {
            results.Add(await task);
        }
        return results;
    }

    private void OnValidationCompleted(
        UnifiedValidationResult result,
        List<string> validatorNames)
    {
        ValidationCompleted?.Invoke(this, new ValidationCompletedEventArgs
        {
            DocumentPath = result.DocumentPath,
            Result = result,
            SuccessfulValidators = validatorNames
        });
    }

    #endregion
}
```

---

## 7. Error Handling

### 7.1 Validator Timeout

**Scenario:** A validator exceeds timeout.

**Handling:**
- Catch `OperationCanceledException`
- Log warning with validator name and timeout value
- Continue with other validators
- Mark validator as failed in event args
- Return partial result with successful validators' issues

**Code:**
```csharp
catch (OperationCanceledException)
{
    _logger.LogWarning("Style Linter validation timed out after {Timeout}ms",
        options.ValidatorTimeoutMs);
    return new(); // Empty result
}
```

### 7.2 Validator Crash

**Scenario:** A validator throws an exception.

**Handling:**
- Catch exception
- Log error with full stack trace
- Continue with other validators
- Mark validator as failed
- Return empty list for that validator

### 7.3 Cache Invalidation

**Scenario:** Validation options change between runs.

**Handling:**
- Include options hash in cache key
- Check hash before returning cached result
- Invalidate if options differ

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class UnifiedValidationServiceTests
{
    private UnifiedValidationService _service;
    private Mock<IStyleLinter> _mockStyleLinter;
    private Mock<IGrammarLinter> _mockGrammarLinter;
    private Mock<IValidationEngine> _mockValidationEngine;
    private Mock<ILicenseContext> _licenseContext;

    [TestInitialize]
    public void Setup()
    {
        _mockStyleLinter = new Mock<IStyleLinter>();
        _mockGrammarLinter = new Mock<IGrammarLinter>();
        _mockValidationEngine = new Mock<IValidationEngine>();
        _licenseContext = new Mock<ILicenseContext>();

        _service = new UnifiedValidationService(
            _mockStyleLinter.Object,
            _mockGrammarLinter.Object,
            _mockValidationEngine.Object,
            _licenseContext.Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<UnifiedValidationService>>().Object);
    }

    [TestMethod]
    public async Task ValidateAsync_CombinesResultsFromAllValidators()
    {
        // Arrange
        var document = new Document { Path = "test.md" };
        var options = new UnifiedValidationOptions();

        _mockStyleLinter.Setup(s => s.LintAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LintResult { Violations = new[] { /* ... */ } });

        _mockGrammarLinter.Setup(g => g.CheckAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrammarResult { Errors = new[] { /* ... */ } });

        // Act
        var result = await _service.ValidateAsync(document, options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Issues.Count > 0);
        Assert.IsTrue(result.Issues.Any(i => i.Category == IssueCategory.Style));
        Assert.IsTrue(result.Issues.Any(i => i.Category == IssueCategory.Grammar));
    }

    [TestMethod]
    public async Task ValidateAsync_DeduplicatesIssues()
    {
        // Arrange: Same issue from two validators
        var document = new Document { Path = "test.md" };
        var options = new UnifiedValidationOptions { EnableDeduplication = true };

        var duplicate = new TextSpan { Start = 10, End = 15 };

        _mockStyleLinter.Setup(s => s.LintAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LintResult
            {
                Violations = new[]
                {
                    new LintViolation { Location = duplicate }
                }
            });

        _mockGrammarLinter.Setup(g => g.CheckAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrammarResult
            {
                Errors = new[]
                {
                    new GrammarError { Location = duplicate }
                }
            });

        // Act
        var result = await _service.ValidateAsync(document, options);

        // Assert
        Assert.AreEqual(1, result.Issues.Count(i => !i.IsDuplicate));
        Assert.AreEqual(1, result.Issues.Count(i => i.IsDuplicate));
    }

    [TestMethod]
    public async Task ValidateAsync_AppliesLicenseFilter()
    {
        // Arrange: Teams tier should see all, Core should see Style only
        _licenseContext.Setup(l => l.GetCurrentTierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseTier.Core);

        var document = new Document { Path = "test.md" };
        var options = new UnifiedValidationOptions();

        // Act
        var result = await _service.ValidateAsync(document, options);

        // Assert: Knowledge Engine excluded
        Assert.IsFalse(result.Issues.Any(i => i.Category == IssueCategory.Knowledge));
    }

    [TestMethod]
    public async Task ValidateAsync_HandlesValidatorTimeout()
    {
        // Arrange
        _mockStyleLinter.Setup(s => s.LintAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var document = new Document { Path = "test.md" };
        var options = new UnifiedValidationOptions();

        // Act: Should not throw
        var result = await _service.ValidateAsync(document, options);

        // Assert: Result should be valid (no Style issues)
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Issues.Any(i => i.Category == IssueCategory.Style));
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class UnifiedValidationIntegrationTests
{
    [TestMethod]
    public async Task FullValidation_WithRealValidators()
    {
        // This would use real validator implementations
        // and actual document samples
    }

    [TestMethod]
    public async Task ValidateAsync_RespectsCacheTTL()
    {
        // Test cache expiration
    }
}
```

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Validator crash | Medium | Try-catch per validator, continue processing |
| Timeout DoS | Medium | Per-validator timeouts, configurable |
| Memory exhaustion | Medium | Max issues limit |
| License bypass | Low | Checked before returning results |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Document with violations | Validating | Issues from all validators returned |
| 2 | Document with no violations | Validating | Empty result |
| 3 | Duplicate issues | Validating | Primary kept, duplicates marked |
| 4 | Core license user | Validating | Style issues only |
| 5 | WriterPro user | Validating | Style + Grammar |
| 6 | Teams user | Validating | All issue categories |
| 7 | Validator timeout | Validating | Other validators still run |
| 8 | Validator crash | Validating | Other validators still run |
| 9 | Options.MaxIssuesPerDocument=10 | Validating 100 issues | Returns 10 (sorted by severity) |

### 10.2 Performance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 10 | 5000-word document | Sequential validation | Completes in < 2s |
| 11 | Same document | Parallel validation | Completes in < 1s |
| 12 | Validation result | Using cache | Completes in < 100ms |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `IUnifiedValidationService` interface | [ ] |
| 2 | `UnifiedValidationOptions` record | [ ] |
| 3 | `UnifiedValidationResult` record | [ ] |
| 4 | `ValidationCompletedEventArgs` class | [ ] |
| 5 | `UnifiedValidationService` implementation | [ ] |
| 6 | Deduplication logic | [ ] |
| 7 | License filtering | [ ] |
| 8 | Caching layer | [ ] |
| 9 | Parallel/sequential execution | [ ] |
| 10 | Unit tests | [ ] |
| 11 | Integration tests | [ ] |
| 12 | DI registration | [ ] |

---

## 12. Verification Commands

```bash
# Run aggregator tests
dotnet test --filter "Version=v0.7.5f" --logger "console;verbosity=detailed"

# Run integration tests
dotnet test --filter "Category=Integration&Version=v0.7.5f"

# Test performance
dotnet run --project tests/Lexichord.Tests.Validation -- --benchmark ValidationPerformance

# Manual verification:
# 1. Create document with violations from all validators
# 2. Call ValidateAsync with various license tiers
# 3. Verify deduplication works
# 4. Verify caching works
# 5. Simulate validator timeout
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Lead Architect | Initial draft |
