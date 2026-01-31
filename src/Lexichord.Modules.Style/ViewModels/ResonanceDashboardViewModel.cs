// <copyright file="ResonanceDashboardViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Resonance Dashboard spider chart visualization.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Provides bindable data for the polar chart control.</para>
/// <para>Features:</para>
/// <list type="bullet">
///   <item>Series binding for current metrics and target overlay</item>
///   <item>Axis configuration for 6 writing dimensions</item>
///   <item>Toggle for showing/hiding target overlay</item>
///   <item>License gating for Writer Pro tier</item>
///   <item>Event-driven updates from ChartDataService</item>
/// </list>
/// <para>Thread-safe: UI thread dispatching handled appropriately.</para>
/// </remarks>
public partial class ResonanceDashboardViewModel : ObservableObject, IResonanceDashboardViewModel
{
    #region Dependencies

    private readonly IChartDataService _chartDataService;
    private readonly ITargetOverlayService _targetOverlayService;
    private readonly ISpiderChartSeriesBuilder _seriesBuilder;
    private readonly IVoiceProfileService _profileService;
    private readonly ILicenseService _licenseService;
    private readonly IResonanceAxisProvider _axisProvider;
    private readonly ILogger<ResonanceDashboardViewModel> _logger;

    #endregion

    #region Observable Properties

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isLoading;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isLicensed;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _showTargetOverlay = true;

    /// <inheritdoc/>
    [ObservableProperty]
    private string _activeProfileName = string.Empty;

    /// <inheritdoc/>
    [ObservableProperty]
    private ISeries[] _series = [];

    /// <inheritdoc/>
    [ObservableProperty]
    private PolarAxis[] _radiusAxes = [];

