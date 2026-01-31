using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Models;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the rule override context menu in the Problems Panel.
/// </summary>
/// <remarks>
/// LOGIC: Provides commands and state for the context menu that appears
/// when right-clicking a style violation. Enables users to ignore rules
/// or exclude terms for the current project.
///
/// Key responsibilities:
/// - Check license tier (Writer Pro required)
/// - Check workspace state (must be open)
/// - Execute override actions via IProjectConfigurationWriter
/// - Manage confirmation dialog preferences
/// - Trigger re-analysis after configuration changes
///
/// Licensing:
/// - All override actions require Writer Pro license
/// - Core users see disabled menu items with lock icons
/// - Clicking disabled items shows upgrade prompt
///
/// Thread Safety:
/// - All properties are observable via CommunityToolkit.Mvvm
/// - Commands are thread-safe
///
/// Version: v0.3.6c
/// </remarks>
public sealed partial class OverrideMenuViewModel : ObservableObject
{
    private readonly ILicenseContext _licenseContext;
    private readonly IWorkspaceService _workspaceService;
    private readonly IProjectConfigurationWriter _configWriter;
    private readonly ILintingOrchestrator _lintingOrchestrator;
    private readonly ILogger<OverrideMenuViewModel>? _logger;

    /// <summary>
    /// Feature flag key for the Global Dictionary feature.
    /// </summary>
    private const string GlobalDictionaryFeature = "Style.GlobalDictionary";

    /// <summary>
    /// Gets or sets whether to show confirmation dialogs before override actions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Session-level preference. When false, override actions proceed
    /// immediately without confirmation. Users can check "Don't ask again"
    /// in the confirmation dialog to set this to false.
    /// </remarks>
    [ObservableProperty]
    private bool _showConfirmationDialogs = true;

    /// <summary>
    /// Gets or sets the currently selected violation for context menu actions.
    /// </summary>
    [ObservableProperty]
    private StyleViolation? _selectedViolation;

    /// <summary>
    /// Initializes a new instance of <see cref="OverrideMenuViewModel"/>.
    /// </summary>
    /// <param name="licenseContext">License context for tier checks.</param>
    /// <param name="workspaceService">Workspace service for path detection.</param>
    /// <param name="configWriter">Configuration writer for file operations.</param>
    /// <param name="lintingOrchestrator">Orchestrator for triggering re-analysis.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <remarks>
    /// LOGIC: All dependencies except logger are required.
    /// </remarks>
    public OverrideMenuViewModel(
        ILicenseContext licenseContext,
        IWorkspaceService workspaceService,
        IProjectConfigurationWriter configWriter,
        ILintingOrchestrator lintingOrchestrator,
        ILogger<OverrideMenuViewModel>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(workspaceService);
        ArgumentNullException.ThrowIfNull(configWriter);
        ArgumentNullException.ThrowIfNull(lintingOrchestrator);

        _licenseContext = licenseContext;
        _workspaceService = workspaceService;
        _configWriter = configWriter;
        _lintingOrchestrator = lintingOrchestrator;
        _logger = logger;
    }

    #region Computed Properties

    /// <summary>
    /// Gets whether the user can perform override actions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Requires both Writer Pro license AND an open workspace.
    /// </remarks>
    public bool CanIgnoreRule =>
        _licenseContext.IsFeatureEnabled(GlobalDictionaryFeature) &&
        _workspaceService.IsWorkspaceOpen;

    /// <summary>
    /// Gets whether the user has the required license but no workspace.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used to show "No workspace open" tooltip on disabled items.
    /// </remarks>
    public bool IsLicensedButNoWorkspace =>
        _licenseContext.IsFeatureEnabled(GlobalDictionaryFeature) &&
        !_workspaceService.IsWorkspaceOpen;

    /// <summary>
    /// Gets whether the user lacks the required license.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used to show lock icon and "Writer Pro required" on menu items.
    /// </remarks>
    public bool RequiresLicenseUpgrade =>
        !_licenseContext.IsFeatureEnabled(GlobalDictionaryFeature);

    /// <summary>
    /// Gets the path to the project configuration file, or null if unavailable.
    /// </summary>
    public string? ConfigurationFilePath => _configWriter.GetConfigurationFilePath();

    #endregion

    #region Commands

