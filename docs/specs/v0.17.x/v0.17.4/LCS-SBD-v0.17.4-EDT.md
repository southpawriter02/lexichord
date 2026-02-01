# LCS-SBD-v0.17.4-EDT: Scope Overview — Style & Rules

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-v0.17.4-EDT                                          |
| **Version**      | v0.17.4                                                      |
| **Codename**     | Style & Rules (Intelligent Editor Phase 4)                   |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-02-01                                                   |
| **Owner**        | Editor Architecture Lead                                     |
| **Depends On**   | v0.17.1-EDT (Editor Foundation), v0.17.2-EDT (Rich Formatting), v0.17.3-EDT (Change Tracking) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.17.4-EDT** delivers **Style & Rules** — a comprehensive formatting rules engine that transforms the editor into a style-aware, intelligent writing environment. This implementation establishes:

- A flexible formatting rules engine supporting 30+ Markdown linting rules with auto-fix capabilities
- A sophisticated style guide system with 7 built-in guides and custom rule creation
- Full EditorConfig support for cross-editor consistency and team standardization
- Real-time rule violation diagnostics with actionable feedback and suggestions
- A visual diagnostics panel integrating problems, violations, and actionable fixes
- Style settings UI enabling users to configure rules, severity levels, and exceptions
- License-gated feature access controlling rule availability across tiers

This is the final foundational layer before AI integration—without style enforcement, the editor cannot guide users toward best practices and consistency.

### 1.2 Business Value

- **Consistency:** Ensures all documents follow configured style standards automatically.
- **Quality:** Catches formatting, accessibility, and best-practice violations in real-time.
- **Productivity:** Auto-fix capabilities eliminate manual corrections for common issues.
- **Collaboration:** EditorConfig and style guides enable teams to share standards across tools.
- **Compliance:** Support for strict rule enforcement meets enterprise governance requirements.
- **Flexibility:** 7 built-in guides plus custom rules support diverse writing styles and domains.
- **Integration:** EditorConfig compatibility allows Lexichord to coexist with other editors.

### 1.3 Success Criteria

1. Formatting rules engine validates documents against 30+ Markdown rules in <500ms per 10KB content.
2. All markdown lint rules (MD001-MD050) are implemented with auto-fix available for 20+ rules.
3. 7 built-in style guides (Technical, Academic, Blog, Marketing, Legal, Minimal, Strict) are functional and tested.
4. EditorConfig parser correctly handles .editorconfig files with file-pattern matching.
5. Diagnostics panel displays all violations with visual indicators, severity levels, and quick-fix buttons.
6. Auto-fix operations complete in <100ms for typical documents with batch operations supported.
7. License gating restricts rules as specified: Core (basic), WriterPro (all rules), Teams (guides), Enterprise (custom).
8. Rule configuration persists across sessions at document, workspace, and global scopes.
9. Performance remains <16ms frame time with 100+ violations displayed simultaneously.
10. Custom rule creation supports rule templates with configurable regex, message, and auto-fix logic.

---

## 2. Key Deliverables

### 2.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.17.4e | Formatting Rules Engine | Core rules engine, validation, fixing, rule management | 12 |
| v0.17.4f | Markdown Linter | 30+ Markdown lint rules (MD001-MD050) with tags and fixes | 10 |
| v0.17.4g | Style Guide System | Built-in guides, custom guide creation, style checking | 10 |
| v0.17.4h | EditorConfig Support | .editorconfig parsing, file matching, setting application | 8 |
| v0.17.4i | Rule Violation Diagnostics | Diagnostic collection, real-time reporting, fix suggestions | 6 |
| v0.17.4j | Style Settings UI | Panel for rule configuration, severity management, customization | 6 |
| **Total** | | | **52 hours** |

### 2.2 Core Interfaces

