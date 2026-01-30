using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Abstractions.Validation;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Term Editor Dialog.
/// </summary>
/// <remarks>
/// LOGIC: Manages form state for adding and editing style terms.
/// 
/// Key responsibilities:
/// - Two-way binding for all form fields
/// - Real-time pattern validation via TermPatternValidator
/// - Pattern testing against sample text
/// - Dirty state tracking for unsaved changes
/// - Save/Cancel operations via ITerminologyService
/// 
/// License requirement: WriterPro (enforced by ITermEditorDialogService)
/// 
/// Version: v0.2.5c
/// </remarks>
public partial class TermEditorViewModel : ObservableObject
{
    private readonly ITerminologyService _terminologyService;
    private readonly ILogger<TermEditorViewModel> _logger;
    private readonly StyleTerm? _originalTerm;
    
    private static readonly TimeSpan PatternTestTimeout = TimeSpan.FromMilliseconds(100);
    
    #region Constructor

    /// <summary>
    /// Creates a TermEditorViewModel for adding a new term.
    /// </summary>
    public TermEditorViewModel(
        ITerminologyService terminologyService,
        ILogger<TermEditorViewModel> logger)
        : this(terminologyService, logger, null)
    {
    }

    /// <summary>
    /// Creates a TermEditorViewModel for editing an existing term.
    /// </summary>
    public TermEditorViewModel(
        ITerminologyService terminologyService,
        ILogger<TermEditorViewModel> logger,
        StyleTerm? existingTerm)
    {
        _terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _originalTerm = existingTerm;

        // LOGIC: Initialize form fields from existing term or defaults
        if (existingTerm is not null)
        {
            _pattern = existingTerm.Term;
            _recommendation = existingTerm.Replacement ?? string.Empty;
            _category = existingTerm.Category;
            _severity = existingTerm.Severity;
            _notes = existingTerm.Notes ?? string.Empty;
            _isActive = existingTerm.IsActive;
            _matchCase = existingTerm.MatchCase;
            
            DialogTitle = $"Edit Term: {existingTerm.Term}";
            _logger.LogDebug("Initialized TermEditorViewModel in Edit mode for term {TermId}", existingTerm.Id);
        }
        else
        {
            _pattern = string.Empty;
            _recommendation = string.Empty;
            _category = "General";
            _severity = "Suggestion";
            _notes = string.Empty;
            _isActive = true;
            _matchCase = false;
            
            DialogTitle = "Add New Term";
            _logger.LogDebug("Initialized TermEditorViewModel in Add mode");
        }
        
        // LOGIC: Perform initial validation
        ValidatePattern();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets whether this dialog is in edit mode (vs add mode).
    /// </summary>
    public bool IsEditMode => _originalTerm is not null;

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string DialogTitle { get; }

    /// <summary>
    /// Gets the available categories for the dropdown.
    /// </summary>
    public IReadOnlyList<string> Categories { get; } = new[]
    {
        "General", "Terminology", "Brand", "Legal", "Technical", "Formatting"
    };

    /// <summary>
    /// Gets the available severities for the dropdown.
    /// </summary>
    public IReadOnlyList<string> Severities { get; } = new[]
    {
        "Error", "Warning", "Suggestion", "Info"
    };

    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string _pattern;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string _recommendation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string _category;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string _severity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string _notes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private bool _isActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private bool _matchCase;

    [ObservableProperty]
    private string _sampleText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    [NotifyPropertyChangedFor(nameof(HasValidationError))]
    private Result<bool>? _validationResult;

    [ObservableProperty]
    private PatternTestResult? _testResult;

    [ObservableProperty]
    private bool _isTesting;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether the form can be saved.
    /// </summary>
    /// <remarks>
    /// LOGIC: Save is enabled when:
    /// - Pattern is not empty
    /// - Category is not empty
    /// - Pattern validation passes
    /// </remarks>
    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Pattern) &&
        !string.IsNullOrWhiteSpace(Category) &&
        ValidationResult is { IsSuccess: true };

    /// <summary>
    /// Gets whether the form has unsaved changes.
    /// </summary>
    public bool IsDirty
    {
        get
        {
            if (_originalTerm is null)
            {
                // LOGIC: Add mode - dirty if any required field has content
                return !string.IsNullOrWhiteSpace(Pattern) ||
                       !string.IsNullOrWhiteSpace(Recommendation);
            }

            // LOGIC: Edit mode - dirty if any field differs from original
            return Pattern != _originalTerm.Term ||
                   Recommendation != (_originalTerm.Replacement ?? string.Empty) ||
                   Category != _originalTerm.Category ||
                   Severity != _originalTerm.Severity ||
                   Notes != (_originalTerm.Notes ?? string.Empty) ||
                   IsActive != _originalTerm.IsActive ||
                   MatchCase != _originalTerm.MatchCase;
        }
    }

