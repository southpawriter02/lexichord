// =============================================================================
// File: AxiomLoadResult.cs
// Project: Lexichord.Modules.Knowledge
// Description: Result and error records for axiom loading operations.
// =============================================================================
// LOGIC: Provides structured result types for loading operations:
//   - AxiomLoadResult: Overall result with axioms, files, errors, timing
//   - AxiomLoadError: Individual error with location and severity
//   - AxiomValidationReport: Validation-only result for dry-run validation
//   - LoadErrorSeverity: Error/Warning/Info classification
//   - AxiomSourceType: BuiltIn/Workspace/File source classification
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Result of loading axioms.
/// </summary>
/// <remarks>
/// LOGIC: Aggregates results from one or more axiom loading operations.
/// Success is determined by absence of Error-severity issues.
/// </remarks>
public record AxiomLoadResult
{
    /// <summary>
    /// Whether loading succeeded overall.
    /// </summary>
    /// <remarks>
    /// LOGIC: True if no errors with <see cref="LoadErrorSeverity.Error"/> severity.
    /// Warnings and info messages do not affect success status.
    /// </remarks>
    public bool Success => !Errors.Any(e => e.Severity == LoadErrorSeverity.Error);

    /// <summary>
    /// Axioms successfully loaded.
    /// </summary>
    public required IReadOnlyList<Axiom> Axioms { get; init; }

    /// <summary>
    /// Files processed.
    /// </summary>
    /// <remarks>
    /// LOGIC: For built-in sources, contains resource names (e.g., 
    /// <c>Lexichord.Modules.Knowledge.Resources.BuiltInAxioms.api-documentation.yaml</c>).
    /// </remarks>
    public required IReadOnlyList<string> FilesProcessed { get; init; }

    /// <summary>
    /// Errors and warnings encountered.
    /// </summary>
    public IReadOnlyList<AxiomLoadError> Errors { get; init; } = Array.Empty<AxiomLoadError>();

    /// <summary>
    /// Load duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Source type (BuiltIn, Workspace, File).
    /// </summary>
    public AxiomSourceType SourceType { get; init; }
}

/// <summary>
/// Error encountered during axiom loading.
/// </summary>
/// <remarks>
/// LOGIC: Captures detailed error context including file location and axiom ID
/// to enable precise error reporting in editors and logs.
/// </remarks>
public record AxiomLoadError
{
    /// <summary>
    /// File where error occurred.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Line number (if applicable).
    /// </summary>
    /// <remarks>
    /// LOGIC: 1-based line number from YAML parser exceptions.
    /// </remarks>
    public int? Line { get; init; }

    /// <summary>
    /// Column number (if applicable).
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Error code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Standardized codes for categorization:
    /// - YAML_SYNTAX_ERROR: Invalid YAML syntax
    /// - EMPTY_FILE: No axioms found in file
    /// - AXIOM_PARSE_ERROR: Error parsing axiom entry
    /// - FILE_LOAD_ERROR: I/O error reading file
    /// - BUILTIN_LOAD_ERROR: Error loading embedded resource
    /// - UNKNOWN_TARGET_TYPE: Target type not in schema registry
    /// - VALIDATION_ERROR: Schema validation failure
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Error severity.
    /// </summary>
    public LoadErrorSeverity Severity { get; init; } = LoadErrorSeverity.Error;

    /// <summary>
    /// Axiom ID (if error is axiom-specific).
    /// </summary>
    public string? AxiomId { get; init; }
}

/// <summary>
/// Validation report for a YAML file.
/// </summary>
/// <remarks>
/// LOGIC: Returned by <see cref="IAxiomLoader.ValidateFileAsync"/> for dry-run validation.
/// Does not persist axioms - only reports validation status.
/// </remarks>
public record AxiomValidationReport
{
    /// <summary>
    /// File path validated.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Whether file is valid.
    /// </summary>
    public bool IsValid => !Errors.Any(e => e.Severity == LoadErrorSeverity.Error);

    /// <summary>
    /// Validation errors.
    /// </summary>
    public required IReadOnlyList<AxiomLoadError> Errors { get; init; }

    /// <summary>
    /// Axiom count if valid.
    /// </summary>
    public int AxiomCount { get; init; }

    /// <summary>
    /// Unrecognized target types.
    /// </summary>
    /// <remarks>
    /// LOGIC: Lists target types referenced by axioms that don't exist in the
    /// schema registry. These generate Warning-severity errors.
    /// </remarks>
    public IReadOnlyList<string> UnknownTargetTypes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Severity of load errors.
/// </summary>
public enum LoadErrorSeverity
{
    /// <summary>Blocking error that prevents axiom loading.</summary>
    Error,

    /// <summary>Non-blocking issue that should be addressed.</summary>
    Warning,

    /// <summary>Informational message.</summary>
    Info
}

/// <summary>
/// Source type for loaded axioms.
/// </summary>
public enum AxiomSourceType
{
    /// <summary>Axioms embedded in application resources.</summary>
    BuiltIn,

    /// <summary>Axioms from workspace .lexichord/knowledge/axioms/ directory.</summary>
    Workspace,

    /// <summary>Axioms from a specific file.</summary>
    File
}