```csharp
/// <summary>
/// Formatting rules engine for enforcing document style.
/// Validates documents against active rules, provides violations, and supports auto-fixing.
/// </summary>
public interface IFormattingRulesEngine
{
    /// <summary>
    /// Get active rules for a document based on its scope and configuration.
    /// </summary>
    Task<IReadOnlyList<FormattingRule>> GetRulesAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Validate document against active rules.
    /// Returns complete validation result with all violations.
    /// </summary>
    Task<ValidationResult> ValidateAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Fix all auto-fixable violations in a document.
    /// Applies fixes according to severity and configuration options.
    /// </summary>
    Task<FixResult> FixAllAsync(
        DocumentId documentId,
        FixOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Fix a specific violation.
    /// Returns the text edit required to fix the violation.
    /// </summary>
    Task<TextEdit> FixViolationAsync(
        RuleViolation violation,
        CancellationToken ct = default);

    /// <summary>
    /// Enable or disable a specific rule.
    /// </summary>
    Task SetRuleEnabledAsync(
        string ruleId,
        bool enabled,
        RuleScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Configure rule parameters and behavior.
    /// </summary>
    Task ConfigureRuleAsync(
        string ruleId,
        RuleConfiguration config,
        RuleScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Set rule severity level (affects diagnostics filtering).
    /// </summary>
    Task SetRuleSeverityAsync(
        string ruleId,
        RuleSeverity severity,
        RuleScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Add an exception for a specific rule at a document position.
    /// </summary>
    Task AddRuleExceptionAsync(
        DocumentId documentId,
        string ruleId,
        TextRange range,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of validation events.
    /// </summary>
    IObservable<ValidationEventMessage> ValidationEvents { get; }
}

/// <summary>
/// A formatting rule definition with metadata and configuration.
/// </summary>
public record FormattingRule
{
    /// <summary>
    /// Unique rule identifier (e.g., "MD001", "STYLE001").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description of what the rule enforces.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Rule category for grouping and filtering.
    /// </summary>
    public required RuleCategory Category { get; init; }

    /// <summary>
    /// Default severity if not overridden by configuration.
    /// </summary>
    public required RuleSeverity DefaultSeverity { get; init; }

    /// <summary>
    /// Currently configured severity level.
    /// </summary>
    public RuleSeverity ConfiguredSeverity { get; init; }

    /// <summary>
    /// Whether the rule is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Whether this rule can be auto-fixed.
    /// </summary>
    public bool IsAutoFixable { get; init; }

    /// <summary>
    /// Link to detailed documentation for this rule.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Current configuration for this rule.
    /// </summary>
    public RuleConfiguration? Configuration { get; init; }

    /// <summary>
    /// Tags for categorization (e.g., "markdown", "accessibility", "performance").
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Minimum license tier required to use this rule.
    /// </summary>
    public LicenseTier MinimumTier { get; init; } = LicenseTier.Core;
}

/// <summary>
/// Categories for grouping related rules.
/// </summary>
public enum RuleCategory
{
    /// <summary>Heading syntax and structure rules.</summary>
    Headings,

    /// <summary>List formatting and consistency rules.</summary>
    Lists,

    /// <summary>Link syntax and validation rules.</summary>
    Links,

    /// <summary>Image syntax and accessibility rules.</summary>
    Images,

    /// <summary>Code block formatting rules.</summary>
    CodeBlocks,

    /// <summary>Table structure and formatting rules.</summary>
    Tables,

    /// <summary>Whitespace and indentation rules.</summary>
    Whitespace,

    /// <summary>Line length limit rules.</summary>
    LineLength,

    /// <summary>Punctuation and character rules.</summary>
    Punctuation,

    /// <summary>Writing consistency rules.</summary>
    Consistency,

    /// <summary>Accessibility and semantic rules.</summary>
    Accessibility,

    /// <summary>Best practices and style rules.</summary>
    BestPractices
}

/// <summary>
/// Severity level for rule violations.
/// </summary>
public enum RuleSeverity
{
    /// <summary>Rule is disabled.</summary>
    Off = 0,

    /// <summary>Informational hint (lowest level).</summary>
    Hint = 1,

    /// <summary>Informational message.</summary>
    Info = 2,

    /// <summary>Warning (recommends action but allows override).</summary>
    Warning = 3,

    /// <summary>Error (blocks or must be fixed).</summary>
    Error = 4
}

/// <summary>
/// Scope for rule configuration application.
/// </summary>
public enum RuleScope
{
    /// <summary>Rule configuration applies to this document only.</summary>
    Document,

    /// <summary>Rule configuration applies to all documents in workspace.</summary>
    Workspace,

    /// <summary>Rule configuration applies globally to all documents.</summary>
    Global
}

/// <summary>
/// Configuration options for a specific rule.
/// </summary>
public record RuleConfiguration
{
    /// <summary>
    /// Rule identifier being configured.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Custom configuration parameters (rule-specific).
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Configuration metadata and schema.
    /// </summary>
    public IReadOnlyList<RuleConfigProperty>? Schema { get; init; }
}

/// <summary>
/// A property that can be configured for a rule.
/// </summary>
public record RuleConfigProperty
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public required ConfigPropertyType Type { get; init; }
    public string? Description { get; init; }
    public object? DefaultValue { get; init; }
    public bool Required { get; init; }
}

public enum ConfigPropertyType
{
    String, Number, Boolean, Select, MultiSelect, Regex
}

/// <summary>
/// Result of a validation run.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// All violations found during validation.
    /// </summary>
    public IReadOnlyList<RuleViolation> Violations { get; init; } = [];

    /// <summary>
    /// Count of error-level violations.
    /// </summary>
    public int ErrorCount => Violations.Count(v => v.Severity == RuleSeverity.Error);

    /// <summary>
    /// Count of warning-level violations.
    /// </summary>
    public int WarningCount => Violations.Count(v => v.Severity == RuleSeverity.Warning);

    /// <summary>
    /// Count of info-level violations.
    /// </summary>
    public int InfoCount => Violations.Count(v => v.Severity == RuleSeverity.Info);

    /// <summary>
    /// Count of hint-level violations.
    /// </summary>
    public int HintCount => Violations.Count(v => v.Severity == RuleSeverity.Hint);

    /// <summary>
    /// Whether the document is valid (no errors).
    /// </summary>
    public bool IsValid => ErrorCount == 0;

    /// <summary>
    /// Time taken to validate the document.
    /// </summary>
    public TimeSpan ValidationTime { get; init; }

    /// <summary>
    /// Validation timestamp.
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A single rule violation found in a document.
/// </summary>
public record RuleViolation
{
    /// <summary>
    /// Identifier of the rule that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Human-readable name of the rule.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// Severity level of this violation.
    /// </summary>
    public required RuleSeverity Severity { get; init; }

    /// <summary>
    /// Message describing the violation.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Location of the violation in the document.
    /// </summary>
    public required TextRange Range { get; init; }

    /// <summary>
    /// Whether this violation can be auto-fixed.
    /// </summary>
    public bool IsAutoFixable { get; init; }

    /// <summary>
    /// Description of what the fix will do.
    /// </summary>
    public string? FixDescription { get; init; }

    /// <summary>
    /// Suggested text edits to fix the violation.
    /// </summary>
    public IReadOnlyList<TextEdit>? SuggestedFixes { get; init; }

    /// <summary>
    /// Link to rule documentation.
    /// </summary>
    public string? DocumentationUrl { get; init; }

    /// <summary>
    /// Unique identifier for this violation instance.
    /// </summary>
    public string ViolationId { get; init; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Result of fixing violations.
/// </summary>
public record FixResult
{
    /// <summary>
    /// Number of violations that were fixed.
    /// </summary>
    public int FixedCount { get; init; }

    /// <summary>
    /// Number of violations that could not be fixed (not auto-fixable).
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Text edits applied to fix violations.
    /// </summary>
    public IReadOnlyList<TextEdit> Edits { get; init; } = [];

    /// <summary>
    /// Violations that remain after fixing.
    /// </summary>
    public IReadOnlyList<RuleViolation> RemainingViolations { get; init; } = [];

    /// <summary>
    /// Time taken to apply all fixes.
    /// </summary>
    public TimeSpan FixTime { get; init; }
}

/// <summary>
/// Options for auto-fix operations.
/// </summary>
public record FixOptions
{
    /// <summary>
    /// Maximum severity level to fix (e.g., Warning = fix warnings and lower).
    /// </summary>
    public RuleSeverity MaxSeverity { get; init; } = RuleSeverity.Warning;

    /// <summary>
    /// Rule IDs to fix; null = fix all auto-fixable rules.
    /// </summary>
    public IReadOnlyList<string>? RuleIds { get; init; }

    /// <summary>
    /// Whether to apply fixes that modify document structure significantly.
    /// </summary>
    public bool AllowStructuralChanges { get; init; } = true;

    /// <summary>
    /// Whether to ask for confirmation before applying fixes.
    /// </summary>
    public bool RequireConfirmation { get; init; } = false;
}

/// <summary>
/// Markdown-specific linting engine.
/// Implements 30+ Markdown lint rules with auto-fix capabilities.
/// </summary>
public interface IMarkdownLinter
{
    /// <summary>
    /// Lint a Markdown document.
    /// </summary>
    Task<LintResult> LintAsync(
        string content,
        LintOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get all available lint rules.
    /// </summary>
    Task<IReadOnlyList<LintRule>> GetRulesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific lint rule by ID.
    /// </summary>
    Task<LintRule?> GetRuleAsync(
        string ruleId,
        CancellationToken ct = default);

    /// <summary>
    /// Configure linter options.
    /// </summary>
    Task ConfigureAsync(
        LintConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Fix a specific lint violation.
    /// </summary>
    Task<TextEdit> FixViolationAsync(
        RuleViolation violation,
        string content,
        CancellationToken ct = default);

    /// <summary>
    /// Fix all auto-fixable violations.
    /// </summary>
    Task<string> FixAllAsync(
        string content,
        LintOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// A Markdown lint rule.
/// </summary>
public record LintRule
{
    /// <summary>
    /// Rule identifier (e.g., "MD001").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Tags for categorization (e.g., "markdown", "headings", "accessibility").
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether the rule is enabled by default.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Default severity.
    /// </summary>
    public RuleSeverity Severity { get; init; } = RuleSeverity.Warning;

    /// <summary>
    /// Whether this rule can be auto-fixed.
    /// </summary>
    public bool IsAutoFixable { get; init; }

    /// <summary>
    /// Link to detailed documentation.
    /// </summary>
    public string? DocumentationUrl { get; init; }
}

/// <summary>
/// Configuration for linting operations.
/// </summary>
public record LintConfiguration
{
    /// <summary>
    /// Rules to enable (others disabled).
    /// </summary>
    public IReadOnlyList<string>? EnabledRules { get; init; }

    /// <summary>
    /// Rules to disable explicitly.
    /// </summary>
    public IReadOnlyList<string>? DisabledRules { get; init; }

    /// <summary>
    /// Severity overrides for specific rules.
    /// </summary>
    public IReadOnlyDictionary<string, RuleSeverity>? SeverityOverrides { get; init; }

    /// <summary>
    /// Configuration for specific rules.
    /// </summary>
    public IReadOnlyDictionary<string, RuleConfiguration>? RuleConfigs { get; init; }
}

/// <summary>
/// Options for linting operations.
/// </summary>
public record LintOptions
{
    /// <summary>
    /// Configuration for this lint operation.
    /// </summary>
    public LintConfiguration? Configuration { get; init; }

    /// <summary>
    /// Maximum violations to return (for performance).
    /// </summary>
    public int? MaxViolations { get; init; }

    /// <summary>
    /// Whether to include fix suggestions.
    /// </summary>
    public bool IncludeFixSuggestions { get; init; } = true;
}

/// <summary>
/// Result of a lint operation.
/// </summary>
public record LintResult
{
    /// <summary>
    /// All violations found.
    /// </summary>
    public IReadOnlyList<RuleViolation> Violations { get; init; } = [];

    /// <summary>
    /// Time taken to lint.
    /// </summary>
    public TimeSpan LintTime { get; init; }

    /// <summary>
    /// Whether all violations were returned (vs truncated due to max).
    /// </summary>
    public bool IsComplete { get; init; } = true;
}

/// <summary>
/// Built-in Markdown lint rules.
/// </summary>
public static class MarkdownRules
{
    // Heading Rules (MD001-MD003, MD025)
    public const string HeadingIncrement = "MD001";       // Heading levels should increment by 1
    public const string FirstHeadingH1 = "MD002";         // First heading should be H1
    public const string HeadingStyle = "MD003";           // Heading style (atx vs setext)
    public const string NoMultipleH1 = "MD025";           // Document should have single H1

    // List Rules (MD004-MD005)
    public const string ListIndent = "MD004";             // Consistent list indentation
    public const string ListMarkerStyle = "MD005";        // Consistent list marker

    // Whitespace Rules (MD009-MD010, MD022, MD032)
    public const string NoTrailingSpaces = "MD009";       // No trailing spaces
    public const string NoHardTabs = "MD010";             // No hard tabs
    public const string BlankLinesAroundHeadings = "MD022";  // Blank lines around headings
    public const string BlankLinesAroundLists = "MD032";     // Blank lines around lists

    // Line Length Rule (MD013)
    public const string LineLength = "MD013";             // Line length limit

    // Link Rules (MD011, MD034)
    public const string NoReversedLinks = "MD011";        // [text](url) not (text)[url]
    public const string NoBareUrls = "MD034";             // No bare URLs

    // Code Rules (MD040, MD046, MD047)
    public const string CodeLanguageRequired = "MD040";   // Fenced code should have language
    public const string CodeFenceStyle = "MD046";         // Code fence style consistency
    public const string CodeBlockBrackets = "MD047";      // Code block brackets

    // Image/Accessibility Rules (MD045)
    public const string ImageAltText = "MD045";           // Images should have alt text

    // Consistency Rules (MD024, MD023)
    public const string NoDuplicateHeadings = "MD024";    // No duplicate headings
    public const string HeadingCapitalization = "MD023";  // Heading capitalization

    // Best Practices (MD042, MD038)
    public const string NoEmptyLinks = "MD042";           // No empty links
    public const string CodeFenceSpacing = "MD038";       // Code fence spacing

    // Additional Rules (MD014, MD019, MD020, MD021, MD026, MD027, MD028, MD029, MD030, MD031)
    public const string ListIndentedItems = "MD014";      // List indentation consistency
    public const string MultipleSpacesAfterHeader = "MD019";  // Multiple spaces after heading
    public const string ClosedHeading = "MD020";          // Heading should be closed
    public const string MultipleHeadingSpaces = "MD021";  // Multiple heading spaces
    public const string ListMarkerSpace = "MD026";        // Trailing punctuation in heading
    public const string ListMarkerIndent = "MD027";       // List marker indent
    public const string BlankLineInList = "MD028";        // Blank line in list
    public const string ListMarkerIndentConsistency = "MD029";  // List marker indent
    public const string ListItemsConsistent = "MD030";    // List item spacing
    public const string CodeBlocksConsistent = "MD031";   // Code block consistency

    // Additional Quality Rules
    public const string StrongEmphasisStyle = "MD035";    // Consistent emphasis style
    public const string RawHtmlBlocks = "MD033";          // Inline HTML
    public const string RawHtmlInline = "MD037";          // Spaces inside emphasis
}

/// <summary>
/// Style guide system for consistent writing across documents.
/// </summary>
public interface IStyleGuideSystem
{
    /// <summary>
    /// Get all available style guides.
    /// </summary>
    Task<IReadOnlyList<StyleGuide>> GetStyleGuidesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get active style guide for a document.
    /// </summary>
    Task<StyleGuide?> GetActiveStyleGuideAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Set active style guide for a document.
    /// </summary>
    Task SetActiveStyleGuideAsync(
        DocumentId documentId,
        string styleGuideId,
        CancellationToken ct = default);

    /// <summary>
    /// Create a custom style guide.
    /// </summary>
    Task<StyleGuide> CreateStyleGuideAsync(
        StyleGuideDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Update a style guide.
    /// </summary>
    Task UpdateStyleGuideAsync(
        string styleGuideId,
        StyleGuideDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a custom style guide.
    /// </summary>
    Task DeleteStyleGuideAsync(
        string styleGuideId,
        CancellationToken ct = default);

    /// <summary>
    /// Check a document against its active style guide.
    /// </summary>
    Task<StyleCheckResult> CheckStyleAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Get style violations for a document.
    /// </summary>
    Task<IReadOnlyList<StyleViolation>> GetViolationsAsync(
        DocumentId documentId,
        CancellationToken ct = default);
}

/// <summary>
/// A style guide defining writing rules and preferences.
/// </summary>
public record StyleGuide
{
    /// <summary>
    /// Unique guide identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Guide name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this is a built-in guide.
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Style rules and constraints.
    /// </summary>
    public StyleGuideRules Rules { get; init; } = new();

    /// <summary>
    /// Severity overrides for specific lint rules.
    /// </summary>
    public IReadOnlyDictionary<string, RuleSeverity> RuleSeverities { get; init; } =
        new Dictionary<string, RuleSeverity>();

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Definition for creating or updating a style guide.
/// </summary>
public record StyleGuideDefinition
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public StyleGuideRules? Rules { get; init; }
    public IReadOnlyDictionary<string, RuleSeverity>? RuleSeverities { get; init; }
}

/// <summary>
/// Writing rules and preferences in a style guide.
/// </summary>
public record StyleGuideRules
{
    // Document structure
    /// <summary>Document must have a title/heading.</summary>
    public bool RequireTitle { get; init; } = true;

    /// <summary>Maximum heading depth (H1-H6).</summary>
    public HeadingLevel MaxHeadingDepth { get; init; } = HeadingLevel.H4;

    /// <summary>Allow multiple H1 headings.</summary>
    public bool AllowMultipleH1 { get; init; } = false;

    // Line and paragraph limits
    /// <summary>Maximum characters per line (0 = no limit).</summary>
    public int MaxLineLength { get; init; } = 120;

    /// <summary>Maximum words per paragraph.</summary>
    public int MaxParagraphLength { get; init; } = 500;

    /// <summary>Maximum words per sentence.</summary>
    public int MaxSentenceLength { get; init; } = 40;

    // Formatting preferences
    /// <summary>Preferred bold marker style.</summary>
    public BoldStyle BoldStyle { get; init; } = BoldStyle.Asterisks;

    /// <summary>Preferred italic marker style.</summary>
    public ItalicStyle ItalicStyle { get; init; } = ItalicStyle.Asterisks;

    /// <summary>Preferred unordered list marker.</summary>
    public ListMarkerStyle UnorderedListStyle { get; init; } = ListMarkerStyle.Dash;

    /// <summary>Require language specifier in code blocks.</summary>
    public bool RequireLanguageInCodeBlocks { get; init; } = true;

    // Writing style preferences
    /// <summary>Preferred voice style.</summary>
    public VoicePreference VoicePreference { get; init; } = VoicePreference.Active;

    /// <summary>Preferred tense.</summary>
    public TensePreference TensePreference { get; init; } = TensePreference.Present;

    /// <summary>Discourage passive voice.</summary>
    public bool AvoidPassiveVoice { get; init; } = true;

    /// <summary>Discourage adverbs.</summary>
    public bool AvoidAdverbs { get; init; } = false;

    /// <summary>Maximum reading level (Flesch-Kincaid grade).</summary>
    public int MaxReadingLevel { get; init; } = 12;

    // Terminology
    /// <summary>Term replacements (e.g., "utilize" -> "use").</summary>
    public IReadOnlyDictionary<string, string> TermReplacements { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Banned words that should not appear.</summary>
    public IReadOnlyList<string> BannedWords { get; init; } = [];

    /// <summary>Required words or phrases.</summary>
    public IReadOnlyList<string> RequiredTerms { get; init; } = [];
}

/// <summary>
/// Heading levels.
/// </summary>
public enum HeadingLevel { H1 = 1, H2 = 2, H3 = 3, H4 = 4, H5 = 5, H6 = 6 }

/// <summary>
/// Bold marker style.
/// </summary>
public enum BoldStyle
{
    /// <summary>Use ** for bold.</summary>
    Asterisks,

    /// <summary>Use __ for bold.</summary>
    Underscores
}

/// <summary>
/// Italic marker style.
/// </summary>
public enum ItalicStyle
{
    /// <summary>Use * for italic.</summary>
    Asterisks,

    /// <summary>Use _ for italic.</summary>
    Underscores
}

/// <summary>
/// List marker style.
/// </summary>
public enum ListMarkerStyle
{
    /// <summary>Use - for lists.</summary>
    Dash,

    /// <summary>Use * for lists.</summary>
    Asterisk,

    /// <summary>Use + for lists.</summary>
    Plus
}

/// <summary>
/// Voice preference for writing.
/// </summary>
public enum VoicePreference
{
    /// <summary>No preference.</summary>
    Any,

    /// <summary>Prefer active voice.</summary>
    Active,

    /// <summary>Prefer passive voice.</summary>
    Passive
}

/// <summary>
/// Tense preference for writing.
/// </summary>
public enum TensePreference
{
    /// <summary>No preference.</summary>
    Any,

    /// <summary>Prefer present tense.</summary>
    Present,

    /// <summary>Prefer past tense.</summary>
    Past,

    /// <summary>Prefer future tense.</summary>
    Future
}

/// <summary>
/// Result of checking a document against a style guide.
/// </summary>
public record StyleCheckResult
{
    /// <summary>
    /// All style violations found.
    /// </summary>
    public IReadOnlyList<StyleViolation> Violations { get; init; } = [];

    /// <summary>
    /// Readability metrics.
    /// </summary>
    public ReadabilityMetrics Metrics { get; init; } = new();

    /// <summary>
    /// Overall style compliance score (0-100).
    /// </summary>
    public int ComplianceScore { get; init; }

    /// <summary>
    /// Time taken to check style.
    /// </summary>
    public TimeSpan CheckTime { get; init; }
}

/// <summary>
/// A single style violation.
/// </summary>
public record StyleViolation
{
    public required string Category { get; init; }
    public required string Message { get; init; }
    public TextRange? Range { get; init; }
    public RuleSeverity Severity { get; init; } = RuleSeverity.Warning;
    public string? Suggestion { get; init; }
}

/// <summary>
/// Readability metrics for a document.
/// </summary>
public record ReadabilityMetrics
{
    /// <summary>Average sentence length in words.</summary>
    public double AverageSentenceLength { get; init; }

    /// <summary>Flesch-Kincaid grade level.</summary>
    public double FleschKincaidGrade { get; init; }

    /// <summary>Average paragraph length in words.</summary>
    public int AverageParagraphLength { get; init; }

    /// <summary>Percentage of passive voice sentences.</summary>
    public double PassiveVoicePercentage { get; init; }

    /// <summary>Count of adverbs.</summary>
    public int AdverbCount { get; init; }
}

/// <summary>
/// Built-in style guides.
/// </summary>
public static class BuiltInStyleGuides
{
    /// <summary>Technical documentation style.</summary>
    public const string Technical = "technical";

    /// <summary>Academic writing style.</summary>
    public const string Academic = "academic";

    /// <summary>Blog post style.</summary>
    public const string Blog = "blog";

    /// <summary>Marketing copy style.</summary>
    public const string Marketing = "marketing";

    /// <summary>Legal document style.</summary>
    public const string Legal = "legal";

    /// <summary>Minimal rules style.</summary>
    public const string Minimal = "minimal";

    /// <summary>Strict enforcement style.</summary>
    public const string Strict = "strict";
}

/// <summary>
/// EditorConfig provider for cross-editor consistency.
/// </summary>
public interface IEditorConfigProvider
{
    /// <summary>
    /// Get EditorConfig settings for a file.
    /// </summary>
    Task<EditorConfigSettings> GetSettingsAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Create or update .editorconfig file.
    /// </summary>
    Task SaveConfigAsync(
        string directory,
        EditorConfigSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Parse an .editorconfig file.
    /// </summary>
    Task<EditorConfigFile> ParseFileAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Find and load .editorconfig files up the directory tree.
    /// </summary>
    Task<IReadOnlyList<EditorConfigFile>> FindConfigFilesAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Validate .editorconfig file format.
    /// </summary>
    Task<EditorConfigValidation> ValidateConfigAsync(
        string filePath,
        CancellationToken ct = default);
}

/// <summary>
/// EditorConfig settings applicable to files.
/// </summary>
public record EditorConfigSettings
{
    // Character encoding
    /// <summary>File character encoding (utf-8, latin1, etc).</summary>
    public string? Charset { get; init; }

    // Line endings
    /// <summary>End of line character (lf, crlf, cr).</summary>
    public EndOfLine? EndOfLine { get; init; }

    // Whitespace
    /// <summary>Insert newline at end of file.</summary>
    public bool? InsertFinalNewline { get; init; }

    /// <summary>Remove trailing whitespace.</summary>
    public bool? TrimTrailingWhitespace { get; init; }

    // Indentation
    /// <summary>Indentation style (space or tab).</summary>
    public IndentStyle? IndentStyle { get; init; }

    /// <summary>Indentation size.</summary>
    public int? IndentSize { get; init; }

    /// <summary>Tab width.</summary>
    public int? TabWidth { get; init; }

    // Line length
    /// <summary>Maximum line length.</summary>
    public int? MaxLineLength { get; init; }

    // Markdown-specific
    /// <summary>Maximum line length for Markdown files.</summary>
    public int? MarkdownMaxLineLength { get; init; }

    /// <summary>Trim trailing whitespace in Markdown.</summary>
    public bool? MarkdownTrimTrailingWhitespace { get; init; }

    // Custom properties
    /// <summary>Custom EditorConfig properties.</summary>
    public IReadOnlyDictionary<string, string> CustomProperties { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>
/// Parsed EditorConfig file.
/// </summary>
public record EditorConfigFile
{
    /// <summary>File path.</summary>
    public required string FilePath { get; init; }

    /// <summary>Parsed sections with file patterns and settings.</summary>
    public IReadOnlyList<EditorConfigSection> Sections { get; init; } = [];

    /// <summary>Whether this file is marked as root (no parent search).</summary>
    public bool IsRoot { get; init; }

    /// <summary>Parsing errors, if any.</summary>
    public IReadOnlyList<EditorConfigParseError> Errors { get; init; } = [];
}

/// <summary>
/// A section in an EditorConfig file with file pattern and settings.
/// </summary>
public record EditorConfigSection
{
    /// <summary>File pattern (glob).</summary>
    public required string FilePattern { get; init; }

    /// <summary>Settings for matching files.</summary>
    public EditorConfigSettings Settings { get; init; } = new();
}

/// <summary>
/// Error parsing EditorConfig file.
/// </summary>
public record EditorConfigParseError
{
    public int LineNumber { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Validation result for EditorConfig file.
/// </summary>
public record EditorConfigValidation
{
    public bool IsValid { get; init; }
    public IReadOnlyList<EditorConfigParseError> Errors { get; init; } = [];
}

/// <summary>
/// End of line style.
/// </summary>
public enum EndOfLine
{
    /// <summary>Line feed (Unix/Linux).</summary>
    Lf,

    /// <summary>Carriage return + line feed (Windows).</summary>
    Crlf,

    /// <summary>Carriage return (old Mac).</summary>
    Cr
}

/// <summary>
/// Indentation style.
/// </summary>
public enum IndentStyle
{
    /// <summary>Use spaces for indentation.</summary>
    Space,

    /// <summary>Use tabs for indentation.</summary>
    Tab
}
```

