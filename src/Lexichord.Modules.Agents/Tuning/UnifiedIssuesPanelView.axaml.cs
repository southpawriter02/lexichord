// =============================================================================
// File: UnifiedIssuesPanelView.axaml.cs
// Project: Lexichord.Modules.Agents
// Description: Code-behind for the Unified Issues Panel Avalonia view.
// =============================================================================
// LOGIC: Minimal code-behind following MVVM pattern. Most logic is in the
//   UnifiedIssuesPanelViewModel. Code-behind handles:
//   - View initialization
//   - UI events that need to invoke ViewModel commands
//   - Navigation double-click handling
//
// v0.7.5g: Unified Issues Panel
// =============================================================================

using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Code-behind for the Unified Issues Panel view.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides minimal code-behind following the MVVM pattern.
/// Most business logic resides in <see cref="UnifiedIssuesPanelViewModel"/>.
/// </para>
/// <para>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item><description>Initialize the view and connect to ViewModel events</description></item>
///   <item><description>Handle double-click navigation to issue locations</description></item>
///   <item><description>Provide event handlers for UI elements that can't directly bind commands</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
public partial class UnifiedIssuesPanelView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedIssuesPanelView"/> class.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Calls InitializeComponent() to load the XAML and wire up bindings.
    /// </remarks>
    public UnifiedIssuesPanelView()
    {
        InitializeComponent();
    }

    #region v0.7.5g Navigation Support

    /// <summary>
    /// Handles double-click on an issue item to navigate to its location.
    /// </summary>
    /// <param name="sender">The border element that was double-clicked.</param>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Extracts the <see cref="IssuePresentation"/> from the sender's
    /// DataContext and invokes the <see cref="UnifiedIssuesPanelViewModel.NavigateToIssueCommand"/>.
    /// </para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    ///   <item><description>Double-clicking an issue navigates to its location in the editor</description></item>
    ///   <item><description>If the issue is already expanded, this also toggles expansion</description></item>
    ///   <item><description>Single-click expands/collapses the detail panel</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void OnIssueItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        // LOGIC: Extract the IssuePresentation from the visual tree.
        if (sender is not Border { DataContext: IssuePresentation issue })
        {
            return;
        }

        // LOGIC: Invoke the NavigateToIssue command on the ViewModel.
        if (DataContext is UnifiedIssuesPanelViewModel viewModel)
        {
            // LOGIC: First toggle expansion state (single-tap behavior).
            issue.ToggleExpandedCommand.Execute(null);

            // LOGIC: Then navigate to the issue location.
            viewModel.NavigateToIssueCommand.Execute(issue);
        }
    }

    #endregion
}

#region Value Converters for XAML Bindings

/// <summary>
/// Converters for severity-based visual styling.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> These converters transform <see cref="UnifiedSeverity"/> values
/// into visual representations (icons, boolean flags for styling).
/// </para>
/// <para>
/// <b>Usage:</b> Referenced in XAML as static properties:
/// <code>
/// Converter={x:Static tuning:SeverityConverters.ToIcon}
/// Converter={x:Static tuning:SeverityConverters.IsError}
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
public static class SeverityConverters
{
    /// <summary>
    /// Converts severity to an icon character (emoji).
    /// </summary>
    public static IValueConverter ToIcon { get; } = new SeverityToIconConverter();

    /// <summary>
    /// Returns true if severity is Error.
    /// </summary>
    public static IValueConverter IsError { get; } = new SeverityToBoolConverter(UnifiedSeverity.Error);

    /// <summary>
    /// Returns true if severity is Warning.
    /// </summary>
    public static IValueConverter IsWarning { get; } = new SeverityToBoolConverter(UnifiedSeverity.Warning);

    /// <summary>
    /// Returns true if severity is Info.
    /// </summary>
    public static IValueConverter IsInfo { get; } = new SeverityToBoolConverter(UnifiedSeverity.Info);

    /// <summary>
    /// Returns true if severity is Hint.
    /// </summary>
    public static IValueConverter IsHint { get; } = new SeverityToBoolConverter(UnifiedSeverity.Hint);

    /// <summary>
    /// Internal converter that maps severity to icon emoji.
    /// </summary>
    private sealed class SeverityToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                UnifiedSeverity.Error => "❌",
                UnifiedSeverity.Warning => "⚠️",
                UnifiedSeverity.Info => "ℹ️",
                UnifiedSeverity.Hint => "💡",
                _ => "•"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("SeverityToIconConverter does not support ConvertBack.");
        }
    }

    /// <summary>
    /// Internal converter that returns true for a specific severity.
    /// </summary>
    private sealed class SeverityToBoolConverter : IValueConverter
    {
        private readonly UnifiedSeverity _targetSeverity;

        public SeverityToBoolConverter(UnifiedSeverity targetSeverity)
        {
            _targetSeverity = targetSeverity;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is UnifiedSeverity severity && severity == _targetSeverity;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("SeverityToBoolConverter does not support ConvertBack.");
        }
    }
}

/// <summary>
/// Converters for expand/collapse visual indicators.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Converts boolean expansion state to chevron characters
/// for visual indication in the UI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
public static class ExpanderConverters
{
    /// <summary>
    /// Converts boolean to chevron character (▼ expanded, ▶ collapsed).
    /// </summary>
    public static IValueConverter ToChevron { get; } = new BoolToChevronConverter();

    /// <summary>
    /// Internal converter for expansion state to chevron.
    /// </summary>
    private sealed class BoolToChevronConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? "▼" : "▶";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToChevronConverter does not support ConvertBack.");
        }
    }
}

/// <summary>
/// Converter for plural suffix based on count.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Returns "s" for counts != 1, empty string for count == 1.
/// Used in MultiBinding for pluralization: "{0} error{1}" where {1} is this converter.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// &lt;MultiBinding StringFormat="{}{0} error{1}"&gt;
///     &lt;Binding Path="ErrorCount"/&gt;
///     &lt;Binding Path="ErrorCount" Converter="{x:Static tuning:PluralConverter.Instance}"/&gt;
/// &lt;/MultiBinding&gt;
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
public sealed class PluralConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML binding.
    /// </summary>
    public static PluralConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // LOGIC: Return "s" for plural (count != 1), empty for singular.
        return value switch
        {
            int count => count == 1 ? "" : "s",
            long count => count == 1 ? "" : "s",
            double count => Math.Abs(count - 1.0) < 0.001 ? "" : "s",
            _ => "s"
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PluralConverter does not support ConvertBack.");
    }
}

#endregion
