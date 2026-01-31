# LCS-CL-034d: Profile Selector UI

**Version:** v0.3.4d  
**Release Date:** 2026-01-31  
**Status:** ✅ Complete  
**Specification:** [LCS-DES-034d](../../../specs/v0.3.x/v0.3.4/LCS-DES-034d.md)

---

## Summary

Implements the Profile Selector widget for the Status Bar, enabling writers to view and switch active voice profiles directly from the application's status bar. The widget displays the current profile name with rich tooltips and provides a context menu for profile selection.

---

## Added

### Abstractions (`Lexichord.Abstractions`)

- **`IProfileSelectorViewModel`** interface: Contract for the profile selector widget:
    - `CurrentProfileName`: Display name of the active profile
    - `ProfileTooltip`: Rich tooltip with profile configuration details
    - `HasActiveProfile`: Whether a profile is currently active
    - `AvailableProfiles`: List of profiles for context menu
    - `SelectProfileCommand`: Command to switch profiles
    - `RefreshAsync()`: Method to refresh profile data

### ViewModels (`Lexichord.Modules.StatusBar`)

- **`ProfileSelectorViewModel`**: Implements `IProfileSelectorViewModel`:
    - Auto-initializes profile data on construction
    - Generates rich tooltips with grade level, passive voice, and weak word settings
    - Handles profile switching with automatic UI refresh
    - Graceful error handling with logging

### Views (`Lexichord.Modules.StatusBar`)

- **`ProfileSelectorWidget.axaml`**: Status bar widget with:
    - Profile name display with context menu
    - Hover effects for interactive feedback
    - Rich tooltip with profile configuration
    - Context menu for profile selection

### DI Registration

- Registered `ProfileSelectorViewModel` as transient in `StatusBarModule.cs`
- Registered `ProfileSelectorWidget` as transient view

### Integration

- Updated `StatusBarViewModel` to include `ProfileSelector` property
- Integrated `ProfileSelectorWidget` into `StatusBarView.axaml`

---

## Technical Notes

### Auto-Initialization Pattern

The ViewModel uses fire-and-forget initialization in the constructor:

```csharp
public ProfileSelectorViewModel(...)
{
    // ... validation ...
    _ = RefreshAsync(); // Fire-and-forget with error handling
}
```

This ensures the widget displays profile data immediately upon loading.

### Rich Tooltip Format

Tooltips display profile configuration in a structured format:

```
Voice Profile: Technical
Grade Level: 12.0 ± 2.0
Passive Voice: Allowed (max 20%)
Flag Adverbs: Yes
Flag Weasel Words: Yes
```

### Integration Test Stub

Added `StubVoiceProfileService` to `GoldenSkeletonTests.cs` to satisfy DI requirements for integration tests.

---

## Testing

### Unit Tests (`ProfileSelectorViewModelTests.cs`)

| Test                                                        | Description                    |
| :---------------------------------------------------------- | :----------------------------- |
| `Constructor_ThrowsArgumentNullException_WhenServiceIsNull` | Null guard validation          |
| `Constructor_ThrowsArgumentNullException_WhenLoggerIsNull`  | Null guard validation          |
| `Constructor_InitializesProfileData_Automatically`          | Auto-refresh on construction   |
| `CurrentProfileName_ReturnsProfileName_AfterRefresh`        | Profile name binding           |
| `CurrentProfileName_UpdatesAfterProfileChange`              | Profile switch workflow        |
| `HasActiveProfile_ReturnsTrue_AfterAutoInit`                | Active profile flag            |
| `ProfileTooltip_ContainsProfileName_AfterRefresh`           | Tooltip includes name          |
| `ProfileTooltip_ContainsGradeLevel_AfterRefresh`            | Tooltip includes grade level   |
| `ProfileTooltip_ContainsPassiveVoiceInfo_AfterRefresh`      | Tooltip includes passive voice |
| `AvailableProfiles_ContainsAllProfiles_AfterAutoInit`       | Profile list population        |
| `SelectProfileCommand_SetsActiveProfile`                    | Profile selection              |
| `SelectProfileCommand_RefreshesAfterSelection`              | Post-selection refresh         |
| `SelectProfileCommand_HandlesServiceError_Gracefully`       | Error handling                 |
| `RefreshAsync_FetchesFromService`                           | Service interaction            |
| `RefreshAsync_HandlesServiceError_Gracefully`               | Error handling                 |

**Test Count:** 15 unit tests, 3 integration tests  
**Pass Rate:** 100%

---

## Dependencies

| Component               | Version |
| :---------------------- | :------ |
| `CommunityToolkit.Mvvm` | 8.2.2   |
| `Avalonia`              | 11.2.7  |
| `IVoiceProfileService`  | v0.3.4a |

---

## Files Changed

| File                                                                 | Change                            |
| :------------------------------------------------------------------- | :-------------------------------- |
| `Lexichord.Abstractions/Contracts/VoiceProfiles.cs`                  | Added `IProfileSelectorViewModel` |
| `Lexichord.Modules.StatusBar/ViewModels/ProfileSelectorViewModel.cs` | **NEW**                           |
| `Lexichord.Modules.StatusBar/ViewModels/StatusBarViewModel.cs`       | Added `ProfileSelector` property  |
| `Lexichord.Modules.StatusBar/Views/ProfileSelectorWidget.axaml`      | **NEW**                           |
| `Lexichord.Modules.StatusBar/Views/ProfileSelectorWidget.axaml.cs`   | **NEW**                           |
| `Lexichord.Modules.StatusBar/Views/StatusBarView.axaml`              | Integrated widget                 |
| `Lexichord.Modules.StatusBar/StatusBarModule.cs`                     | Registered VM and View            |
| `Lexichord.Tests.Unit/.../ProfileSelectorViewModelTests.cs`          | **NEW**                           |
| `Lexichord.Tests.Integration/GoldenSkeletonTests.cs`                 | Added stub service                |
