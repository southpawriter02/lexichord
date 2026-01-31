// <copyright file="IResonanceDashboardViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// Defines the contract for the Resonance Dashboard ViewModel.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Contract for testability and abstraction.</para>
/// <para>Exposes observable properties for chart binding.</para>
/// </remarks>
public interface IResonanceDashboardViewModel : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether data is being loaded.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets a value indicating whether the user has the required license.
    /// </summary>
    bool IsLicensed { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the target overlay should be shown.
    /// </summary>
    bool ShowTargetOverlay { get; set; }

    /// <summary>
    /// Gets the name of the currently active voice profile.
    /// </summary>
    string ActiveProfileName { get; }

    /// <summary>
    /// Gets the chart series for binding to the PolarChart control.
    /// </summary>
    ISeries[] Series { get; }

    /// <summary>
    /// Gets the radius axes configuration for the polar chart.
    /// </summary>
    PolarAxis[] RadiusAxes { get; }

    /// <summary>
    /// Gets the angle axes configuration for the polar chart.
    /// </summary>
    PolarAxis[] AngleAxes { get; }

    /// <summary>
    /// Gets a value indicating whether the chart has data to display.
    /// </summary>
    bool HasData { get; }

    /// <summary>
    /// Initializes the ViewModel and loads initial chart data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Triggers a manual refresh of the chart data.
    /// </summary>
    void Refresh();
}