---

## 3. Implementation Details

### 3.1 v0.17.4e: Formatting Rules Engine (12 hours)

**Goal:** Build the core rules engine supporting validation, auto-fix, rule management, and configuration.

**Key Features:**
- Rule registry with 30+ built-in rules
- Validation pipeline validating documents against active rules
- Auto-fix engine applying fixes to violations (20+ fixable rules)
- Rule configuration system with scope support (document, workspace, global)
- Exception management for rule violations
- Performance optimization for large documents
- Observable validation events for real-time feedback

**Implementation Tasks:**
1. Design and implement FormattingRule record and related enums
2. Implement IFormattingRulesEngine interface with core methods
3. Build rule registry with metadata and initialization
4. Implement validation pipeline with concurrent rule evaluation
5. Build auto-fix engine with TextEdit generation
6. Implement configuration persistence (JSON-based storage)
7. Add exception tracking at document/range level
8. Implement validation event streaming
9. Add caching for rule lookups and validation results
10. Performance testing and optimization

**Database Schema:**
```sql
-- Rules storage
CREATE TABLE formatting_rules (
    rule_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    category TEXT,
    default_severity TEXT,
    is_auto_fixable BOOLEAN,
    documentation_url TEXT,
    is_built_in BOOLEAN,
    minimum_license_tier TEXT,
    created_at TIMESTAMP
);

-- Rule configurations per scope
CREATE TABLE rule_configurations (
    id TEXT PRIMARY KEY,
    rule_id TEXT NOT NULL,
    scope TEXT NOT NULL,  -- document, workspace, global
    scope_id TEXT,  -- document_id, workspace_id, or null for global
    configured_severity TEXT,
    is_enabled BOOLEAN,
    parameters JSON,
    created_at TIMESTAMP,
    FOREIGN KEY (rule_id) REFERENCES formatting_rules(rule_id)
);

-- Rule exceptions
CREATE TABLE rule_exceptions (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    rule_id TEXT NOT NULL,
    start_line INT,
    start_column INT,
    end_line INT,
    end_column INT,
    reason TEXT,
    created_at TIMESTAMP,
    FOREIGN KEY (rule_id) REFERENCES formatting_rules(rule_id)
);
```