    /// <inheritdoc/>
    [ObservableProperty]
    private PolarAxis[] _angleAxes = [];

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _hasData;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ResonanceDashboardViewModel"/> class.
    /// </summary>
    /// <param name="chartDataService">Service for chart data aggregation.</param>
    /// <param name="targetOverlayService">Service for target overlay computation.</param>
    /// <param name="seriesBuilder">Builder for chart series.</param>
    /// <param name="profileService">Service for voice profile management.</param>
    /// <param name="licenseService">Service for license validation.</param>
    /// <param name="axisProvider">Provider for axis definitions.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ResonanceDashboardViewModel(
        IChartDataService chartDataService,
        ITargetOverlayService targetOverlayService,
        ISpiderChartSeriesBuilder seriesBuilder,
        IVoiceProfileService profileService,
        ILicenseService licenseService,
        IResonanceAxisProvider axisProvider,
        ILogger<ResonanceDashboardViewModel> logger)
    {
        _chartDataService = chartDataService ?? throw new ArgumentNullException(nameof(chartDataService));
        _targetOverlayService = targetOverlayService ?? throw new ArgumentNullException(nameof(targetOverlayService));
        _seriesBuilder = seriesBuilder ?? throw new ArgumentNullException(nameof(seriesBuilder));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _axisProvider = axisProvider ?? throw new ArgumentNullException(nameof(axisProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to chart data updates
        _chartDataService.DataUpdated += OnChartDataUpdated;

        // Check initial license status
        IsLicensed = _licenseService.IsFeatureEnabled(FeatureCodes.ResonanceDashboard);

        _logger.LogDebug(
            "ResonanceDashboardViewModel initialized. Licensed: {IsLicensed}",
            IsLicensed);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    /// <remarks>
    /// <para>LOGIC: Initialization sequence:</para>
    /// <list type="number">
    ///   <item>Check license status</item>
    ///   <item>If unlicensed, show upgrade prompt and return</item>
    ///   <item>Initialize axes</item>
    ///   <item>Load initial chart data</item>
    ///   <item>Load active profile and target overlay</item>
    /// </list>
    /// </remarks>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("ResonanceDashboardViewModel.InitializeAsync called");

        // STEP 1: Refresh license status
        IsLicensed = _licenseService.IsFeatureEnabled(FeatureCodes.ResonanceDashboard);

        // STEP 2: Check license
        if (!IsLicensed)
        {
            _logger.LogWarning("Resonance Dashboard not licensed, skipping initialization");
            return;
        }

        try
        {
            IsLoading = true;

            // STEP 3: Initialize axes
            BuildAxes();

            // STEP 4: Load chart data
            await RefreshChartAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("ResonanceDashboardViewModel initialized successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Initialization cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ResonanceDashboardViewModel initialization");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Triggers a chart refresh by invalidating cached data.
    /// </remarks>
    public void Refresh()
    {
        _logger.LogDebug("Manual refresh requested");
        _chartDataService.InvalidateCache();
        _ = RefreshChartAsync(CancellationToken.None);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Toggles the visibility of the target overlay on the chart.
    /// </summary>
    [RelayCommand]
    private async Task ToggleTargetOverlayAsync()
    {
        ShowTargetOverlay = !ShowTargetOverlay;
        _logger.LogDebug("Target overlay toggled: {ShowTargetOverlay}", ShowTargetOverlay);
        await RebuildSeriesAsync(CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles chart data update events from the ChartDataService.
    /// </summary>
    private void OnChartDataUpdated(object? sender, ChartDataUpdatedEventArgs e)
    {
        _logger.LogDebug(
            "Chart data updated event received. Computation time: {ComputationTime}ms",
            e.ComputationTime.TotalMilliseconds);

        // Rebuild series with new data
        _ = RebuildSeriesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Refreshes the chart by loading current data and active profile.
    /// </summary>
    private async Task RefreshChartAsync(CancellationToken ct)
    {
        if (!IsLicensed)
        {
            return;
        }

        try
        {
            // Get active profile
            var profile = await _profileService.GetActiveProfileAsync(ct).ConfigureAwait(false);
            ActiveProfileName = profile.Name;
            _logger.LogDebug("Active profile: {ProfileName}", profile.Name);

            // Invalidate target overlay cache on profile change
            _targetOverlayService.InvalidateCache(profile.Id.ToString());

            // Rebuild series
            await RebuildSeriesAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Chart refresh cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing chart");
        }
    }

    /// <summary>
    /// Rebuilds the chart series from current data and overlay.
    /// </summary>
    private async Task RebuildSeriesAsync(CancellationToken ct)
    {
        try
        {
            var seriesList = new List<ISeries>();

            // Get current chart data
            var chartData = await _chartDataService.GetChartDataAsync(ct).ConfigureAwait(false);
            HasData = chartData.DataPoints.Count > 0;

            // Build current values series
            var currentSeries = _seriesBuilder.BuildCurrentSeries(chartData);
            if (currentSeries is not null)
            {
                seriesList.Add(currentSeries);
                _logger.LogDebug("Built current values series with {PointCount} points",
                    chartData.DataPoints.Count);
            }

            // Build target overlay if enabled
            if (ShowTargetOverlay)
            {
                var profile = await _profileService.GetActiveProfileAsync(ct).ConfigureAwait(false);
                var overlay = await _targetOverlayService.GetOverlayAsync(profile, ct).ConfigureAwait(false);

                if (overlay?.HasData == true)
                {
                    var overlayData = new ResonanceChartData(
                        overlay.DataPoints,
                        overlay.ComputedAt);

                    var targetSeries = _seriesBuilder.BuildTargetSeries(overlayData, profile.Name);
                    if (targetSeries is not null)
                    {
                        seriesList.Add(targetSeries);
                        _logger.LogDebug("Built target overlay series for profile: {ProfileName}",
                            profile.Name);
                    }
                }
            }

            Series = seriesList.ToArray();
            _logger.LogDebug("Chart series rebuilt with {SeriesCount} series", seriesList.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Series rebuild cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding chart series");
            Series = [];
            HasData = false;
        }
    }

    /// <summary>
    /// Builds the polar axes for the spider chart.
    /// </summary>
    private void BuildAxes()
    {
        var axisDefs = _axisProvider.GetAxes();
        var labels = axisDefs.Select(a => a.Name).ToArray();

        // LOGIC: Angle axes show the category labels around the circle
        AngleAxes =
        [
            new PolarAxis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                MinStep = 1,
                ForceStepToMin = true
            }
        ];

        // LOGIC: Radius axes show the 0-100 scale from center to edge
        RadiusAxes =
        [
            new PolarAxis
            {
                Labeler = value => $"{value:0}",
                MinLimit = 0,
                MaxLimit = 100,
                LabelsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(150))
            }
        ];

        _logger.LogDebug("Built axes with {LabelCount} labels", labels.Length);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        _chartDataService.DataUpdated -= OnChartDataUpdated;
        _logger.LogDebug("ResonanceDashboardViewModel disposed");
    }

    #endregion
}