    /// <summary>
    /// Gets the validation error message, if any.
    /// </summary>
    public string? ValidationError => ValidationResult?.Error;

    /// <summary>
    /// Gets whether there is a validation error.
    /// </summary>
    public bool HasValidationError => ValidationResult is { IsFailure: true };

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    /// <remarks>
    /// LOGIC: The bool parameter indicates success (true) or cancel (false).
    /// </remarks>
    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Property Change Handlers

    partial void OnPatternChanged(string value)
    {
        ValidatePattern();
        if (!string.IsNullOrWhiteSpace(SampleText))
        {
            RunPatternTest();
        }
    }

    partial void OnSampleTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            RunPatternTest();
        }
        else
        {
            TestResult = null;
        }
    }

    partial void OnMatchCaseChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(SampleText))
        {
            RunPatternTest();
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Saves the term and closes the dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving term: {Pattern}", Pattern);

        try
        {
            if (_originalTerm is null)
            {
                // LOGIC: Create new term
                var command = new CreateTermCommand(
                    Term: Pattern,
                    Replacement: string.IsNullOrWhiteSpace(Recommendation) ? null : Recommendation,
                    Category: Category,
                    Severity: Severity,
                    Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    MatchCase: MatchCase);

                var result = await _terminologyService.CreateAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Created new term with ID {TermId}", result.Value);
                    CloseRequested?.Invoke(this, true);
                }
                else
                {
                    _logger.LogWarning("Failed to create term: {Error}", result.Error);
                    ValidationResult = Result<bool>.Failure(result.Error ?? "Failed to create term");
                }
            }
            else
            {
                // LOGIC: Update existing term
                var command = new UpdateTermCommand(
                    Id: _originalTerm.Id,
                    Term: Pattern,
                    Replacement: string.IsNullOrWhiteSpace(Recommendation) ? null : Recommendation,
                    Category: Category,
                    Severity: Severity,
                    Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    MatchCase: MatchCase);

                var result = await _terminologyService.UpdateAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Updated term {TermId}", _originalTerm.Id);
                    CloseRequested?.Invoke(this, true);
                }
                else
                {
                    _logger.LogWarning("Failed to update term: {Error}", result.Error);
                    ValidationResult = Result<bool>.Failure(result.Error ?? "Failed to update term");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving term");
            ValidationResult = Result<bool>.Failure($"Error saving term: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels editing and closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _logger.LogDebug("Cancelling term editor dialog");
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>
    /// Manually triggers a pattern test.
    /// </summary>
    [RelayCommand]
    private void TestPattern()
    {
        RunPatternTest();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates the current pattern.
    /// </summary>
    private void ValidatePattern()
    {
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            ValidationResult = Result<bool>.Failure("Pattern is required");
            return;
        }

        ValidationResult = TermPatternValidator.Validate(Pattern);
        _logger.LogTrace("Pattern validation: {IsValid}", ValidationResult.IsSuccess);
    }

    /// <summary>
    /// Tests the pattern against the sample text.
    /// </summary>
    private void RunPatternTest()
    {
        if (string.IsNullOrWhiteSpace(Pattern) || string.IsNullOrWhiteSpace(SampleText))
        {
            TestResult = null;
            return;
        }

        if (ValidationResult is { IsFailure: true })
        {
            TestResult = PatternTestResult.Failure("Pattern is invalid");
            return;
        }

        IsTesting = true;

        try
        {
            var matches = new List<PatternMatch>();
            var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // LOGIC: Check if pattern looks like regex
            if (TermPatternValidator.LooksLikeRegex(Pattern))
            {
                try
                {
                    var options = MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    var regex = new Regex(Pattern, options | RegexOptions.Compiled, PatternTestTimeout);

                    foreach (Match match in regex.Matches(SampleText))
                    {
                        matches.Add(new PatternMatch(match.Index, match.Length, match.Value));
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    TestResult = PatternTestResult.Timeout();
                    return;
                }
            }
            else
            {
                // LOGIC: Literal pattern matching
                var index = 0;
                while ((index = SampleText.IndexOf(Pattern, index, comparison)) >= 0)
                {
                    matches.Add(new PatternMatch(index, Pattern.Length, SampleText.Substring(index, Pattern.Length)));
                    index += Pattern.Length;
                }
            }

            TestResult = PatternTestResult.Success(matches);
            _logger.LogTrace("Pattern test found {Count} matches", matches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during pattern test");
            TestResult = PatternTestResult.Failure($"Test error: {ex.Message}");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion
}
