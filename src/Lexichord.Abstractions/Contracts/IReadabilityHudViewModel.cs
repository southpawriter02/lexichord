// <copyright file="IReadabilityHudViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// ViewModel interface for the Readability HUD Widget.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3d - Provides a compact visual display for readability metrics.</para>
/// <para>Displays three key metrics from IReadabilityService:</para>
/// <list type="bullet">
///   <item>Flesch-Kincaid Grade Level - Color-coded by difficulty</item>
///   <item>Gunning Fog Index - Education years badge</item>
///   <item>Flesch Reading Ease - 0-100 circular score ring</item>
/// </list>
/// <para>Thread-safe: all operations are UI-thread safe.</para>
/// <para>License: Writer Pro tier (soft-gated, graceful degradation).</para>
/// </remarks>
/// <example>
/// <code>
/// // Inject via DI
/// public MyViewModel(IReadabilityHudViewModel readabilityHud)
/// {
///     _readabilityHud = readabilityHud;
/// }
/// 
/// // Update on document change
/// await _readabilityHud.UpdateAsync(documentText, cancellationToken);
/// </code>
/// </example>
public interface IReadabilityHudViewModel
{
    #region Core Properties

    /// <summary>
    /// Gets a value indicating whether the widget is currently analyzing text.
    /// </summary>
    /// <remarks>
    /// LOGIC: True during async analysis, enables loading indicators in UI.
    /// </remarks>
    bool IsAnalyzing { get; }

    /// <summary>
    /// Gets a value indicating whether the readability feature is licensed.
    /// </summary>
    /// <remarks>
    /// LOGIC: False when user lacks Writer Pro tier. Widget shows upgrade prompt.
    /// </remarks>
    bool IsLicensed { get; }

    /// <summary>
    /// Gets a value indicating whether metrics are available for display.
    /// </summary>
    /// <remarks>
    /// LOGIC: True after successful analysis with valid text (non-empty, non-whitespace).
    /// </remarks>
    bool HasMetrics { get; }

    #endregion

    #region Metric Values

    /// <summary>
    /// Gets the Flesch-Kincaid Grade Level score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Represents U.S. school grade level (0-18+).
    /// </remarks>
    double FleschKincaidGradeLevel { get; }

    /// <summary>
    /// Gets the Gunning Fog Index score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Years of formal education needed (0-20+).
    /// </remarks>
    double GunningFogIndex { get; }

    /// <summary>
    /// Gets the Flesch Reading Ease score.
    /// </summary>
    /// <remarks>
    /// LOGIC: 0-100 scale where higher = easier reading.
    /// </remarks>
    double FleschReadingEase { get; }

    /// <summary>
    /// Gets the average words per sentence.
    /// </summary>
    double WordsPerSentence { get; }

    /// <summary>
    /// Gets the total word count.
    /// </summary>
    int WordCount { get; }

    /// <summary>
    /// Gets the total sentence count.
    /// </summary>
    int SentenceCount { get; }

    #endregion

    #region Display Properties

    /// <summary>
    /// Gets the formatted Flesch-Kincaid Grade Level display string (e.g., "8.2").
    /// </summary>
    string GradeLevelDisplay { get; }

    /// <summary>
    /// Gets the formatted Gunning Fog Index display string (e.g., "10.5").
    /// </summary>
    string FogIndexDisplay { get; }

    /// <summary>
    /// Gets the formatted Flesch Reading Ease display string (e.g., "65").
    /// </summary>
    string ReadingEaseDisplay { get; }

    /// <summary>
    /// Gets the formatted words per sentence display string (e.g., "15.3").
    /// </summary>
    string WordsPerSentenceDisplay { get; }

    /// <summary>
    /// Gets the human-readable interpretation of reading ease (e.g., "Standard").
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps Reading Ease score to descriptive categories:
    /// 90-100: Very Easy, 80-89: Easy, 70-79: Fairly Easy,
    /// 60-69: Standard, 50-59: Fairly Difficult, 30-49: Difficult, 0-29: Very Confusing
    /// </remarks>
    string InterpretationDisplay { get; }

    /// <summary>
    /// Gets the grade level description (e.g., "8th Grade", "College").
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps FK Grade Level to education level:
    /// 0-5: Elementary, 6-8: Middle School, 9-12: High School, 13+: College/Graduate
    /// </remarks>
    string GradeLevelDescription { get; }

    #endregion

    #region Color Properties

    /// <summary>
    /// Gets the hex color for the Grade Level pill based on difficulty.
    /// </summary>
    /// <remarks>
    /// LOGIC: Color scale per spec:
    /// 0-6: #22C55E (Green - Easy), 7-9: #84CC16 (Light Green),
    /// 10-12: #EAB308 (Yellow), 13-15: #F97316 (Orange), 16+: #EF4444 (Red - Hard)
    /// </remarks>
    string GradeLevelColor { get; }

    /// <summary>
    /// Gets the hex color for the Reading Ease score ring.
    /// </summary>
    /// <remarks>
    /// LOGIC: Color scale per spec (inverted - higher ease = greener):
    /// 80-100: #22C55E (Green - Easy), 60-79: #84CC16 (Light Green),
    /// 40-59: #EAB308 (Yellow), 20-39: #F97316 (Orange), 0-19: #EF4444 (Red - Hard)
    /// </remarks>
    string ReadingEaseColor { get; }

    /// <summary>
    /// Gets the hex color for the Fog Index badge.
    /// </summary>
    /// <remarks>
    /// LOGIC: Color scale per spec:
    /// 0-8: #22C55E (Green - Easy), 9-12: #EAB308 (Yellow), 13+: #EF4444 (Red - Hard)
    /// </remarks>
    string FogIndexColor { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Asynchronously analyzes text and updates all metric properties.
    /// </summary>
    /// <param name="text">The text to analyze. MAY be null or empty.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>LOGIC: Update sequence:</para>
    /// <list type="number">
    ///   <item>Check license status (graceful degradation if unlicensed)</item>
    ///   <item>Set IsAnalyzing = true</item>
    ///   <item>Call IReadabilityService.AnalyzeAsync()</item>
    ///   <item>Update all observable properties</item>
    ///   <item>Set IsAnalyzing = false</item>
    ///   <item>Notify PropertyChanged for all computed properties</item>
    /// </list>
    /// <para>Thread-safe: safe to call from any thread.</para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task UpdateAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Resets the widget to its initial state with no metrics.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Clears all metrics and sets HasMetrics = false.</para>
    /// <para>Used when switching documents or clearing the panel.</para>
    /// </remarks>
    void Reset();

    #endregion
}
