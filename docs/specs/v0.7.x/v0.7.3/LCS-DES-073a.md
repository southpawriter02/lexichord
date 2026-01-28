# LCS-DES-073a: Design Specification — EditorViewModel Integration

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `AGT-073a` | Sub-part of AGT-073 |
| **Feature Name** | `EditorViewModel Integration` | Context menu integration |
| **Target Version** | `v0.7.3a` | First sub-part of v0.7.3 |
| **Module Scope** | `Lexichord.Modules.Agents` | Agents module |
| **Swimlane** | `Ensemble` | Agent vertical |
| **License Tier** | `Writer Pro` | Requires Writer Pro |
| **Feature Gate Key** | `FeatureFlags.Agents.Editor` | License gate |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-073-INDEX](./LCS-DES-073-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-073 Section 3.1](./LCS-SBD-073.md#31-v073a-editorviewmodel-integration) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Writers need seamless access to AI rewriting capabilities directly from the editor's context menu. The current context menu provides standard editing operations (cut, copy, paste) but lacks AI-powered transformation options.

> **Goal:** Integrate AI rewrite commands into the right-click context menu so users can transform selected text without leaving the editing flow.

### 2.2 The Proposed Solution

Implement an `EditorAgentContextMenuProvider` that:

1. Provides a "Rewrite with AI..." submenu to the editor's context menu
2. Includes predefined rewrite intents (Formal, Simplify, Expand, Custom)
3. Respects license gating (Writer Pro required)
4. Enables/disables based on text selection state
5. Provides keyboard shortcuts for power users

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Modules

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IEditorViewModel` | v0.1.3b | Context menu integration point |
| `ILicenseContext` | v0.0.4c | License verification |
| `IRewriteCommandHandler` | v0.7.3b | Command execution |
| `IMediator` | v0.0.7a | Event publishing |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `CommunityToolkit.Mvvm` | 8.x | MVVM infrastructure |
| `Avalonia.Controls` | 11.x | UI controls |

### 3.2 Licensing Behavior

*   **Load Behavior:** Soft Gate — Module loads, but commands show as disabled with upgrade prompt
*   **Fallback Experience:**
    *   "Rewrite with AI" submenu visible but items disabled
    *   Lock icon displayed next to submenu header
    *   Clicking disabled item opens upgrade modal
    *   Tooltip explains "Upgrade to Writer Pro to unlock"

---

## 4. Data Contract (The API)

### 4.1 IEditorAgentContextMenuProvider

```csharp
namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Provides Editor Agent commands to the editor's context menu.
/// Integrates with <see cref="IEditorViewModel"/> to add AI rewriting options.
/// </summary>
public interface IEditorAgentContextMenuProvider
{
    /// <summary>
    /// Gets the context menu items for AI rewriting commands.
    /// Items are automatically disabled when no text is selected or
    /// when the user lacks Writer Pro license.
    /// </summary>
    /// <returns>Collection of context menu items for the "Rewrite with AI" submenu.</returns>
    IReadOnlyList<ContextMenuItem> GetRewriteMenuItems();

    /// <summary>
    /// Gets the parent submenu item that contains all rewrite commands.
    /// </summary>
    ContextMenuItem GetRewriteSubmenu();

    /// <summary>
    /// Gets whether rewrite commands should be enabled based on:
    /// - Current selection state
    /// - License authorization
    /// </summary>
    bool CanRewrite { get; }

    /// <summary>
    /// Gets whether the user has the required license for rewriting.
    /// </summary>
    bool IsLicensed { get; }

    /// <summary>
    /// Event raised when rewrite availability changes due to
    /// selection change or license state change.
    /// </summary>
    event EventHandler<bool> CanRewriteChanged;

    /// <summary>
    /// Refreshes the enabled state of all menu items.
    /// Called when selection changes or license state updates.
    /// </summary>
    void RefreshState();
}
```

### 4.2 ContextMenuItem

```csharp
namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Represents a context menu item for editor commands.
/// </summary>
public record ContextMenuItem
{
    /// <summary>
    /// Unique identifier for the command.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Display text shown in the menu.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description shown in tooltip.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Icon name from the icon library.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Keyboard shortcut display text (e.g., "Ctrl+Shift+R").
    /// </summary>
    public string? KeyboardShortcut { get; init; }

    /// <summary>
    /// Whether the item is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Whether the item shows a license lock icon.
    /// </summary>
    public bool ShowLicenseLock { get; init; }

    /// <summary>
    /// Command to execute when clicked.
    /// </summary>
    public ICommand? Command { get; init; }

    /// <summary>
    /// Child items for submenus.
    /// </summary>
    public IReadOnlyList<ContextMenuItem>? Children { get; init; }
}
```

### 4.3 RewriteCommandOption

```csharp
namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Defines a rewrite command option available in the context menu.
/// </summary>
public record RewriteCommandOption
{
    /// <summary>
    /// Unique command identifier.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Display name shown in menu.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description for tooltip.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Icon name (Lucide icons).
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Keyboard shortcut (e.g., "Ctrl+Shift+R").
    /// </summary>
    public string? KeyboardShortcut { get; init; }

    /// <summary>
    /// The rewrite intent this command triggers.
    /// </summary>
    public required RewriteIntent Intent { get; init; }

    /// <summary>
    /// Whether this command opens a dialog (Custom Rewrite).
    /// </summary>
    public bool OpensDialog { get; init; }
}

/// <summary>
/// The type of rewrite transformation requested.
/// </summary>
public enum RewriteIntent
{
    /// <summary>
    /// Transform to formal, professional tone.
    /// </summary>
    Formal,

    /// <summary>
    /// Simplify for broader audience.
    /// </summary>
    Simplified,

    /// <summary>
    /// Expand with more detail.
    /// </summary>
    Expanded,

    /// <summary>
    /// Custom transformation with user instruction.
    /// </summary>
    Custom
}
```

---

## 5. Implementation Logic

### 5.1 EditorAgentContextMenuProvider

```csharp
namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Provides AI rewrite commands to the editor's context menu.
/// </summary>
public class EditorAgentContextMenuProvider : IEditorAgentContextMenuProvider, IDisposable
{
    private readonly ILicenseContext _licenseContext;
    private readonly IRewriteCommandHandler _commandHandler;
    private readonly IEditorViewModel _editorViewModel;
    private readonly IMediator _mediator;
    private readonly ILogger<EditorAgentContextMenuProvider> _logger;

    private bool _canRewrite;
    private bool _isLicensed;
    private readonly List<IDisposable> _subscriptions = new();

    public event EventHandler<bool>? CanRewriteChanged;

    public bool CanRewrite => _canRewrite;
    public bool IsLicensed => _isLicensed;

    /// <summary>
    /// Predefined rewrite command options.
    /// </summary>
    private static readonly IReadOnlyList<RewriteCommandOption> RewriteOptions = new[]
    {
        new RewriteCommandOption
        {
            CommandId = "rewrite-formal",
            DisplayName = "Rewrite Formally",
            Description = "Transform to formal, professional tone",
            Icon = "sparkles",
            KeyboardShortcut = "Ctrl+Shift+R",
            Intent = RewriteIntent.Formal,
            OpensDialog = false
        },
        new RewriteCommandOption
        {
            CommandId = "rewrite-simplify",
            DisplayName = "Simplify",
            Description = "Reduce complexity for broader audience",
            Icon = "file-text",
            KeyboardShortcut = "Ctrl+Shift+S",
            Intent = RewriteIntent.Simplified,
            OpensDialog = false
        },
        new RewriteCommandOption
        {
            CommandId = "rewrite-expand",
            DisplayName = "Expand",
            Description = "Add more detail and explanation",
            Icon = "book-open",
            KeyboardShortcut = "Ctrl+Shift+E",
            Intent = RewriteIntent.Expanded,
            OpensDialog = false
        },
        new RewriteCommandOption
        {
            CommandId = "rewrite-custom",
            DisplayName = "Custom Rewrite...",
            Description = "Enter custom rewrite instruction",
            Icon = "settings",
            KeyboardShortcut = "Ctrl+Shift+C",
            Intent = RewriteIntent.Custom,
            OpensDialog = true
        }
    };

    public EditorAgentContextMenuProvider(
        ILicenseContext licenseContext,
        IRewriteCommandHandler commandHandler,
        IEditorViewModel editorViewModel,
        IMediator mediator,
        ILogger<EditorAgentContextMenuProvider> logger)
    {
        _licenseContext = licenseContext;
        _commandHandler = commandHandler;
        _editorViewModel = editorViewModel;
        _mediator = mediator;
        _logger = logger;

        Initialize();
    }

    private void Initialize()
    {
        // Subscribe to selection changes
        _subscriptions.Add(
            Observable.FromEventPattern<EventArgs>(
                h => _editorViewModel.SelectionChanged += h,
                h => _editorViewModel.SelectionChanged -= h)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => RefreshState()));

        // Subscribe to license changes
        _subscriptions.Add(
            Observable.FromEventPattern<LicenseChangedEventArgs>(
                h => _licenseContext.LicenseChanged += h,
                h => _licenseContext.LicenseChanged -= h)
            .Subscribe(_ => RefreshState()));

        // Initial state
        RefreshState();
    }

    public void RefreshState()
    {
        var hasSelection = !string.IsNullOrEmpty(_editorViewModel.SelectedText);
        var isLicensed = _licenseContext.HasFeature(FeatureFlags.Agents.Editor);

        var newCanRewrite = hasSelection && isLicensed;

        if (_canRewrite != newCanRewrite || _isLicensed != isLicensed)
        {
            _canRewrite = newCanRewrite;
            _isLicensed = isLicensed;

            _logger.LogDebug(
                "Rewrite state changed: CanRewrite={CanRewrite}, HasSelection={HasSelection}, IsLicensed={IsLicensed}",
                _canRewrite, hasSelection, isLicensed);

            CanRewriteChanged?.Invoke(this, _canRewrite);
        }
    }

    public ContextMenuItem GetRewriteSubmenu()
    {
        return new ContextMenuItem
        {
            CommandId = "rewrite-submenu",
            DisplayName = "Rewrite with AI...",
            Icon = "wand-2",
            IsEnabled = true, // Submenu always enabled for discoverability
            ShowLicenseLock = !_isLicensed,
            Children = GetRewriteMenuItems()
        };
    }

    public IReadOnlyList<ContextMenuItem> GetRewriteMenuItems()
    {
        return RewriteOptions.Select(CreateMenuItem).ToList();
    }

    private ContextMenuItem CreateMenuItem(RewriteCommandOption option)
    {
        return new ContextMenuItem
        {
            CommandId = option.CommandId,
            DisplayName = option.DisplayName,
            Description = option.Description,
            Icon = option.Icon,
            KeyboardShortcut = option.KeyboardShortcut,
            IsEnabled = _canRewrite,
            ShowLicenseLock = !_isLicensed,
            Command = new AsyncRelayCommand(
                () => ExecuteRewriteAsync(option),
                () => _canRewrite)
        };
    }

    private async Task ExecuteRewriteAsync(RewriteCommandOption option)
    {
        if (!_canRewrite)
        {
            if (!_isLicensed)
            {
                await ShowUpgradeModalAsync();
            }
            return;
        }

        _logger.LogInformation(
            "Executing rewrite command: {CommandId} for {CharCount} characters",
            option.CommandId,
            _editorViewModel.SelectedText?.Length ?? 0);

        await _mediator.Publish(new RewriteRequestedEvent(
            option.Intent,
            _editorViewModel.SelectedText!,
            _editorViewModel.DocumentPath));

        if (option.OpensDialog)
        {
            await ShowCustomRewriteDialogAsync();
        }
        else
        {
            await _commandHandler.ExecuteAsync(new RewriteRequest
            {
                SelectedText = _editorViewModel.SelectedText!,
                SelectionSpan = _editorViewModel.SelectionSpan,
                Intent = option.Intent,
                CustomInstruction = null,
                DocumentPath = _editorViewModel.DocumentPath,
                AdditionalContext = null
            });
        }
    }

    private async Task ShowUpgradeModalAsync()
    {
        _logger.LogDebug("Showing upgrade modal for Editor Agent");
        await _mediator.Publish(new ShowUpgradeModalEvent(
            FeatureName: "Editor Agent",
            RequiredTier: LicenseTier.WriterPro,
            Message: "Upgrade to Writer Pro to unlock AI-powered text rewriting."));
    }

    private async Task ShowCustomRewriteDialogAsync()
    {
        // Implementation delegates to view model
        _logger.LogDebug("Opening custom rewrite dialog");
        await _mediator.Publish(new ShowCustomRewriteDialogEvent(
            SelectedText: _editorViewModel.SelectedText!,
            SelectionSpan: _editorViewModel.SelectionSpan,
            DocumentPath: _editorViewModel.DocumentPath));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }
}
```

### 5.2 RewriteCommandViewModel

```csharp
namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// ViewModel for rewrite command state and execution.
/// </summary>
public partial class RewriteCommandViewModel : ObservableObject
{
    private readonly IEditorAgentContextMenuProvider _menuProvider;
    private readonly IRewriteCommandHandler _commandHandler;
    private readonly IEditorViewModel _editorViewModel;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    [ObservableProperty]
    private bool _canRewrite;

