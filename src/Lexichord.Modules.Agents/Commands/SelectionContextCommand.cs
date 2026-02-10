// -----------------------------------------------------------------------
// <copyright file="SelectionContextCommand.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Events;
using Lexichord.Modules.Agents.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Commands;

/// <summary>
/// RelayCommand that sends the current editor selection to Co-pilot.
/// </summary>
/// <remarks>
/// <para>
/// This command is bound to <c>Ctrl+Shift+A</c> by default and is also
/// triggered from the editor context menu via "Ask Co-pilot about selection".
/// </para>
/// <para>
/// <b>Execution Flow:</b>
/// </para>
/// <list type="number">
///   <item><description>Retrieve selected text from <see cref="IEditorService"/></description></item>
///   <item><description>Validate selection is not empty</description></item>
///   <item><description>Send selection to Co-pilot via <see cref="ISelectionContextService"/></description></item>
///   <item><description>Publish <see cref="SelectionContextSetEvent"/> via MediatR</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public partial class SelectionContextCommand : ObservableObject, ICommand
{
    private readonly ISelectionContextService _selectionService;
    private readonly IEditorService _editorService;
    private readonly IMediator _mediator;
    private readonly ILogger<SelectionContextCommand> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionContextCommand"/>.
    /// </summary>
    /// <param name="selectionService">The selection context service.</param>
    /// <param name="editorService">The editor service for retrieving selection.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public SelectionContextCommand(
        ISelectionContextService selectionService,
        IEditorService editorService,
        IMediator mediator,
        ILogger<SelectionContextCommand> logger)
    {
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Determines whether the command can execute.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <returns><c>true</c> if the editor has an active selection; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter) => _selectionService.HasActiveSelection;

    /// <summary>
    /// Executes the selection-to-copilot flow.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// LOGIC: Retrieves selected text, sends to Co-pilot service, and publishes
    /// an event on success. Catches <see cref="LicenseTierException"/> for UI
    /// handling and logs general errors.
    /// </remarks>
    public async void Execute(object? parameter)
    {
        try
        {
            var selection = _editorService.GetSelectedText();
            if (string.IsNullOrWhiteSpace(selection))
            {
                _logger.LogDebug("No text selected, command skipped");
                return;
            }

            _logger.LogDebug("Sending selection to Co-pilot: {CharCount} chars",
                selection.Length);

            await _selectionService.SendSelectionToCoPilotAsync(selection);

            await _mediator.Publish(new SelectionContextSetEvent(
                selection.Length,
                DateTime.UtcNow));
        }
        catch (LicenseTierException ex)
        {
            _logger.LogWarning(ex, "License required for selection context");
            // Let the exception propagate for UI handling
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send selection to Co-pilot");
        }
    }

    /// <summary>
    /// Notifies UI of potential can-execute changes.
    /// </summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Event raised when CanExecute may have changed.
    /// </summary>
    public event EventHandler? CanExecuteChanged;
}
