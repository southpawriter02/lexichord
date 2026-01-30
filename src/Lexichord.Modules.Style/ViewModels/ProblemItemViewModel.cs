using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for a single problem item in the Problems Panel.
/// </summary>
/// <remarks>
/// LOGIC: Represents one style violation as a displayable row.
/// Maps data from <see cref="AggregatedStyleViolation"/> to view-friendly properties.
///
/// Design Decisions:
/// - Immutable after construction for thread safety
/// - All properties are read-only (display only)
/// - SeverityIcon computed from severity for UI consistency
///
/// Version: v0.2.6a
/// </remarks>
public sealed partial class ProblemItemViewModel : ObservableObject, IProblemItem
{
    private readonly ILogger<ProblemItemViewModel>? _logger;

    #region Properties

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public int Line { get; }

    /// <inheritdoc/>
    public int Column { get; }

    /// <inheritdoc/>
    public string Message { get; }

    /// <inheritdoc/>
    public string RuleId { get; }

    /// <inheritdoc/>
    public ViolationSeverity Severity { get; }

    /// <inheritdoc/>
    public string ViolatingText { get; }

    /// <inheritdoc/>
    public string DocumentId { get; }

    /// <inheritdoc/>
    public int StartOffset { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Returns Unicode icons for severity levels:
    /// - Error: ⛔ (U+26D4)
    /// - Warning: ⚠️ (U+26A0)
    /// - Info: ℹ️ (U+2139)
    /// - Hint: 💡 (U+1F4A1)
    /// </remarks>
    public string SeverityIcon => Severity switch
    {
        ViolationSeverity.Error => "⛔",
        ViolationSeverity.Warning => "⚠",
        ViolationSeverity.Info => "ℹ",
        ViolationSeverity.Hint => "💡",
        _ => "•"
    };

    /// <summary>
    /// Gets the formatted location string (Line:Column).
    /// </summary>
    /// <remarks>
    /// LOGIC: Standard location format for problem panels.
    /// Example: "42:15" for line 42, column 15.
    /// </remarks>
    public string Location => $"{Line}:{Column}";

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemItemViewModel"/> class.
    /// </summary>
    /// <param name="violation">The aggregated style violation to display.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when violation is null.</exception>
    /// <remarks>
    /// LOGIC: Maps all properties from the violation record.
    /// Logger is optional to support test scenarios without DI.
    ///
    /// Version: v0.2.6a
    /// </remarks>
    public ProblemItemViewModel(
        AggregatedStyleViolation violation,
        ILogger<ProblemItemViewModel>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(violation);

        _logger = logger;

        Id = violation.Id;
        Line = violation.Line;
        Column = violation.Column;
        Message = violation.Message;
        RuleId = violation.RuleId;
        Severity = violation.Severity;
        ViolatingText = violation.ViolatingText;
        DocumentId = violation.DocumentId;
        StartOffset = violation.StartOffset;

        _logger?.LogTrace(
            "Created ProblemItemViewModel for violation {ViolationId} at {Line}:{Column}",
            Id, Line, Column);
    }

    #endregion
}
