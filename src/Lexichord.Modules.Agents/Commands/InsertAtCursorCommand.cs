// -----------------------------------------------------------------------
// <copyright file="InsertAtCursorCommand.cs" company="Lexichord">
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
/// Command that inserts AI-generated text at the current cursor position.
/// </summary>
/// <remarks>
/// <para>
/// This command is displayed as a button in AI response messages and can be
/// invoked from the chat panel. It supports preview mode by default.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
public partial class InsertAtCursorCommand : ObservableObject, ICommand
{
    private readonly IEditorInsertionService _insertionService;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _license;
    private readonly ILogger<InsertAtCursorCommand> _logger;

    [ObservableProperty]
    private bool _isExecuting;

    /// <summary>
    /// Initializes the command with required dependencies.
    /// </summary>
    public InsertAtCursorCommand(
        IEditorInsertionService insertionService,
        IEditorService editorService,
        ILicenseContext license,
        ILogger<InsertAtCursorCommand> logger)
    {
        _insertionService = insertionService ?? throw new ArgumentNullException(nameof(insertionService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _license = license ?? throw new ArgumentNullException(nameof(license));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the text to insert (bound from message content).
    /// </summary>
    public string? TextToInsert { get; set; }

    /// <summary>
    /// Gets or sets whether to show preview before inserting.
    /// </summary>
    public bool ShowPreview { get; set; } = true;

    /// <inheritdoc/>
    public bool CanExecute(object? parameter)
    {
        return !IsExecuting
            && _license.GetCurrentTier() >= LicenseTier.WriterPro
            && !string.IsNullOrEmpty(TextToInsert ?? parameter as string);
    }

    /// <summary>
    /// Executes the insert-at-cursor operation.
    /// </summary>
    /// <param name="parameter">Optional text parameter override.</param>
    /// <remarks>
    /// LOGIC: Inserts AI-generated text at the current cursor position.
    /// If <see cref="ShowPreview"/> is true, shows a preview overlay first.
    /// Otherwise inserts immediately. All insertions are wrapped in undo groups.
    /// </remarks>
    public async void Execute(object? parameter)
    {
        var text = TextToInsert ?? parameter as string;
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning("Insert command invoked without text");
            return;
        }

        IsExecuting = true;
        try
        {
            _logger.LogDebug("Inserting text at cursor: {CharCount} chars", text.Length);

            if (ShowPreview)
            {
                var position = _editorService.CaretOffset;
                var location = new TextSpan(position, 0);
                await _insertionService.ShowPreviewAsync(text, location);
                _logger.LogDebug("Preview shown at position {Position}", position);
            }
            else
            {
                await _insertionService.InsertAtCursorAsync(text);
                _logger.LogInformation("Text inserted at cursor: {CharCount} chars",
                    text.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert text at cursor");
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
