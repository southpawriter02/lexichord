// -----------------------------------------------------------------------
// <copyright file="KeyTermViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: ViewModel wrapper for KeyTerm display in the Summary Panel (v0.7.6c).
//   Provides computed properties for UI visualization including:
//   - ImportanceLevel (1-5) for dot visualization
//   - IsTechnical flag for distinct styling
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.MetadataExtraction;

namespace Lexichord.Modules.Agents.SummaryExport.ViewModels;

/// <summary>
/// ViewModel wrapper for displaying a key term in the Summary Panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Wraps <see cref="KeyTerm"/> to provide UI-specific computed properties
/// for visualization. The <see cref="ImportanceLevel"/> is used for the 1-5 dot display,
/// and <see cref="IsTechnical"/> controls distinct styling for technical terms.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is immutable and inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="KeyTerm"/>
/// <seealso cref="SummaryPanelViewModel"/>
public sealed class KeyTermViewModel
{
    /// <summary>
    /// Gets the term text.
    /// </summary>
    /// <value>
    /// The key term string extracted from the document.
    /// </value>
    public string Term { get; }

    /// <summary>
    /// Gets the raw importance score.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0 indicating term importance.
    /// </value>
    public double Importance { get; }

    /// <summary>
    /// Gets the importance level for visual display.
    /// </summary>
    /// <value>
    /// An integer from 1 to 5 for dot visualization.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Converts the continuous 0.0-1.0 importance score to a discrete
    /// 1-5 scale by ceiling: <c>(int)Math.Ceiling(Importance * 5)</c>.
    /// </remarks>
    public int ImportanceLevel { get; }

    /// <summary>
    /// Gets whether this is a technical term.
    /// </summary>
    /// <value>
    /// <c>true</c> if the term was classified as technical; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Technical terms receive distinct styling in the UI
    /// (using <c>Brush.Accent.Secondary</c> instead of <c>Brush.Accent.Tertiary</c>).
    /// </remarks>
    public bool IsTechnical { get; }

    /// <summary>
    /// Gets the formatted importance percentage.
    /// </summary>
    /// <value>
    /// A string like "85%" for display.
    /// </value>
    public string ImportancePercent => $"{Importance:P0}";

    /// <summary>
    /// Gets dots representing filled importance level.
    /// </summary>
    /// <value>
    /// A string of filled dots (⬤) based on importance level.
    /// </value>
    public string FilledDots => new string('⬤', ImportanceLevel);

    /// <summary>
    /// Gets dots representing empty importance level.
    /// </summary>
    /// <value>
    /// A string of empty dots (○) for remaining slots up to 5.
    /// </value>
    public string EmptyDots => new string('○', 5 - ImportanceLevel);

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTermViewModel"/> class.
    /// </summary>
    /// <param name="term">The key term to wrap.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="term"/> is <c>null</c>.
    /// </exception>
    public KeyTermViewModel(KeyTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);

        Term = term.Term;
        Importance = term.Importance;
        ImportanceLevel = Math.Max(1, Math.Min(5, (int)Math.Ceiling(term.Importance * 5)));
        IsTechnical = term.IsTechnical;
    }
}