    [ObservableProperty]
    private bool _isLicensed;

    public RewriteCommandViewModel(
        IEditorAgentContextMenuProvider menuProvider,
        IRewriteCommandHandler commandHandler,
        IEditorViewModel editorViewModel)
    {
        _menuProvider = menuProvider;
        _commandHandler = commandHandler;
        _editorViewModel = editorViewModel;

        _menuProvider.CanRewriteChanged += (_, canRewrite) =>
        {
            CanRewrite = canRewrite;
            IsLicensed = _menuProvider.IsLicensed;
        };

        CanRewrite = _menuProvider.CanRewrite;
        IsLicensed = _menuProvider.IsLicensed;
    }

    [RelayCommand(CanExecute = nameof(CanRewrite))]
    private async Task RewriteFormallyAsync(CancellationToken ct)
    {
        await ExecuteRewriteAsync(RewriteIntent.Formal, null, ct);
    }

    [RelayCommand(CanExecute = nameof(CanRewrite))]
    private async Task SimplifyAsync(CancellationToken ct)
    {
        await ExecuteRewriteAsync(RewriteIntent.Simplified, null, ct);
    }

    [RelayCommand(CanExecute = nameof(CanRewrite))]
    private async Task ExpandAsync(CancellationToken ct)
    {
        await ExecuteRewriteAsync(RewriteIntent.Expanded, null, ct);
    }

