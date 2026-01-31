// <copyright file="ReadabilityHudViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Readability HUD Widget displaying readability metrics.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3d - Provides a compact visual display for readability metrics.</para>
/// <para>Displays three key metrics from IReadabilityService:</para>
/// <list type="bullet">
///   <item>Flesch-Kincaid Grade Level - Color-coded by difficulty (0-18+)</item>
///   <item>Gunning Fog Index - Education years badge (0-20+)</item>
///   <item>Flesch Reading Ease - 0-100 circular score ring (higher = easier)</item>
/// </list>
/// <para>Color Scale (Grade Level):</para>
/// <list type="bullet">
///   <item>0-6: #22C55E (Green - Easy)</item>
///   <item>7-9: #84CC16 (Light Green)</item>
///   <item>10-12: #EAB308 (Yellow)</item>
///   <item>13-15: #F97316 (Orange)</item>
///   <item>16+: #EF4444 (Red - Hard)</item>
/// </list>
/// <para>Thread-safe: all operations are UI-thread safe.</para>
/// <para>License: Writer Pro tier (soft-gated, graceful degradation).</para>
/// </remarks>
public partial class ReadabilityHudViewModel : ObservableObject, IReadabilityHudViewModel
{
    #region Dependencies

    private readonly IReadabilityService _readabilityService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<ReadabilityHudViewModel> _logger;

    #endregion

    #region Color Constants

    // Grade Level colors (lower = easier = greener)
    private const string GradeLevelEasyColor = "#22C55E";      // 0-6: Green
    private const string GradeLevelFairlyEasyColor = "#84CC16"; // 7-9: Light Green
    private const string GradeLevelStandardColor = "#EAB308";   // 10-12: Yellow
    private const string GradeLevelDifficultColor = "#F97316";  // 13-15: Orange
    private const string GradeLevelHardColor = "#EF4444";       // 16+: Red

    // Reading Ease colors (higher = easier = greener)
    private const string ReadingEaseVeryEasyColor = "#22C55E"; // 80-100: Green
    private const string ReadingEaseEasyColor = "#84CC16";     // 60-79: Light Green
    private const string ReadingEaseStandardColor = "#EAB308"; // 40-59: Yellow
    private const string ReadingEaseDifficultColor = "#F97316"; // 20-39: Orange
    private const string ReadingEaseHardColor = "#EF4444";     // 0-19: Red

    // Fog Index colors
    private const string FogIndexEasyColor = "#22C55E";    // 0-8: Green
    private const string FogIndexMediumColor = "#EAB308";  // 9-12: Yellow
    private const string FogIndexHardColor = "#EF4444";    // 13+: Red

    // Default/unlicensed color
    private const string DefaultColor = "#9CA3AF"; // Gray

    #endregion

    #region Observable Properties

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isAnalyzing;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isLicensed;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _hasMetrics;

    /// <inheritdoc/>
    [ObservableProperty]
    private double _fleschKincaidGradeLevel;

    /// <inheritdoc/>
    [ObservableProperty]
    private double _gunningFogIndex;

    /// <inheritdoc/>
    [ObservableProperty]
    private double _fleschReadingEase;

    /// <inheritdoc/>
    [ObservableProperty]
    private double _wordsPerSentence;

    /// <inheritdoc/>
    [ObservableProperty]
    private int _wordCount;

    /// <inheritdoc/>
    [ObservableProperty]
    private int _sentenceCount;

    #endregion

