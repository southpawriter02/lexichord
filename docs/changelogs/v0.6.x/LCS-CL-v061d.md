# LCS-CL-061d: Detailed Changelog — API Key Management UI

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.1d                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Presentation / Module                        |
| **Parent**   | [v0.6.1 Changelog](../CHANGELOG.md#v061)     |
| **Spec**     | [LCS-DES-061d](../../specs/v0.6.x/v0.6.1/LCS-DES-v0.6.1d.md) |

---

## Summary

This release implements the LLM Settings UI for the Settings dialog, enabling users to configure API keys for LLM providers, test connections, and select a default provider. It builds upon v0.6.1c (Provider Registry) to provide a user-facing configuration interface following the established `ISettingsPage` pattern from v0.1.6a.

---

## New Features

### 1. Connection Status Enum

Added `ConnectionStatus` enum for representing provider connection states:

```csharp
public enum ConnectionStatus
{
    Unknown = 0,    // Not yet tested
    Checking = 1,   // Test in progress
    Connected = 2,  // Successfully connected
    Failed = 3      // Connection failed
}
```

**File:** `src/Lexichord.Modules.LLM/Presentation/ConnectionStatus.cs`

### 2. Provider Configuration ViewModel

Added `ProviderConfigViewModel` for per-provider configuration state:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Name` | `string` | Provider identifier (e.g., "openai") |
| `DisplayName` | `string` | Human-readable name (e.g., "OpenAI") |
| `ApiKeyInput` | `string` | User input for new API key |
| `ApiKeyDisplay` | `string` | Masked display (e.g., `sk-1••••••••••••ghij`) |
| `IsConfigured` | `bool` | Whether API key is stored |
| `IsDefault` | `bool` | Whether this is the default provider |
| `Status` | `ConnectionStatus` | Connection test result |
| `StatusMessage` | `string?` | Status details (e.g., "Connected (150ms)") |
| `SupportedModels` | `IReadOnlyList<string>` | Available models |
| `SelectedModel` | `string?` | Currently selected model |
| `SupportsStreaming` | `bool` | Whether provider supports streaming |

**Key Methods:**

| Method | Return | Description |
| ------ | ------ | ----------- |
| `LoadApiKeyAsync()` | `Task` | Load and display masked key from vault |
| `SaveApiKeyAsync()` | `Task<bool>` | Store API key in vault |
| `DeleteApiKeyAsync()` | `Task<bool>` | Remove API key from vault |
| `UpdateConnectionStatus(status, message)` | `void` | Update status display |
| `MaskApiKey(key)` (static) | `string` | Mask API key for display |

**Computed Properties:**

| Property | Description |
| -------- | ----------- |
| `CanSaveApiKey` | True when input has content and not busy |
| `CanDeleteApiKey` | True when configured and not busy |
| `CanTestConnection` | True when configured and not busy |

**API Key Masking:**

The `MaskApiKey` method displays first 4 characters + 12 bullet characters + last 4 characters:
- `sk-1234567890abcdefghij` → `sk-1••••••••••••ghij`
- Keys shorter than 8 characters show prefix + mask only
- Null or empty keys display "Invalid key"

**Vault Key Pattern:** `{providerName}:api-key` (e.g., `openai:api-key`)

**File:** `src/Lexichord.Modules.LLM/Presentation/ViewModels/ProviderConfigViewModel.cs`

### 3. LLM Settings ViewModel

Added `LLMSettingsViewModel` as the main settings page ViewModel:

**Dependencies:**
- `ILLMProviderRegistry` — Provider list and default management
- `ISecureVault` — API key storage
- `ILicenseContext` — License tier checking
- `ILogger<LLMSettingsViewModel>` — Logging
- `ILogger<ProviderConfigViewModel>` — Child ViewModel logging

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Providers` | `ObservableCollection<ProviderConfigViewModel>` | All available providers |
| `SelectedProvider` | `ProviderConfigViewModel?` | Currently selected for configuration |
| `IsBusy` | `bool` | Whether an operation is in progress |
| `ErrorMessage` | `string?` | Error message to display |
| `DefaultProviderName` | `string?` | Name of default provider |

**Computed Properties:**

| Property | Description |
| -------- | ----------- |
| `CurrentTier` | Current license tier |
| `CanConfigure` | True if WriterPro tier or higher |
| `CanTestConnection` | True when provider is configured and not busy |
| `CanSetAsDefault` | True when provider is configured, not default, can configure, not busy |
| `CanSaveApiKey` | True when input present, can configure, not busy |
| `CanDeleteApiKey` | True when configured, can configure, not busy |

**Commands:**

| Command | Description |
| ------- | ----------- |
| `TestConnectionCommand` | Tests provider with minimal chat request |
| `SaveApiKeyCommand` | Saves API key to vault via ProviderConfigViewModel |
| `DeleteApiKeyCommand` | Deletes API key from vault via ProviderConfigViewModel |
| `SetAsDefaultCommand` | Sets selected provider as default |

**License Gating:**

```csharp
public const LicenseTier RequiredTierForConfiguration = LicenseTier.WriterPro;
```

- **View:** Available to all tiers (Core+)
- **Configure:** Requires WriterPro+ tier

**File:** `src/Lexichord.Modules.LLM/Presentation/ViewModels/LLMSettingsViewModel.cs`

### 4. LLM Settings Page

Added `LLMSettingsPage` implementing `ISettingsPage`:

```csharp
public class LLMSettingsPage : ISettingsPage
{
    public string CategoryId => "llm.providers";
    public string DisplayName => "LLM Providers";
    public string? ParentCategoryId => null;
    public string? Icon => "Robot";
    public int SortOrder => 75;
    public LicenseTier RequiredTier => LicenseTier.Core;
    public IReadOnlyList<string> SearchKeywords { get; }
}
```

**Search Keywords:** "llm", "ai", "provider", "openai", "anthropic", "gpt", "claude", "api key", "chat", "model"

**File:** `src/Lexichord.Modules.LLM/Settings/LLMSettingsPage.cs`

### 5. Connection Status Badge Control

Added Avalonia `ConnectionStatusBadge` control for visual status display:

**Styled Properties:**
- `Status` (`ConnectionStatus`) — The connection status
- `StatusMessage` (`string?`) — Optional detailed message

**Visual States:**
| Status | Color | Indicator |
| ------ | ----- | --------- |
| Unknown | Gray | Static dot |
| Checking | Blue | Animated spinner |
| Connected | Green | Static dot |
| Failed | Red | Static dot |

**Files:**
- `src/Lexichord.Modules.LLM/Presentation/Views/ConnectionStatusBadge.axaml`
- `src/Lexichord.Modules.LLM/Presentation/Views/ConnectionStatusBadge.axaml.cs`

### 6. LLM Settings View

Added Avalonia `LLMSettingsView` for the settings page:

**Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│ LLM Providers                                                   │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌────────────────────────────────────────┐│
│  │ Providers       │  │ OpenAI Configuration                   ││
│  │                 │  │                                        ││
│  │ ● OpenAI    ★   │  │ API Key                                ││
│  │   Anthropic     │  │ ┌────────────────────────────────────┐ ││
│  │                 │  │ │ sk-1••••••••••••abcd               │ ││
│  │                 │  │ └────────────────────────────────────┘ ││
│  │                 │  │ [Save API Key]  [Delete API Key]       ││
│  │                 │  │                                        ││
│  │                 │  │ [Test Connection]  [Set as Default]    ││
│  │                 │  │                                        ││
│  │                 │  │ Status: ● Connected (150ms)            ││
│  └─────────────────┘  └────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

**Features:**
- Provider list with selection
- Default indicator (★) for current default
- Configuration status indicator (● configured, ○ unconfigured)
- Password input for API key entry
- Masked display of stored key
- Connection test with latency display
- License tier gating notice for Core users

**Files:**
- `src/Lexichord.Modules.LLM/Presentation/Views/LLMSettingsView.axaml`
- `src/Lexichord.Modules.LLM/Presentation/Views/LLMSettingsView.axaml.cs`

### 7. Structured Logging Events (LLM Module)

Added settings page logging events (1500-1510 range):

| Event ID | Level       | Description                              |
| -------- | ----------- | ---------------------------------------- |
| 1500     | Information | Settings page loaded                     |
| 1501     | Information | API key saved for provider               |
| 1502     | Warning     | API key deleted for provider             |
| 1503     | Debug       | Connection test started                  |
| 1504     | Information | Connection test succeeded                |
| 1505     | Warning     | Connection test failed                   |
| 1506     | Information | Default provider changed via settings    |
| 1507     | Debug       | Provider selected in settings            |
| 1508     | Debug       | Provider configuration loaded            |
| 1509     | Warning     | API key save failed                      |
| 1510     | Warning     | API key delete failed                    |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

---

## Files Changed

### Created (9 files)

| File | Lines | Description |
| ---- | ----- | ----------- |
| `Modules.LLM/Presentation/ConnectionStatus.cs` | ~55 | Connection status enum |
| `Modules.LLM/Presentation/ViewModels/ProviderConfigViewModel.cs` | ~320 | Provider configuration ViewModel |
| `Modules.LLM/Presentation/ViewModels/LLMSettingsViewModel.cs` | ~590 | Settings page ViewModel |
| `Modules.LLM/Settings/LLMSettingsPage.cs` | ~110 | ISettingsPage implementation |
| `Modules.LLM/Presentation/Views/ConnectionStatusBadge.axaml` | ~80 | Status badge XAML |
| `Modules.LLM/Presentation/Views/ConnectionStatusBadge.axaml.cs` | ~150 | Status badge code-behind |
| `Modules.LLM/Presentation/Views/LLMSettingsView.axaml` | ~200 | Settings view XAML |
| `Modules.LLM/Presentation/Views/LLMSettingsView.axaml.cs` | ~70 | Settings view code-behind |

### Modified (3 files)

| File | Change |
| ---- | ------ |
| `Modules.LLM/LLMModule.cs` | Added DI registrations for ViewModels and settings page |
| `Modules.LLM/Logging/LLMLogEvents.cs` | Added events 1500-1510 |
| `Modules.LLM/Lexichord.Modules.LLM.csproj` | Added CommunityToolkit.Mvvm 8.4.0, Avalonia 11.2.3 |

---

## Test Coverage

### New Test Files

| File | Tests | Description |
| ---- | ----- | ----------- |
| `ConnectionStatusTests.cs` | 18 | Enum values, parsing, casting |
| `ProviderConfigViewModelTests.cs` | 48 | Constructor, masking, async operations, computed properties |
| `LLMSettingsViewModelTests.cs` | 26 | Constructor, loading, commands, license gating |
| `LLMSettingsPageTests.cs` | 16 | Properties, interface implementation |

### Test Summary

- **New Tests:** 108
- **All Tests Pass:** ✅

---

## Dependencies

### Internal Dependencies

| Dependency | Purpose |
| ---------- | ------- |
| `Lexichord.Abstractions` | LLM contracts, `ISettingsPage`, `ILicenseContext` |
| `ILLMProviderRegistry` | Provider enumeration and default management |
| `ISecureVault` | API key storage and retrieval |
| `IChatCompletionService` | Connection testing |

### External Dependencies

| Package | Version | Purpose |
| ------- | ------- | ------- |
| CommunityToolkit.Mvvm | 8.4.0 | ObservableObject, RelayCommand |
| Avalonia | 11.2.3 | UI framework |

---

## Usage Example

```csharp
// DI registration (in LLMModule.RegisterServices)
services.AddTransient<ProviderConfigViewModel>();
services.AddTransient<LLMSettingsViewModel>();
services.AddSingleton<ISettingsPage, LLMSettingsPage>();

// Opening settings will automatically show LLM Providers page
// based on ISettingsPage discovery

// Manual ViewModel usage (for testing)
var vm = new LLMSettingsViewModel(
    registry,
    vault,
    licenseContext,
    logger,
    providerConfigLogger);

await vm.LoadProvidersAsync();

// Select a provider
vm.SelectedProvider = vm.Providers.First(p => p.Name == "openai");

// Enter API key
vm.SelectedProvider.ApiKeyInput = "sk-your-api-key";

// Save the key
await vm.SaveApiKeyCommand.ExecuteAsync(null);

// Test connection
await vm.TestConnectionCommand.ExecuteAsync(null);

if (vm.SelectedProvider.Status == ConnectionStatus.Connected)
{
    // Set as default
    vm.SetAsDefaultCommand.Execute(null);
}
```

---

## Migration Notes

### From v0.6.1c

No breaking changes. The settings UI is additive functionality. Existing code using `ILLMProviderRegistry`, provider resolution, and other v0.6.1c services continues to work unchanged.

### Settings Page Discovery

The `LLMSettingsPage` is automatically discovered by the settings service through `ISettingsPage` registration. No manual configuration is required.

---

## Design Decisions

### 1. ViewModel Pattern

Uses CommunityToolkit.Mvvm for MVVM implementation:
- `[ObservableProperty]` for automatic property notification
- `[RelayCommand]` for command generation
- Follows established patterns from `AccountSettingsViewModel`

### 2. License Gating Strategy

**View vs Configure separation:**
- Settings page visible at Core tier (users can see what LLM features exist)
- Configuration requires WriterPro+ (prevents wasted API keys on limited accounts)

### 3. API Key Masking

Shows first 4 + last 4 characters with 12 mask characters:
- Provides enough context to identify which key is stored
- Prevents accidental exposure in screenshots
- Consistent with industry practices (Stripe, OpenAI dashboard)

### 4. Connection Testing

Uses a minimal chat request for testing:
```csharp
var testRequest = new ChatRequest(
    Messages: [new ChatMessage(ChatRole.User, "Hello")],
    Options: ChatOptions.Default with { MaxTokens = 1 }
);
```
- Minimal token usage (approximately 2-3 tokens total)
- Quick response time
- Validates both API key and connectivity

### 5. Transient ViewModel Registration

ViewModels registered as transient rather than scoped:
- Each settings page instance gets fresh state
- Prevents stale data between settings dialog sessions
- Follows established pattern from other settings pages

---

## Security Considerations

1. **API Key Storage:** Keys stored via `ISecureVault` using operating system secure storage
2. **Key Display:** Always masked in UI, never logged
3. **Input Clearing:** Input field cleared immediately after successful save
4. **No Key Echo:** Test connection results don't include key data

---

## Known Limitations

1. **No Model Selection UI:** Model dropdown is present but not fully integrated with the options resolver
2. **Single Key Per Provider:** Cannot store multiple API keys for the same provider
3. **No Key Rotation:** No built-in key rotation or expiry tracking
4. **Synchronous Default Change:** `SetAsDefaultCommand` uses synchronous settings access

---

## Future Work

- v0.6.1e: Provider implementations (OpenAI, Anthropic HTTP clients)
- v0.6.1f: Streaming support with SSE parsing
- v0.6.1g: Rate limiting and retry policies
- Future: Model-specific configuration, usage tracking, key rotation
