# LDS-01: Feature Design Specification — v0.3.2b Term Editor Dialog

## 1. Metadata & Categorization

| Field                | Value                       | Description                                      |
| :------------------- | :-------------------------- | :----------------------------------------------- |
| **Feature ID**       | `STY-032b`                  | Style Module - Term Editor Dialog                |
| **Feature Name**     | Term Editor Dialog          | Modal dialog for creating and editing terms.     |
| **Target Version**   | `v0.3.2b`                   | Sub-part of v0.3.2 Dictionary Manager.           |
| **Module Scope**     | `Lexichord.Modules.Style`   | Style governance module.                         |
| **Swimlane**         | `Governance`                | Style & Terminology Enforcement.                 |
| **License Tier**     | `Writer Pro`                | Premium feature (Core users see upgrade prompt). |
| **Feature Gate Key** | `Feature.DictionaryManager` | Key used in `ILicenseContext.HasFeature()`.      |
| **Depends On**       | `v0.3.2a` (DataGrid)        | Requires LexiconView for navigation.             |
| **Author**           | System Architect            |                                                  |
| **Status**           | **Draft**                   | Pending approval.                                |
| **Last Updated**     | 2026-01-26                  |                                                  |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need a dialog interface to create and edit terminology entries. The DataGrid (v0.3.2a) provides viewing and selection, but there is no mechanism for:

| User Need           | Current Status  | Expected Behavior                 |
| :------------------ | :-------------- | :-------------------------------- |
| Create new term     | ❌ Not possible | ✅ Modal form with all fields     |
| Edit existing term  | ❌ Not possible | ✅ Pre-populated form for editing |
| Regex pattern entry | ❌ Not possible | ✅ Checkbox to toggle regex mode  |
| Category selection  | ❌ Not possible | ✅ Dropdown with enum values      |
| Severity selection  | ❌ Not possible | ✅ Dropdown with enum values      |
| Fuzzy threshold     | ❌ Not possible | ✅ Slider with percentage display |

### 2.2 The Proposed Solution

Implement `TermEditorView` modal dialog:

1. **TermEditorView.axaml** — Modal dialog with form fields.
2. **TermEditorViewModel** — MVVM ViewModel with validation.
3. **Form Fields** — Pattern, IsRegex, Recommendation, Category, Severity, Tags, Fuzzy controls.
4. **License Gating** — Fuzzy controls disabled for Core users with tooltip.
5. **Validation Integration** — Uses `StyleTermValidator` (v0.3.2c) for input checking.

---

## 3. Architecture

### 3.1 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      TERM EDITOR DIALOG                         │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Dialog Header: "New Term" / "Edit Term"            [X]  │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │  Pattern*          [________________________]            │   │
│  │  □ Is Regular Expression                                 │   │
│  │                                                          │   │
│  │  Recommendation*   [________________________]            │   │
│  │                    [________________________]            │   │
│  │                                                          │   │
│  │  Category          [Terminology        ▼]                │   │
│  │  Severity          [Warning            ▼]                │   │
│  │                                                          │   │
│  │  Tags              [________________________]            │   │
│  │                    (comma-separated)                     │   │
│  │                                                          │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │ FUZZY MATCHING (Writer Pro)                        │  │   │
│  │  │ □ Enable Fuzzy Match                               │  │   │
│  │  │ Threshold: [=========●====] 80%                    │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │                       [Cancel]  [Save Term]              │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Dependencies

**Upstream Interfaces:**

| Interface           | Source Version | Purpose                   |
| :------------------ | :------------- | :------------------------ |
| `StyleTermDto`      | v0.3.2a        | DTO for term data binding |
| `IDialogService`    | v0.3.2         | Modal dialog presentation |
| `ILicenseContext`   | v0.0.4c        | License tier checking     |
| `RuleCategory`      | v0.2.1b        | Category enum             |
| `ViolationSeverity` | v0.2.1b        | Severity enum             |
| `ViewModelBase`     | v0.1.1         | Base ViewModel            |

**New Types (v0.3.2b):**

| Type                  | Module        | Purpose                          |
| :-------------------- | :------------ | :------------------------------- |
| `TermEditorView`      | Modules.Style | Modal dialog AXAML               |
| `TermEditorViewModel` | Modules.Style | Dialog ViewModel with validation |

---

## 4. Data Contract

### 4.1 TermEditorViewModel

