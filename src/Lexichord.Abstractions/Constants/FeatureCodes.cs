// <copyright file="FeatureCodes.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Constants;

/// <summary>
/// Constants for feature codes used in license gating throughout Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1d - Feature codes provide granular control over feature enablement.
/// These codes are used with <see cref="Contracts.ILicenseContext.IsFeatureEnabled"/> to
/// check if specific features are available to the current license.
///
/// Feature Code Naming Convention:
/// - Format: {module}.{feature}
/// - Module: The primary module that owns the feature
/// - Feature: The specific feature within that module
///
/// Usage:
/// <code>
/// if (!licenseContext.IsFeatureEnabled(FeatureCodes.FuzzyMatching))
/// {
///     // Show upgrade prompt or disable feature UI
/// }
/// </code>
///
/// Design Decisions:
/// - Codes are dot-separated for readability and namespace-like organization
/// - All codes are compile-time constants for performance and IntelliSense support
/// - New feature codes should be documented with their required tier
/// </remarks>
public static class FeatureCodes
{
    #region Fuzzy Engine Features (v0.3.1)

    /// <summary>
    /// The Fuzzy Matching feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Levenshtein-based fuzzy matching algorithm.
    /// This feature enables approximate string matching for terminology detection.
    /// </remarks>
    public const string FuzzyMatching = "Feature.FuzzyMatching";

    /// <summary>
    /// The Fuzzy Scanning feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the fuzzy document scanner.
    /// When enabled, documents are scanned for approximate terminology matches
    /// in addition to exact regex matches.
    /// </remarks>
    public const string FuzzyScanning = "Feature.FuzzyScanning";

    /// <summary>
    /// The Fuzzy Threshold Configuration feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to per-term fuzzy threshold configuration.
    /// Users can customize the match sensitivity for individual terminology entries.
    /// </remarks>
    public const string FuzzyThresholdConfig = "Feature.FuzzyThresholdConfig";

    #endregion

    #region Feature Code Utilities

    /// <summary>
    /// Gets all fuzzy-related feature codes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for bulk feature checks in UI to determine if any fuzzy features are enabled.
    /// </remarks>
    public static readonly string[] FuzzyFeatures =
    [
        FuzzyMatching,
        FuzzyScanning,
        FuzzyThresholdConfig
    ];

    #endregion

    #region Readability Engine Features (v0.3.3)

    /// <summary>
    /// The Readability HUD feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Readability HUD Widget for displaying
    /// Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease.
    /// </remarks>
    public const string ReadabilityHud = "Feature.ReadabilityHud";

    #endregion

    #region Voice Profile Features (v0.3.4)

    /// <summary>
    /// The Custom Profiles feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: Teams
    /// Controls access to creating and managing custom Voice Profiles.
    /// Built-in profiles are available to all Writer Pro users.
    /// </remarks>
    public const string CustomProfiles = "Feature.CustomProfiles";

    #endregion

    #region Resonance Dashboard Features (v0.3.5)

    /// <summary>
    /// The Resonance Dashboard feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Resonance Dashboard spider chart visualization.
    /// This feature provides a 6-axis chart showing writing style metrics
    /// compared against target values from the active Voice Profile.
    /// </remarks>
    public const string ResonanceDashboard = "Feature.ResonanceDashboard";

    #endregion

    #region Global Dictionary Features (v0.3.6)

    /// <summary>
    /// The Global Dictionary feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to project-level configuration settings.
    /// This feature enables .lexichord/style.yaml project configuration,
    /// .lexichordignore file patterns, and per-project rule overrides.
    /// </remarks>
    public const string GlobalDictionary = "Feature.GlobalDictionary";

    #endregion

    #region Hybrid Search Features (v0.5.1)

    /// <summary>
    /// The Hybrid Search feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Hybrid Search mode which combines BM25 keyword
    /// search and semantic vector search using Reciprocal Rank Fusion (RRF).
    /// When this feature is not enabled, the Hybrid option in the search mode
    /// toggle is locked and the user is prompted to upgrade.
    /// Semantic and Keyword modes remain available to all tiers.
    /// Introduced in v0.5.1d.
    /// </remarks>
    public const string HybridSearch = "Feature.HybridSearch";

    #endregion

    #region Citation Engine Features (v0.5.2)

    /// <summary>
    /// The Citation feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to formatted citation creation and copy actions.
    /// When this feature is not enabled, users see basic document paths only
    /// and are prompted to upgrade for formatted citations.
    /// Introduced in v0.5.2a.
    /// </remarks>
    public const string Citation = "Feature.Citation";

    /// <summary>
    /// The Citation Validation feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to stale citation detection and validation.
    /// When this feature is not enabled, ValidateIfLicensedAsync returns null
    /// and stale indicators are hidden in the UI.
    /// Introduced in v0.5.2c.
    /// </remarks>
    public const string CitationValidation = "Feature.CitationValidation";

    #endregion

    #region Context Window Features (v0.5.3)

    /// <summary>
    /// The Context Expansion feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the context expansion preview in search results.
    /// When this feature is not enabled, the expand button shows a lock icon
    /// and clicking it triggers an upgrade prompt instead of expanding context.
    /// Context expansion retrieves sibling chunks (before/after) and heading
    /// breadcrumbs for a retrieved search result.
    /// Introduced in v0.5.3d.
    /// </remarks>
    public const string ContextExpansion = "Feature.ContextExpansion";

    #endregion

    #region Filter System Features (v0.5.5)

    /// <summary>
    /// The Date Range Filter feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to date range filtering in the search filter panel.
    /// When this feature is not enabled, the date range section shows a lock
    /// icon and clicking it triggers an upgrade prompt. Core tier users can
    /// still use path and extension filtering.
    /// Introduced in v0.5.5b.
    /// </remarks>
    public const string DateRangeFilter = "Feature.DateRangeFilter";

    /// <summary>
    /// The Saved Presets feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to saving and managing filter presets. When this feature
    /// is not enabled, the presets section shows a lock icon and preset
    /// save/apply functionality is disabled. Users can still manually configure
    /// filters but cannot save them for quick reuse.
    /// Introduced in v0.5.5b.
    /// </remarks>
    public const string SavedPresets = "Feature.SavedPresets";

    #endregion
}
