// Copyright (c) 2025 Ryan Witting. Licensed under the MIT License. See LICENSE in root.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Represents the result of validating an agent configuration YAML file.
/// </summary>
/// <param name="IsValid">Whether the configuration is valid and can be used.</param>
/// <param name="Configuration">The parsed configuration if valid, <c>null</c> otherwise.</param>
/// <param name="Errors">Validation errors that prevented loading (empty if valid).</param>
/// <param name="Warnings">Warnings that don't prevent loading but should be addressed.</param>
/// <param name="SourceName">Source file or resource name for error reporting.</param>
/// <remarks>
/// <para>
/// This record is returned by <see cref="IAgentConfigLoader"/> methods to indicate
/// whether a YAML file was successfully parsed and validated. If <see cref="IsValid"/>
/// is <c>true</c>, the <see cref="Configuration"/> property contains the parsed agent.
/// </para>
/// <para>
/// <strong>Errors vs. Warnings:</strong><br/>
/// - <see cref="Errors"/>: Prevent the configuration from being loaded.<br/>
/// - <see cref="Warnings"/>: Suggest improvements but allow loading.
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public sealed record AgentConfigValidationResult(
    bool IsValid,
    AgentConfiguration? Configuration,
    IReadOnlyList<AgentConfigError> Errors,
    IReadOnlyList<AgentConfigWarning> Warnings,
    string SourceName)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigValidationResult"/> record
    /// with default empty collections for errors and warnings.
    /// </summary>
    public AgentConfigValidationResult() : this(
        IsValid: false,
        Configuration: null,
        Errors: Array.Empty<AgentConfigError>(),
        Warnings: Array.Empty<AgentConfigWarning>(),
        SourceName: string.Empty)
    {
    }

    /// <summary>
    /// Creates a successful validation result with an optional list of warnings.
    /// </summary>
    /// <param name="config">The validated agent configuration.</param>
    /// <param name="sourceName">Source file or resource name.</param>
    /// <param name="warnings">Optional warnings that don't prevent loading.</param>
    /// <returns>A validation result indicating success.</returns>
    public static AgentConfigValidationResult Success(
        AgentConfiguration config,
        string sourceName,
        IReadOnlyList<AgentConfigWarning>? warnings = null) =>
        new(
            IsValid: true,
            Configuration: config,
            Errors: Array.Empty<AgentConfigError>(),
            Warnings: warnings ?? Array.Empty<AgentConfigWarning>(),
            SourceName: sourceName);

    /// <summary>
    /// Creates a failed validation result with a list of errors.
    /// </summary>
    /// <param name="sourceName">Source file or resource name.</param>
    /// <param name="errors">Validation errors that prevented loading.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static AgentConfigValidationResult Failure(
        string sourceName,
        IReadOnlyList<AgentConfigError> errors) =>
        new(
            IsValid: false,
            Configuration: null,
            Errors: errors,
            Warnings: Array.Empty<AgentConfigWarning>(),
            SourceName: sourceName);

    /// <summary>
    /// Gets a formatted string containing all error messages.
    /// </summary>
    /// <returns>A multi-line string with all errors, or empty if no errors.</returns>
    public string GetErrorSummary() =>
        Errors.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, Errors.Select(e => $"- {e.Property}: {e.Message}"));

    /// <summary>
    /// Gets a formatted string containing all warning messages.
    /// </summary>
    /// <returns>A multi-line string with all warnings, or empty if no warnings.</returns>
    public string GetWarningSummary() =>
        Warnings.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, Warnings.Select(w => $"- {w.Property}: {w.Message}"));
}