    #region Computed Display Properties

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Format to one decimal place for precision without clutter.
    /// </remarks>
    public string GradeLevelDisplay => HasMetrics ? $"{FleschKincaidGradeLevel:F1}" : "--";

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Format to one decimal place for precision without clutter.
    /// </remarks>
    public string FogIndexDisplay => HasMetrics ? $"{GunningFogIndex:F1}" : "--";

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Format as integer since Reading Ease is a 0-100 scale.
    /// </remarks>
    public string ReadingEaseDisplay => HasMetrics ? $"{FleschReadingEase:F0}" : "--";

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Format to one decimal place for sentence length analysis.
    /// </remarks>
    public string WordsPerSentenceDisplay => HasMetrics ? $"{WordsPerSentence:F1}" : "--";

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Maps Reading Ease score to descriptive categories per specification.
    /// </remarks>
    public string InterpretationDisplay => FleschReadingEase switch
    {
        >= 90 => "Very Easy",
        >= 80 => "Easy",
        >= 70 => "Fairly Easy",
        >= 60 => "Standard",
        >= 50 => "Fairly Difficult",
        >= 30 => "Difficult",
        _ when HasMetrics => "Very Confusing",
        _ => "No Data"
    };

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Maps FK Grade Level to U.S. education levels per specification.
    /// </remarks>
    public string GradeLevelDescription => FleschKincaidGradeLevel switch
    {
        <= 5 when HasMetrics => "Elementary",
        <= 8 when HasMetrics => "Middle School",
        <= 12 when HasMetrics => "High School",
        <= 16 when HasMetrics => "College",
        _ when HasMetrics => "Graduate",
        _ => "No Data"
    };

    #endregion

