// <copyright file="LicenseGateValueConverter.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;

using Lexichord.Abstractions.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Converters;

/// <summary>
/// XAML value converter that gates UI visibility based on license feature status.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1d - Converts a feature code string to a boolean indicating whether
/// the feature is enabled for the current license context.
///
/// Usage in XAML:
/// <code>
/// &lt;Window.Resources&gt;
///     &lt;converters:LicenseGateValueConverter x:Key="LicenseGate" /&gt;
/// &lt;/Window.Resources&gt;
///
/// &lt;CheckBox IsEnabled="{Binding Source={x:Static Member=...},
///     Converter={StaticResource LicenseGate},
///     ConverterParameter='Feature.FuzzyMatching'}" /&gt;
/// </code>
///
/// Design Decisions:
/// - Uses service locator pattern to access ILicenseContext from XAML context
/// - Returns false for null/invalid inputs (fail-safe for disabled state)
/// - Thread-safe: license context calls are thread-safe by contract
///
/// Dependencies:
/// - Requires ILicenseContext registered in application DI container
/// - Requires IServiceProvider accessible via App.Current.Services or similar
/// </remarks>
public sealed class LicenseGateValueConverter : IValueConverter
{
    private static IServiceProvider? _serviceProvider;
    private static ILicenseContext? _cachedLicenseContext;
    private static ILogger<LicenseGateValueConverter>? _logger;

    /// <summary>
    /// Initializes the converter with the application's service provider.
    /// </summary>
    /// <param name="serviceProvider">The application's DI service provider.</param>
    /// <remarks>
    /// LOGIC: Called during application startup to provide DI access.
    /// Must be called before the converter is used in any bindings.
    /// </remarks>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cachedLicenseContext = serviceProvider.GetService<ILicenseContext>();
        _logger = serviceProvider.GetService<ILogger<LicenseGateValueConverter>>();

        _logger?.LogDebug(
            "LicenseGateValueConverter initialized. License context available: {Available}",
            _cachedLicenseContext != null);
    }

    /// <summary>
    /// Converts a feature code to a boolean indicating feature availability.
    /// </summary>
    /// <param name="value">The binding value (typically unused, can be null).</param>
    /// <param name="targetType">The target type for conversion (should be bool).</param>
    /// <param name="parameter">
    /// The feature code string to check (e.g., "Feature.FuzzyMatching").
    /// </param>
    /// <param name="culture">The culture info (unused).</param>
    /// <returns>
    /// True if the feature is enabled for the current license; false otherwise.
    /// Returns false for null/invalid parameters or if license context is unavailable.
    /// </returns>
    /// <remarks>
    /// LOGIC: Checks both tier-based access and feature-specific enablement.
    /// - First validates the feature code parameter
    /// - Then delegates to ILicenseContext.IsFeatureEnabled
    /// - Returns false as fail-safe for any error condition
    /// </remarks>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // LOGIC: Validate the feature code parameter
        if (parameter is not string featureCode || string.IsNullOrWhiteSpace(featureCode))
        {
            _logger?.LogWarning(
                "LicenseGateValueConverter called with invalid parameter: {Parameter}",
                parameter);
            return false;
        }

        // LOGIC: Check if license context is available
        if (_cachedLicenseContext == null)
        {
            _logger?.LogWarning(
                "LicenseGateValueConverter: ILicenseContext not available. Feature '{FeatureCode}' returning disabled.",
                featureCode);
            return false;
        }

        try
        {
            var isEnabled = _cachedLicenseContext.IsFeatureEnabled(featureCode);

            _logger?.LogTrace(
                "LicenseGateValueConverter: Feature '{FeatureCode}' => {IsEnabled}",
                featureCode,
                isEnabled);

            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "LicenseGateValueConverter: Error checking feature '{FeatureCode}'",
                featureCode);
            return false;
        }
    }

    /// <summary>
    /// Converts back from boolean to feature code (not supported).
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns><see cref="BindingOperations.DoNothing"/> as back-conversion is not supported.</returns>
    /// <remarks>
    /// LOGIC: Back-conversion is not meaningful for license gating.
    /// This is a one-way binding converter only.
    /// </remarks>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