**Testing Strategy:**
- Unit tests for each rule with fixtures
- Integration tests validating documents with various configurations
- Performance tests with large documents (100KB+)
- Regression tests for edge cases in rule validation
- Auto-fix correctness tests

---

### 3.2 v0.17.4f: Markdown Linter (10 hours)

**Goal:** Implement 30+ Markdown-specific lint rules covering headings, lists, links, code, accessibility, and best practices.

**Markdown Rules (30+ implemented):**

```
Heading Rules (MD001-MD003, MD025):
- MD001: Heading levels should increment by 1
- MD002: First heading should be H1
- MD003: Heading style (atx vs setext)
- MD025: Single H1 per document

List Rules (MD004-MD005):
- MD004: Consistent list indentation
- MD005: Consistent list marker style

Whitespace Rules (MD009-MD010, MD022, MD032):
- MD009: No trailing spaces
- MD010: No hard tabs
- MD022: Blank lines around headings
- MD032: Blank lines around lists

Line Length (MD013):
- MD013: Line length limit

Link Rules (MD011, MD034):
- MD011: [text](url) not (text)[url]
- MD034: No bare URLs

Code Rules (MD040, MD046, MD047):
- MD040: Fenced code should have language
- MD046: Code fence style consistency
- MD047: Code block brackets

Image/Accessibility (MD045):
- MD045: Images should have alt text

Consistency Rules (MD024, MD023):
- MD024: No duplicate headings
- MD023: Heading capitalization

Best Practices (MD042, MD038):
- MD042: No empty links
- MD038: Code fence spacing

Additional Rules (MD014, MD019, MD020, MD021, MD026-MD031, MD035, MD033, MD037):
- MD014: List indentation consistency
- MD019: Multiple spaces after heading
- MD020: Heading should be closed
- MD021: Multiple heading spaces
- MD026: Trailing punctuation in heading
- MD027: List marker indent
- MD028: Blank line in list
- MD029: List marker indent consistency
- MD030: List item spacing
- MD031: Code block consistency
- MD035: Consistent emphasis style
- MD033: Inline HTML
- MD037: Spaces inside emphasis
```

