// -----------------------------------------------------------------------
// <copyright file="EditorAgentContextMenuProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Provides context menu items for the Editor Agent rewrite functionality.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class manages the integration of AI-powered rewrite commands
/// into the editor's right-click context menu. It:
/// </para>
/// <list type="bullet">
///   <item><description>Defines the static list of available rewrite options</description></item>
///   <item><description>Subscribes to selection changes to track when text is selected</description></item>
///   <item><description>Subscribes to license changes to track when features are available</description></item>
///   <item><description>Publishes MediatR events to decouple from the rewrite handler (v0.7.3b)</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. State updates are performed
/// atomically and events are raised on the thread that triggered the change.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IEditorAgentContextMenuProvider"/>
/// <seealso cref="RewriteCommandOption"/>
public sealed class EditorAgentContextMenuProvider : IEditorAgentContextMenuProvider, IDisposable
{
    /// <summary>
    /// The predefined list of rewrite command options.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> These options are static and immutable. They define the
    /// menu items that appear in the "AI Rewrite" context menu group.
    /// </remarks>
    private static readonly IReadOnlyList<RewriteCommandOption> RewriteOptions = new List<RewriteCommandOption>
    {
        new(
            CommandId: "rewrite-formal",
            DisplayName: "Rewrite Formally",
            Description: "Transform casual text to formal, professional tone",
            Icon: "sparkles",
            KeyboardShortcut: "Ctrl+Shift+R",
            Intent: RewriteIntent.Formal,
            OpensDialog: false),
        new(
            CommandId: "rewrite-simplify",
            DisplayName: "Simplify",
            Description: "Simplify text for a broader audience",
            Icon: "text-decrease",
            KeyboardShortcut: "Ctrl+Shift+S",
            Intent: RewriteIntent.Simplified,
            OpensDialog: false),
        new(
            CommandId: "rewrite-expand",
            DisplayName: "Expand",
            Description: "Expand text with more detail and explanation",
            Icon: "text-increase",
            KeyboardShortcut: "Ctrl+Shift+E",
            Intent: RewriteIntent.Expanded,
            OpensDialog: false),
        new(
            CommandId: "rewrite-custom",
            DisplayName: "Custom Rewrite...",
            Description: "Enter a custom transformation instruction",
            Icon: "edit",
            KeyboardShortcut: "Ctrl+Shift+C",
            Intent: RewriteIntent.Custom,
            OpensDialog: true)
    }.AsReadOnly();

    private readonly IEditorService _editorService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<EditorAgentContextMenuProvider> _logger;

    private bool _hasSelection;
    private bool _isLicensed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="EditorAgentContextMenuProvider"/>.
    /// </summary>
    /// <param name="editorService">The editor service for selection state.</param>
    /// <param name="licenseContext">The license context for tier checks.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Constructor subscribes to <see cref="IEditorService.SelectionChanged"/>
    /// and <see cref="ILicenseContext.LicenseChanged"/> events to track state changes.
    /// Initial state is captured synchronously during construction.
    /// </remarks>
    public EditorAgentContextMenuProvider(
        IEditorService editorService,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<EditorAgentContextMenuProvider> logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Initializing EditorAgentContextMenuProvider");

        // LOGIC: Capture initial state before subscribing to events.
        RefreshState();

        // LOGIC: Subscribe to selection changes to update HasSelection.
        _editorService.SelectionChanged += OnSelectionChanged;

        // LOGIC: Subscribe to license changes to update IsLicensed.
        _licenseContext.LicenseChanged += OnLicenseChanged;

        _logger.LogInformation(
            "EditorAgentContextMenuProvider initialized with {MenuItemCount} rewrite options. " +
            "HasSelection={HasSelection}, IsLicensed={IsLicensed}",
            RewriteOptions.Count,
            _hasSelection,
            _isLicensed);
    }

    /// <inheritdoc />
    public IReadOnlyList<RewriteCommandOption> GetRewriteMenuItems() => RewriteOptions;

    /// <inheritdoc />
    public bool CanRewrite => HasSelection && IsLicensed;

    /// <inheritdoc />
    public bool HasSelection => _hasSelection;

    /// <inheritdoc />
    public bool IsLicensed => _isLicensed;

    /// <inheritdoc />
    public event EventHandler? CanRewriteChanged;

    /// <inheritdoc />
    public async Task ExecuteRewriteAsync(RewriteIntent intent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "ExecuteRewriteAsync called with Intent={Intent}, HasSelection={HasSelection}, IsLicensed={IsLicensed}",
            intent,
            HasSelection,
            IsLicensed);

        // LOGIC: If not licensed, show the upgrade modal instead of executing.
        if (!IsLicensed)
        {
            _logger.LogInformation(
                "User attempted rewrite without license. Publishing ShowUpgradeModalEvent");

            await _mediator.Publish(
                ShowUpgradeModalEvent.ForEditorAgent(),
                cancellationToken);
            return;
        }