    #region Computed Color Properties

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Color scale for Grade Level (lower = easier = greener).
    /// </remarks>
    public string GradeLevelColor => FleschKincaidGradeLevel switch
    {
        <= 6 when HasMetrics => GradeLevelEasyColor,
        <= 9 when HasMetrics => GradeLevelFairlyEasyColor,
        <= 12 when HasMetrics => GradeLevelStandardColor,
        <= 15 when HasMetrics => GradeLevelDifficultColor,
        _ when HasMetrics => GradeLevelHardColor,
        _ => DefaultColor
    };

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Color scale for Reading Ease (higher = easier = greener).
    /// </remarks>
    public string ReadingEaseColor => FleschReadingEase switch
    {
        >= 80 when HasMetrics => ReadingEaseVeryEasyColor,
        >= 60 when HasMetrics => ReadingEaseEasyColor,
        >= 40 when HasMetrics => ReadingEaseStandardColor,
        >= 20 when HasMetrics => ReadingEaseDifficultColor,
        _ when HasMetrics => ReadingEaseHardColor,
        _ => DefaultColor
    };

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Color scale for Fog Index (lower = easier = greener).
    /// </remarks>
    public string FogIndexColor => GunningFogIndex switch
    {
        <= 8 when HasMetrics => FogIndexEasyColor,
        <= 12 when HasMetrics => FogIndexMediumColor,
        _ when HasMetrics => FogIndexHardColor,
        _ => DefaultColor
    };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadabilityHudViewModel"/> class.
    /// </summary>
    /// <param name="readabilityService">The readability analysis service.</param>
    /// <param name="licenseService">The license validation service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ReadabilityHudViewModel(
        IReadabilityService readabilityService,
        ILicenseService licenseService,
        ILogger<ReadabilityHudViewModel> logger)
    {
        _readabilityService = readabilityService ?? throw new ArgumentNullException(nameof(readabilityService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Check initial license status
        IsLicensed = _licenseService.IsFeatureEnabled(FeatureCodes.ReadabilityHud);

        _logger.LogDebug(
            "ReadabilityHudViewModel initialized. Licensed: {IsLicensed}",
            IsLicensed);
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    /// <remarks>
    /// <para>LOGIC: Update sequence with license gating:</para>
    /// <list type="number">
    ///   <item>Check license status (refresh each call)</item>
    ///   <item>If unlicensed, reset and return gracefully</item>
    ///   <item>Set IsAnalyzing = true</item>
    ///   <item>Call IReadabilityService.AnalyzeAsync()</item>
    ///   <item>Update all observable properties from metrics</item>
    ///   <item>Set IsAnalyzing = false, HasMetrics = true</item>
    ///   <item>Notify PropertyChanged for all computed properties</item>
    /// </list>
    /// </remarks>
    public async Task UpdateAsync(string text, CancellationToken ct = default)
    {
        _logger.LogDebug("ReadabilityHudViewModel.UpdateAsync called with {TextLength} characters",
            text?.Length ?? 0);

        // STEP 1: Refresh license status
        IsLicensed = _licenseService.IsFeatureEnabled(FeatureCodes.ReadabilityHud);

        // STEP 2: Check license - graceful degradation
        if (!IsLicensed)
        {
            _logger.LogDebug("Readability feature not licensed, skipping analysis");
            Reset();
            return;
        }

        // STEP 3: Handle empty/null text
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text provided, resetting metrics");
            Reset();
            return;
        }

        try
        {
            // STEP 4: Begin analysis
            IsAnalyzing = true;
            _logger.LogDebug("Starting readability analysis");

            // STEP 5: Call readability service
            var metrics = await _readabilityService.AnalyzeAsync(text, ct).ConfigureAwait(false);

            // STEP 6: Check for cancellation
            ct.ThrowIfCancellationRequested();

            // STEP 7: Update all metric properties
            FleschKincaidGradeLevel = metrics.FleschKincaidGradeLevel;
            GunningFogIndex = metrics.GunningFogIndex;
            FleschReadingEase = metrics.FleschReadingEase;
            WordsPerSentence = metrics.AverageWordsPerSentence;
            WordCount = metrics.WordCount;
            SentenceCount = metrics.SentenceCount;

            // STEP 8: Mark as having valid metrics
            HasMetrics = metrics.WordCount > 0;

            // STEP 9: Notify computed property changes
            NotifyComputedPropertiesChanged();

            _logger.LogInformation(
                "Readability analysis complete: FK Grade={GradeLevel:F1}, Fog={FogIndex:F1}, " +
                "Ease={ReadingEase:F0} ({Interpretation}), Words={WordCount}, Sentences={SentenceCount}",
                FleschKincaidGradeLevel,
                GunningFogIndex,
                FleschReadingEase,
                InterpretationDisplay,
                WordCount,
                SentenceCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Readability analysis cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during readability analysis");
            Reset();
        }
        finally
        {
            // STEP 10: Always mark analysis complete
            IsAnalyzing = false;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Clears all metrics to initial state. Used when:
    /// - Switching documents
    /// - Clearing the panel
    /// - License becomes invalid
    /// - Empty text is provided
    /// </remarks>
    public void Reset()
    {
        _logger.LogDebug("ReadabilityHudViewModel.Reset called");

        // Clear all metric values
        FleschKincaidGradeLevel = 0;
        GunningFogIndex = 0;
        FleschReadingEase = 0;
        WordsPerSentence = 0;
        WordCount = 0;
        SentenceCount = 0;

        // Clear state flags
        HasMetrics = false;
        IsAnalyzing = false;

        // Notify computed property changes
        NotifyComputedPropertiesChanged();

        _logger.LogInformation("ReadabilityHudViewModel reset to initial state");
    }

    /// <summary>
    /// Notifies PropertyChanged for all computed properties.
    /// </summary>
    /// <remarks>
    /// LOGIC: Computed properties don't trigger automatically, so we must
    /// manually notify when underlying values change.
    /// </remarks>
    private void NotifyComputedPropertiesChanged()
    {
        OnPropertyChanged(nameof(GradeLevelDisplay));
        OnPropertyChanged(nameof(FogIndexDisplay));
        OnPropertyChanged(nameof(ReadingEaseDisplay));
        OnPropertyChanged(nameof(WordsPerSentenceDisplay));
        OnPropertyChanged(nameof(InterpretationDisplay));
        OnPropertyChanged(nameof(GradeLevelDescription));
        OnPropertyChanged(nameof(GradeLevelColor));
        OnPropertyChanged(nameof(ReadingEaseColor));
        OnPropertyChanged(nameof(FogIndexColor));
    }

    #endregion
}