**Implementation Tasks:**
1. Implement MarkdownRules class with all rule constants
2. Create rule definition registry with metadata
3. Implement heading level validation rules
4. Implement list formatting rules
5. Implement whitespace rules
6. Implement line length checking
7. Implement link validation rules
8. Implement code block rules
9. Implement accessibility rules
10. Build fix functions for auto-fixable rules (20+)
11. Implement performance optimization (streaming/chunking)

**Auto-Fixable Rules:**
- MD009 (trailing spaces removal)
- MD010 (hard tabs replacement)
- MD022 (add blank lines)
- MD025 (remove duplicate H1)
- MD023 (capitalize headings)
- MD034 (convert bare URLs)
- MD038 (fix code fence spacing)
- MD042 (remove empty links)
- MD045 (add alt text template)
- MD046 (normalize code fence style)
- And 10+ more...

**Testing:**
- Rule-specific fixtures for each Markdown rule
- Edge case testing (empty files, large documents, complex lists)
- Performance testing (linting 100KB+ documents)
- Fix correctness verification

---

### 3.3 v0.17.4g: Style Guide System (10 hours)

**Goal:** Build the style guide system with 7 built-in guides and custom guide creation.

**Built-In Style Guides:**

```
1. Technical (Documentation)
   - Max line: 100 chars
   - Max sentence: 30 words
   - Require code language
   - Avoid passive voice
   - Strict heading structure

2. Academic
   - Max line: 80 chars
   - Formal voice
   - Citations required
   - Active voice preferred
   - Complex terminology allowed

3. Blog
   - Max line: 120 chars
   - Conversational tone
   - Shorter sentences (25 words)
   - Allow lists and examples
   - Engaging language

4. Marketing
   - Max line: 100 chars
   - Call-to-action phrases
   - Active voice
   - Emotional language allowed
   - Short sentences

5. Legal
   - Max line: 80 chars
   - Passive voice acceptable
   - Complex terminology
   - Precise definitions
   - Formal structure

6. Minimal
   - Few rules
   - Basic Markdown compliance
   - No style preferences
   - Maximum flexibility

7. Strict
   - All rules enabled
   - Tight constraints
   - High compliance score required
   - Enterprise-ready
```

**Implementation Tasks:**
1. Define StyleGuide and StyleGuideRules records
2. Implement built-in guide definitions
3. Implement IStyleGuideSystem interface
4. Build custom guide creation and persistence
5. Implement style checking against guides
6. Build readability metric calculation
7. Implement term replacement checking
8. Implement banned word detection
9. Implement compliance scoring algorithm
10. Create style violation generation

**Readability Metrics:**
- Average sentence length
- Flesch-Kincaid grade level
- Average paragraph length
- Passive voice percentage
- Adverb frequency
- Reading time estimate

**Database Schema:**
```sql
CREATE TABLE style_guides (
    guide_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    is_built_in BOOLEAN,
    rules JSON NOT NULL,  -- StyleGuideRules
    rule_severities JSON,  -- {rule_id: severity}
    created_at TIMESTAMP,
    modified_at TIMESTAMP
);

CREATE TABLE active_style_guides (
    document_id TEXT PRIMARY KEY,
    guide_id TEXT NOT NULL,
    set_at TIMESTAMP,
    FOREIGN KEY (guide_id) REFERENCES style_guides(guide_id)
);
```

---

### 3.4 v0.17.4h: EditorConfig Support (8 hours)

**Goal:** Implement full EditorConfig parsing, file matching, and setting application.

**Key Features:**
- Parse .editorconfig files with glob patterns
- Traverse directory tree for config files (respecting root)
- Apply settings to editor configuration
- Validate EditorConfig format
- Support standard and Markdown-specific properties

**Implementation Tasks:**
1. Implement EditorConfig parser with section/pattern support
2. Implement glob pattern matching algorithm
3. Build file traversal (up directory tree)
4. Implement root detection (root = true)
5. Build setting merging/override logic
6. Implement Markdown-specific property handling
7. Add validation for EditorConfig format
8. Integrate with formatting rules
9. Add caching for parsed configs
10. Performance optimization for repeated lookups

**EditorConfig Properties:**
```
Standard:
- charset (utf-8, latin1, etc)
- end_of_line (lf, crlf, cr)
- indent_style (space, tab)
- indent_size (int)
- tab_width (int)
- max_line_length (int)
- insert_final_newline (bool)
- trim_trailing_whitespace (bool)

Markdown Extensions:
- markdown_max_line_length
- markdown_trim_trailing_whitespace

Section Matching:
- *.md (all Markdown files)
- docs/**/*.md (in docs directory)
- README.* (README files)
```

---

### 3.5 v0.17.4i: Rule Violation Diagnostics (6 hours)

**Goal:** Build comprehensive diagnostic collection and real-time reporting of rule violations.

**Key Features:**
- Real-time diagnostic updates as document changes
- Violation grouping by severity and category
- Suggested fix generation
- Documentation links
- Scope-aware filtering
- Batch diagnostic operations

**Implementation Tasks:**
1. Implement diagnostic stream integration
2. Build violation collectors per rule type
3. Implement real-time update batching
4. Build diagnostic filtering/grouping
5. Implement fix suggestion generation
6. Add performance throttling (debouncing)
7. Build diagnostic cache management
8. Implement scope filtering

**Diagnostic Levels:**
- Error (critical, blocks)
- Warning (should fix)
- Info (informational)
- Hint (suggestion)

---

### 3.6 v0.17.4j: Style Settings UI (6 hours)

**Goal:** Build UI panel for configuring rules, severity levels, and style guides.

**UI Components:**
- Style guide selector (dropdown)
- Rules list with toggle/enable/disable
- Severity level selector per rule
- Category filter buttons
- Search/filter for rules
- Quick fix actions (with inline buttons)
- Rule documentation viewer
- Configuration parameter editor
- Exception management
- Batch operations (fix all, disable all, etc)

**Implementation Tasks:**
1. Design settings panel layout
2. Implement rule list component with filtering
3. Build rule configuration UI (parameters)
4. Implement severity selector
5. Build style guide selector
6. Implement quick fix button integration
7. Add rule documentation viewer
8. Build exception manager UI
9. Add keyboard shortcuts
10. Implement undo/redo for settings

**UI Mockup:**

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Style & Rules Configuration                                    [✕]        │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Style Guide: [Technical ▾]  [Save As Custom ▾]  [Reset to Default]      │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  ⊞ All Rules  ⊞ Enabled  ○ Errors  ○ Warnings  [🔍 Search rules] │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │                                                                     │   │
│  │  Category: Headings (5 rules)                                      │   │
│  │  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │   │
│  │  ☑ MD001  Heading increment       [⚠ Warning] [Documentation]    │   │
│  │  ☑ MD002  First heading is H1     [🛑 Error]  [Documentation]    │   │
│  │  ☑ MD003  Heading style (atx)     [⚠ Warning] [Documentation]    │   │
│  │  ☑ MD025  Single H1               [🛑 Error]  [Documentation]    │   │
│  │  ☑ MD023  Capitalize heading      [⚠ Warning] [Documentation]    │   │
│  │                                                                     │   │
│  │  Category: Lists (2 rules)                                         │   │
│  │  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │   │
│  │  ☑ MD004  List indentation        [⚠ Warning] [Documentation]    │   │
│  │  ☑ MD005  List marker style       [ℹ Info]   [Documentation]    │   │
│  │                                                                     │   │
│  │  [More categories...]                                              │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [Help & Examples]  [Import .editorconfig]  [Export Config]  [Apply]     │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Architecture Overview