    [RelayCommand(CanExecute = nameof(CanRewrite))]
    private async Task CustomRewriteAsync(string instruction, CancellationToken ct)
    {
        await ExecuteRewriteAsync(RewriteIntent.Custom, instruction, ct);
    }

    private async Task ExecuteRewriteAsync(
        RewriteIntent intent,
        string? customInstruction,
        CancellationToken ct)
    {
        if (IsExecuting) return;

        try
        {
            IsExecuting = true;
            Progress = 0;
            ProgressMessage = "Preparing...";

            var request = new RewriteRequest
            {
                SelectedText = _editorViewModel.SelectedText!,
                SelectionSpan = _editorViewModel.SelectionSpan,
                Intent = intent,
                CustomInstruction = customInstruction,
                DocumentPath = _editorViewModel.DocumentPath,
                AdditionalContext = null
            };

            await foreach (var update in _commandHandler.ExecuteStreamingAsync(request, ct))
            {
                Progress = update.ProgressPercentage;
                ProgressMessage = update.State switch
                {
                    RewriteProgressState.Initializing => "Initializing...",
                    RewriteProgressState.GatheringContext => "Gathering context...",
                    RewriteProgressState.GeneratingRewrite => "Generating rewrite...",
                    RewriteProgressState.Completed => "Complete!",
                    RewriteProgressState.Failed => "Failed",
                    _ => "Processing..."
                };
            }
        }
        finally
        {
            IsExecuting = false;
            Progress = 0;
            ProgressMessage = string.Empty;
        }
    }
}
```

### 5.3 Keyboard Shortcut Registration

```csharp
namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Registers keyboard shortcuts for rewrite commands.
/// </summary>
public class RewriteKeyboardShortcuts
{
    public static readonly KeyBinding[] Bindings = new[]
    {
        new KeyBinding
        {
            Gesture = new KeyGesture(Key.R, KeyModifiers.Control | KeyModifiers.Shift),
            Command = "RewriteFormally"
        },
        new KeyBinding
        {
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift),
            Command = "Simplify"
        },
        new KeyBinding
        {
            Gesture = new KeyGesture(Key.E, KeyModifiers.Control | KeyModifiers.Shift),
            Command = "Expand"
        },
        new KeyBinding
        {
            Gesture = new KeyGesture(Key.C, KeyModifiers.Control | KeyModifiers.Shift),
            Command = "CustomRewrite"
        }
    };

    public static void Register(IKeyBindingService keyBindingService)
    {
        foreach (var binding in Bindings)
        {
            keyBindingService.Register(binding);
        }
    }
}
```

---

## 6. UI/UX Specifications

### 6.1 Context Menu Layout

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ [Standard Edit Commands]                                                  │
├──────────────────────────────────────────────────────────────────────────┤
│ Cut                                                          Ctrl+X      │
│ Copy                                                         Ctrl+C      │
│ Paste                                                        Ctrl+V      │
├──────────────────────────────────────────────────────────────────────────┤
│ ▶ 🪄 Rewrite with AI...                                        [Pro]    │
│   ┌────────────────────────────────────────────────────────────────────┐ │
│   │ ✨ Rewrite Formally                              Ctrl+Shift+R      │ │
│   │ 📝 Simplify                                      Ctrl+Shift+S      │ │
│   │ 📖 Expand                                        Ctrl+Shift+E      │ │
│   │ ─────────────────────────────────────────────────────────────────  │ │
│   │ ⚙️ Custom Rewrite...                             Ctrl+Shift+C      │ │
│   └────────────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────┤
│ Check Style                                                              │
│ Define Term...                                                           │
└──────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Disabled State (No Selection)

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ ▶ 🪄 Rewrite with AI...                                        [Pro]    │
│   ┌────────────────────────────────────────────────────────────────────┐ │
│   │ ✨ Rewrite Formally (disabled, grayed out)                         │ │
│   │ 📝 Simplify (disabled, grayed out)                                 │ │
│   │ 📖 Expand (disabled, grayed out)                                   │ │
│   │ ⚙️ Custom Rewrite... (disabled, grayed out)                        │ │
│   └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│ Tooltip: "Select text to enable rewrite options"                         │
└──────────────────────────────────────────────────────────────────────────┘
```

