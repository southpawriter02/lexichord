# LCS-DES-074a: Design Specification — Readability Target Service

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `AGT-074a` | Sub-part of AGT-074 |
| **Feature Name** | `Readability Target Service` | Audience configuration and target calculation |
| **Target Version** | `v0.7.4a` | First sub-part of v0.7.4 |
| **Module Scope** | `Lexichord.Modules.Agents` | Agent services module |
| **Swimlane** | `Ensemble` | Part of Agents vertical |
| **License Tier** | `WriterPro` | Required for simplification features |
| **Feature Gate Key** | `FeatureFlags.Agents.Simplifier` | Shared with parent feature |
| **Author** | Lead Architect | |
| **Reviewer** | | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-074-INDEX](./LCS-DES-074-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-074 Section 3.1](./LCS-SBD-074.md#31-v074a-readability-target-service) | |

---

## 2. Executive Summary

### 2.1 The Requirement

The Simplifier Agent needs to know what readability level to target. Different audiences require different complexity levels:

- General public: 8th grade reading level
- Technical professionals: 12th grade (college freshman)
- C-suite executives: 10th grade (concise, business-focused)
- International/ESL readers: 6th grade (simple vocabulary, short sentences)

Writers need a way to configure these targets either through their Voice Profile or by selecting predefined audience presets.

> **Goal:** Provide a service that determines the target Flesch-Kincaid grade level and associated constraints (sentence length, jargon handling) based on audience configuration.

### 2.2 The Proposed Solution

Implement `IReadabilityTargetService` that:

1. Reads audience settings from the active Voice Profile
2. Provides built-in audience presets with sensible defaults
3. Allows custom preset definition and storage
4. Returns a `ReadabilityTarget` record with all simplification constraints
5. Validates targets against the current text to ensure achievability

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `VoiceProfile` | v0.3.4a | Read target audience configuration |
| `IVoiceProfileService` | v0.3.4a | Access active Voice Profile |
| `IReadabilityService` | v0.3.3c | Validate target feasibility |
| `ISettingsService` | v0.1.6a | Store custom presets |
| `ILicenseContext` | v0.0.4c | Feature gating |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Text.Json` | 9.x | JSON serialization for presets |
| (No new packages) | - | - |

### 3.2 Licensing Behavior

- **Load Behavior:** Soft Gate - service loads but returns defaults for unlicensed users
- **Fallback Experience:**
  - Core/Writer tiers get default "General Public" target
  - Preset selection UI shows lock icons with upgrade prompts
  - Custom preset creation requires WriterPro

---

## 4. Data Contract (The API)

### 4.1 Core Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Determines the target readability level based on audience configuration.
/// Integrates with Voice Profile and provides audience presets.
/// </summary>
public interface IReadabilityTargetService
{
    /// <summary>
    /// Gets the readability target from the current Voice Profile.
    /// Falls back to "general-public" if no profile or target configured.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculated readability target.</returns>
    Task<ReadabilityTarget> GetTargetFromProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the readability target for a specific audience preset.
    /// </summary>
    /// <param name="presetId">The preset identifier (e.g., "general-public").</param>
    /// <returns>The readability target for the preset.</returns>
    /// <exception cref="ArgumentException">If preset not found.</exception>
    ReadabilityTarget GetTargetForPreset(string presetId);

    /// <summary>
    /// Gets all available audience presets (built-in and custom).
    /// </summary>
    /// <returns>List of available presets.</returns>
    IReadOnlyList<AudiencePreset> GetAvailablePresets();

    /// <summary>
    /// Gets only built-in (non-deletable) presets.
    /// </summary>
    /// <returns>List of built-in presets.</returns>
    IReadOnlyList<AudiencePreset> GetBuiltInPresets();

    /// <summary>
    /// Gets only custom (user-defined) presets.
    /// </summary>
    /// <returns>List of custom presets.</returns>
    IReadOnlyList<AudiencePreset> GetCustomPresets();

    /// <summary>
    /// Registers a custom audience preset.
    /// </summary>
    /// <param name="preset">The preset to register.</param>
    /// <exception cref="InvalidOperationException">If preset ID already exists.</exception>
    void RegisterPreset(AudiencePreset preset);

    /// <summary>
    /// Updates an existing custom preset.
    /// </summary>
    /// <param name="preset">The updated preset.</param>
    /// <exception cref="InvalidOperationException">If preset is built-in or not found.</exception>
    void UpdatePreset(AudiencePreset preset);

    /// <summary>
    /// Deletes a custom preset.
    /// </summary>
    /// <param name="presetId">The preset identifier.</param>
    /// <exception cref="InvalidOperationException">If preset is built-in.</exception>
    void DeletePreset(string presetId);

    /// <summary>
    /// Validates whether a target is achievable for the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <param name="target">The proposed target.</param>
    /// <returns>Validation result with warnings if target may be difficult.</returns>
    TargetValidationResult ValidateTarget(string text, ReadabilityTarget target);
}
```

### 4.2 Data Records

```csharp
namespace Lexichord.Abstractions.Models;

/// <summary>
/// Defines a target audience configuration for readability simplification.
/// </summary>
public record AudiencePreset
{
    /// <summary>
    /// Unique identifier (e.g., "general-public", "technical", "executive").
    /// Must be lowercase with hyphens, no spaces.
    /// </summary>
    public required string PresetId { get; init; }

    /// <summary>
    /// Human-readable display name (e.g., "General Public").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the target audience and use cases.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Target Flesch-Kincaid Grade Level (1-18).
    /// Common values: 6 (elementary), 8 (middle school), 10-12 (high school), 13+ (college).
    /// </summary>
    public required int TargetGradeLevel { get; init; }

    /// <summary>
    /// Whether to replace jargon with plain language.
    /// True = replace, False = explain but preserve.
    /// </summary>
    public bool AvoidJargon { get; init; } = true;

    /// <summary>
    /// Whether to convert passive voice to active voice.
    /// </summary>
    public bool PreferActiveVoice { get; init; } = true;

    /// <summary>
    /// Maximum recommended words per sentence.
    /// Sentences exceeding this may be split.
    /// </summary>
    public int MaxSentenceLength { get; init; } = 20;

    /// <summary>
    /// Maximum recommended sentences per paragraph.
    /// </summary>
    public int MaxParagraphSentences { get; init; } = 5;

    /// <summary>
    /// Optional list of domain-specific terms to always simplify.
    /// </summary>
    public IReadOnlyList<string>? TermsToSimplify { get; init; }

    /// <summary>
    /// Optional custom term replacements (term -> replacement).
    /// </summary>
    public IReadOnlyDictionary<string, string>? TermReplacements { get; init; }

    /// <summary>
    /// Whether this is a built-in preset (cannot be modified or deleted).
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Icon identifier for UI display (e.g., "users", "code", "briefcase").
    /// </summary>
    public string Icon { get; init; } = "users";
}

/// <summary>
/// The calculated readability target for a simplification operation.
/// </summary>
public record ReadabilityTarget
{
    /// <summary>
    /// Target Flesch-Kincaid Grade Level to achieve.
    /// </summary>
    public required int TargetFleschKincaidGrade { get; init; }

    /// <summary>
    /// Maximum words per sentence (sentences above this should be split).
    /// </summary>
    public required int MaxSentenceWords { get; init; }

    /// <summary>
    /// Maximum sentences per paragraph.
    /// </summary>
    public required int MaxParagraphSentences { get; init; }

    /// <summary>
    /// Whether to replace jargon with plain language.
    /// </summary>
    public required bool SimplifyJargon { get; init; }

    /// <summary>
    /// Whether to convert passive voice to active voice.
    /// </summary>
    public required bool ConvertPassiveToActive { get; init; }

    /// <summary>
    /// The preset ID this target was derived from (null if custom).
    /// </summary>
    public string? SourcePresetId { get; init; }

    /// <summary>
    /// Optional term replacements to apply during simplification.
    /// </summary>
    public IReadOnlyDictionary<string, string>? TermReplacements { get; init; }

    /// <summary>
    /// Tolerance for grade level (target achieved if within this range).
    /// Default is 1 grade level.
    /// </summary>
    public int GradeTolerance { get; init; } = 1;
}

/// <summary>
/// Result of validating a readability target against source text.
/// </summary>
public record TargetValidationResult(
    bool IsAchievable,
    double CurrentGradeLevel,
    double GradeLevelGap,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Suggestions
);
```

---

## 5. Implementation Logic

### 5.1 Built-in Presets Definition

```csharp
namespace Lexichord.Modules.Agents.Services;

public static class BuiltInPresets
{
    public static readonly AudiencePreset GeneralPublic = new()
    {
        PresetId = "general-public",
        Name = "General Public",
        Description = "Everyday readers, news articles, blog posts, marketing content",
        TargetGradeLevel = 8,
        AvoidJargon = true,
        PreferActiveVoice = true,
        MaxSentenceLength = 20,
        MaxParagraphSentences = 5,
        IsBuiltIn = true,
        Icon = "users"
    };

    public static readonly AudiencePreset Technical = new()
    {
        PresetId = "technical",
        Name = "Technical Audience",
        Description = "Developers, engineers, scientists, domain experts",
        TargetGradeLevel = 12,
        AvoidJargon = false, // Explain rather than replace
        PreferActiveVoice = true,
        MaxSentenceLength = 30,
        MaxParagraphSentences = 6,
        IsBuiltIn = true,
        Icon = "code"
    };

    public static readonly AudiencePreset Executive = new()
    {
        PresetId = "executive",
        Name = "Executive Summary",
        Description = "C-suite, decision makers, time-constrained busy readers",
        TargetGradeLevel = 10,
        AvoidJargon = true,
        PreferActiveVoice = true,
        MaxSentenceLength = 15, // Very concise
        MaxParagraphSentences = 4,
        IsBuiltIn = true,
        Icon = "briefcase"
    };

    public static readonly AudiencePreset International = new()
    {
        PresetId = "international",
        Name = "International (ESL)",
        Description = "Non-native English speakers, global audiences",
        TargetGradeLevel = 6,
        AvoidJargon = true,
        PreferActiveVoice = true,
        MaxSentenceLength = 15,
        MaxParagraphSentences = 4,
        IsBuiltIn = true,
        Icon = "globe"
    };

    public static IReadOnlyList<AudiencePreset> All => new[]
    {
        GeneralPublic,
        Technical,
        Executive,
        International
    };
}
```

### 5.2 Service Implementation

```csharp
namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Determines readability targets from Voice Profile or audience presets.
/// </summary>
public class ReadabilityTargetService : IReadabilityTargetService
{
    private readonly IVoiceProfileService _voiceProfileService;
    private readonly IReadabilityService _readabilityService;
    private readonly ISettingsService _settingsService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<ReadabilityTargetService> _logger;

    private readonly Dictionary<string, AudiencePreset> _presets;
    private const string CustomPresetsKey = "SimplifierAgent.CustomPresets";

    public ReadabilityTargetService(
        IVoiceProfileService voiceProfileService,
        IReadabilityService readabilityService,
        ISettingsService settingsService,
        ILicenseContext licenseContext,
        ILogger<ReadabilityTargetService> logger)
    {
        _voiceProfileService = voiceProfileService;
        _readabilityService = readabilityService;
        _settingsService = settingsService;
        _licenseContext = licenseContext;
        _logger = logger;

        _presets = BuiltInPresets.All.ToDictionary(p => p.PresetId);
        LoadCustomPresets();
    }

    public async Task<ReadabilityTarget> GetTargetFromProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var profile = await _voiceProfileService.GetActiveProfileAsync(ct);

            if (profile?.TargetAudience is not null)
            {
                _logger.LogDebug(
                    "Using Voice Profile target audience: {Audience}",
                    profile.TargetAudience);

                // Map Voice Profile audience to preset
                var presetId = MapAudienceToPreset(profile.TargetAudience);
                return GetTargetForPreset(presetId);
            }

            // Check for explicit grade level in profile
            if (profile?.TargetReadingLevel.HasValue == true)
            {
                return new ReadabilityTarget
                {
                    TargetFleschKincaidGrade = profile.TargetReadingLevel.Value,
                    MaxSentenceWords = profile.MaxSentenceLength ?? 20,
                    MaxParagraphSentences = 5,
                    SimplifyJargon = profile.SimplifyJargon ?? true,
                    ConvertPassiveToActive = profile.PreferActiveVoice ?? true,
                    SourcePresetId = null
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Voice Profile, using default");
        }

        // Default to General Public
        return GetTargetForPreset("general-public");
    }

    public ReadabilityTarget GetTargetForPreset(string presetId)
    {
        if (!_presets.TryGetValue(presetId, out var preset))
        {
            throw new ArgumentException($"Preset '{presetId}' not found", nameof(presetId));
        }

        _logger.LogDebug("Loaded preset: {PresetId} with grade {GradeLevel}",
            presetId, preset.TargetGradeLevel);

        return new ReadabilityTarget
        {
            TargetFleschKincaidGrade = preset.TargetGradeLevel,
            MaxSentenceWords = preset.MaxSentenceLength,
            MaxParagraphSentences = preset.MaxParagraphSentences,
            SimplifyJargon = preset.AvoidJargon,
            ConvertPassiveToActive = preset.PreferActiveVoice,
            SourcePresetId = presetId,
            TermReplacements = preset.TermReplacements
        };
    }

    public IReadOnlyList<AudiencePreset> GetAvailablePresets()
    {
        return _presets.Values.OrderBy(p => p.IsBuiltIn ? 0 : 1)
                              .ThenBy(p => p.Name)
                              .ToList();
    }

    public IReadOnlyList<AudiencePreset> GetBuiltInPresets()
    {
        return _presets.Values.Where(p => p.IsBuiltIn).ToList();
    }

    public IReadOnlyList<AudiencePreset> GetCustomPresets()
    {
        return _presets.Values.Where(p => !p.IsBuiltIn).ToList();
    }

    public void RegisterPreset(AudiencePreset preset)
    {
        if (!_licenseContext.HasFeature(FeatureFlags.Agents.Simplifier))
        {
            throw new LicenseRequiredException("Custom presets require WriterPro");
        }

        if (_presets.ContainsKey(preset.PresetId))
        {
            throw new InvalidOperationException($"Preset '{preset.PresetId}' already exists");
        }

        ValidatePreset(preset);

        var customPreset = preset with { IsBuiltIn = false };
        _presets[customPreset.PresetId] = customPreset;
        SaveCustomPresets();

        _logger.LogInformation("Registered custom preset: {PresetId}", preset.PresetId);
    }

    public void UpdatePreset(AudiencePreset preset)
    {
        if (!_presets.TryGetValue(preset.PresetId, out var existing))
        {
            throw new InvalidOperationException($"Preset '{preset.PresetId}' not found");
        }

        if (existing.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot modify built-in presets");
        }

        ValidatePreset(preset);

        _presets[preset.PresetId] = preset with { IsBuiltIn = false };
        SaveCustomPresets();

        _logger.LogInformation("Updated custom preset: {PresetId}", preset.PresetId);
    }

    public void DeletePreset(string presetId)
    {
        if (!_presets.TryGetValue(presetId, out var preset))
        {
            throw new InvalidOperationException($"Preset '{presetId}' not found");
        }

        if (preset.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot delete built-in presets");
        }

        _presets.Remove(presetId);
        SaveCustomPresets();

        _logger.LogInformation("Deleted custom preset: {PresetId}", presetId);
    }

    public TargetValidationResult ValidateTarget(string text, ReadabilityTarget target)
    {
        var metrics = _readabilityService.Analyze(text);
        var gap = metrics.FleschKincaidGradeLevel - target.TargetFleschKincaidGrade;

        var warnings = new List<string>();
        var suggestions = new List<string>();

        // Check if target is achievable
        bool isAchievable = true;

        if (gap > 8)
        {
            warnings.Add($"Large gap ({gap:F1} grade levels) may require significant rewriting");
            suggestions.Add("Consider using the 'Aggressive' simplification strategy");
        }

        if (target.TargetFleschKincaidGrade < 4 && gap > 4)
        {
            warnings.Add("Very low target grade may compromise technical accuracy");
            suggestions.Add("Consider the 'International (ESL)' preset for extremely simple output");
        }

        if (metrics.AverageSentenceLength > target.MaxSentenceWords * 2)
        {
            warnings.Add($"Average sentence length ({metrics.AverageSentenceLength:F0} words) is very high");
            suggestions.Add("Many sentences will need to be split");
        }

        if (metrics.ComplexWordPercentage > 30 && target.SimplifyJargon)
        {
            warnings.Add($"High complex word percentage ({metrics.ComplexWordPercentage:F0}%)");
            suggestions.Add("Consider generating a glossary for technical terms");
        }

        return new TargetValidationResult(
            IsAchievable: isAchievable,
            CurrentGradeLevel: metrics.FleschKincaidGradeLevel,
            GradeLevelGap: gap,
            Warnings: warnings,
            Suggestions: suggestions
        );
    }

    private string MapAudienceToPreset(string audience)
    {
        return audience.ToLowerInvariant() switch
        {
            "general" or "public" or "general public" => "general-public",
            "technical" or "developer" or "engineer" => "technical",
            "executive" or "business" or "c-suite" => "executive",
            "international" or "esl" or "global" => "international",
            _ => _presets.ContainsKey(audience.ToLowerInvariant())
                ? audience.ToLowerInvariant()
                : "general-public"
        };
    }

    private void ValidatePreset(AudiencePreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.PresetId))
            throw new ArgumentException("PresetId is required");

        if (!System.Text.RegularExpressions.Regex.IsMatch(preset.PresetId, @"^[a-z0-9-]+$"))
            throw new ArgumentException("PresetId must be lowercase with hyphens only");

        if (string.IsNullOrWhiteSpace(preset.Name))
            throw new ArgumentException("Name is required");

        if (preset.TargetGradeLevel < 1 || preset.TargetGradeLevel > 18)
            throw new ArgumentException("TargetGradeLevel must be between 1 and 18");

        if (preset.MaxSentenceLength < 5 || preset.MaxSentenceLength > 50)
            throw new ArgumentException("MaxSentenceLength must be between 5 and 50");
    }

    private void LoadCustomPresets()
    {
        try
        {
            var json = _settingsService.GetValue<string>(CustomPresetsKey);
            if (!string.IsNullOrEmpty(json))
            {
                var customPresets = JsonSerializer.Deserialize<List<AudiencePreset>>(json);
                if (customPresets != null)
                {
                    foreach (var preset in customPresets)
                    {
                        _presets[preset.PresetId] = preset with { IsBuiltIn = false };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load custom presets");
        }
    }

    private void SaveCustomPresets()
    {
        var customPresets = _presets.Values.Where(p => !p.IsBuiltIn).ToList();
        var json = JsonSerializer.Serialize(customPresets);
        _settingsService.SetValue(CustomPresetsKey, json);
    }
}
```

### 5.3 Target Calculation Flow

```text
GET TARGET FROM PROFILE:
│
├── 1. Load active Voice Profile
│   └── voiceProfileService.GetActiveProfileAsync()
│
├── 2. Check for target audience setting
│   ├── IF profile.TargetAudience exists
│   │   ├── Map audience string to preset ID
│   │   └── Return GetTargetForPreset(presetId)
│   │
│   └── IF profile.TargetReadingLevel exists
│       └── Build custom target from profile settings
│
├── 3. No profile or settings?
│   └── Return default: GetTargetForPreset("general-public")
│
└── 4. Return ReadabilityTarget
```

---

## 6. Data Persistence

### 6.1 Storage Location

Custom presets are stored via `ISettingsService`:

- Windows: `%APPDATA%/Lexichord/settings.json` (under `SimplifierAgent.CustomPresets` key)
- macOS: `~/Library/Application Support/Lexichord/settings.json`
- Linux: `~/.config/Lexichord/settings.json`

### 6.2 Schema

```json
{
  "SimplifierAgent.CustomPresets": [
    {
      "presetId": "my-company",
      "name": "Company Style",
      "description": "Internal communications for employees",
      "targetGradeLevel": 9,
      "avoidJargon": true,
      "preferActiveVoice": true,
      "maxSentenceLength": 18,
      "maxParagraphSentences": 4,
      "isBuiltIn": false,
      "icon": "building",
      "termReplacements": {
        "leverage": "use",
        "synergy": "collaboration"
      }
    }
  ]
}
```

---

## 7. UI/UX Specifications

### 7.1 Preset Selector Component

```text
┌─────────────────────────────────────────────────────────┐
│  Target Audience                                        │
├─────────────────────────────────────────────────────────┤
│  ┌───────────────────────────────────────────────────┐  │
│  │ [👥] General Public                            ▼ │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │ ○ [👥] General Public                             │  │
│  │   8th grade | Everyday readers                    │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ ○ [💻] Technical Audience                         │  │
│  │   12th grade | Developers, engineers              │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ ○ [💼] Executive Summary                          │  │
│  │   10th grade | C-suite, decision makers           │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ ○ [🌐] International (ESL)                        │  │
│  │   6th grade | Non-native speakers                 │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ ── Custom Presets ──                              │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ ○ [🏢] Company Style                              │  │
│  │   9th grade | Internal communications             │  │
│  ├───────────────────────────────────────────────────┤  │
│  │ [+ Create Custom Preset...]                       │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 7.2 Custom Preset Editor

```text
┌─────────────────────────────────────────────────────────┐
│  Create Custom Preset                         [×]       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Preset ID*     [my-company-style         ]             │
│                 Lowercase, hyphens only                 │
│                                                         │
│  Display Name*  [Company Style            ]             │
│                                                         │
│  Description    [Internal communications for employees] │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Target Grade Level                                │  │
│  │                                                   │  │
│  │  1 ──────●────────────────────────────────── 18   │  │
│  │          9                                        │  │
│  │                                                   │  │
│  │  Grade 9 = 9th grade / high school freshman       │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  Max Sentence Length   [18      ] words                 │
│  Max Paragraph Length  [4       ] sentences             │
│                                                         │
│  ☑ Avoid jargon (replace with plain language)           │
│  ☑ Prefer active voice                                  │
│                                                         │
│  Icon  [🏢 building ▼]                                  │
│                                                         │
│           [Cancel]  [Create Preset]                     │
└─────────────────────────────────────────────────────────┘
```

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Using Voice Profile target audience: {Audience}"` |
| Debug | `"Loaded preset: {PresetId} with grade {GradeLevel}"` |
| Info | `"Registered custom preset: {PresetId}"` |
| Info | `"Updated custom preset: {PresetId}"` |
| Info | `"Deleted custom preset: {PresetId}"` |
| Warning | `"Failed to load Voice Profile, using default"` |
| Warning | `"Failed to load custom presets"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Invalid preset injection | Low | Validate preset ID format (alphanumeric + hyphens only) |
| Denial of service via presets | Low | Limit custom presets to 50 per workspace |
| Sensitive term replacements | Low | Term replacements stored locally, not synced |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | No Voice Profile configured | Getting target | Returns "general-public" preset (Grade 8) |
| 2 | Voice Profile with "Technical" audience | Getting target | Returns "technical" preset (Grade 12) |
| 3 | "general-public" preset ID | Getting target for preset | Returns Grade 8, MaxSentence 20, AvoidJargon true |
| 4 | "technical" preset ID | Getting target for preset | Returns Grade 12, MaxSentence 30, AvoidJargon false |
| 5 | "executive" preset ID | Getting target for preset | Returns Grade 10, MaxSentence 15 |
| 6 | "international" preset ID | Getting target for preset | Returns Grade 6, MaxSentence 15 |
| 7 | Invalid preset ID | Getting target for preset | Throws ArgumentException |
| 8 | Valid custom preset | Registering preset | Preset added and persisted |
| 9 | Duplicate preset ID | Registering preset | Throws InvalidOperationException |
| 10 | Built-in preset ID | Deleting preset | Throws InvalidOperationException |
| 11 | Custom preset ID | Deleting preset | Preset removed and change persisted |
| 12 | Core tier license | Registering custom preset | Throws LicenseRequiredException |

### 10.2 Validation Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 13 | Text at Grade 14, Target Grade 8 | Validating target | Returns gap of 6, achievable |
| 14 | Text at Grade 16, Target Grade 4 | Validating target | Returns warning about large gap |
| 15 | Text with 40-word average sentences | Validating target | Returns suggestion about splitting |

---

## 11. Test Scenarios

### 11.1 Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.4a")]
public class ReadabilityTargetServiceTests
{
    private readonly ReadabilityTargetService _sut;
    private readonly Mock<IVoiceProfileService> _mockVoiceProfile;
    private readonly Mock<IReadabilityService> _mockReadability;
    private readonly Mock<ISettingsService> _mockSettings;
    private readonly Mock<ILicenseContext> _mockLicense;

    public ReadabilityTargetServiceTests()
    {
        _mockVoiceProfile = new Mock<IVoiceProfileService>();
        _mockReadability = new Mock<IReadabilityService>();
        _mockSettings = new Mock<ISettingsService>();
        _mockLicense = new Mock<ILicenseContext>();
        _mockLicense.Setup(l => l.HasFeature(It.IsAny<string>())).Returns(true);

        _sut = new ReadabilityTargetService(
            _mockVoiceProfile.Object,
            _mockReadability.Object,
            _mockSettings.Object,
            _mockLicense.Object,
            NullLogger<ReadabilityTargetService>.Instance);
    }

    #region Built-in Preset Tests

    [Fact]
    public void GetTargetForPreset_GeneralPublic_ReturnsCorrectValues()
    {
        var result = _sut.GetTargetForPreset("general-public");

        result.TargetFleschKincaidGrade.Should().Be(8);
        result.MaxSentenceWords.Should().Be(20);
        result.SimplifyJargon.Should().BeTrue();
        result.ConvertPassiveToActive.Should().BeTrue();
        result.SourcePresetId.Should().Be("general-public");
    }

    [Fact]
    public void GetTargetForPreset_Technical_PreservesJargon()
    {
        var result = _sut.GetTargetForPreset("technical");

        result.TargetFleschKincaidGrade.Should().Be(12);
        result.MaxSentenceWords.Should().Be(30);
        result.SimplifyJargon.Should().BeFalse();
    }

    [Fact]
    public void GetTargetForPreset_Executive_HasShortSentences()
    {
        var result = _sut.GetTargetForPreset("executive");

        result.TargetFleschKincaidGrade.Should().Be(10);
        result.MaxSentenceWords.Should().Be(15);
    }

    [Fact]
    public void GetTargetForPreset_International_HasLowestGrade()
    {
        var result = _sut.GetTargetForPreset("international");

        result.TargetFleschKincaidGrade.Should().Be(6);
        result.MaxSentenceWords.Should().Be(15);
    }

    [Theory]
    [InlineData("general-public", 8, 20)]
    [InlineData("technical", 12, 30)]
    [InlineData("executive", 10, 15)]
    [InlineData("international", 6, 15)]
    public void GetTargetForPreset_AllBuiltIn_ReturnExpectedValues(
        string presetId, int expectedGrade, int expectedMaxSentence)
    {
        var result = _sut.GetTargetForPreset(presetId);

        result.TargetFleschKincaidGrade.Should().Be(expectedGrade);
        result.MaxSentenceWords.Should().Be(expectedMaxSentence);
    }

    [Fact]
    public void GetTargetForPreset_InvalidId_ThrowsArgumentException()
    {
        var act = () => _sut.GetTargetForPreset("nonexistent");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*not found*");
    }

    #endregion

    #region Voice Profile Integration Tests

    [Fact]
    public async Task GetTargetFromProfileAsync_NoProfile_ReturnsGeneralPublic()
    {
        _mockVoiceProfile.Setup(v => v.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceProfile?)null);

        var result = await _sut.GetTargetFromProfileAsync();

        result.TargetFleschKincaidGrade.Should().Be(8);
        result.SourcePresetId.Should().Be("general-public");
    }

    [Fact]
    public async Task GetTargetFromProfileAsync_WithTechnicalAudience_ReturnsTechnical()
    {
        var profile = new VoiceProfile { TargetAudience = "Technical" };
        _mockVoiceProfile.Setup(v => v.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _sut.GetTargetFromProfileAsync();

        result.TargetFleschKincaidGrade.Should().Be(12);
        result.SourcePresetId.Should().Be("technical");
    }

    [Fact]
    public async Task GetTargetFromProfileAsync_WithCustomGradeLevel_UsesProfileValue()
    {
        var profile = new VoiceProfile { TargetReadingLevel = 7 };
        _mockVoiceProfile.Setup(v => v.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _sut.GetTargetFromProfileAsync();

        result.TargetFleschKincaidGrade.Should().Be(7);
        result.SourcePresetId.Should().BeNull();
    }

    #endregion

    #region Custom Preset Tests

    [Fact]
    public void RegisterPreset_ValidCustomPreset_AddsToCollection()
    {
        var preset = new AudiencePreset
        {
            PresetId = "my-custom",
            Name = "My Custom",
            Description = "Test preset",
            TargetGradeLevel = 9
        };

        _sut.RegisterPreset(preset);

        var result = _sut.GetTargetForPreset("my-custom");
        result.TargetFleschKincaidGrade.Should().Be(9);
    }

    [Fact]
    public void RegisterPreset_DuplicateId_ThrowsInvalidOperationException()
    {
        var preset = new AudiencePreset
        {
            PresetId = "general-public", // Built-in ID
            Name = "Duplicate",
            Description = "Test",
            TargetGradeLevel = 8
        };

        var act = () => _sut.RegisterPreset(preset);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already exists*");
    }

    [Fact]
    public void DeletePreset_BuiltInPreset_ThrowsInvalidOperationException()
    {
        var act = () => _sut.DeletePreset("general-public");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*built-in*");
    }

    [Fact]
    public void DeletePreset_CustomPreset_RemovesFromCollection()
    {
        var preset = new AudiencePreset
        {
            PresetId = "to-delete",
            Name = "To Delete",
            Description = "Test",
            TargetGradeLevel = 9
        };
        _sut.RegisterPreset(preset);

        _sut.DeletePreset("to-delete");

        var act = () => _sut.GetTargetForPreset("to-delete");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateTarget_SmallGap_ReturnsAchievable()
    {
        _mockReadability.Setup(r => r.Analyze(It.IsAny<string>()))
            .Returns(new ReadabilityMetrics { FleschKincaidGradeLevel = 10 });

        var target = new ReadabilityTarget
        {
            TargetFleschKincaidGrade = 8,
            MaxSentenceWords = 20,
            MaxParagraphSentences = 5,
            SimplifyJargon = true,
            ConvertPassiveToActive = true
        };

        var result = _sut.ValidateTarget("test text", target);

        result.IsAchievable.Should().BeTrue();
        result.GradeLevelGap.Should().Be(2);
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTarget_LargeGap_ReturnsWarning()
    {
        _mockReadability.Setup(r => r.Analyze(It.IsAny<string>()))
            .Returns(new ReadabilityMetrics { FleschKincaidGradeLevel = 18 });

        var target = new ReadabilityTarget
        {
            TargetFleschKincaidGrade = 6,
            MaxSentenceWords = 15,
            MaxParagraphSentences = 4,
            SimplifyJargon = true,
            ConvertPassiveToActive = true
        };

        var result = _sut.ValidateTarget("complex text", target);

        result.GradeLevelGap.Should().Be(12);
        result.Warnings.Should().Contain(w => w.Contains("Large gap"));
    }

    #endregion
}
```

---

## 12. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `IReadabilityTargetService.cs` interface | [ ] |
| 2 | `ReadabilityTargetService.cs` implementation | [ ] |
| 3 | `AudiencePreset.cs` record | [ ] |
| 4 | `ReadabilityTarget.cs` record | [ ] |
| 5 | `TargetValidationResult.cs` record | [ ] |
| 6 | `BuiltInPresets.cs` static class | [ ] |
| 7 | `FeatureFlags.Agents.Simplifier` constant | [ ] |
| 8 | Custom preset persistence | [ ] |
| 9 | Voice Profile integration | [ ] |
| 10 | `ReadabilityTargetServiceTests.cs` | [ ] |
| 11 | DI registration in AgentsModule | [ ] |

---

## 13. Verification Commands

```bash
# Run all v0.7.4a tests
dotnet test --filter "Version=v0.7.4a" --logger "console;verbosity=detailed"

# Run only ReadabilityTargetService tests
dotnet test --filter "FullyQualifiedName~ReadabilityTargetServiceTests"

# Run with coverage
dotnet test --filter "Version=v0.7.4a" --collect:"XPlat Code Coverage"

# Manual verification:
# a) Open Simplification panel
# b) Verify all 4 built-in presets appear in dropdown
# c) Select each preset and verify correct grade target shown
# d) Create a custom preset and verify it persists after restart
# e) Verify Core tier shows upgrade prompt for custom presets
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