/// <summary>
/// Represents a validation error that prevents loading an agent configuration.
/// </summary>
/// <param name="Property">The property or field name that caused the error (e.g., "agent_id").</param>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="Line">Optional line number in the YAML file where the error occurred.</param>
/// <param name="Column">Optional column number in the YAML file where the error occurred.</param>
/// <remarks>
/// <para>
/// Validation errors are blocking issues that prevent an agent configuration from being
/// loaded. Common errors include:
/// <list type="bullet">
/// <item><description>Missing required fields (e.g., <c>agent_id</c>, <c>name</c>).</description></item>
/// <item><description>Invalid format (e.g., non-kebab-case IDs, out-of-range temperatures).</description></item>
/// <item><description>YAML syntax errors (e.g., malformed structure).</description></item>
/// <item><description>Duplicate values (e.g., duplicate persona IDs).</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public sealed record AgentConfigError(
    string Property,
    string Message,
    int? Line = null,
    int? Column = null)
{
    /// <summary>
    /// Gets a formatted string representation of this error with line/column info if available.
    /// </summary>
    /// <returns>A string like "agent_id: Must be kebab-case" or "agent_id (line 5): Must be kebab-case".</returns>
    public override string ToString()
    {
        var location = Line.HasValue
            ? Column.HasValue
                ? $" (line {Line}, col {Column})"
                : $" (line {Line})"
            : string.Empty;

        return $"{Property}{location}: {Message}";
    }
}

/// <summary>
/// Represents a validation warning that doesn't prevent loading but should be addressed.
/// </summary>
/// <param name="Property">The property or field name that triggered the warning.</param>
/// <param name="Message">A human-readable description of the warning.</param>
/// <param name="Line">Optional line number in the YAML file where the warning was triggered.</param>
/// <remarks>
/// <para>
/// Validation warnings indicate potential issues that don't block loading but suggest
/// improvements or best practices. Common warnings include:
/// <list type="bullet">
/// <item><description>High token limits (e.g., <c>max_tokens &gt; 128000</c>).</description></item>
/// <item><description>Deprecated fields or patterns.</description></item>
/// <item><description>Suboptimal configurations (e.g., very high temperatures).</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public sealed record AgentConfigWarning(
    string Property,
    string Message,
    int? Line = null)
{
    /// <summary>
    /// Gets a formatted string representation of this warning with line info if available.
    /// </summary>
    /// <returns>A string like "max_tokens: Value is very high" or "max_tokens (line 12): Value is very high".</returns>
    public override string ToString()
    {
        var location = Line.HasValue ? $" (line {Line})" : string.Empty;
        return $"{Property}{location}: {Message}";
    }
}

/// <summary>
/// Event arguments for the <see cref="IAgentConfigLoader.AgentFileChanged"/> event.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when a workspace agent YAML file is created, modified, or deleted.
/// The <see cref="ValidationResult"/> property contains the parsed configuration if the
/// file was valid, or errors if it was invalid.
/// </para>
/// <para>
/// For deleted files, <see cref="ValidationResult"/> is <c>null</c>.
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public sealed class AgentFileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the absolute path to the file that changed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the type of change that occurred.
    /// </summary>
    public required AgentFileChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets or sets the validation result after parsing the changed file.
    /// </summary>
    /// <remarks>
    /// This property is <c>null</c> if <see cref="ChangeType"/> is <see cref="AgentFileChangeType.Deleted"/>.
    /// For <see cref="AgentFileChangeType.Created"/> or <see cref="AgentFileChangeType.Modified"/>,
    /// this contains the result of parsing and validating the file.
    /// </remarks>
    public AgentConfigValidationResult? ValidationResult { get; init; }
}

/// <summary>
/// Specifies the type of change detected for a workspace agent file.
/// </summary>
/// <remarks>
/// <para>
/// This enum is used in <see cref="AgentFileChangedEventArgs"/> to indicate what kind
/// of file system event triggered the <see cref="IAgentConfigLoader.AgentFileChanged"/> event.
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public enum AgentFileChangeType
{
    /// <summary>
    /// A new agent configuration file was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing agent configuration file was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// An agent configuration file was deleted.
    /// </summary>
    Deleted
}