### 4.1 System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Editor with Style & Rules (v0.17.4)                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                   UI Layer (Settings Panel & Diagnostics)             │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │   │
│  │  │ Style Guide      │  │ Rules Panel      │  │ Diagnostics      │   │   │
│  │  │ Selector         │  │ Configuration    │  │ Panel            │   │   │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘   │   │
│  └────────────────────────────────────────────────────────────────────┬─┘   │
│                                           │                              │   │
│  ┌────────────────────────────────────────────────────────────────────┬─┘   │
│  │                     Style & Rules Engine (Core)                     │     │
│  │  ┌────────────────────────────────────────────────────────────┐    │     │
│  │  │           IFormattingRulesEngine                            │    │     │
│  │  │  ┌─────────────────────────────────────────────────────┐   │    │     │
│  │  │  │ Validation Pipeline                                 │   │    │     │
│  │  │  │  - Concurrent rule evaluation                       │   │    │     │
│  │  │  │  - Violation detection                              │   │    │     │
│  │  │  │  - Performance optimization                         │   │    │     │
│  │  │  └─────────────────────────────────────────────────────┘   │    │     │
│  │  │  ┌─────────────────────────────────────────────────────┐   │    │     │
│  │  │  │ Auto-Fix Engine                                     │   │    │     │
│  │  │  │  - Violation fixing                                 │   │    │     │
│  │  │  │  - TextEdit generation                              │   │    │     │
│  │  │  │  - Batch operations                                 │   │    │     │
│  │  │  └─────────────────────────────────────────────────────┘   │    │     │
│  │  │  ┌─────────────────────────────────────────────────────┐   │    │     │
│  │  │  │ Rule Registry & Configuration                       │   │    │     │
│  │  │  │  - 30+ built-in rules                               │   │    │     │
│  │  │  │  - Rule metadata                                    │   │    │     │
│  │  │  │  - Configuration persistence                        │   │    │     │
│  │  │  └─────────────────────────────────────────────────────┘   │    │     │
│  │  └────────────────────────────────────────────────────────────┘    │    │
│  │  ┌────────────────────────────────────────────────────────────┐    │    │
│  │  │           IMarkdownLinter                                   │    │    │
│  │  │  - 30+ Markdown-specific rules (MD001-MD050)               │    │    │
│  │  │  - Auto-fix for 20+ rules                                  │    │    │
│  │  │  - Performance-optimized linting                           │    │    │
│  │  └────────────────────────────────────────────────────────────┘    │    │
│  │  ┌────────────────────────────────────────────────────────────┐    │    │
│  │  │           IStyleGuideSystem                                 │    │    │
│  │  │  - 7 built-in style guides                                 │    │    │
│  │  │  - Custom guide creation                                   │    │    │
│  │  │  - Style checking & compliance scoring                     │    │    │
│  │  │  - Readability metrics                                     │    │    │
│  │  └────────────────────────────────────────────────────────────┘    │    │
│  │  ┌────────────────────────────────────────────────────────────┐    │    │
│  │  │           IEditorConfigProvider                             │    │    │
│  │  │  - .editorconfig parsing                                    │    │    │
│  │  │  - Glob pattern matching                                    │    │    │
│  │  │  - Configuration traversal                                  │    │    │
│  │  └────────────────────────────────────────────────────────────┘    │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │              Data Layer (Persistence & Caching)                        │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐      │  │
│  │  │ Rules Database  │  │ Config Cache    │  │ Diagnostics      │      │  │
│  │  │ - Rule metadata │  │ - Config cache  │  │ - Violation      │      │  │
│  │  │ - Built-in      │  │ - Session data  │  │   tracking       │      │  │
│  │  │   definitions   │  │                 │  │ - Fix history    │      │  │
│  │  └─────────────────┘  └─────────────────┘  └──────────────────┘      │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Rule Validation Pipeline

```
Document Content
    │
    ├─→ Tokenize/Parse
    │
    ├─→ For Each Rule:
    │   │
    │   ├─→ Check Enabled? (Yes/No)
    │   │   │
    │   │   └─→ Check Scope (Document/Workspace/Global)
    │   │       │
    │   │       ├─→ Check Exceptions (Range-based skip)
    │   │       │
    │   │       └─→ Apply Rule Logic
    │   │           │
    │   │           ├─→ Violation? Create RuleViolation
    │   │           │   │
    │   │           │   ├─→ Can Fix? Generate TextEdit
    │   │           │   │
    │   │           │   └─→ Get Severity (Configured/Default)
    │   │           │
    │   │           └─→ No Violation? Skip
    │   │
    │   └─→ Performance Check (Batch violations if too many)
    │
    ├─→ Collect All Violations
    │
    ├─→ Apply Severity Filters (if configured)
    │
    ├─→ Group by Category
    │
    └─→ Return ValidationResult
        │
        ├─→ Violations List
        ├─→ Error/Warning/Info Counts
        ├─→ Overall Validity
        └─→ Validation Time
```

### 4.3 Auto-Fix Flow

```
User Triggers "Fix All"
    │
    ├─→ Validate Document (get all violations)
    │
    ├─→ Filter violations (max severity level from FixOptions)
    │
    ├─→ For Each Fixable Violation:
    │   │
    │   ├─→ Get Auto-Fix Logic
    │   │
    │   ├─→ Generate TextEdit(s)
    │   │
    │   ├─→ Check for Conflicts (overlapping edits)
    │   │
    │   └─→ Apply Edit (accumulate in batch)
    │
    ├─→ Sort Edits (reverse order to preserve offsets)
    │
    ├─→ Apply All Edits Atomically
    │
    ├─→ Re-validate Document
    │
    └─→ Return FixResult
        │
        ├─→ Fixed Count
        ├─→ Skipped Count
        ├─→ Applied Edits
        └─→ Remaining Violations
```

---

## 5. Data Storage & Persistence

### 5.1 Rules Database Schema

```sql
-- Formatting Rules
CREATE TABLE formatting_rules (
    rule_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    category TEXT NOT NULL,  -- enum: Headings, Lists, etc.
    default_severity TEXT NOT NULL,  -- Off, Hint, Info, Warning, Error
    is_auto_fixable BOOLEAN NOT NULL DEFAULT FALSE,
    documentation_url TEXT,
    tags TEXT,  -- JSON array
    is_built_in BOOLEAN NOT NULL DEFAULT TRUE,
    minimum_license_tier TEXT NOT NULL,  -- Core, WriterPro, Teams, Enterprise
    rule_type TEXT NOT NULL,  -- markdown, style, consistency, etc.
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Rule Configuration (scope-based)
CREATE TABLE rule_configurations (
    config_id TEXT PRIMARY KEY,
    rule_id TEXT NOT NULL,
    scope TEXT NOT NULL,  -- document, workspace, global
    scope_id TEXT,  -- document_id for document scope, workspace_id for workspace
    configured_severity TEXT,  -- overrides default
    is_enabled BOOLEAN,
    parameters JSON,  -- rule-specific configuration
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    FOREIGN KEY (rule_id) REFERENCES formatting_rules(rule_id),
    UNIQUE(rule_id, scope, scope_id)
);

-- Rule Exceptions (document-level overrides)
CREATE TABLE rule_exceptions (
    exception_id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    rule_id TEXT NOT NULL,
    text_range_start_line INT,
    text_range_start_column INT,
    text_range_end_line INT,
    text_range_end_column INT,
    reason TEXT,
    expires_at TIMESTAMP,
    created_at TIMESTAMP,
    FOREIGN KEY (rule_id) REFERENCES formatting_rules(rule_id)
);

-- Style Guides
CREATE TABLE style_guides (
    guide_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    is_built_in BOOLEAN NOT NULL DEFAULT FALSE,
    rules JSON NOT NULL,  -- Serialized StyleGuideRules
    rule_severities JSON,  -- {rule_id: severity}
    created_by TEXT,  -- user_id for custom guides
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Active Style Guide per Document
CREATE TABLE active_style_guides (
    document_id TEXT PRIMARY KEY,
    guide_id TEXT NOT NULL,
    set_at TIMESTAMP,
    set_by TEXT,  -- user_id
    FOREIGN KEY (guide_id) REFERENCES style_guides(guide_id)
);

-- EditorConfig Cache
CREATE TABLE editorconfig_cache (
    file_path TEXT PRIMARY KEY,
    config_data JSON NOT NULL,
    last_checked TIMESTAMP,
    hash TEXT  -- for invalidation
);

-- Validation Cache
CREATE TABLE validation_cache (
    document_id TEXT,
    content_hash TEXT,
    violations JSON,  -- ValidationResult
    cached_at TIMESTAMP,
    PRIMARY KEY (document_id, content_hash)
);
```

