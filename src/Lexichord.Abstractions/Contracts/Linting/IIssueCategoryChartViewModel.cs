// <copyright file="IIssueCategoryChartViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a category of style issues with aggregated count.
/// </summary>
/// <remarks>
/// LOGIC: v0.6.4b - Provides category-level aggregation for bar chart visualization.
/// Categories are derived from rule metadata or inferred from rule patterns.
/// </remarks>
public record IssueCategoryData(
    string CategoryName,
    string CategoryId,
    int IssueCount,
    string Color)
{
    /// <summary>
    /// Gets whether this category has any issues.
    /// </summary>
    public bool HasIssues => IssueCount > 0;

    /// <summary>
    /// Creates a category with a default color based on the category name.
    /// </summary>
    public static IssueCategoryData Create(string categoryName, string categoryId, int count) =>
        new(categoryName, categoryId, count, GetDefaultColor(categoryId));

    private static string GetDefaultColor(string categoryId) => categoryId.ToLowerInvariant() switch
    {
        "terminology" => "#F87171", // Red
        "passive-voice" => "#FBBF24", // Amber
        "sentence-length" => "#60A5FA", // Blue
        "readability" => "#34D399", // Green
        "grammar" => "#A78BFA", // Purple
        "style" => "#FB923C", // Orange
        "structure" => "#2DD4BF", // Teal
        "custom" => "#94A3B8", // Slate
        _ => "#6B7280" // Gray
    };
}

/// <summary>
/// ViewModel interface for the Issues by Category bar chart component.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.6.4b - The Issues by Category chart provides visual breakdown of violations.</para>
/// <para>Features:</para>
/// <list type="bullet">
///   <item>Horizontal bar chart visualization</item>
///   <item>Category-level aggregation from violations</item>
///   <item>Real-time updates via LintingCompletedEvent</item>
///   <item>Click-to-filter support for drilling down</item>
///   <item>Responsive layout with tooltip details</item>
/// </list>
/// <para>Data flows from ProblemsPanelViewModel violations grouped by rule category.</para>
/// </remarks>
public interface IIssueCategoryChartViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Gets whether the chart has data to display.
    /// </summary>
    bool HasData { get; }

    /// <summary>
    /// Gets whether the chart is currently loading/computing.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the total count of issues across all categories.
    /// </summary>
    int TotalIssueCount { get; }

    /// <summary>
    /// Gets the collection of issue categories with counts.
    /// </summary>
    ReadOnlyObservableCollection<IssueCategoryData> Categories { get; }

    /// <summary>
    /// Gets the currently selected category (null if none selected).
    /// </summary>
    IssueCategoryData? SelectedCategory { get; }

    /// <summary>
    /// Refreshes the category data from current violations.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Event raised when a category is clicked for filtering.
    /// </summary>
    event EventHandler<IssueCategoryData>? CategorySelected;
}
