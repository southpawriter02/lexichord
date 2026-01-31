# LCS-CL-034a: Voice Profile Definition

**Version:** v0.3.4a  
**Release Date:** 2026-01-30  
**Status:** ✅ Complete  
**Specification:** [LCS-DES-034a](../../../specs/v0.3.x/v0.3.4/LCS-DES-034a.md)

---

## Summary

Implements the Voice Profile Definition sub-part of the Writing Coach feature. Voice Profiles enable context-aware style enforcement by allowing writers to select from 5 built-in profiles (Technical, Marketing, Academic, Narrative, Casual) tailored to different content types. Custom profile creation requires a Teams+ license.

---

## Added

### Abstractions (`Lexichord.Abstractions`)

- **`VoiceProfile`** record with:
    - `Id`, `Name`, `Description` — profile identity
    - `TargetGradeLevel`, `GradeLevelTolerance` — readability targets
    - `MaxSentenceLength` — sentence length constraint
    - `AllowPassiveVoice`, `MaxPassiveVoicePercentage` — passive voice rules
    - `FlagAdverbs`, `FlagWeaselWords` — style flags
    - `ForbiddenCategories` — terminology categories to forbid
    - `IsBuiltIn`, `SortOrder` — profile metadata
    - `Validate()` method for constraint validation

- **`IVoiceProfileService`** interface defining:
    - `GetAllProfilesAsync()` — all profiles (built-in + custom)
    - `GetProfileAsync()`, `GetProfileByNameAsync()` — profile retrieval
    - `GetActiveProfileAsync()` — current active profile
    - `SetActiveProfileAsync()` — activate a profile
    - `CreateProfileAsync()`, `UpdateProfileAsync()`, `DeleteProfileAsync()` — CRUD
    - `ResetToDefaultAsync()` — reset to Technical profile

- **`IVoiceProfileRepository`** interface for custom profile persistence

- **`ProfileChangedEvent`** MediatR notification for reactive UI updates

### Services (`Lexichord.Modules.Style`)

- **`BuiltInProfiles`** static class with 5 predefined profiles:

| Profile   | Grade Level | Max Sentence | Passive Voice | Adverbs | Weasel Words |
| --------- | ----------- | ------------ | ------------- | ------- | ------------ |
| Technical | 11.0        | 20           | ❌            | ✓ Flag  | ✓ Flag       |
| Marketing | 9.0         | 25           | ✓ 20%         | ✗       | ✓ Flag       |
| Academic  | 13.0        | 30           | ✓ 30%         | ✗       | ✗            |
| Narrative | 9.0         | 35           | ✓ 15%         | ✗       | ✗            |
| Casual    | 7.0         | 20           | ✓ 25%         | ✗       | ✗            |

- **`VoiceProfileRepository`** Dapper-based implementation with:
    - PostgreSQL persistence for custom profiles
    - JSON serialization for `ForbiddenCategories`
    - CRUD operations for custom profile management

- **`VoiceProfileService`** implementation with:
    - In-memory caching for profile list and active profile
    - License gating via `ILicenseContext.IsFeatureEnabled(FeatureCodes.CustomProfiles)`
    - `ProfileChangedEvent` publishing via MediatR
    - Default profile initialization (Technical)

### Unit Tests (`Lexichord.Tests.Unit`)

- **`VoiceProfileTests.cs`** with 20 test cases:
    - Validation success/failure scenarios
    - Constraint boundary testing
    - Record equality and default values

- **`VoiceProfileServiceTests.cs`** with 24 test cases:
    - Constructor null checks
    - CRUD operations with license gating
    - Caching behavior verification
    - Event publishing verification
    - Active profile management

- **`BuiltInProfilesTests.cs`** with 30 test cases:
    - Profile structure validation
    - Individual profile constraint verification
    - Sort order and uniqueness checks

---

## Technical Notes

### License Gating

Custom profile creation requires `FeatureCodes.CustomProfiles` ("Style.CustomProfiles") feature enabled via Teams+ license. Built-in profiles are available to all users.

### Caching Strategy

- Profiles are cached on first access
- Cache is invalidated on create/update/delete operations
- Active profile persisted in memory (database persistence deferred)

### Default Profile

Technical profile is used as the default when no profile is explicitly selected. It enforces:

- Direct, precise writing
- No passive voice
- Adverbs and weasel words flagged

---

## Dependencies

| Interface         | Version | Purpose                            |
| ----------------- | ------- | ---------------------------------- |
| `ILicenseContext` | v0.1.6c | Feature gating for custom profiles |
| `IMediator`       | v0.0.4  | Event publishing                   |
| `Dapper`          | v2.1.x  | Database persistence               |

---

## Changed Files

| File                                                                     | Change                    |
| ------------------------------------------------------------------------ | ------------------------- |
| `src/Lexichord.Abstractions/Contracts/VoiceProfiles.cs`                  | **NEW**                   |
| `src/Lexichord.Abstractions/Events/StyleDomainEvents.cs`                 | Added ProfileChangedEvent |
| `src/Lexichord.Modules.Style/Services/BuiltInProfiles.cs`                | **NEW**                   |
| `src/Lexichord.Modules.Style/Data/VoiceProfileRepository.cs`             | **NEW**                   |
| `src/Lexichord.Modules.Style/Services/VoiceProfileService.cs`            | **NEW**                   |
| `src/Lexichord.Modules.Style/StyleModule.cs`                             | Added DI registration     |
| `tests/Lexichord.Tests.Unit/Abstractions/Contracts/VoiceProfileTests.cs` | **NEW**                   |
| `tests/Lexichord.Tests.Unit/Modules/Style/VoiceProfileServiceTests.cs`   | **NEW**                   |
| `tests/Lexichord.Tests.Unit/Modules/Style/BuiltInProfilesTests.cs`       | **NEW**                   |

---

## Verification

```bash
# Build verification
dotnet build
# Result: Build succeeded, 0 warnings, 0 errors

# Feature tests
dotnet test --filter "Feature=v0.3.4a"
# Result: 74 passed, 0 failed

# Full test suite
dotnet test tests/Lexichord.Tests.Unit/
# Result: All tests passing
```

---

## What This Enables

- **v0.3.4b (Profile UI):** Profile selector dropdown in the status bar
- **v0.3.4c (Profile Application):** Auto-apply profile constraints during analysis
- **v0.3.4d (Custom Profile Editor):** CRUD UI for custom profiles (Teams+)