```csharp
namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Term Editor dialog.
/// </summary>
/// <remarks>
/// LOGIC: Provides two-way binding for term editing.
/// Fuzzy controls are disabled for Core license users.
/// </remarks>
public sealed partial class TermEditorViewModel : ViewModelBase
{
    private readonly ILicenseContext _licenseContext;
    private readonly StyleTermValidator _validator;
    private readonly ILogger<TermEditorViewModel> _logger;

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _pattern = string.Empty;

    [ObservableProperty]
    private bool _isRegex;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _recommendation = string.Empty;

    [ObservableProperty]
    private RuleCategory _category = RuleCategory.Terminology;

    [ObservableProperty]
    private ViolationSeverity _severity = ViolationSeverity.Warning;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private bool _fuzzyEnabled;

    [ObservableProperty]
    private double _fuzzyThreshold = 0.80;

    /// <summary>
    /// Gets whether the dialog is in edit mode (existing term).
    /// </summary>
    public bool IsEditMode { get; }

    /// <summary>
    /// Gets the dialog title based on mode.
    /// </summary>
    public string DialogTitle => IsEditMode ? "Edit Term" : "New Term";

    /// <summary>
    /// Gets whether fuzzy controls should be enabled.
    /// </summary>
    public bool CanUseFuzzy => _licenseContext.HasFeature(Feature.FuzzyMatching);

    /// <summary>
    /// Gets available category values.
    /// </summary>
    public IReadOnlyList<RuleCategory> Categories { get; } =
        Enum.GetValues<RuleCategory>().ToList();

    /// <summary>
    /// Gets available severity values.
    /// </summary>
    public IReadOnlyList<ViolationSeverity> Severities { get; } =
        Enum.GetValues<ViolationSeverity>().ToList();

    /// <summary>
    /// Initializes a new Term Editor ViewModel.
    /// </summary>
    /// <param name="existingTerm">Existing term to edit, or null for new.</param>
    /// <param name="licenseContext">License context for feature checks.</param>
    /// <param name="validator">Validator for input validation.</param>
    /// <param name="logger">Logger instance.</param>
    public TermEditorViewModel(
        StyleTermDto? existingTerm,
        ILicenseContext licenseContext,
        StyleTermValidator validator,
        ILogger<TermEditorViewModel> logger)
    {
        _licenseContext = licenseContext;
        _validator = validator;
        _logger = logger;

        IsEditMode = existingTerm is not null;

        if (existingTerm is not null)
        {
            Id = existingTerm.Id;
            Pattern = existingTerm.Pattern;
            IsRegex = existingTerm.IsRegex;
            Recommendation = existingTerm.Recommendation;
            Category = existingTerm.Category;
            Severity = existingTerm.Severity;
            Tags = existingTerm.Tags ?? string.Empty;
            FuzzyEnabled = existingTerm.FuzzyEnabled;
            FuzzyThreshold = existingTerm.FuzzyThreshold;

            _logger.LogDebug("Editing term {TermId}: {Pattern}", Id, Pattern);
        }
        else
        {
            Id = Guid.NewGuid();
            _logger.LogDebug("Creating new term {TermId}", Id);
        }
    }

    /// <summary>
    /// Creates the term DTO from current form values.
    /// </summary>
    public StyleTermDto ToDto() => new()
    {
        Id = Id,
        Pattern = Pattern,
        IsRegex = IsRegex,
        Recommendation = Recommendation,
        Category = Category,
        Severity = Severity,
        Tags = string.IsNullOrWhiteSpace(Tags) ? null : Tags,
        FuzzyEnabled = CanUseFuzzy && FuzzyEnabled,
        FuzzyThreshold = FuzzyThreshold
    };

    /// <summary>
    /// Validates the current form state.
    /// </summary>
    [RelayCommand]
    private bool Validate()
    {
        var dto = ToDto();
        var result = _validator.Validate(dto);

        ClearErrors();
        foreach (var error in result.Errors)
        {
            AddError(error.PropertyName, error.ErrorMessage);
        }

        _logger.LogDebug(
            "Validation result: IsValid={IsValid}, Errors={ErrorCount}",
            result.IsValid,
            result.Errors.Count);

        return result.IsValid;
    }

    /// <summary>
    /// Saves the term if validation passes.
    /// </summary>
    [RelayCommand]
    private StyleTermDto? Save()
    {
        if (!Validate())
        {
            _logger.LogWarning("Save blocked: validation failed");
            return null;
        }

        var dto = ToDto();
        _logger.LogInformation(
            "Term {TermId} ready to save: {Pattern}",
            dto.Id,
            dto.Pattern);

        return dto;
    }
}
```

