// -----------------------------------------------------------------------
// <copyright file="ConnectionStatusBadge.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia;
using Avalonia.Controls;

namespace Lexichord.Modules.LLM.Presentation.Views;

/// <summary>
/// A badge control that displays the connection status of an LLM provider.
/// </summary>
/// <remarks>
/// <para>
/// This control provides a visual indicator (colored dot) and status text
/// based on the current <see cref="Status"/> value.
/// </para>
/// <para>
/// <b>Visual States:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Unknown (gray): No test performed</description></item>
///   <item><description>Checking (blue, animated): Test in progress</description></item>
///   <item><description>Connected (green): Successfully connected</description></item>
///   <item><description>Failed (red): Connection failed</description></item>
/// </list>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;views:ConnectionStatusBadge
///     Status="{Binding SelectedProvider.Status}"
///     StatusMessage="{Binding SelectedProvider.StatusMessage}" /&gt;
/// </code>
/// </example>
public partial class ConnectionStatusBadge : UserControl
{
    /// <summary>
    /// Defines the <see cref="Status"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ConnectionStatus> StatusProperty =
        AvaloniaProperty.Register<ConnectionStatusBadge, ConnectionStatus>(
            nameof(Status),
            defaultValue: ConnectionStatus.Unknown);

    /// <summary>
    /// Defines the <see cref="StatusMessage"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string?> StatusMessageProperty =
        AvaloniaProperty.Register<ConnectionStatusBadge, string?>(
            nameof(StatusMessage));

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStatusBadge"/> class.
    /// </summary>
    public ConnectionStatusBadge()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Gets or sets the connection status.
    /// </summary>
    /// <value>
    /// The current <see cref="ConnectionStatus"/> value.
    /// Default is <see cref="ConnectionStatus.Unknown"/>.
    /// </value>
    public ConnectionStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    /// <value>
    /// An optional message providing details about the status.
    /// For example: "Connected (150ms)" or "Invalid API key".
    /// </value>
    public string? StatusMessage
    {
        get => GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the status is Unknown.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="ConnectionStatus.Unknown"/>.</value>
    public bool IsUnknown => Status == ConnectionStatus.Unknown;

    /// <summary>
    /// Gets a value indicating whether the status is Checking.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="ConnectionStatus.Checking"/>.</value>
    public bool IsChecking => Status == ConnectionStatus.Checking;

    /// <summary>
    /// Gets a value indicating whether the status is Connected.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="ConnectionStatus.Connected"/>.</value>
    public bool IsConnected => Status == ConnectionStatus.Connected;

    /// <summary>
    /// Gets a value indicating whether the status is Failed.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="ConnectionStatus.Failed"/>.</value>
    public bool IsFailed => Status == ConnectionStatus.Failed;

    /// <summary>
    /// Gets the display text for the current status.
    /// </summary>
    /// <value>
    /// If <see cref="StatusMessage"/> is set, returns it.
    /// Otherwise, returns a default text based on <see cref="Status"/>.
    /// </value>
    public string StatusText => StatusMessage ?? Status switch
    {
        ConnectionStatus.Unknown => "Not tested",
        ConnectionStatus.Checking => "Testing...",
        ConnectionStatus.Connected => "Connected",
        ConnectionStatus.Failed => "Failed",
        _ => "Unknown"
    };

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // When Status or StatusMessage changes, notify the computed properties
        if (change.Property == StatusProperty || change.Property == StatusMessageProperty)
        {
            // Force re-evaluation of computed properties by raising property changed
            // The control uses direct binding to these properties
            InvalidateVisual();
        }
    }
}