### 5.2 File-Based Configuration Storage

```
.lexichord/
├── rules/
│   ├── formatting-rules.json      # All rule definitions
│   ├── rule-configs.json          # User rule configurations
│   └── custom-rules.json          # User-defined custom rules
├── styles/
│   ├── style-guides.json          # User custom style guides
│   └── active-styles.json         # Per-document active styles
├── editorconfig/
│   └── cache.json                 # Parsed .editorconfig files
└── diagnostics/
    └── violation-history.json     # Recent violations tracking
```

---

## 6. Performance Considerations

### 6.1 Validation Performance Targets

| Metric | Target | Implementation |
|--------|--------|-----------------|
| Validation time per 10KB | <500ms | Concurrent rule evaluation |
| Diagnostics panel update | <100ms | Batched updates, debouncing |
| Auto-fix operation | <100ms | Batch TextEdit application |
| Rule lookup | <5ms | In-memory registry with caching |
| Linting 100KB file | <2s | Streaming/chunking, lazy parsing |
| Memory overhead | <50MB | Rule caching, result pooling |

### 6.2 Optimization Strategies

1. **Concurrent Evaluation:** Rules evaluated in parallel where possible
2. **Caching:** Rule registry, configuration, and validation results cached
3. **Debouncing:** UI updates debounced to batch changes
4. **Lazy Parsing:** Markdown parse tree built incrementally
5. **Streaming:** Large document violations streamed progressively
6. **Early Exit:** Stop evaluation if critical violations found
7. **Rule Ordering:** Fast rules evaluated first

### 6.3 Memory Management

- Validation results kept in memory only for current document
- Old results discarded when document edited
- Configuration cache pruned periodically
- Large violation lists paginated in UI

---

## 7. Testing Strategy

### 7.1 Unit Testing

**Rules Engine Tests:**
```csharp
[TestClass]
public class FormattingRulesEngineTests
{
    [TestMethod]
    public async Task ValidateAsync_WithValidDocument_ReturnsNoViolations() { }

    [TestMethod]
    public async Task ValidateAsync_WithViolations_ReturnsAllViolations() { }

    [TestMethod]
    public async Task FixAllAsync_WithAutoFixable_FixesViolations() { }

    [TestMethod]
    public async Task SetRuleEnabledAsync_DisablesRule_ExcludesFromValidation() { }

    [TestMethod]
    public async Task ConfigureRuleAsync_UpdatesConfiguration_AppliesSettings() { }

    [TestMethod]
    public async Task GetRulesAsync_WithDifferentScopes_ReturnsCorrectRules() { }
}

[TestClass]
public class MarkdownLinterTests
{
    // Test each rule (MD001-MD050)
    [TestMethod]
    public async Task LintAsync_HeadingIncrement_DetectsViolation() { }

    [TestMethod]
    public async Task LintAsync_ImageAltText_RequiresAlt() { }

    // Test auto-fix
    [TestMethod]
    public async Task FixViolationAsync_TrailingSpaces_RemovesSpaces() { }

    // Test edge cases
    [TestMethod]
    public async Task LintAsync_EmptyDocument_ReturnsNoViolations() { }

    [TestMethod]
    public async Task LintAsync_LargeDocument_PerformsEfficiently() { }
}

[TestClass]
public class StyleGuideSystemTests
{
    [TestMethod]
    public async Task CheckStyleAsync_AgainstGuide_DetectsViolations() { }

    [TestMethod]
    public async Task GetStyleGuidesAsync_ReturnsBuiltInGuides() { }

    [TestMethod]
    public async Task CreateStyleGuideAsync_CreatesCustomGuide_Persists() { }
}

[TestClass]
public class EditorConfigProviderTests
{
    [TestMethod]
    public async Task GetSettingsAsync_FindsParentConfig_AppliesMerged() { }

    [TestMethod]
    public async Task ParseFileAsync_ParsesEditorConfig_ValidatesFormat() { }

    [TestMethod]
    public async Task FindConfigFilesAsync_TraversesTree_StopsAtRoot() { }
}
```

### 7.2 Integration Testing

- Full validation pipeline with real documents
- Style guide application with rule verification
- EditorConfig loading and application
- Batch fix operations
- Rule exception handling
- Performance under load (1000+ violations)

### 7.3 End-to-End Testing

- UI panel interactions (enable/disable rules, change severity)
- Quick fix buttons and suggestions
- Style guide switching
- Real-time diagnostics updates
- Undo/redo after fixes
- Multi-document scenarios

### 7.4 Performance Testing

- Linting speed benchmarks
- Validation caching effectiveness
- UI responsiveness with many violations
- Memory usage profiling
- Cache hit/miss ratios

---

## 8. UI/UX Design

### 8.1 Diagnostics Panel

```
┌────────────────────────────────────────────────────────────────────────────┐
│ PROBLEMS                                              [Style: Technical]   │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Errors: 2 ⛔  │  Warnings: 5 ⚠  │  Info: 3 ℹ  │  [× Clear All] [⚙ Config] │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  ⛔ ERRORS (2)                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  📌 Line 6, Col 5                                            [✓ Fix] [→]  │
│     ⛔ Image must have alt text (MD045)                                    │
│     ![](image.png)                                                         │
│     ~~~~~~~~~~~~                                                           │
│     Suggestion: Add alt text for accessibility                            │
│     [Learn More](docs/MD045)                                              │
│                                                                             │
│  📌 Line 15, Col 1                                           [✓ Fix] [→]  │
│     ⛔ Duplicate heading "Installation" (MD024)                           │
│     ## Installation                                                        │
│     ~~~~~~~~~~~~~~~~~~                                                     │
│     Only one section with this heading should exist.                       │
│     [Learn More](docs/MD024)                                              │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  ⚠ WARNINGS (5)                                                           │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  📌 Line 1, Col 1                                            [✓ Fix] [→]  │
│     ⚠ Heading should be capitalized (MD023)                              │
│     # introduction                                                         │
│     ~~~~~~~~~~~~~                                                         │
│     [Learn More]                                                          │
│                                                                             │
│  📌 Line 3, Col 1                                            [✓ Fix] [→]  │
│     ⚠ Line exceeds 100 characters - 115 chars (MD013)                   │
│     This is a very long line that exceeds the configured maximum...       │
│     ──────────────────────────────────────────────────────────────────    │
│     [Learn More]                                                          │
│                                                                             │
│  [More violations...] (2 additional not shown)                            │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  [Fix All Issues] [Disable All Warnings] [Clear Configuration] [Help]    │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Style & Rules Settings Panel

```
┌────────────────────────────────────────────────────────────────────────────┐
│ STYLE & RULES                                            [Help] [✕]       │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Active Style: [Technical ▼] [📋 Custom...] [↺ Reset to Default]         │
│                                                                             │
│  ┌─ Writing Style ──────────────────────────────────────────────────────┐  │
│  │  Max Line Length: [100]        Max Sentence: [30] words             │  │
│  │  Voice: [Active ▼]  Tense: [Present ▼]  Max Grade Level: [12] ▼]   │  │
│  │  ☑ Require Title        ☐ Avoid Passive Voice    ☐ Avoid Adverbs   │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─ Formatting ─────────────────────────────────────────────────────────┐  │
│  │  Bold Style: [Asterisks ▼]   Italic: [Asterisks ▼]                 │  │
│  │  List Markers: [Dash ▼]        Require Code Language: ☑             │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─ Rules & Linting ────────────────────────────────────────────────────┐  │
│  │  [⊞ All] [☑ Enabled] [⊕ Errors] [⊕ Warnings]  🔍[Search rules...] │  │
│  │                                                                       │  │
│  │  Category: Headings (5 rules)                                        │  │
│  │  ─────────────────────────────────────────────────────────────────  │  │
│  │  ☑ MD001  Heading increment          [⚠ Warning ▼] [ℹ Info]       │  │
│  │  ☑ MD002  First heading is H1        [🛑 Error ▼]   [ℹ Info]       │  │
│  │  ☑ MD003  Heading style (atx)        [⚠ Warning ▼] [ℹ Info]       │  │
│  │  ☑ MD025  Single H1                  [🛑 Error ▼]   [ℹ Info]       │  │
│  │  ☑ MD023  Capitalize heading         [⚠ Warning ▼] [ℹ Info]       │  │
│  │                                                                       │  │
│  │  Category: Lists (2 rules)                                           │  │
│  │  ─────────────────────────────────────────────────────────────────  │  │
│  │  ☑ MD004  List indentation           [⚠ Warning ▼] [ℹ Info]       │  │
│  │  ☑ MD005  List marker style          [ℹ Info ▼]     [ℹ Info]       │  │
│  │                                                                       │  │
│  │  [Scroll for more...]                                                │  │
│  │                                                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  [Apply] [Save as Custom Style] [Import .editorconfig] [Export] [Help]   │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 9. License Gating