---

## 5. UI/UX Specifications

### 5.1 Form Field Specifications

| Field           | Control Type | Validation               | Notes                     |
| :-------------- | :----------- | :----------------------- | :------------------------ |
| Pattern         | TextBox      | Required, max 500 chars  | Single line               |
| Is Regex        | CheckBox     | None                     | Triggers regex validation |
| Recommendation  | TextBox      | Required, max 1000 chars | Multi-line (3 rows)       |
| Category        | ComboBox     | Required                 | Enum dropdown             |
| Severity        | ComboBox     | Required                 | Enum dropdown             |
| Tags            | TextBox      | Optional                 | Comma-separated           |
| Fuzzy Enabled   | CheckBox     | None                     | Gated by license          |
| Fuzzy Threshold | Slider       | 0.5-1.0 range            | Shows percentage          |

### 5.2 Fuzzy Section License Gating

- **Writer Pro+:** Fuzzy section fully enabled.
- **Core:** Fuzzy section visible but disabled with tooltip: "Fuzzy matching requires Writer Pro."

### 5.3 Theme Integration

| Element           | Resource                   |
| :---------------- | :------------------------- |
| Dialog Background | `Brush.Surface.Overlay`    |
| Field Labels      | `Brush.Text.Secondary`     |
| Field Borders     | `LexTextBox` theme         |
| Save Button       | `LexButtonPrimary`         |
| Cancel Button     | `LexButtonSecondary`       |
| Fuzzy Section     | `Brush.Surface.Elevated`   |
| Disabled Overlay  | `Brush.Surface.Base` @ 50% |

---

## 6. Decision Trees

### 6.1 Should Fuzzy Controls Be Enabled?

```text
START: "Should fuzzy controls be enabled?"
│
├── Does user have Feature.FuzzyMatching?
│   ├── NO → DISABLED: Show tooltip, gray out controls
│   └── YES → ENABLED: Full interactivity
│
└── END
```

### 6.2 Save Button State

```text
START: "Should Save button be enabled?"
│
├── Is Pattern empty?
│   └── YES → DISABLED
├── Is Recommendation empty?
│   └── YES → DISABLED
├── Is IsRegex true AND Pattern is invalid regex?
│   └── YES → DISABLED
├── Is FuzzyEnabled true AND Threshold out of range?
│   └── YES → DISABLED
│
└── All checks pass → ENABLED
```

---

## 7. User Stories

| ID    | Role            | Story                                                     | AC                                              |
| :---- | :-------------- | :-------------------------------------------------------- | :---------------------------------------------- |
| US-01 | Writer Pro User | As a paid user, I want to create a new term via a dialog. | Add button opens empty form; Save creates term. |
| US-02 | Writer Pro User | As a paid user, I want to edit an existing term.          | Edit opens pre-filled form; Save updates term.  |
| US-03 | Writer Pro User | As a paid user, I want to mark a pattern as regex.        | Checkbox toggles regex mode with validation.    |
| US-04 | Writer Pro User | As a paid user, I want to enable fuzzy matching.          | Fuzzy section enabled with threshold slider.    |
| US-05 | Core User       | As a free user, I see fuzzy controls disabled.            | Fuzzy section visible but grayed with tooltip.  |
| US-06 | Developer       | As a developer, I want validation errors shown inline.    | Red borders and error messages below fields.    |
| US-07 | Developer       | As a developer, I want Cancel to close without saving.    | Cancel returns null DialogResult.               |

---

## 8. Use Cases

### UC-01: Create New Term

**Preconditions:** User has Writer Pro license.

**Flow:**

1. User clicks "Add Term" in LexiconView.
2. TermEditorView opens with empty fields.
3. User enters Pattern: "whitelist".
4. User enters Recommendation: "Use 'allowlist' instead."
5. User selects Category: Terminology.
6. User selects Severity: Error.
7. User clicks Save.
8. Validation passes.
9. Dialog closes, term is created.

**Postconditions:** New term appears in DataGrid.

---

### UC-02: Edit Existing Term

**Preconditions:** User selects term in DataGrid.

**Flow:**

1. User right-clicks and selects "Edit".
2. TermEditorView opens with pre-filled values.
3. User modifies Severity from Warning to Error.
4. User clicks Save.
5. Dialog closes, term is updated.