### 6.3 Free License State

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ ▶ 🪄 Rewrite with AI...                                         [🔒]    │
│   ┌────────────────────────────────────────────────────────────────────┐ │
│   │ 🔒 Rewrite Formally                              Ctrl+Shift+R      │ │
│   │ 🔒 Simplify                                      Ctrl+Shift+S      │ │
│   │ 🔒 Expand                                        Ctrl+Shift+E      │ │
│   │ 🔒 Custom Rewrite...                             Ctrl+Shift+C      │ │
│   └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│ Click action: Opens upgrade modal                                        │
│ Tooltip: "Upgrade to Writer Pro to unlock AI-powered rewriting"         │
└──────────────────────────────────────────────────────────────────────────┘
```

### 6.4 Component Styling

| Component | Theme Resource | Notes |
| :--- | :--- | :--- |
| Context menu | `LexContextMenu` | Standard Lexichord menu |
| Submenu header | `LexMenuItemHeader` | Bold, with icon |
| Menu item | `LexMenuItem` | Standard item |
| Disabled item | `LexMenuItemDisabled` | Grayed out, 50% opacity |
| Lock icon | `Icon.Lock` | 12x12, right-aligned |
| Shortcut text | `LexMenuShortcut` | Right-aligned, lighter color |
| Separator | `LexMenuSeparator` | Before "Custom Rewrite" |

---

## 7. Observability & Logging

| Level | Source | Message Template |
| :--- | :--- | :--- |
| Debug | ContextMenuProvider | `Rewrite state changed: CanRewrite={CanRewrite}, HasSelection={HasSelection}, IsLicensed={IsLicensed}` |
| Info | ContextMenuProvider | `Executing rewrite command: {CommandId} for {CharCount} characters` |
| Debug | ContextMenuProvider | `Showing upgrade modal for Editor Agent` |
| Debug | ContextMenuProvider | `Opening custom rewrite dialog` |
| Warning | ContextMenuProvider | `Rewrite command attempted without selection` |

---

## 8. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| License bypass | Medium | Server-side license validation before LLM call |
| Command injection | None | Commands are predefined, no user input in command IDs |
| Selection state race | Low | Throttle selection change events, validate before execute |

---

## 9. Acceptance Criteria

### 9.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Text is selected, Writer Pro license | Right-click | "Rewrite with AI" submenu shows with enabled items |
| 2 | No text selected | Right-click | "Rewrite with AI" items are disabled |
| 3 | Free license | Right-click | "Rewrite with AI" items show lock icons |
| 4 | Free license | Click rewrite item | Upgrade modal opens |
| 5 | Writer Pro | Click "Rewrite Formally" | Formal rewrite executes |
| 6 | Writer Pro | Press Ctrl+Shift+R | Formal rewrite executes |
| 7 | Writer Pro | Press Ctrl+Shift+S | Simplify rewrite executes |
| 8 | Writer Pro | Press Ctrl+Shift+E | Expand rewrite executes |
| 9 | Writer Pro | Press Ctrl+Shift+C | Custom rewrite dialog opens |
| 10 | Selection changes | — | Menu items enable/disable state updates |

### 9.2 Performance Criteria

| # | Criterion | Target |
| :--- | :--- | :--- |
| 1 | Context menu render time | < 50ms |
| 2 | Selection state update | < 20ms |
| 3 | Keyboard shortcut response | < 10ms |

---

## 10. Unit Testing Requirements

### 10.1 EditorAgentContextMenuProviderTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.3a")]
public class EditorAgentContextMenuProviderTests
{
    private Mock<ILicenseContext> _licenseContext;
    private Mock<IRewriteCommandHandler> _commandHandler;
    private Mock<IEditorViewModel> _editorViewModel;
    private Mock<IMediator> _mediator;
    private EditorAgentContextMenuProvider _sut;

    [SetUp]
    public void Setup()
    {
        _licenseContext = new Mock<ILicenseContext>();
        _commandHandler = new Mock<IRewriteCommandHandler>();
        _editorViewModel = new Mock<IEditorViewModel>();
        _mediator = new Mock<IMediator>();

        _sut = new EditorAgentContextMenuProvider(
            _licenseContext.Object,
            _commandHandler.Object,
            _editorViewModel.Object,
            _mediator.Object,
            Mock.Of<ILogger<EditorAgentContextMenuProvider>>());
    }

    [Fact]
    public void GetRewriteMenuItems_ReturnsAllFourOptions()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();

        // Assert
        items.Should().HaveCount(4);
        items.Should().Contain(i => i.CommandId == "rewrite-formal");
        items.Should().Contain(i => i.CommandId == "rewrite-simplify");
        items.Should().Contain(i => i.CommandId == "rewrite-expand");
        items.Should().Contain(i => i.CommandId == "rewrite-custom");
    }

    [Fact]
    public void GetRewriteSubmenu_ReturnsSubmenuWithChildren()
    {
        // Act
        var submenu = _sut.GetRewriteSubmenu();

        // Assert
        submenu.CommandId.Should().Be("rewrite-submenu");
        submenu.DisplayName.Should().Be("Rewrite with AI...");
        submenu.Children.Should().HaveCount(4);
    }

    [Fact]
    public void CanRewrite_WithSelectionAndLicense_ReturnsTrue()
    {
        // Arrange
        _editorViewModel.Setup(e => e.SelectedText).Returns("Hello world");
        _licenseContext.Setup(l => l.HasFeature(FeatureFlags.Agents.Editor)).Returns(true);

        // Act
        _sut.RefreshState();

        // Assert
        _sut.CanRewrite.Should().BeTrue();
    }

    [Fact]
    public void CanRewrite_WithoutSelection_ReturnsFalse()
    {
        // Arrange
        _editorViewModel.Setup(e => e.SelectedText).Returns(string.Empty);
        _licenseContext.Setup(l => l.HasFeature(FeatureFlags.Agents.Editor)).Returns(true);

        // Act
        _sut.RefreshState();

        // Assert
        _sut.CanRewrite.Should().BeFalse();
    }

    [Fact]
    public void CanRewrite_WithoutLicense_ReturnsFalse()
    {
        // Arrange
        _editorViewModel.Setup(e => e.SelectedText).Returns("Hello world");
        _licenseContext.Setup(l => l.HasFeature(FeatureFlags.Agents.Editor)).Returns(false);

        // Act
        _sut.RefreshState();

        // Assert
        _sut.CanRewrite.Should().BeFalse();
        _sut.IsLicensed.Should().BeFalse();
    }

    [Fact]
    public void GetRewriteMenuItems_WithoutLicense_ShowsLockIcon()
    {
        // Arrange
        _licenseContext.Setup(l => l.HasFeature(FeatureFlags.Agents.Editor)).Returns(false);
        _sut.RefreshState();

        // Act
        var items = _sut.GetRewriteMenuItems();

        // Assert
        items.Should().AllSatisfy(item =>
            item.ShowLicenseLock.Should().BeTrue());
    }

    [Fact]
    public void CanRewriteChanged_RaisedOnSelectionChange()
    {
        // Arrange
        var eventRaised = false;
        _sut.CanRewriteChanged += (_, _) => eventRaised = true;
        _licenseContext.Setup(l => l.HasFeature(FeatureFlags.Agents.Editor)).Returns(true);

        // Act
        _editorViewModel.Setup(e => e.SelectedText).Returns("Hello");
        _sut.RefreshState();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Theory]
    [InlineData("rewrite-formal", RewriteIntent.Formal)]
    [InlineData("rewrite-simplify", RewriteIntent.Simplified)]
    [InlineData("rewrite-expand", RewriteIntent.Expanded)]
    [InlineData("rewrite-custom", RewriteIntent.Custom)]
    public void GetRewriteMenuItems_HasCorrectIntents(string commandId, RewriteIntent expectedIntent)
    {
        // Act
        var items = _sut.GetRewriteMenuItems();
        var item = items.Single(i => i.CommandId == commandId);

        // Assert - verify command executes correct intent
        // (Implementation would test through command execution)
    }

    [Fact]
    public void GetRewriteMenuItems_HasCorrectKeyboardShortcuts()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();

        // Assert
        items.Single(i => i.CommandId == "rewrite-formal")
            .KeyboardShortcut.Should().Be("Ctrl+Shift+R");
        items.Single(i => i.CommandId == "rewrite-simplify")
            .KeyboardShortcut.Should().Be("Ctrl+Shift+S");
        items.Single(i => i.CommandId == "rewrite-expand")
            .KeyboardShortcut.Should().Be("Ctrl+Shift+E");
        items.Single(i => i.CommandId == "rewrite-custom")
            .KeyboardShortcut.Should().Be("Ctrl+Shift+C");
    }
}
```

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `IEditorAgentContextMenuProvider.cs` interface | [ ] |
| 2 | `EditorAgentContextMenuProvider.cs` implementation | [ ] |
| 3 | `ContextMenuItem.cs` record | [ ] |
| 4 | `RewriteCommandOption.cs` record | [ ] |
| 5 | `RewriteIntent.cs` enum | [ ] |
| 6 | `RewriteCommandViewModel.cs` | [ ] |
| 7 | `RewriteKeyboardShortcuts.cs` | [ ] |
| 8 | Integration with `IEditorViewModel.ContextMenuItems` | [ ] |
| 9 | Unit tests for all components | [ ] |
| 10 | DI registration | [ ] |

---

## 12. Verification Commands

```bash
# Run all v0.7.3a tests
dotnet test --filter "Version=v0.7.3a" --logger "console;verbosity=detailed"

# Run context menu provider tests
dotnet test --filter "FullyQualifiedName~EditorAgentContextMenuProviderTests"

# Run command view model tests
dotnet test --filter "FullyQualifiedName~RewriteCommandViewModelTests"

# Manual verification:
# 1. Open document, select text, right-click
#    Expected: "Rewrite with AI" submenu appears with 4 options
# 2. Clear selection, right-click
#    Expected: Rewrite options are disabled
# 3. Test with Free license
#    Expected: Lock icons shown, clicking opens upgrade modal
# 4. Test keyboard shortcuts Ctrl+Shift+R/S/E/C
#    Expected: Corresponding commands execute
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
