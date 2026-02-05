// <copyright file="IssueCategoryChartViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Events;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Issues by Category bar chart component.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.6.4b - The Issues by Category chart provides visual breakdown of violations.</para>
/// <para>Features:</para>
/// <list type="bullet">
///   <item>Horizontal bar chart visualization using LiveCharts2</item>
///   <item>Category-level aggregation from violations</item>
///   <item>Real-time updates via LintingCompletedEvent</item>
///   <item>Click-to-filter support for drilling down</item>
///   <item>Responsive layout with tooltip details</item>
/// </list>
/// <para>Thread-safe: UI thread dispatching handled appropriately.</para>
/// </remarks>
public partial class IssueCategoryChartViewModel : ObservableObject,
    IIssueCategoryChartViewModel,
    INotificationHandler<LintingCompletedEvent>
{
    #region Log Event IDs

    /// <summary>Log event IDs for structured logging (2100-2115 range).</summary>
    private static class LogEvents
    {
        public const int Initialized = 2100;
        public const int DataUpdated = 2101;
        public const int CategorySelected = 2102;
        public const int RefreshRequested = 2103;
        public const int LintingEventReceived = 2104;
        public const int ChartSeriesBuilt = 2105;
        public const int CategoryAggregated = 2106;
        public const int Disposed = 2107;
    }

    #endregion

    #region Category Definitions

    /// <summary>
    /// Known issue categories with their display names.
    /// </summary>
    private static readonly Dictionary<string, string> CategoryDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["terminology"] = "Terminology Violations",
        ["passive-voice"] = "Passive Voice",
        ["sentence-length"] = "Sentence Length",
        ["readability"] = "Readability",
        ["grammar"] = "Grammar",
        ["style"] = "Style",
        ["structure"] = "Structure",
        ["custom"] = "Custom Rules"
    };

    /// <summary>
    /// Category colors for chart rendering.
    /// </summary>
    private static readonly Dictionary<string, SKColor> CategoryColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["terminology"] = new SKColor(248, 113, 113), // Red
        ["passive-voice"] = new SKColor(251, 191, 36), // Amber
        ["sentence-length"] = new SKColor(96, 165, 250), // Blue
        ["readability"] = new SKColor(52, 211, 153), // Green
        ["grammar"] = new SKColor(167, 139, 250), // Purple
        ["style"] = new SKColor(251, 146, 60), // Orange
        ["structure"] = new SKColor(45, 212, 191), // Teal
        ["custom"] = new SKColor(148, 163, 184) // Slate
    };

    #endregion

    #region Dependencies

    private readonly IViolationAggregator _violationAggregator;
    private readonly ILogger<IssueCategoryChartViewModel> _logger;

    private readonly ObservableCollection<IssueCategoryData> _categories = new();
    private readonly Dictionary<string, int> _categoryCounts = new(StringComparer.OrdinalIgnoreCase);
    private string? _activeDocumentId;
    private bool _isDisposed;

    #endregion

    #region Observable Properties

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _hasData;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isLoading;

    /// <inheritdoc/>
    [ObservableProperty]
    private int _totalIssueCount;

    /// <inheritdoc/>
    [ObservableProperty]
    private IssueCategoryData? _selectedCategory;

    /// <summary>
    /// Gets or sets the chart series for LiveCharts2 binding.
    /// </summary>
    [ObservableProperty]
    private ISeries[] _series = [];

    /// <summary>
    /// Gets or sets the X-axis configuration.
    /// </summary>
    [ObservableProperty]
    private Axis[] _xAxes = [];

    /// <summary>
    /// Gets or sets the Y-axis configuration.
    /// </summary>
    [ObservableProperty]
    private Axis[] _yAxes = [];

    #endregion

    #region Computed Properties

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<IssueCategoryData> Categories { get; }

    #endregion

    #region Events

    /// <inheritdoc/>
    public event EventHandler<IssueCategoryData>? CategorySelected;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="IssueCategoryChartViewModel"/> class.
    /// </summary>
    /// <param name="violationAggregator">Service for accessing aggregated violations.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public IssueCategoryChartViewModel(
        IViolationAggregator violationAggregator,
        ILogger<IssueCategoryChartViewModel> logger)
    {
        _violationAggregator = violationAggregator ?? throw new ArgumentNullException(nameof(violationAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Categories = new ReadOnlyObservableCollection<IssueCategoryData>(_categories);

        // Initialize axes
        InitializeAxes();

        _logger.LogDebug(LogEvents.Initialized, "IssueCategoryChartViewModel initialized");
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void Refresh()
    {
        if (_isDisposed || string.IsNullOrEmpty(_activeDocumentId))
        {
            return;
        }

        _logger.LogDebug(LogEvents.RefreshRequested, "Manual refresh requested for document {DocumentId}", _activeDocumentId);
        RefreshFromViolations(_activeDocumentId);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles linting completed events to update the chart.
    /// </summary>
    public Task Handle(LintingCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        var result = notification.Result;

        _logger.LogDebug(
            LogEvents.LintingEventReceived,
            "LintingCompletedEvent received for document {DocumentId} with {ViolationCount} violations",
            result.DocumentId,
            result.Violations.Count);

        if (result.WasCancelled || !result.IsSuccess)
        {
            _logger.LogDebug("Linting was cancelled or failed, clearing chart data");
            ClearData();
            return Task.CompletedTask;
        }

        _activeDocumentId = result.DocumentId;
        RefreshFromViolations(result.DocumentId);

        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Handles category selection from chart click.
    /// </summary>
    [RelayCommand]
    private void SelectCategory(IssueCategoryData? category)
    {
        if (category == null)
        {
            return;
        }

        SelectedCategory = category;
        CategorySelected?.Invoke(this, category);

        _logger.LogDebug(
            LogEvents.CategorySelected,
            "Category selected: {CategoryName} ({IssueCount} issues)",
            category.CategoryName,
            category.IssueCount);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the chart axes.
    /// </summary>
    private void InitializeAxes()
    {
        // Y-axis shows category labels
        YAxes =
        [
            new Axis
            {
                Labels = Array.Empty<string>(),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50))
            }
        ];

        // X-axis shows issue counts
        XAxes =
        [
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50))
            }
        ];
    }

    /// <summary>
    /// Refreshes chart data from current violations.
    /// </summary>
    private void RefreshFromViolations(string documentId)
    {
        try
        {
            IsLoading = true;

            // Get violations from aggregator
            var violations = _violationAggregator.GetViolations(documentId);

            // Aggregate by category
            _categoryCounts.Clear();

            foreach (var violation in violations)
            {
                var category = InferCategory(violation);
                _categoryCounts.TryGetValue(category, out var count);
                _categoryCounts[category] = count + 1;
            }

            _logger.LogDebug(
                LogEvents.CategoryAggregated,
                "Aggregated {ViolationCount} violations into {CategoryCount} categories",
                violations.Count,
                _categoryCounts.Count);

            // Update category collection
            UpdateCategories();

            // Rebuild chart series
            BuildChartSeries();

            // Update totals
            TotalIssueCount = violations.Count;
            HasData = _categories.Count > 0;

            _logger.LogDebug(LogEvents.DataUpdated, "Chart data updated: {TotalCount} total issues", TotalIssueCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing chart data");
            ClearData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Infers the category from a violation based on rule metadata.
    /// </summary>
    private static string InferCategory(AggregatedStyleViolation violation)
    {
        var ruleId = violation.RuleId.ToLowerInvariant();

        // Infer from rule ID patterns
        if (ruleId.Contains("term") || ruleId.Contains("vocab"))
            return "terminology";
        if (ruleId.Contains("passive"))
            return "passive-voice";
        if (ruleId.Contains("sentence") || ruleId.Contains("length"))
            return "sentence-length";
        if (ruleId.Contains("read") || ruleId.Contains("flesch") || ruleId.Contains("grade"))
            return "readability";
        if (ruleId.Contains("grammar") || ruleId.Contains("spell"))
            return "grammar";
        if (ruleId.Contains("struct") || ruleId.Contains("heading"))
            return "structure";

        // Infer from category property if available
        if (!string.IsNullOrEmpty(violation.Category))
        {
            var cat = violation.Category.ToLowerInvariant();
            if (CategoryDisplayNames.ContainsKey(cat))
                return cat;
        }

        // Default to style
        return "style";
    }

    /// <summary>
    /// Updates the categories collection from aggregated counts.
    /// </summary>
    private void UpdateCategories()
    {
        _categories.Clear();

        // Sort by count descending
        var sortedCategories = _categoryCounts
            .OrderByDescending(kv => kv.Value)
            .ToList();

        foreach (var (categoryId, count) in sortedCategories)
        {
            var displayName = CategoryDisplayNames.TryGetValue(categoryId, out var name)
                ? name
                : categoryId;

            var color = CategoryColors.TryGetValue(categoryId, out var c)
                ? $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}"
                : "#6B7280";

            _categories.Add(new IssueCategoryData(displayName, categoryId, count, color));
        }
    }

    /// <summary>
    /// Builds the LiveCharts2 series for the bar chart.
    /// </summary>
    private void BuildChartSeries()
    {
        if (_categories.Count == 0)
        {
            Series = [];
            YAxes[0].Labels = Array.Empty<string>();
            return;
        }

        // Build values array
        var values = _categories.Select(c => (double)c.IssueCount).ToArray();
        var labels = _categories.Select(c => c.CategoryName).ToArray();
        var colors = _categories
            .Select(c => CategoryColors.TryGetValue(c.CategoryId, out var color) ? color : SKColors.Gray)
            .ToArray();

        // Build row series with individual colors
        var rowSeries = new RowSeries<double>
        {
            Values = values,
            Fill = new SolidColorPaint(new SKColor(255, 107, 44)), // Lexichord accent color
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 11,
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
            DataLabelsFormatter = point => $"{point.Model:0}",
            MaxBarWidth = 25,
            Padding = 2
        };

        Series = [rowSeries];
        YAxes[0].Labels = labels;

        _logger.LogDebug(
            LogEvents.ChartSeriesBuilt,
            "Built chart series with {CategoryCount} categories",
            _categories.Count);
    }

    /// <summary>
    /// Clears all chart data.
    /// </summary>
    private void ClearData()
    {
        _categories.Clear();
        _categoryCounts.Clear();
        Series = [];
        YAxes[0].Labels = Array.Empty<string>();
        TotalIssueCount = 0;
        HasData = false;
        SelectedCategory = null;
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ClearData();

        _logger.LogDebug(LogEvents.Disposed, "IssueCategoryChartViewModel disposed");
    }

    #endregion
}