**Postconditions:** DataGrid shows updated severity.

---

## 9. Unit Testing

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.2b")]
public class TermEditorViewModelTests
{
    private readonly Mock<ILicenseContext> _mockLicense;
    private readonly Mock<ILogger<TermEditorViewModel>> _mockLogger;
    private readonly StyleTermValidator _validator;

    public TermEditorViewModelTests()
    {
        _mockLicense = new Mock<ILicenseContext>();
        _mockLogger = new Mock<ILogger<TermEditorViewModel>>();
        _validator = new StyleTermValidator();

        _mockLicense.Setup(l => l.HasFeature(Feature.FuzzyMatching))
            .Returns(true);
    }

    [Fact]
    public void Constructor_WithNullTerm_SetsCreateMode()
    {
        var sut = CreateViewModel(null);

        sut.IsEditMode.Should().BeFalse();
        sut.DialogTitle.Should().Be("New Term");
        sut.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithExistingTerm_SetsEditMode()
    {
        var term = CreateDto("whitelist");

        var sut = CreateViewModel(term);

        sut.IsEditMode.Should().BeTrue();
        sut.DialogTitle.Should().Be("Edit Term");
        sut.Pattern.Should().Be("whitelist");
    }

    [Fact]
    public void CanUseFuzzy_WithWriterPro_ReturnsTrue()
    {
        var sut = CreateViewModel(null);

        sut.CanUseFuzzy.Should().BeTrue();
    }

    [Fact]
    public void CanUseFuzzy_WithCoreLicense_ReturnsFalse()
    {
        _mockLicense.Setup(l => l.HasFeature(Feature.FuzzyMatching))
            .Returns(false);

        var sut = CreateViewModel(null);

        sut.CanUseFuzzy.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyPattern_ReturnsFalse()
    {
        var sut = CreateViewModel(null);
        sut.Pattern = "";
        sut.Recommendation = "Use something else";

        var result = sut.ValidateCommand.Execute(null);

        result.Should().BeFalse();
        sut.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidData_ReturnsTrue()
    {
        var sut = CreateViewModel(null);
        sut.Pattern = "whitelist";
        sut.Recommendation = "Use allowlist";

        var result = sut.ValidateCommand.Execute(null);

        result.Should().BeTrue();
        sut.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var sut = CreateViewModel(null);
        sut.Pattern = "test";
        sut.IsRegex = true;
        sut.Recommendation = "rec";
        sut.Category = RuleCategory.Formatting;
        sut.Severity = ViolationSeverity.Error;
        sut.Tags = "tag1,tag2";
        sut.FuzzyEnabled = true;
        sut.FuzzyThreshold = 0.9;

        var dto = sut.ToDto();

        dto.Pattern.Should().Be("test");
        dto.IsRegex.Should().BeTrue();
        dto.Recommendation.Should().Be("rec");
        dto.Category.Should().Be(RuleCategory.Formatting);
        dto.Severity.Should().Be(ViolationSeverity.Error);
        dto.Tags.Should().Be("tag1,tag2");
        dto.FuzzyEnabled.Should().BeTrue();
        dto.FuzzyThreshold.Should().Be(0.9);
    }

    [Fact]
    public void ToDto_WhenCoreLicense_DisablesFuzzy()
    {
        _mockLicense.Setup(l => l.HasFeature(Feature.FuzzyMatching))
            .Returns(false);

        var sut = CreateViewModel(null);
        sut.FuzzyEnabled = true;

        var dto = sut.ToDto();

        dto.FuzzyEnabled.Should().BeFalse();
    }

    private TermEditorViewModel CreateViewModel(StyleTermDto? term) =>
        new(term, _mockLicense.Object, _validator, _mockLogger.Object);

    private static StyleTermDto CreateDto(string pattern) => new()
    {
        Id = Guid.NewGuid(),
        Pattern = pattern,
        Recommendation = "Use alternative",
        Category = RuleCategory.Terminology,
        Severity = ViolationSeverity.Warning
    };
}
```

---

## 10. Observability

| Level   | Source              | Message Template                                       |
| :------ | :------------------ | :----------------------------------------------------- |
| Debug   | TermEditorViewModel | `Editing term {TermId}: {Pattern}`                     |
| Debug   | TermEditorViewModel | `Creating new term {TermId}`                           |
| Debug   | TermEditorViewModel | `Validation result: IsValid={IsValid}, Errors={Count}` |
| Warning | TermEditorViewModel | `Save blocked: validation failed`                      |
| Info    | TermEditorViewModel | `Term {TermId} ready to save: {Pattern}`               |

---

## 11. Code Example: TermEditorView.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Lexichord.Modules.Style.ViewModels"
        x:Class="Lexichord.Modules.Style.Views.TermEditorView"
        x:DataType="vm:TermEditorViewModel"
        Title="{Binding DialogTitle}"
        Width="500" SizeToContent="Height"
        WindowStartupLocation="CenterOwner"
        CanResize="False"
        Background="{StaticResource Brush.Surface.Overlay}">

  <Grid RowDefinitions="Auto,*,Auto" Margin="24">
    <!-- Header -->
    <TextBlock Grid.Row="0"
               Text="{Binding DialogTitle}"
               FontSize="20" FontWeight="SemiBold"
               Foreground="{StaticResource Brush.Text.Primary}"
               Margin="0,0,0,16" />

    <!-- Form -->
    <StackPanel Grid.Row="1" Spacing="16">
      <!-- Pattern -->
      <StackPanel Spacing="4">
        <TextBlock Text="Pattern *"
                   Foreground="{StaticResource Brush.Text.Secondary}" />
        <TextBox Theme="{StaticResource LexTextBox}"
                 Text="{Binding Pattern}"
                 Watermark="Enter term pattern..."
                 AutomationProperties.Name="Pattern" />
      </StackPanel>

      <!-- Is Regex -->
      <CheckBox Content="Is Regular Expression"
                IsChecked="{Binding IsRegex}" />

      <!-- Recommendation -->
      <StackPanel Spacing="4">
        <TextBlock Text="Recommendation *"
                   Foreground="{StaticResource Brush.Text.Secondary}" />
        <TextBox Theme="{StaticResource LexTextBox}"
                 Text="{Binding Recommendation}"
                 TextWrapping="Wrap"
                 AcceptsReturn="True"
                 Height="80"
                 Watermark="Enter recommended alternative..." />
      </StackPanel>

      <!-- Category / Severity Row -->
      <Grid ColumnDefinitions="*,16,*">
        <StackPanel Grid.Column="0" Spacing="4">
          <TextBlock Text="Category"
                     Foreground="{StaticResource Brush.Text.Secondary}" />
          <ComboBox ItemsSource="{Binding Categories}"
                    SelectedItem="{Binding Category}"
                    HorizontalAlignment="Stretch" />
        </StackPanel>
        <StackPanel Grid.Column="2" Spacing="4">
          <TextBlock Text="Severity"
                     Foreground="{StaticResource Brush.Text.Secondary}" />
          <ComboBox ItemsSource="{Binding Severities}"
                    SelectedItem="{Binding Severity}"
                    HorizontalAlignment="Stretch" />
        </StackPanel>
      </Grid>

      <!-- Tags -->
      <StackPanel Spacing="4">
        <TextBlock Text="Tags"
                   Foreground="{StaticResource Brush.Text.Secondary}" />
        <TextBox Theme="{StaticResource LexTextBox}"
                 Text="{Binding Tags}"
                 Watermark="tag1, tag2, tag3..." />
        <TextBlock Text="Comma-separated values"
                   FontSize="11"
                   Foreground="{StaticResource Brush.Text.Tertiary}" />
      </StackPanel>

      <!-- Fuzzy Section -->
      <Border Background="{StaticResource Brush.Surface.Elevated}"
              CornerRadius="8"
              Padding="16"
              IsEnabled="{Binding CanUseFuzzy}">
        <StackPanel Spacing="12">
          <TextBlock Text="Fuzzy Matching"
                     FontWeight="SemiBold"
                     Foreground="{StaticResource Brush.Text.Primary}" />
          <CheckBox Content="Enable Fuzzy Match"
                    IsChecked="{Binding FuzzyEnabled}" />
          <Grid ColumnDefinitions="Auto,*,Auto" IsVisible="{Binding FuzzyEnabled}">
            <TextBlock Text="Threshold:"
                       VerticalAlignment="Center"
                       Foreground="{StaticResource Brush.Text.Secondary}" />
            <Slider Grid.Column="1"
                    Minimum="0.5" Maximum="1.0"
                    Value="{Binding FuzzyThreshold}"
                    Margin="12,0" />
            <TextBlock Grid.Column="2"
                       Text="{Binding FuzzyThreshold, StringFormat=P0}"
                       VerticalAlignment="Center"
                       MinWidth="40" />
          </Grid>
        </StackPanel>

        <ToolTip.Tip>
          <TextBlock Text="Fuzzy matching requires Writer Pro"
                     IsVisible="{Binding !CanUseFuzzy}" />
        </ToolTip.Tip>
      </Border>
    </StackPanel>

    <!-- Footer Buttons -->
    <StackPanel Grid.Row="2"
                Orientation="Horizontal"
                HorizontalAlignment="Right"
                Spacing="12"
                Margin="0,24,0,0">
      <Button Theme="{StaticResource LexButtonSecondary}"
              Content="Cancel"
              Command="{Binding $parent[Window].Close}" />
      <Button Theme="{StaticResource LexButtonPrimary}"
              Content="Save Term"
              Command="{Binding SaveCommand}" />
    </StackPanel>
  </Grid>
</Window>
```

---

## 12. Acceptance Criteria

| #   | Category            | Criterion                                               |
| :-- | :------------------ | :------------------------------------------------------ |
| 1   | **[Dialog]**        | Dialog opens from Add/Edit commands in LexiconView.     |
| 2   | **[Dialog]**        | Title shows "New Term" or "Edit Term" appropriately.    |
| 3   | **[Fields]**        | All fields bind correctly to ViewModel.                 |
| 4   | **[Fields]**        | Category and Severity use enum dropdowns.               |
| 5   | **[Validation]**    | Empty Pattern shows validation error.                   |
| 6   | **[Validation]**    | Empty Recommendation shows validation error.            |
| 7   | **[Validation]**    | Invalid regex pattern shows error when IsRegex checked. |
| 8   | **[Fuzzy]**         | Fuzzy controls enabled for Writer Pro users.            |
| 9   | **[Fuzzy]**         | Fuzzy controls disabled with tooltip for Core users.    |
| 10  | **[Fuzzy]**         | Threshold slider ranges from 50% to 100%.               |
| 11  | **[Buttons]**       | Cancel closes dialog without saving.                    |
| 12  | **[Buttons]**       | Save validates and returns DTO on success.              |
| 13  | **[Buttons]**       | Save blocked when validation fails.                     |
| 14  | **[Accessibility]** | All controls have AutomationProperties.Name.            |
| 15  | **[Theme]**         | All colors use theme resources.                         |

---

## 13. Deliverable Checklist

| Step | Description                                       | Status |
| :--- | :------------------------------------------------ | :----- |
| 1    | `TermEditorView.axaml` dialog layout.             | [ ]    |
| 2    | `TermEditorView.axaml.cs` code-behind.            | [ ]    |
| 3    | `TermEditorViewModel` with all properties.        | [ ]    |
| 4    | Validation integration with `StyleTermValidator`. | [ ]    |
| 5    | License check for fuzzy controls.                 | [ ]    |
| 6    | ToDto() method for result creation.               | [ ]    |
| 7    | Unit tests for `TermEditorViewModel`.             | [ ]    |
| 8    | Integration with `LexiconViewModel` commands.     | [ ]    |

---

## 14. Verification Commands

```bash
# Build solution
dotnet build

# Run v0.3.2b unit tests
dotnet test --filter "Version=v0.3.2b"

# Manual verification:
# 1. Click Add Term → verify empty dialog opens
# 2. Fill Pattern and Recommendation → Save enabled
# 3. Leave Pattern empty → Save disabled, error shown
# 4. Check "Is Regex" with invalid pattern → error shown
# 5. As Core user → Fuzzy section disabled with tooltip
# 6. As Writer Pro → Fuzzy section fully functional
# 7. Edit existing term → fields pre-populated
# 8. Cancel → dialog closes, no changes
```

---

## 15. Changelog Entry

```markdown
### v0.3.2b — Term Editor Dialog

**Added:**

- `TermEditorView` modal dialog for term creation/editing (STY-032b)
- `TermEditorViewModel` with validation and license integration (STY-032b)
- Pattern, IsRegex, Recommendation, Category, Severity, Tags fields (STY-032b)
- Fuzzy matching controls with license gating (STY-032b)
- Validation error display with inline messages (STY-032b)

**Technical:**

- Integration with StyleTermValidator for input validation
- License-gated fuzzy controls with tooltip for Core users
- Two-way binding with StyleTermDto
```
