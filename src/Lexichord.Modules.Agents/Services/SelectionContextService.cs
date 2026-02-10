// -----------------------------------------------------------------------
// <copyright file="SelectionContextService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Implementation of <see cref="ISelectionContextService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates the flow of selected text from the editor to the Co-pilot chat panel.
/// Handles license verification, prompt generation, ViewModel updates, and stale
/// context detection via editor selection change events.
/// </para>
/// <para>
/// <b>License Gating:</b> Requires <see cref="LicenseTier.WriterPro"/> or higher.
/// Throws <see cref="LicenseTierException"/> when the user's tier is insufficient.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public class SelectionContextService : ISelectionContextService
{
    private readonly CoPilotViewModel _viewModel;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _license;
    private readonly DefaultPromptGenerator _promptGenerator;
    private readonly ILogger<SelectionContextService> _logger;

    private string? _currentSelection;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionContextService"/>.
    /// </summary>
    /// <param name="viewModel">The Co-pilot ViewModel to update with selection context.</param>
    /// <param name="editorService">The editor service for selection tracking.</param>
    /// <param name="license">The license context for feature gating.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public SelectionContextService(
        CoPilotViewModel viewModel,
        IEditorService editorService,
        ILicenseContext license,
        ILogger<SelectionContextService> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _license = license ?? throw new ArgumentNullException(nameof(license));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _promptGenerator = new DefaultPromptGenerator();

        // LOGIC: Subscribe to editor selection changes to detect stale context.
        _editorService.SelectionChanged += OnSelectionChanged;

        _logger.LogDebug("SelectionContextService initialized");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="IEditorService.GetSelectedText"/>
    /// to determine if there is active text selection in the editor.
    /// </remarks>
    public bool HasActiveSelection =>
        !string.IsNullOrWhiteSpace(_editorService.GetSelectedText());

    /// <inheritdoc/>
    public string? CurrentSelection => _currentSelection;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Execution flow:
    /// 1. Verify WriterPro license (throws <see cref="LicenseTierException"/> if insufficient)
    /// 2. Validate selection is not null/empty (throws <see cref="ArgumentException"/>)
    /// 3. Store selection and generate default prompt
    /// 4. Update ViewModel: set context, pre-fill input, focus chat
    /// </remarks>
    public async Task SendSelectionToCoPilotAsync(string selection, CancellationToken ct = default)
    {
        // ───────────────────────────────────────────────────────────────
        // License Verification
        // ───────────────────────────────────────────────────────────────
        if (_license.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogWarning(
                "Selection context attempted without WriterPro license");
            throw new LicenseTierException(
                "Selection Context requires a WriterPro license.",
                LicenseTier.WriterPro);
        }

        // ───────────────────────────────────────────────────────────────
        // Input Validation
        // ───────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(selection))
        {
            _logger.LogDebug("Empty selection, no action taken");
            throw new ArgumentException("Selection cannot be empty.", nameof(selection));
        }

        _logger.LogDebug(
            "Processing selection context: {CharCount} chars",
            selection.Length);

        // ───────────────────────────────────────────────────────────────
        // Store Selection and Generate Prompt
        // ───────────────────────────────────────────────────────────────
        _currentSelection = selection;
        var defaultPrompt = GenerateDefaultPrompt(selection);

        _logger.LogDebug(
            "Generated default prompt: {PromptType}",
            defaultPrompt.Split(':')[0]);

        // ───────────────────────────────────────────────────────────────
        // Update ViewModel
        // ───────────────────────────────────────────────────────────────
        _viewModel.SetSelectionContext(selection);
        _viewModel.InputText = defaultPrompt + " ";
        _viewModel.FocusChatInput();

        _logger.LogInformation(
            "Selection context set: {CharCount} chars, prompt: {Prompt}",
            selection.Length,
            defaultPrompt);

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public string GenerateDefaultPrompt(string selection)
    {
        return _promptGenerator.Generate(selection);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Clears stored selection, updates ViewModel, and logs the action.
    /// </remarks>
    public void ClearSelectionContext()
    {
        _currentSelection = null;
        _viewModel.ClearSelectionContext();
        _logger.LogDebug("Selection context cleared");
    }

    /// <summary>
    /// Handles editor selection changes by clearing stale context.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Selection change details.</param>
    /// <remarks>
    /// LOGIC: If the stored selection no longer matches what the editor
    /// reports, clear the stored selection to prevent stale context.
    /// </remarks>
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // LOGIC: Clear stale context when selection changes.
        if (_currentSelection != null && e.NewSelection != _currentSelection)
        {
            _logger.LogDebug("Selection changed, clearing stale context");
            _currentSelection = null;
        }
    }
}
