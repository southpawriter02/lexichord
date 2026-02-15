// -----------------------------------------------------------------------
// <copyright file="ReadabilityTargetService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Implementation of <see cref="IReadabilityTargetService"/> that determines and validates
/// readability targets for text simplification.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service is the foundation of the Simplifier Agent. It determines
/// what readability level to target when simplifying text by resolving parameters from
/// Voice Profile settings, audience presets, or explicit overrides.
/// </para>
/// <para>
/// <b>Preset Storage:</b>
/// Custom presets are stored via <see cref="ISettingsService"/> using the key
/// <see cref="CustomPresetsSettingsKey"/>. On construction, the service loads any
/// existing custom presets and merges them with built-in presets.
/// </para>
/// <para>
/// <b>Target Resolution Priority:</b>
/// <list type="number">
///   <item><description>Explicit parameters (if targetGradeLevel specified)</description></item>
///   <item><description>Preset settings (if presetId specified and found)</description></item>
///   <item><description>Voice Profile settings (if active profile has TargetGradeLevel)</description></item>
///   <item><description>Default target (Grade 8, 20 words, avoid jargon)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This service is thread-safe. Custom preset operations use locking to ensure
/// consistent reads and writes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
public sealed class ReadabilityTargetService : IReadabilityTargetService
{
    /// <summary>
    /// The settings key used to store custom presets.
    /// </summary>
    internal const string CustomPresetsSettingsKey = "SimplifierAgent:CustomPresets";

    /// <summary>
    /// Default grade level when no profile or preset is specified.
    /// </summary>
    internal const double DefaultTargetGradeLevel = 8.0;

    /// <summary>
    /// Default maximum sentence length when no profile or preset is specified.
    /// </summary>
    internal const int DefaultMaxSentenceLength = 20;

    /// <summary>
    /// Default grade level tolerance.
    /// </summary>
    internal const double DefaultGradeLevelTolerance = 2.0;

    private readonly IVoiceProfileService _voiceProfileService;
    private readonly IReadabilityService _readabilityService;
    private readonly ISettingsService _settingsService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<ReadabilityTargetService> _logger;

