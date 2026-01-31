# v0.3.1d: License Gating UI Changelog

**Version**: v0.3.1d  
**Date**: 2026-01-31  
**Scope**: UI components and ViewModel integration for license-based feature gating

## Summary

Introduces the license gating User Interface layer, completing the v0.3.1 Fuzzy Engine feature set. This includes reusable UI controls (`FeatureGate`, `LicenseRestrictionBanner`, `UpgradePromptDialog`) and their integration into the Lexicon View for displaying fuzzy matching status with appropriate upgrade prompts for non-Writer Pro users.

## Added

### UI Components (`Lexichord.Modules.Style.Views`)

- **`FeatureGate`**: Content wrapper that shows/hides child content based on feature enablement
    - `FeatureCode` attached property for declarative gating
    - `FallbackContent` for upgrade prompt when feature is disabled
    - Static initialization pattern for `ILicenseContext` access
- **`LicenseRestrictionBanner`**: Dismissible banner for upgrade prompts
    - `FeatureCode` and `Message` properties for customization
    - "Learn More" and "Upgrade" buttons with command bindings
    - Gradient background with warning icon
- **`UpgradePromptDialog`**: Modal dialog for tier comparison
    - Feature comparison grid (Free vs Writer Pro)
    - "Maybe Later" and "Upgrade Now" buttons
    - Static initialization for license context access

### Constants (`Lexichord.Abstractions.Constants`)

- **`FeatureCodes`**: Static class with feature gate constants
    - `FuzzyMatching`, `FuzzyScanning`, `FuzzyThresholdConfig`
    - `FuzzyFeatures` array for bulk checks

### Converters (`Lexichord.Host.Converters`)

- **`LicenseGateValueConverter`**: XAML value converter for license-based visibility
    - `Convert(featureCode)` returns Visibility based on feature enablement
    - Registered in `App.axaml` application resources

### ViewModel Updates (`Lexichord.Modules.Style.ViewModels`)

- **`StyleTermRowViewModel`**: Added fuzzy properties
    - `FuzzyEnabled` (bool): Exposes term's fuzzy match status
    - `FuzzyThreshold` (double): Exposes fuzzy match threshold (0.0-1.0)
    - `FuzzyStatusText` (string): "80%" or "Off" display value
- **`LexiconViewModel`**: Added license gating properties
    - `IsFuzzyEnabled`: Checks `FeatureCodes.FuzzyMatching` enablement
    - `ShowFuzzyUpgradeBanner`: Inverse of `IsFuzzyEnabled` for banner visibility

### View Integration (`Lexichord.Modules.Style.Views`)

- **`LexiconView.axaml`**: Updated with license gating
    - `LicenseRestrictionBanner` for fuzzy matching upgrade prompt
    - "Fuzzy" column in DataGrid showing `FuzzyStatusText`

## Modified

### Module Initialization (`Lexichord.Modules.Style`)

- **`StyleModule.InitializeAsync`**: Added UI control initialization
    - Calls `FeatureGate.Initialize(licenseContext)`
    - Calls `UpgradePromptDialog.Initialize(licenseContext)`

### Host Initialization (`Lexichord.Host`)

- **`App.OnFrameworkInitializationCompleted`**: Added converter initialization
    - `LicenseGateValueConverter.Initialize(serviceProvider)`

## Technical Notes

### Circular Dependency Resolution

The initial design had `Lexichord.Modules.Style` UI controls requiring direct access to host services, creating a circular dependency. This was resolved using:

1. **Static Initialization Pattern**: UI controls expose `static void Initialize(ILicenseContext)` methods
2. **Module-Level Initialization**: `StyleModule.InitializeAsync` calls these initializers after DI is available
3. **Clean Separation**: Host only initializes converters, modules initialize their own UI controls

## Verification

- Build: Success with 0 warnings, 0 errors
- All phases completed: Abstractions, UI Controls, Integration
