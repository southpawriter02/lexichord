# LCS-CL-003d: Configuration Service

## Version Information

| Field        | Value                 |
| :----------- | :-------------------- |
| Version      | v0.0.3d               |
| Feature Name | Configuration Service |
| Release Date | 2026-01-28            |
| Status       | ✅ Complete           |

---

## Summary

Implemented the Microsoft.Extensions.Configuration infrastructure with multi-source
configuration loading and the Options pattern for strongly-typed access.

---

## What's New

### Configuration Infrastructure

- **Multi-source loading** with proper precedence:
    1. `appsettings.json` (base)
    2. `appsettings.{Environment}.json` (environment overrides)
    3. Environment variables (`LEXICHORD_` prefix)
    4. Command-line arguments (highest priority)

- **`BuildConfiguration(args)`** method in `HostServices` for centralized config building

- **CLI argument mapping**:
    - `--debug-mode` / `-d` → Enable debug mode
    - `--log-level` / `-l` → Set Serilog minimum level
    - `--data-path` → Override data directory
    - `--environment` / `-e` → Set environment
    - `--show-devtools` → Enable Avalonia DevTools

### Options Pattern

| Record               | Section        | Purpose                                      |
| :------------------- | :------------- | :------------------------------------------- |
| `LexichordOptions`   | `Lexichord`    | App name, environment, data path, debug mode |
| `DebugOptions`       | `Debug`        | DevTools, performance logging, network delay |
| `FeatureFlagOptions` | `FeatureFlags` | Experimental features toggle                 |

### Configuration Files

- `appsettings.json` — Production defaults
- `appsettings.Development.json` — Debug settings enabled

---

## Files Created

| File                                                           | Purpose               |
| :------------------------------------------------------------- | :-------------------- |
| `src/Lexichord.Host/appsettings.json`                          | Base configuration    |
| `src/Lexichord.Host/appsettings.Development.json`              | Development overrides |
| `src/Lexichord.Abstractions/Contracts/ConfigurationOptions.cs` | Options records       |
| `tests/Lexichord.Tests.Unit/Host/ConfigurationOptionsTests.cs` | 10 unit tests         |

---

## Files Modified

| File                                                     | Change                                             |
| :------------------------------------------------------- | :------------------------------------------------- |
| `src/Lexichord.Host/Lexichord.Host.csproj`               | Added 5 configuration packages, copy rules         |
| `src/Lexichord.Host/HostServices.cs`                     | Added `BuildConfiguration()`, options registration |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj` | Added configuration packages                       |

---

## NuGet Packages Added

```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.CommandLine" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="9.0.0" />
```

---

## Testing

**10 unit tests** covering:

- JSON file loading
- Environment override precedence
- CLI argument override precedence
- Environment variable prefix handling
- Options binding from configuration
- Default values for all option records
- Custom data path resolution

---

## Usage Examples

### Injecting Options

```csharp
public class MyService(IOptions<LexichordOptions> options)
{
    public void DoWork()
    {
        if (options.Value.DebugMode)
            _logger.LogDebug("Verbose output enabled");
    }
}
```

### Running with CLI Arguments

```bash
# Enable debug mode
dotnet run --project src/Lexichord.Host -- --debug-mode

# Set log level to Debug
dotnet run --project src/Lexichord.Host -- --log-level Debug

# Run in Development environment
LEXICHORD_ENVIRONMENT=Development dotnet run --project src/Lexichord.Host
```

---

## Related Documents

- **Design Specification**: [LCS-DES-003d.md](../specs/v0.0.x/v0.0.3/LCS-DES-003d.md)
- **Parent Version**: [LCS-DES-003-INDEX.md](../specs/v0.0.x/v0.0.3/LCS-DES-003-INDEX.md)
- **Scope Breakdown**: [LCS-SBD-003.md](../specs/v0.0.x/v0.0.3/LCS-SBD-003.md)
