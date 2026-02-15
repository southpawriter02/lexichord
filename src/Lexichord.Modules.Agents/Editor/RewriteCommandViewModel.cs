// -----------------------------------------------------------------------
// <copyright file="RewriteCommandViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// ViewModel for rewrite command execution from the context menu.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel provides bindable commands for the rewrite
/// context menu items. It subscribes to <see cref="IEditorAgentContextMenuProvider.CanRewriteChanged"/>
/// to update command execution state and exposes progress state for UI feedback.
/// </para>
/// <para>
/// <b>Commands:</b>
/// <list type="bullet">
///   <item><description><see cref="RewriteFormallyCommand"/> - Transform to formal tone</description></item>
///   <item><description><see cref="SimplifyCommand"/> - Simplify for broader audience</description></item>
///   <item><description><see cref="ExpandCommand"/> - Add detail and explanation</description></item>
///   <item><description><see cref="CustomRewriteCommand"/> - Open custom instruction dialog</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IEditorAgentContextMenuProvider"/>
/// <seealso cref="RewriteIntent"/>
public partial class RewriteCommandViewModel : ObservableObject, IDisposable
{
    private readonly IEditorAgentContextMenuProvider _contextMenuProvider;
    private readonly ILogger<RewriteCommandViewModel> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets or sets whether a rewrite operation is currently executing.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When true, all rewrite commands are disabled and
    /// a spinner may be shown in the UI.
    /// </remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RewriteFormallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(SimplifyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExpandCommand))]
    [NotifyCanExecuteChangedFor(nameof(CustomRewriteCommand))]
    private bool _isExecuting;

    /// <summary>
    /// Gets or sets the current progress value (0-100) for an executing rewrite.
    /// </summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>
    /// Gets or sets the progress message for display during rewrite execution.
    /// </summary>
    [ObservableProperty]
    private string _progressMessage = string.Empty;

    /// <summary>
    /// Gets or sets whether rewrite commands can be executed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Bound to <see cref="IEditorAgentContextMenuProvider.CanRewrite"/>.
    /// Updated via the <see cref="IEditorAgentContextMenuProvider.CanRewriteChanged"/> event.
    /// </remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RewriteFormallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(SimplifyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExpandCommand))]
    [NotifyCanExecuteChangedFor(nameof(CustomRewriteCommand))]
    private bool _canRewrite;

    /// <summary>
    /// Gets or sets whether the user has a valid license for rewrite features.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Bound to <see cref="IEditorAgentContextMenuProvider.IsLicensed"/>.
    /// When false, commands still execute but show an upgrade modal.
    /// </remarks>
    [ObservableProperty]
    private bool _isLicensed;