### 9.1 Feature Availability by Tier

| Feature | Core | WriterPro | Teams | Enterprise |
|---------|:----:|:---------:|:-----:|:----------:|
| Basic lint rules (MD001-MD010) | ✓ | ✓ | ✓ | ✓ |
| All lint rules (30+ MD rules) | ✓ | ✓ | ✓ | ✓ |
| Auto-fix (any rule) | ✗ | ✓ | ✓ | ✓ |
| Built-in style guides (7) | ✗ | ✓ | ✓ | ✓ |
| EditorConfig support | ✗ | ✓ | ✓ | ✓ |
| Custom style guides | ✗ | ✗ | ✓ | ✓ |
| Custom lint rules | ✗ | ✗ | ✗ | ✓ |
| Organization policies | ✗ | ✗ | ✗ | ✓ |

### 9.2 Implementation

```csharp
// License check in rule registry initialization
public class FormattingRulesEngine
{
    public async Task<IReadOnlyList<FormattingRule>> GetRulesAsync(
        DocumentId documentId,
        CancellationToken ct = default)
    {
        var license = await _licenseProvider.GetLicenseAsync();

        return _ruleRegistry
            .GetAllRules()
            .Where(r => license.IsFeatureAvailable(r.MinimumTier))
            .ToList();
    }
}

// License check for auto-fix
public async Task<FixResult> FixAllAsync(
    DocumentId documentId,
    FixOptions options,
    CancellationToken ct = default)
{
    var license = await _licenseProvider.GetLicenseAsync();

    if (!license.IsFeatureAvailable(LicenseTier.WriterPro))
    {
        throw new FeatureNotAvailableException("Auto-fix requires WriterPro or higher");
    }

    // ... proceed with fixes
}
```

---

## 10. Dependency Graph

```
v0.17.4-EDT (Style & Rules)
├── Depends On:
│   ├── v0.17.1-EDT (Editor Foundation)
│   │   └── Editor state, document lifecycle
│   ├── v0.17.2-EDT (Rich Formatting)
│   │   └── Formatting toolbar, keyboard shortcuts
│   └── v0.17.3-EDT (Change Tracking)
│       └── Document change events for diagnostics
│
├── Provides To:
│   └── v0.17.5-EDT (AI Integration)
│       └── Style context for AI suggestions
│
└── External Dependencies:
    ├── License Provider (LicenseTier checking)
    ├── Document Storage (persistence)
    ├── Configuration Storage (.editorconfig support)
    └── Diagnostic System (real-time updates)
```

---

## 11. Milestone Deliverables

### Phase 1: Core Rules Engine (Week 1-2)
- IFormattingRulesEngine interface and implementation
- Rule registry with 30+ built-in rules
- Validation pipeline
- Basic configuration persistence
- Unit tests

### Phase 2: Markdown Linter (Week 2-3)
- All 30+ MD lint rules implemented
- Auto-fix logic for 20+ rules
- Performance optimization
- Comprehensive test fixtures
- Documentation

### Phase 3: Style Guide System (Week 3-4)
- 7 built-in style guides
- Custom guide creation
- Style checking
- Readability metrics
- Guide persistence

### Phase 4: EditorConfig & UI (Week 4-5)
- EditorConfig parsing and application
- Diagnostics panel
- Style settings UI
- Rule configuration UI
- Integration testing

### Phase 5: Polish & Testing (Week 5-6)
- Performance optimization
- E2E testing
- License gating enforcement
- Documentation
- Bug fixes

---

## 12. Success Metrics

### Quality Metrics
- All 30+ Markdown rules implemented and tested
- 20+ rules have auto-fix capability
- Zero critical bugs in GA release
- All feature tests passing (100%)

### Performance Metrics
- Validation completes in <500ms for 10KB documents
- UI remains responsive with 100+ violations displayed
- Memory overhead <50MB
- Cache hit rate >80%

### User Adoption Metrics
- 80%+ of users enable at least one style guide
- Auto-fix used in 60%+ of editing sessions
- Rule configuration customization rate >40%
- Support tickets <2% of total issues

---

## 13. Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|:-----------:|:------:|:-----------|
| Performance degradation with large documents | Medium | High | Early profiling, streaming implementation |
| Complex rule interactions causing incorrect fixes | Medium | High | Comprehensive testing, fix preview before apply |
| EditorConfig compatibility issues | Low | Medium | Test with popular .editorconfig files |
| License gating enforcement | Low | High | Thorough integration testing |
| User confusion with rule configuration | Medium | Low | Good UI/UX, documentation, tooltips |

---

## 14. Appendix: Rule Implementation Templates

### 14.1 Simple Rule Template

```csharp
public class MD001HeadingIncrementRule : IMarkdownRule
{
    public string Id => MarkdownRules.HeadingIncrement;
    public string Name => "Heading levels should increment by 1";
    public RuleSeverity DefaultSeverity => RuleSeverity.Error;
    public bool IsAutoFixable => false;

    public List<RuleViolation> Validate(string content)
    {
        var violations = new List<RuleViolation>();
        var headingLines = ParseHeadings(content);

        int lastLevel = 0;
        foreach (var (lineNumber, level) in headingLines)
        {
            if (lastLevel > 0 && level > lastLevel + 1)
            {
                violations.Add(new RuleViolation
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = DefaultSeverity,
                    Message = $"Heading level should increment by 1 (was H{lastLevel}, found H{level})",
                    Range = new TextRange(lineNumber, 0, lineNumber, 100)
                });
            }
            lastLevel = level;
        }

        return violations;
    }

    private List<(int lineNumber, int level)> ParseHeadings(string content)
    {
        // Implementation: find all headings and their levels
        var headings = new List<(int, int)>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var match = Regex.Match(lines[i], @"^(#{1,6})\s+");
            if (match.Success)
            {
                int level = match.Groups[1].Length;
                headings.Add((i + 1, level));
            }
        }

        return headings;
    }
}
```

### 14.2 Auto-Fix Rule Template

```csharp
public class MD009NoTrailingSpacesRule : IMarkdownRule
{
    public string Id => MarkdownRules.NoTrailingSpaces;
    public string Name => "No trailing spaces";
    public RuleSeverity DefaultSeverity => RuleSeverity.Warning;
    public bool IsAutoFixable => true;

    public List<RuleViolation> Validate(string content)
    {
        var violations = new List<RuleViolation>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 0 && char.IsWhiteSpace(lines[i][^1]))
            {
                int trailingCount = lines[i].Length - lines[i].TrimEnd().Length;
                violations.Add(new RuleViolation
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = DefaultSeverity,
                    Message = $"No trailing spaces ({trailingCount} found)",
                    Range = new TextRange(i + 1, lines[i].Length - trailingCount, i + 1, lines[i].Length),
                    IsAutoFixable = true,
                    FixDescription = "Remove trailing spaces",
                    SuggestedFixes = new[]
                    {
                        new TextEdit
                        {
                            Range = new TextRange(i + 1, lines[i].Length - trailingCount, i + 1, lines[i].Length),
                            NewText = ""
                        }
                    }
                });
            }
        }

        return violations;
    }
}
```

---

## 15. Related Documentation

- **Roadmap:** `/sessions/eloquent-practical-archimedes/mnt/lexichord/docs/specs/v0.17.x/roadmap-v0.17.x.md`
- **v0.17.1-EDT Spec:** Editor Foundation specs
- **v0.17.2-EDT Spec:** Rich Formatting specs
- **v0.17.3-EDT Spec:** Change Tracking specs
- **v0.17.5-EDT Spec:** AI Integration specs
- **License Tier Documentation:** License feature matrix
- **EditorConfig Spec:** https://editorconfig.org/

---

**Document Prepared By:** Editor Architecture Lead
**Review Status:** Draft - Pending Architecture Review
**Next Steps:** Architecture review, dependency validation, resource estimation refinement