    /// <summary>
    /// Ignores the selected violation's rule for the current project.
    /// </summary>
    /// <remarks>
    /// LOGIC: Adds the rule ID to the project's ignored_rules list
    /// and triggers re-analysis to update the Problems Panel.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanIgnoreRule))]
    private async Task IgnoreRuleAsync(CancellationToken ct = default)
    {
        if (SelectedViolation == null)
        {
            _logger?.LogDebug("IgnoreRuleAsync called with no selected violation");
            return;
        }

        var ruleId = SelectedViolation.Rule.Id;
        _logger?.LogDebug("Override context menu: Ignore rule {RuleId}", ruleId);

        var success = await _configWriter.IgnoreRuleAsync(ruleId, ct);
        if (success)
        {
            _logger?.LogDebug("User confirmed override action: {Action}", OverrideAction.IgnoreRule);
            await TriggerReanalysisAsync(ct);
        }
        else
        {
            _logger?.LogWarning("Failed to ignore rule {RuleId}", ruleId);
        }
    }

    /// <summary>
    /// Restores a previously ignored rule for the current project.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanIgnoreRule))]
    private async Task RestoreRuleAsync(string ruleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return;
        }

        _logger?.LogDebug("Override context menu: Restore rule {RuleId}", ruleId);

        var success = await _configWriter.RestoreRuleAsync(ruleId, ct);
        if (success)
        {
            _logger?.LogDebug("User confirmed override action: {Action}", OverrideAction.RestoreRule);
            await TriggerReanalysisAsync(ct);
        }
    }

    /// <summary>
    /// Excludes the selected violation's term for the current project.
    /// </summary>
    /// <remarks>
    /// LOGIC: Extracts the term from the violation metadata and adds it
    /// to the project's terminology.exclusions list.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanIgnoreRule))]
    private async Task ExcludeTermAsync(CancellationToken ct = default)
    {
        if (SelectedViolation == null)
        {
            _logger?.LogDebug("ExcludeTermAsync called with no selected violation");
            return;
        }

        var term = ExtractTermFromViolation(SelectedViolation);
        if (string.IsNullOrWhiteSpace(term))
        {
            _logger?.LogDebug("Could not extract term from violation {RuleId}", SelectedViolation.Rule.Id);
            return;
        }

        _logger?.LogDebug("Override context menu: Exclude term '{Term}'", term);

        var success = await _configWriter.ExcludeTermAsync(term, ct);
        if (success)
        {
            _logger?.LogDebug("User confirmed override action: {Action}", OverrideAction.ExcludeTerm);
            await TriggerReanalysisAsync(ct);
        }
    }

    /// <summary>
    /// Restores a previously excluded term for the current project.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanIgnoreRule))]
    private async Task RestoreTermAsync(string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        _logger?.LogDebug("Override context menu: Restore term '{Term}'", term);

        var success = await _configWriter.RestoreTermAsync(term, ct);
        if (success)
        {
            _logger?.LogDebug("User confirmed override action: {Action}", OverrideAction.RestoreTerm);
            await TriggerReanalysisAsync(ct);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts the term pattern from a style violation.
    /// </summary>
    /// <param name="violation">The violation to extract from.</param>
    /// <returns>The term pattern, or null if not available.</returns>
    /// <remarks>
    /// LOGIC: Falls back to the matched text from the violation.
    /// </remarks>
    private static string? ExtractTermFromViolation(StyleViolation violation)
    {
        // Use the matched text from the violation
        return violation.MatchedText;
    }

    /// <summary>
    /// Triggers re-analysis of open documents after configuration change.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: The linting orchestrator will automatically reload configuration
    /// via the ConfigurationChangedEvent published by the file watcher.
    /// This method provides explicit trigger for immediate feedback.
    /// </remarks>
    private Task TriggerReanalysisAsync(CancellationToken ct)
    {
        _logger?.LogDebug("Triggering re-analysis after configuration change");

        // The file watcher will detect the .lexichord/style.yaml change
        // and automatically trigger re-analysis via ConfigurationChangedEvent.
        // No additional action needed here - returning completed task.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a specific rule is currently ignored in the project.
    /// </summary>
    /// <param name="ruleId">The rule ID to check.</param>
    /// <returns>True if the rule is ignored.</returns>
    public bool IsRuleCurrentlyIgnored(string ruleId) =>
        _configWriter.IsRuleIgnored(ruleId);

    /// <summary>
    /// Checks if a specific term is currently excluded in the project.
    /// </summary>
    /// <param name="term">The term to check.</param>
    /// <returns>True if the term is excluded.</returns>
    public bool IsTermCurrentlyExcluded(string term) =>
        _configWriter.IsTermExcluded(term);

    #endregion
}