    /// <summary>
    /// Initializes a new instance of <see cref="RewriteCommandViewModel"/>.
    /// </summary>
    /// <param name="contextMenuProvider">The context menu provider for state and execution.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Constructor subscribes to <see cref="IEditorAgentContextMenuProvider.CanRewriteChanged"/>
    /// and captures initial state from the provider.
    /// </remarks>
    public RewriteCommandViewModel(
        IEditorAgentContextMenuProvider contextMenuProvider,
        ILogger<RewriteCommandViewModel> logger)
    {
        _contextMenuProvider = contextMenuProvider ?? throw new ArgumentNullException(nameof(contextMenuProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Initializing RewriteCommandViewModel");

        // LOGIC: Capture initial state from the provider.
        RefreshState();

        // LOGIC: Subscribe to state changes for continuous updates.
        _contextMenuProvider.CanRewriteChanged += OnCanRewriteChanged;

        _logger.LogInformation(
            "RewriteCommandViewModel initialized. CanRewrite={CanRewrite}, IsLicensed={IsLicensed}",
            CanRewrite,
            IsLicensed);
    }

    /// <summary>
    /// Gets the available rewrite menu items for data binding.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="IEditorAgentContextMenuProvider.GetRewriteMenuItems"/>.
    /// </remarks>
    public IReadOnlyList<RewriteCommandOption> RewriteMenuItems =>
        _contextMenuProvider.GetRewriteMenuItems();

    /// <summary>
    /// Command to rewrite selected text formally.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="ExecuteRewriteAsync"/> with
    /// <see cref="RewriteIntent.Formal"/>.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanExecuteRewrite))]
    private async Task RewriteFormallyAsync(CancellationToken cancellationToken)
    {
        await ExecuteRewriteAsync(RewriteIntent.Formal, cancellationToken);
    }

    /// <summary>
    /// Command to simplify selected text.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="ExecuteRewriteAsync"/> with
    /// <see cref="RewriteIntent.Simplified"/>.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanExecuteRewrite))]
    private async Task SimplifyAsync(CancellationToken cancellationToken)
    {
        await ExecuteRewriteAsync(RewriteIntent.Simplified, cancellationToken);
    }

    /// <summary>
    /// Command to expand selected text.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="ExecuteRewriteAsync"/> with
    /// <see cref="RewriteIntent.Expanded"/>.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanExecuteRewrite))]
    private async Task ExpandAsync(CancellationToken cancellationToken)
    {
        await ExecuteRewriteAsync(RewriteIntent.Expanded, cancellationToken);
    }

    /// <summary>
    /// Command to open the custom rewrite dialog.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Delegates to <see cref="ExecuteRewriteAsync"/> with
    /// <see cref="RewriteIntent.Custom"/>. This opens a dialog for the user
    /// to enter their custom transformation instruction.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanExecuteRewrite))]
    private async Task CustomRewriteAsync(CancellationToken cancellationToken)
    {
        await ExecuteRewriteAsync(RewriteIntent.Custom, cancellationToken);
    }

    /// <summary>
    /// Determines whether rewrite commands can execute.
    /// </summary>
    /// <returns>True if not executing and can rewrite; false otherwise.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Used as the CanExecute predicate for all rewrite commands.
    /// Commands are disabled while another rewrite is in progress or when
    /// <see cref="CanRewrite"/> is false.
    /// </remarks>
    private bool CanExecuteRewrite() => !IsExecuting && CanRewrite;

    /// <summary>
    /// Executes a rewrite operation with the specified intent.
    /// </summary>
    /// <param name="intent">The type of rewrite transformation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Sets <see cref="IsExecuting"/> to true during execution,
    /// delegates to <see cref="IEditorAgentContextMenuProvider.ExecuteRewriteAsync"/>,
    /// and resets state on completion.
    /// </remarks>
    private async Task ExecuteRewriteAsync(RewriteIntent intent, CancellationToken cancellationToken)
    {
        _logger.LogDebug("ExecuteRewriteAsync starting. Intent={Intent}", intent);

        try
        {
            IsExecuting = true;
            Progress = 0;
            ProgressMessage = $"Preparing {GetIntentDisplayName(intent)} rewrite...";

            await _contextMenuProvider.ExecuteRewriteAsync(intent, cancellationToken);

            _logger.LogInformation("Rewrite execution completed. Intent={Intent}", intent);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Rewrite operation cancelled. Intent={Intent}", intent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rewrite. Intent={Intent}", intent);
            throw;
        }
        finally
        {
            IsExecuting = false;
            Progress = 0;
            ProgressMessage = string.Empty;
        }
    }

    /// <summary>
    /// Refreshes state from the context menu provider.
    /// </summary>
    private void RefreshState()
    {
        CanRewrite = _contextMenuProvider.CanRewrite;
        IsLicensed = _contextMenuProvider.IsLicensed;

        _logger.LogTrace(
            "State refreshed. CanRewrite={CanRewrite}, IsLicensed={IsLicensed}",
            CanRewrite,
            IsLicensed);
    }

    /// <summary>
    /// Handles the CanRewriteChanged event from the context menu provider.
    /// </summary>
    private void OnCanRewriteChanged(object? sender, EventArgs e)
    {
        _logger.LogTrace("CanRewriteChanged event received");

        // LOGIC: Marshal to UI thread for safe property updates.
        Dispatcher.UIThread.Post(RefreshState);
    }

    /// <summary>
    /// Gets a display name for the specified intent.
    /// </summary>
    private static string GetIntentDisplayName(RewriteIntent intent) => intent switch
    {
        RewriteIntent.Formal => "formal",
        RewriteIntent.Simplified => "simplify",
        RewriteIntent.Expanded => "expand",
        RewriteIntent.Custom => "custom",
        _ => intent.ToString().ToLowerInvariant()
    };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing RewriteCommandViewModel");

        // LOGIC: Unsubscribe from events to prevent memory leaks.
        _contextMenuProvider.CanRewriteChanged -= OnCanRewriteChanged;

        _disposed = true;

        _logger.LogInformation("RewriteCommandViewModel disposed");
    }
}
