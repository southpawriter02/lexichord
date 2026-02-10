// -----------------------------------------------------------------------
// <copyright file="ReplaceSelectionCommand.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Commands;

/// <summary>
/// Command that replaces the current selection with AI-generated text.
/// </summary>
/// <remarks>
/// <para>
/// This command is displayed as a button in AI response messages when a
/// text selection is active. It supports preview mode by default.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
public partial class ReplaceSelectionCommand : ObservableObject, ICommand
{
    private readonly IEditorInsertionService _insertionService;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _license;
    private readonly ILogger<ReplaceSelectionCommand> _logger;

    [ObservableProperty]
    private bool _isExecuting;

    /// <summary>
    /// Initializes the command with required dependencies.
    /// </summary>
    public ReplaceSelectionCommand(
        IEditorInsertionService insertionService,
        IEditorService editorService,
        ILicenseContext license,
        ILogger<ReplaceSelectionCommand> logger)
    {
        _insertionService = insertionService ?? throw new ArgumentNullException(nameof(insertionService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _license = license ?? throw new ArgumentNullException(nameof(license));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the replacement text.
    /// </summary>
    public string? ReplacementText { get; set; }

    /// <summary>
    /// Gets or sets whether to show preview before replacing.
    /// </summary>
    public bool ShowPreview { get; set; } = true;

    /// <inheritdoc/>
    public bool CanExecute(object? parameter)
    {
        return !IsExecuting
            && _license.GetCurrentTier() >= LicenseTier.WriterPro
            && _editorService.HasSelection
            && !string.IsNullOrEmpty(ReplacementText ?? parameter as string);
    }

    /// <summary>
    /// Executes the replace-selection operation.
    /// </summary>
    /// <param name="parameter">Optional text parameter override.</param>
    /// <remarks>
    /// LOGIC: Replaces the current editor selection with AI-generated text.
    /// If <see cref="ShowPreview"/> is true, shows a preview overlay first.
    /// Otherwise replaces immediately. All replacements are wrapped in undo groups.
    /// </remarks>
    public async void Execute(object? parameter)
    {
        var text = ReplacementText ?? parameter as string;
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning("Replace command invoked without text");
            return;
        }

        if (!_editorService.HasSelection)
        {
            _logger.LogWarning("Replace command invoked without selection");
            return;
        }

        IsExecuting = true;
        try
        {
            var selectionStart = _editorService.SelectionStart;
            var selectionLength = _editorService.SelectionLength;

            _logger.LogDebug(
                "Replacing selection: {SelLength} chars with {NewLength} chars",
                selectionLength, text.Length);

            if (ShowPreview)
            {
                var location = new TextSpan(selectionStart, selectionLength);
                await _insertionService.ShowPreviewAsync(text, location);
                _logger.LogDebug("Preview shown for selection replacement");
            }
            else
            {
                await _insertionService.ReplaceSelectionAsync(text);
                _logger.LogInformation(
                    "Selection replaced: {OldLength} → {NewLength} chars",
                    selectionLength, text.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace selection");
            throw;
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Notifies that CanExecute may have changed.
    /// </summary>
    public void NotifyCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;
}