    private readonly object _presetsLock = new();
    private readonly List<AudiencePreset> _customPresets;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadabilityTargetService"/> class.
    /// </summary>
    /// <param name="voiceProfileService">Service for retrieving the active Voice Profile.</param>
    /// <param name="readabilityService">Service for analyzing text readability.</param>
    /// <param name="settingsService">Service for persisting custom presets.</param>
    /// <param name="licenseContext">License context for tier checking.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> On construction, loads any existing custom presets from settings.
    /// Logs the number of built-in and custom presets available.
    /// </remarks>
    public ReadabilityTargetService(
        IVoiceProfileService voiceProfileService,
        IReadabilityService readabilityService,
        ISettingsService settingsService,
        ILicenseContext licenseContext,
        ILogger<ReadabilityTargetService> logger)
    {
        ArgumentNullException.ThrowIfNull(voiceProfileService);
        ArgumentNullException.ThrowIfNull(readabilityService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(logger);

        _voiceProfileService = voiceProfileService;
        _readabilityService = readabilityService;
        _settingsService = settingsService;
        _licenseContext = licenseContext;
        _logger = logger;

        // LOGIC: Load custom presets from settings storage
        _customPresets = LoadCustomPresets();

        _logger.LogDebug(
            "ReadabilityTargetService initialized with {BuiltInCount} built-in presets and {CustomCount} custom presets",
            BuiltInPresets.All.Count,
            _customPresets.Count);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Returns built-in presets first (in display order), followed by
    /// custom presets sorted alphabetically by name. This ordering ensures consistent
    /// UI display across sessions.
    /// </remarks>
    public Task<IReadOnlyList<AudiencePreset>> GetAllPresetsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace("GetAllPresetsAsync called");

        lock (_presetsLock)
        {
            // LOGIC: Built-in presets first, then custom presets sorted by name
            var allPresets = BuiltInPresets.All
                .Concat(_customPresets.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();

            _logger.LogDebug("Returning {Count} total presets ({BuiltIn} built-in, {Custom} custom)",
                allPresets.Count, BuiltInPresets.All.Count, _customPresets.Count);

            return Task.FromResult<IReadOnlyList<AudiencePreset>>(allPresets);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Searches both built-in and custom presets with case-insensitive
    /// matching. Built-in presets are checked first for performance.
    /// </remarks>
    public Task<AudiencePreset?> GetPresetByIdAsync(string presetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            throw new ArgumentException("Preset ID cannot be null, empty, or whitespace.", nameof(presetId));

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace("GetPresetByIdAsync called for preset '{PresetId}'", presetId);

        // LOGIC: Check built-in presets first (no lock needed)
        var builtIn = BuiltInPresets.GetById(presetId);
        if (builtIn != null)
        {
            _logger.LogDebug("Found built-in preset '{PresetId}'", presetId);
            return Task.FromResult<AudiencePreset?>(builtIn);
        }

        // LOGIC: Check custom presets (with lock)
        lock (_presetsLock)
        {
            var custom = _customPresets.FirstOrDefault(p =>
                string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase));

            if (custom != null)
            {
                _logger.LogDebug("Found custom preset '{PresetId}'", presetId);
            }
            else
            {
                _logger.LogDebug("Preset '{PresetId}' not found", presetId);
            }

            return Task.FromResult(custom);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Resolution priority (highest to lowest):
    /// <list type="number">
    ///   <item><description>Explicit targetGradeLevel parameter</description></item>
    ///   <item><description>Preset specified by presetId</description></item>
    ///   <item><description>Active Voice Profile's TargetGradeLevel (if configured)</description></item>
    ///   <item><description>Default values (Grade 8, 20 words, avoid jargon)</description></item>
    /// </list>
    /// When using a preset, explicit parameters (maxSentenceLength, avoidJargon) can
    /// override specific preset values.
    /// </remarks>
    public async Task<ReadabilityTarget> GetTargetAsync(
        string? presetId = null,
        double? targetGradeLevel = null,
        int? maxSentenceLength = null,
        bool? avoidJargon = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace(
            "GetTargetAsync called with presetId={PresetId}, targetGradeLevel={GradeLevel}, " +
            "maxSentenceLength={SentenceLength}, avoidJargon={AvoidJargon}",
            presetId, targetGradeLevel, maxSentenceLength, avoidJargon);

        // LOGIC: Priority 1 - Explicit target grade level
        if (targetGradeLevel.HasValue)
        {
            _logger.LogDebug("Using explicit target grade level: {GradeLevel}", targetGradeLevel.Value);

            return ReadabilityTarget.FromExplicit(
                targetGradeLevel: targetGradeLevel.Value,
                maxSentenceLength: maxSentenceLength ?? DefaultMaxSentenceLength,
                avoidJargon: avoidJargon ?? true,
                tolerance: DefaultGradeLevelTolerance);
        }

        // LOGIC: Priority 2 - Preset
        if (!string.IsNullOrWhiteSpace(presetId))
        {
            var preset = await GetPresetByIdAsync(presetId, cancellationToken);
            if (preset != null)
            {
                _logger.LogDebug("Using preset '{PresetId}' for target resolution", presetId);

                // LOGIC: Allow explicit overrides for specific properties
                return new ReadabilityTarget(
                    TargetGradeLevel: preset.TargetGradeLevel,
                    GradeLevelTolerance: DefaultGradeLevelTolerance,
                    MaxSentenceLength: maxSentenceLength ?? preset.MaxSentenceLength,
                    AvoidJargon: avoidJargon ?? preset.AvoidJargon,
                    Source: ReadabilityTargetSource.Preset,
                    SourcePresetId: preset.Id);
            }
            else
            {
                _logger.LogWarning("Preset '{PresetId}' not found, falling back to profile/defaults", presetId);
            }
        }

        // LOGIC: Priority 3 - Voice Profile settings
        var profile = await _voiceProfileService.GetActiveProfileAsync(cancellationToken);
        if (profile.TargetGradeLevel.HasValue)
        {
            _logger.LogDebug(
                "Using Voice Profile '{ProfileName}' target grade level: {GradeLevel}",
                profile.Name, profile.TargetGradeLevel.Value);

            return new ReadabilityTarget(
                TargetGradeLevel: profile.TargetGradeLevel.Value,
                GradeLevelTolerance: profile.GradeLevelTolerance,
                MaxSentenceLength: maxSentenceLength ?? profile.MaxSentenceLength,
                AvoidJargon: avoidJargon ?? true,
                Source: ReadabilityTargetSource.VoiceProfile,
                SourcePresetId: null);
        }

        // LOGIC: Priority 4 - Defaults
        _logger.LogDebug("Using default target (Grade {GradeLevel}, {SentenceLength} words/sentence)",
            DefaultTargetGradeLevel, DefaultMaxSentenceLength);

        return new ReadabilityTarget(
            TargetGradeLevel: DefaultTargetGradeLevel,
            GradeLevelTolerance: DefaultGradeLevelTolerance,
            MaxSentenceLength: maxSentenceLength ?? DefaultMaxSentenceLength,
            AvoidJargon: avoidJargon ?? true,
            Source: ReadabilityTargetSource.Default,
            SourcePresetId: null);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Analyzes source text using <see cref="IReadabilityService"/> and
    /// determines achievability based on:
    /// <list type="bullet">
    ///   <item><description>Grade level delta (source - target)</description></item>
    ///   <item><description>Complex word ratio (warning if >30%)</description></item>
    ///   <item><description>Average sentence length vs target</description></item>
    /// </list>
    /// </remarks>
    public async Task<TargetValidationResult> ValidateTarget(
        string sourceText,
        ReadabilityTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(target);

        if (string.IsNullOrWhiteSpace(sourceText))
            throw new ArgumentException("Source text cannot be empty or whitespace.", nameof(sourceText));

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace("ValidateTarget called for text of length {Length} with target grade {GradeLevel}",
            sourceText.Length, target.TargetGradeLevel);

        // LOGIC: Analyze source text readability
        var metrics = await _readabilityService.AnalyzeAsync(sourceText, cancellationToken);

        _logger.LogDebug(
            "Source text metrics: Grade={GradeLevel:F1}, ComplexWordRatio={ComplexRatio:P1}, " +
            "AvgWordsPerSentence={AvgWords:F1}",
            metrics.FleschKincaidGradeLevel,
            metrics.ComplexWordRatio,
            metrics.AverageWordsPerSentence);

        var sourceGrade = metrics.FleschKincaidGradeLevel;
        var targetGrade = target.TargetGradeLevel;
        var delta = sourceGrade - targetGrade;

        // LOGIC: Check if already met
        if (sourceGrade <= targetGrade)
        {
            _logger.LogDebug("Target already met: source grade {Source:F1} <= target {Target:F1}",
                sourceGrade, targetGrade);
            return TargetValidationResult.AlreadyMet(sourceGrade, targetGrade);
        }

        // LOGIC: Gather warnings
        var warnings = new List<string>();

        // LOGIC: Warning for high complex word ratio (>30%)
        if (metrics.ComplexWordRatio > 0.30)
        {
            var percentage = metrics.ComplexWordRatio * 100;
            warnings.Add($"High complex word percentage ({percentage:F0}%) may limit simplification potential.");
            _logger.LogDebug("Warning: High complex word ratio {Ratio:P1}", metrics.ComplexWordRatio);
        }

        // LOGIC: Warning for long sentences exceeding target by significant margin
        if (metrics.AverageWordsPerSentence > target.MaxSentenceLength + 10)
        {
            warnings.Add($"Average sentence length ({metrics.AverageWordsPerSentence:F0} words) " +
                         $"significantly exceeds target ({target.MaxSentenceLength} words).");
            _logger.LogDebug("Warning: Long average sentence length {Length:F1} vs target {Target}",
                metrics.AverageWordsPerSentence, target.MaxSentenceLength);
        }

        // LOGIC: Warning for extreme grade reduction
        if (delta > 4)
        {
            warnings.Add($"Target requires reducing {delta:F1} grade levels, which may require substantial rewriting.");
            _logger.LogDebug("Warning: Large grade delta {Delta:F1}", delta);
        }

        // LOGIC: Determine achievability
        TargetAchievability achievability;
        string? suggestedPreset = null;

        if (delta <= 3 && warnings.Count == 0)
        {
            achievability = TargetAchievability.Achievable;
            _logger.LogDebug("Target is achievable (delta={Delta:F1}, no warnings)", delta);
        }
        else if (delta <= 5 || (delta <= 6 && warnings.Count <= 1))
        {
            achievability = TargetAchievability.Challenging;
            _logger.LogDebug("Target is challenging (delta={Delta:F1}, warnings={Warnings})", delta, warnings.Count);
        }
        else
        {
            achievability = TargetAchievability.Unlikely;

            // LOGIC: Suggest a more appropriate preset based on source grade
            suggestedPreset = SuggestAppropriatePreset(sourceGrade);
            _logger.LogDebug("Target is unlikely (delta={Delta:F1}), suggesting preset '{Preset}'",
                delta, suggestedPreset);
        }

        return new TargetValidationResult(
            Achievability: achievability,
            SourceGradeLevel: sourceGrade,
            TargetGradeLevel: targetGrade,
            GradeLevelDelta: delta,
            Warnings: warnings.AsReadOnly(),
            SuggestedPreset: suggestedPreset);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Validates the preset, checks license tier, ensures no duplicate ID,
    /// adds to collection, and persists to settings. Forces IsBuiltIn to false.
    /// </remarks>
    public Task<AudiencePreset> CreateCustomPresetAsync(
        AudiencePreset preset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace("CreateCustomPresetAsync called for preset '{PresetId}'", preset.Id);

        // LOGIC: Check license tier
        EnsureCustomPresetLicense();

        // LOGIC: Validate preset
        var (isValid, errors) = preset.Validate();
        if (!isValid)
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Preset validation failed: {Errors}", errorMessage);
            throw new ArgumentException($"Invalid preset: {errorMessage}", nameof(preset));
        }

        lock (_presetsLock)
        {
            // LOGIC: Check for duplicate ID (including built-in presets)
            if (BuiltInPresets.IsBuiltInId(preset.Id))
            {
                _logger.LogWarning("Cannot create preset with built-in ID '{PresetId}'", preset.Id);
                throw new InvalidOperationException(
                    $"Cannot create a custom preset with built-in ID '{preset.Id}'.");
            }

            if (_customPresets.Any(p => string.Equals(p.Id, preset.Id, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Preset with ID '{PresetId}' already exists", preset.Id);
                throw new InvalidOperationException(
                    $"A preset with ID '{preset.Id}' already exists.");
            }

            // LOGIC: Force IsBuiltIn to false
            var customPreset = preset with { IsBuiltIn = false };

            _customPresets.Add(customPreset);
            SaveCustomPresets();

            _logger.LogInformation("Created custom preset '{PresetId}' ('{PresetName}')",
                customPreset.Id, customPreset.Name);

            return Task.FromResult(customPreset);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Validates the preset, checks license tier, ensures it's a custom preset
    /// (not built-in), updates in collection, and persists to settings.
    /// </remarks>
    public Task<AudiencePreset> UpdateCustomPresetAsync(
        AudiencePreset preset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace("UpdateCustomPresetAsync called for preset '{PresetId}'", preset.Id);

        // LOGIC: Check license tier
        EnsureCustomPresetLicense();

        // LOGIC: Cannot update built-in presets
        if (BuiltInPresets.IsBuiltInId(preset.Id))
        {
            _logger.LogWarning("Cannot update built-in preset '{PresetId}'", preset.Id);
            throw new InvalidOperationException(
                $"Cannot update built-in preset '{preset.Id}'. Create a custom copy instead.");
        }

        // LOGIC: Validate preset
        var (isValid, errors) = preset.Validate();
        if (!isValid)
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Preset validation failed: {Errors}", errorMessage);
            throw new ArgumentException($"Invalid preset: {errorMessage}", nameof(preset));
        }

        lock (_presetsLock)
        {
            // LOGIC: Find existing preset
            var index = _customPresets.FindIndex(p =>
                string.Equals(p.Id, preset.Id, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
            {
                _logger.LogWarning("Custom preset '{PresetId}' not found", preset.Id);
                throw new InvalidOperationException(
                    $"Custom preset '{preset.Id}' not found. Use CreateCustomPresetAsync to create it.");
            }

            // LOGIC: Force IsBuiltIn to false
            var updatedPreset = preset with { IsBuiltIn = false };

            _customPresets[index] = updatedPreset;
            SaveCustomPresets();

            _logger.LogInformation("Updated custom preset '{PresetId}' ('{PresetName}')",
                updatedPreset.Id, updatedPreset.Name);

            return Task.FromResult(updatedPreset);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>LOGIC:</b> Checks license tier, ensures it's not a built-in preset,
    /// removes from collection, and persists to settings.
    /// </remarks>
    public Task<bool> DeleteCustomPresetAsync(string presetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            throw new ArgumentException("Preset ID cannot be null, empty, or whitespace.", nameof(presetId));

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogTrace("DeleteCustomPresetAsync called for preset '{PresetId}'", presetId);

        // LOGIC: Check license tier
        EnsureCustomPresetLicense();

        // LOGIC: Cannot delete built-in presets
        if (BuiltInPresets.IsBuiltInId(presetId))
        {
            _logger.LogWarning("Cannot delete built-in preset '{PresetId}'", presetId);
            throw new InvalidOperationException($"Cannot delete built-in preset '{presetId}'.");
        }

        lock (_presetsLock)
        {
            var removed = _customPresets.RemoveAll(p =>
                string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
            {
                SaveCustomPresets();
                _logger.LogInformation("Deleted custom preset '{PresetId}'", presetId);
            }
            else
            {
                _logger.LogDebug("Custom preset '{PresetId}' not found (already deleted or never existed)", presetId);
            }

            return Task.FromResult(removed);
        }
    }

    /// <summary>
    /// Loads custom presets from settings storage.
    /// </summary>
    /// <returns>List of custom presets, or empty list if none stored.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Deserializes from settings service. If deserialization fails,
    /// logs a warning and returns an empty list to avoid startup failures.
    /// </remarks>
    private List<AudiencePreset> LoadCustomPresets()
    {
        try
        {
            var presets = _settingsService.Get<List<AudiencePreset>>(
                CustomPresetsSettingsKey,
                new List<AudiencePreset>());

            // LOGIC: Filter out any invalid presets that may have been corrupted
            var validPresets = presets
                .Where(p => p.IsValid())
                .ToList();

            if (validPresets.Count != presets.Count)
            {
                _logger.LogWarning(
                    "Filtered out {Count} invalid custom presets during load",
                    presets.Count - validPresets.Count);
            }

            _logger.LogDebug("Loaded {Count} custom presets from settings", validPresets.Count);
            return validPresets;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load custom presets from settings, starting with empty list");
            return new List<AudiencePreset>();
        }
    }

    /// <summary>
    /// Saves custom presets to settings storage.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Serializes to settings service. Must be called within
    /// <see cref="_presetsLock"/> to ensure consistency.
    /// </remarks>
    private void SaveCustomPresets()
    {
        try
        {
            _settingsService.Set(CustomPresetsSettingsKey, _customPresets);
            _logger.LogDebug("Saved {Count} custom presets to settings", _customPresets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save custom presets to settings");
            throw;
        }
    }

    /// <summary>
    /// Ensures the current license tier allows custom preset operations.
    /// </summary>
    /// <exception cref="LicenseTierException">Thrown when license tier is insufficient.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Checks the <see cref="FeatureCodes.CustomAudiencePresets"/> feature code.
    /// Requires WriterPro or higher tier.
    /// </remarks>
    private void EnsureCustomPresetLicense()
    {
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.CustomAudiencePresets))
        {
            _logger.LogWarning("Custom preset operation denied: license tier insufficient");
            throw new LicenseTierException(
                "Custom audience presets require a WriterPro or Teams license.",
                LicenseTier.WriterPro);
        }
    }

    /// <summary>
    /// Suggests an appropriate preset based on the source text's grade level.
    /// </summary>
    /// <param name="sourceGrade">The source text's Flesch-Kincaid grade level.</param>
    /// <returns>The suggested preset ID.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Suggests a preset that is achievable (within ~3-4 grade levels of source):
    /// <list type="bullet">
    ///   <item><description>Source 9-12: suggest "general-public" (Grade 8)</description></item>
    ///   <item><description>Source 13-15: suggest "executive" (Grade 10)</description></item>
    ///   <item><description>Source 16+: suggest "technical" (Grade 12)</description></item>
    ///   <item><description>Default: suggest "general-public"</description></item>
    /// </list>
    /// </remarks>
    private static string SuggestAppropriatePreset(double sourceGrade)
    {
        return sourceGrade switch
        {
            >= 16 => BuiltInPresets.TechnicalId,
            >= 13 => BuiltInPresets.ExecutiveId,
            _ => BuiltInPresets.GeneralPublicId
        };
    }
}