        // LOGIC: If no selection, log warning and return without action.
        if (!HasSelection)
        {
            _logger.LogWarning(
                "ExecuteRewriteAsync called without active selection. Intent={Intent}",
                intent);
            return;
        }

        // LOGIC: Retrieve the current selection state.
        var selectedText = _editorService.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
        {
            _logger.LogWarning(
                "Selection was cleared between check and retrieval. Intent={Intent}",
                intent);
            return;
        }

        var selectionSpan = new TextSpan(_editorService.SelectionStart, _editorService.SelectionLength);
        var documentPath = _editorService.CurrentDocumentPath;

        _logger.LogDebug(
            "Preparing rewrite request. Intent={Intent}, SelectionLength={Length}, DocumentPath={Path}",
            intent,
            selectionSpan.Length,
            documentPath ?? "(untitled)");

        // LOGIC: For Custom intent, show the dialog to get user instruction.
        if (intent == RewriteIntent.Custom)
        {
            _logger.LogInformation(
                "Publishing ShowCustomRewriteDialogEvent for Custom rewrite");

            await _mediator.Publish(
                ShowCustomRewriteDialogEvent.Create(selectedText, selectionSpan, documentPath),
                cancellationToken);
            return;
        }

        // LOGIC: For predefined intents, publish the rewrite request directly.
        _logger.LogInformation(
            "Publishing RewriteRequestedEvent. Intent={Intent}, SelectionLength={Length}",
            intent,
            selectionSpan.Length);

        await _mediator.Publish(
            RewriteRequestedEvent.Create(intent, selectedText, selectionSpan, documentPath),
            cancellationToken);
    }

    /// <summary>
    /// Refreshes the internal state from the editor and license services.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Called during construction and can be called to force
    /// a state refresh if needed. Updates <see cref="HasSelection"/> and
    /// <see cref="IsLicensed"/>, then raises <see cref="CanRewriteChanged"/>.
    /// </remarks>
    private void RefreshState()
    {
        var oldCanRewrite = CanRewrite;

        _hasSelection = _editorService.HasSelection;
        _isLicensed = _licenseContext.GetCurrentTier() >= LicenseTier.WriterPro;

        _logger.LogTrace(
            "State refreshed. HasSelection={HasSelection}, IsLicensed={IsLicensed}, CanRewrite={CanRewrite}",
            _hasSelection,
            _isLicensed,
            CanRewrite);

        // LOGIC: Only raise event if CanRewrite actually changed.
        if (oldCanRewrite != CanRewrite)
        {
            _logger.LogDebug(
                "CanRewrite changed from {Old} to {New}",
                oldCanRewrite,
                CanRewrite);

            CanRewriteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Handles selection changes in the editor.
    /// </summary>
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var newHasSelection = !string.IsNullOrEmpty(e.NewSelection);

        _logger.LogTrace(
            "Selection changed. NewSelection={HasNewSelection}, Length={Length}",
            newHasSelection,
            e.NewSelection?.Length ?? 0);

        // LOGIC: Only update and raise event if HasSelection actually changed.
        if (_hasSelection != newHasSelection)
        {
            var oldCanRewrite = CanRewrite;
            _hasSelection = newHasSelection;

            _logger.LogDebug(
                "HasSelection changed from {Old} to {New}",
                !newHasSelection,
                newHasSelection);

            if (oldCanRewrite != CanRewrite)
            {
                CanRewriteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Handles license changes.
    /// </summary>
    private void OnLicenseChanged(object? sender, LicenseChangedEventArgs e)
    {
        var newIsLicensed = e.NewTier >= LicenseTier.WriterPro;

        _logger.LogDebug(
            "License changed. OldTier={OldTier}, NewTier={NewTier}, NewIsLicensed={IsLicensed}",
            e.OldTier,
            e.NewTier,
            newIsLicensed);

        // LOGIC: Only update and raise event if IsLicensed actually changed.
        if (_isLicensed != newIsLicensed)
        {
            var oldCanRewrite = CanRewrite;
            _isLicensed = newIsLicensed;

            _logger.LogDebug(
                "IsLicensed changed from {Old} to {New}",
                !newIsLicensed,
                newIsLicensed);

            if (oldCanRewrite != CanRewrite)
            {
                CanRewriteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing EditorAgentContextMenuProvider");

        // LOGIC: Unsubscribe from events to prevent memory leaks.
        _editorService.SelectionChanged -= OnSelectionChanged;
        _licenseContext.LicenseChanged -= OnLicenseChanged;

        _disposed = true;

        _logger.LogInformation("EditorAgentContextMenuProvider disposed");
    }
}
