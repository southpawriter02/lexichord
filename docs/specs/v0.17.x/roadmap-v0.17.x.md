# Lexichord Intelligent Editor Roadmap (v0.17.1 - v0.17.5)

In v0.16.x, we delivered **Local LLM Integration** — enabling users to run AI models entirely on their own hardware. In v0.17.x, we introduce **The Intelligent Editor** — a deeply AI-integrated writing environment that transforms the editor from a simple text area into a collaborative AI-powered authoring experience.

**Architectural Note:** This version introduces `Lexichord.Editor` module with advanced text editing capabilities, real-time AI integration, and professional document authoring features. The editor becomes the primary surface for human-AI collaboration, not just a passive input field.

**Total Sub-Parts:** 37 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 286 hours (~7.2 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.17.1-EDT | Editor Foundation | Split view, preview pane, enhanced rendering, editor chrome | 56 |
| v0.17.2-EDT | Rich Formatting | Formatting toolbar, shortcuts, context menus, auto-formatting | 54 |
| v0.17.3-EDT | Change Tracking | Diff view, version history, track changes mode, annotations | 58 |
| v0.17.4-EDT | Style & Rules | Formatting rules, linting, style guides, EditorConfig | 52 |
| v0.17.5-EDT | AI Integration | Inline AI, selection actions, comment prompts, ghost text | 66 |

---

## Intelligent Editor Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Intelligent Editor System                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                      AI Integration Layer (v0.17.5)                     │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │ Inline AI    │  │  Selection   │  │   Comment    │  │   Ghost    │ │ │
│  │  │ Suggestions  │  │   Actions    │  │   Prompts    │  │   Text     │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     Style & Rules Engine (v0.17.4)                      │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │  Formatting  │  │    Linter    │  │    Style     │                  │ │
│  │  │    Rules     │  │   Engine     │  │    Guides    │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Change Tracking System (v0.17.3)                     │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │  Diff View   │  │   Version    │  │    Track     │                  │ │
│  │  │   Engine     │  │   History    │  │   Changes    │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Rich Formatting Layer (v0.17.2)                      │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │  Formatting  │  │   Context    │  │    Auto      │                  │ │
│  │  │   Toolbar    │  │    Menus     │  │   Format     │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     Editor Foundation (v0.17.1)                         │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │  Split View  │  │   Preview    │  │   Editor     │  │  Minimap   │ │ │
│  │  │   Manager    │  │    Pane      │  │   Chrome     │  │            │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.17.1-EDT: Editor Foundation

**Goal:** Establish the foundational editor infrastructure with split view capabilities, live preview, enhanced rendering, and professional editor chrome (line numbers, minimap, breadcrumbs).

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.17.1e | Split View Manager | 12 |
| v0.17.1f | Markdown Preview Pane | 12 |
| v0.17.1g | Enhanced Syntax Highlighting | 10 |
| v0.17.1h | Editor Chrome (Line Numbers, Minimap) | 10 |
| v0.17.1i | Breadcrumb Navigation | 6 |
| v0.17.1j | Editor Settings & Themes | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Manages the editor layout with split views and panels.
/// </summary>
public interface ISplitViewManager
{
    /// <summary>
    /// Current layout configuration.
    /// </summary>
    SplitViewLayout Layout { get; }

    /// <summary>
    /// Split the editor view.
    /// </summary>
    Task<EditorPane> SplitAsync(
        SplitDirection direction,
        float ratio = 0.5f,
        CancellationToken ct = default);

    /// <summary>
    /// Close a specific pane.
    /// </summary>
    Task ClosePaneAsync(
        PaneId paneId,
        CancellationToken ct = default);

    /// <summary>
    /// Toggle preview pane visibility.
    /// </summary>
    Task TogglePreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the layout mode.
    /// </summary>
    Task SetLayoutAsync(
        SplitViewMode mode,
        CancellationToken ct = default);

    /// <summary>
    /// Resize panes.
    /// </summary>
    Task ResizePanesAsync(
        float ratio,
        CancellationToken ct = default);

    /// <summary>
    /// Swap pane positions.
    /// </summary>
    Task SwapPanesAsync(CancellationToken ct = default);

    /// <summary>
    /// Observable stream of layout changes.
    /// </summary>
    IObservable<SplitViewLayoutChanged> LayoutChanges { get; }
}

public enum SplitDirection
{
    Horizontal,  // Side by side
    Vertical     // Top and bottom
}

public enum SplitViewMode
{
    EditorOnly,          // Full editor, no preview
    PreviewOnly,         // Full preview, no editor
    SplitHorizontal,     // Editor | Preview side by side
    SplitVertical,       // Editor on top, Preview below
    SyncScroll           // Split with synchronized scrolling
}

public record SplitViewLayout
{
    public SplitViewMode Mode { get; init; }
    public IReadOnlyList<EditorPane> Panes { get; init; } = [];
    public float SplitRatio { get; init; } = 0.5f;
    public bool PreviewVisible { get; init; }
    public bool SyncScrollEnabled { get; init; }
}

public record EditorPane
{
    public PaneId Id { get; init; }
    public PaneType Type { get; init; }
    public float WidthRatio { get; init; }
    public float HeightRatio { get; init; }
    public bool IsFocused { get; init; }
    public DocumentId? DocumentId { get; init; }
    public EditorViewState? ViewState { get; init; }
}

public enum PaneType
{
    Editor,
    Preview,
    Diff,
    Outline,
    Terminal
}

public readonly record struct PaneId(Guid Value)
{
    public static PaneId New() => new(Guid.NewGuid());
}

/// <summary>
/// Renders Markdown content as live preview.
/// </summary>
public interface IMarkdownPreviewRenderer
{
    /// <summary>
    /// Render Markdown to HTML for preview.
    /// </summary>
    Task<RenderedPreview> RenderAsync(
        string markdown,
        PreviewRenderOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Render incrementally for performance.
    /// </summary>
    Task<RenderedPreview> RenderIncrementalAsync(
        string markdown,
        TextChange change,
        RenderedPreview previousRender,
        CancellationToken ct = default);

    /// <summary>
    /// Get scroll position mapping between source and preview.
    /// </summary>
    Task<ScrollMapping> GetScrollMappingAsync(
        string markdown,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of render updates.
    /// </summary>
    IObservable<PreviewUpdate> Updates { get; }
}

public record PreviewRenderOptions
{
    public bool SyntaxHighlighting { get; init; } = true;
    public bool MathRendering { get; init; } = true;      // LaTeX/KaTeX
    public bool DiagramRendering { get; init; } = true;   // Mermaid
    public bool EmojiRendering { get; init; } = true;
    public bool TaskListRendering { get; init; } = true;
    public bool TableOfContents { get; init; } = true;
    public bool FootnoteRendering { get; init; } = true;
    public string? CustomCss { get; init; }
    public PreviewTheme Theme { get; init; } = PreviewTheme.Auto;
}

public enum PreviewTheme
{
    Auto,       // Match editor theme
    Light,
    Dark,
    Sepia,
    HighContrast
}

public record RenderedPreview
{
    public required string Html { get; init; }
    public required string SourceHash { get; init; }
    public IReadOnlyList<PreviewSection> Sections { get; init; } = [];
    public IReadOnlyList<PreviewLink> Links { get; init; } = [];
    public IReadOnlyList<PreviewImage> Images { get; init; } = [];
    public IReadOnlyList<PreviewHeading> Headings { get; init; } = [];
    public TimeSpan RenderTime { get; init; }
}

public record PreviewSection
{
    public required int SourceStartLine { get; init; }
    public required int SourceEndLine { get; init; }
    public required string HtmlElementId { get; init; }
}

public record ScrollMapping
{
    public IReadOnlyList<ScrollAnchor> Anchors { get; init; } = [];
}

public record ScrollAnchor
{
    public int SourceLine { get; init; }
    public float PreviewScrollPercent { get; init; }
    public string? ElementId { get; init; }
}

/// <summary>
/// Enhanced syntax highlighting for multiple languages.
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>
    /// Get syntax tokens for highlighting.
    /// </summary>
    Task<IReadOnlyList<SyntaxToken>> TokenizeAsync(
        string text,
        string languageId,
        CancellationToken ct = default);

    /// <summary>
    /// Get supported languages.
    /// </summary>
    Task<IReadOnlyList<LanguageDefinition>> GetLanguagesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Detect language from content.
    /// </summary>
    Task<LanguageDetection> DetectLanguageAsync(
        string text,
        string? filename = null,
        CancellationToken ct = default);

    /// <summary>
    /// Register a custom language definition.
    /// </summary>
    Task RegisterLanguageAsync(
        LanguageDefinition language,
        CancellationToken ct = default);
}

public record SyntaxToken
{
    public required int StartOffset { get; init; }
    public required int Length { get; init; }
    public required SyntaxTokenType Type { get; init; }
    public IReadOnlyList<string>? Modifiers { get; init; }
    public string? Scope { get; init; }  // TextMate scope
}

public enum SyntaxTokenType
{
    // Basic
    Comment,
    String,
    Number,
    Keyword,
    Operator,
    Punctuation,

    // Identifiers
    Variable,
    Function,
    Class,
    Interface,
    Namespace,
    Property,
    Parameter,

    // Markdown specific
    Heading,
    Bold,
    Italic,
    Link,
    Image,
    Code,
    CodeBlock,
    Quote,
    List,
    Table,

    // Special
    Error,
    Warning,
    Info,
    Deprecated
}

public record LanguageDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Extensions { get; init; } = [];
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public string? MimeType { get; init; }
    public string? Grammar { get; init; }  // TextMate grammar JSON
}

/// <summary>
/// Editor chrome components (line numbers, minimap, etc.).
/// </summary>
public interface IEditorChrome
{
    /// <summary>
    /// Current chrome configuration.
    /// </summary>
    EditorChromeConfig Config { get; }

    /// <summary>
    /// Update chrome configuration.
    /// </summary>
    Task UpdateConfigAsync(
        EditorChromeConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Add a gutter decoration.
    /// </summary>
    Task<GutterDecoration> AddGutterDecorationAsync(
        int line,
        GutterDecorationType type,
        GutterDecorationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a gutter decoration.
    /// </summary>
    Task RemoveGutterDecorationAsync(
        Guid decorationId,
        CancellationToken ct = default);

    /// <summary>
    /// Set line highlight.
    /// </summary>
    Task SetLineHighlightAsync(
        int line,
        LineHighlightType type,
        CancellationToken ct = default);

    /// <summary>
    /// Get minimap data.
    /// </summary>
    Task<MinimapData> GetMinimapDataAsync(CancellationToken ct = default);
}

public record EditorChromeConfig
{
    // Line numbers
    public bool ShowLineNumbers { get; init; } = true;
    public LineNumberStyle LineNumberStyle { get; init; } = LineNumberStyle.Absolute;

    // Minimap
    public bool ShowMinimap { get; init; } = true;
    public MinimapPosition MinimapPosition { get; init; } = MinimapPosition.Right;
    public float MinimapScale { get; init; } = 0.1f;
    public bool MinimapRenderCharacters { get; init; } = true;

    // Gutter
    public bool ShowFoldingControls { get; init; } = true;
    public bool ShowGlyphMargin { get; init; } = true;
    public int GutterWidth { get; init; } = 60;

    // Scrollbar
    public bool ShowScrollbar { get; init; } = true;
    public ScrollbarStyle ScrollbarStyle { get; init; } = ScrollbarStyle.Overlay;

    // Other
    public bool ShowBreadcrumbs { get; init; } = true;
    public bool ShowIndentGuides { get; init; } = true;
    public bool HighlightCurrentLine { get; init; } = true;
    public bool ShowWhitespace { get; init; } = false;
    public bool ShowRulers { get; init; } = false;
    public IReadOnlyList<int>? RulerColumns { get; init; }
}

public enum LineNumberStyle
{
    Absolute,   // 1, 2, 3, 4...
    Relative,   // Relative to cursor
    Interval    // Every 5 or 10 lines
}

public enum MinimapPosition { Left, Right }

public enum ScrollbarStyle { Visible, Overlay, Hidden }

public record GutterDecoration
{
    public Guid Id { get; init; }
    public int Line { get; init; }
    public GutterDecorationType Type { get; init; }
    public string? Tooltip { get; init; }
    public string? IconPath { get; init; }
    public string? Color { get; init; }
    public Action? OnClick { get; init; }
}

public enum GutterDecorationType
{
    Breakpoint,
    Bookmark,
    Error,
    Warning,
    Info,
    Hint,
    FoldOpen,
    FoldClosed,
    Diff,
    GitChange,
    AISuggestion,
    Comment,
    Custom
}

public enum LineHighlightType
{
    None,
    CurrentLine,
    Selection,
    Search,
    Error,
    Warning,
    Added,
    Removed,
    Modified
}

/// <summary>
/// Breadcrumb navigation within documents.
/// </summary>
public interface IBreadcrumbNavigation
{
    /// <summary>
    /// Get breadcrumb trail for current position.
    /// </summary>
    Task<BreadcrumbTrail> GetBreadcrumbsAsync(
        DocumentId documentId,
        TextPosition position,
        CancellationToken ct = default);

    /// <summary>
    /// Navigate to a breadcrumb.
    /// </summary>
    Task NavigateToAsync(
        BreadcrumbItem item,
        CancellationToken ct = default);

    /// <summary>
    /// Get children of a breadcrumb item (for dropdown).
    /// </summary>
    Task<IReadOnlyList<BreadcrumbItem>> GetChildrenAsync(
        BreadcrumbItem parent,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of breadcrumb updates.
    /// </summary>
    IObservable<BreadcrumbTrail> BreadcrumbUpdates { get; }
}

public record BreadcrumbTrail
{
    public IReadOnlyList<BreadcrumbItem> Items { get; init; } = [];
}

public record BreadcrumbItem
{
    public required string Label { get; init; }
    public required BreadcrumbItemType Type { get; init; }
    public TextRange? Range { get; init; }
    public string? IconPath { get; init; }
    public bool HasChildren { get; init; }
    public object? Data { get; init; }
}

public enum BreadcrumbItemType
{
    File,
    Folder,
    Section,        // Markdown heading
    CodeBlock,
    Function,
    Class,
    Namespace,
    Symbol
}

/// <summary>
/// Editor settings and theming.
/// </summary>
public interface IEditorSettings
{
    /// <summary>
    /// Get current editor settings.
    /// </summary>
    Task<EditorConfig> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Update editor settings.
    /// </summary>
    Task UpdateConfigAsync(
        EditorConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Get available themes.
    /// </summary>
    Task<IReadOnlyList<EditorTheme>> GetThemesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Apply a theme.
    /// </summary>
    Task ApplyThemeAsync(
        string themeId,
        CancellationToken ct = default);

    /// <summary>
    /// Import a VS Code theme.
    /// </summary>
    Task<EditorTheme> ImportVSCodeThemeAsync(
        string themeJson,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of settings changes.
    /// </summary>
    IObservable<EditorConfig> ConfigChanges { get; }
}

public record EditorConfig
{
    // Font
    public string FontFamily { get; init; } = "JetBrains Mono, Consolas, monospace";
    public int FontSize { get; init; } = 14;
    public int LineHeight { get; init; } = 22;
    public FontWeight FontWeight { get; init; } = FontWeight.Normal;
    public bool FontLigatures { get; init; } = true;

    // Editing
    public int TabSize { get; init; } = 4;
    public bool InsertSpaces { get; init; } = true;
    public bool WordWrap { get; init; } = true;
    public int WordWrapColumn { get; init; } = 80;
    public bool AutoIndent { get; init; } = true;
    public bool AutoClosingBrackets { get; init; } = true;
    public bool AutoClosingQuotes { get; init; } = true;
    public bool AutoSurround { get; init; } = true;

    // Cursor
    public CursorStyle CursorStyle { get; init; } = CursorStyle.Line;
    public CursorBlinking CursorBlinking { get; init; } = CursorBlinking.Blink;
    public bool SmoothCaretAnimation { get; init; } = true;

    // Scrolling
    public bool SmoothScrolling { get; init; } = true;
    public int ScrollBeyondLastLine { get; init; } = 5;
    public bool ScrollPastEnd { get; init; } = false;

    // Theme
    public string ThemeId { get; init; } = "lexichord-dark";

    // Chrome (references EditorChromeConfig)
    public EditorChromeConfig Chrome { get; init; } = new();
}

public enum CursorStyle { Line, Block, Underline }
public enum CursorBlinking { Blink, Smooth, Phase, Expand, Solid }
public enum FontWeight { Thin, Light, Normal, Medium, Bold }

public record EditorTheme
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public ThemeType Type { get; init; }
    public required ThemeColors Colors { get; init; }
    public IReadOnlyList<TokenColorRule> TokenColors { get; init; } = [];
    public string? Description { get; init; }
    public string? Author { get; init; }
}

public enum ThemeType { Light, Dark, HighContrast }

public record ThemeColors
{
    // Editor
    public required string EditorBackground { get; init; }
    public required string EditorForeground { get; init; }
    public string? EditorLineHighlight { get; init; }
    public string? EditorSelection { get; init; }
    public string? EditorCursor { get; init; }

    // Gutter
    public string? GutterBackground { get; init; }
    public string? LineNumberForeground { get; init; }
    public string? LineNumberActiveForeground { get; init; }

    // Minimap
    public string? MinimapBackground { get; init; }
    public string? MinimapSliderBackground { get; init; }

    // Scrollbar
    public string? ScrollbarSlider { get; init; }
    public string? ScrollbarSliderHover { get; init; }
}

public record TokenColorRule
{
    public required string Scope { get; init; }  // TextMate scope selector
    public string? Foreground { get; init; }
    public string? Background { get; init; }
    public FontStyle? FontStyle { get; init; }
}

public enum FontStyle { Normal, Italic, Bold, Underline }
```

### Editor Foundation UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│ 📁 docs / 📄 README.md / # Getting Started                    [Split ▾]   │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─ Editor ──────────────────────────┐ ┌─ Preview ─────────────────────┐  │
│  │   1 │ # Getting Started           │ │                               │  │
│  │   2 │                             │ │  Getting Started              │  │
│  │   3 │ Welcome to **Lexichord**,   │ │  ═══════════════              │  │
│  │   4 │ the intelligent writing     │ │                               │  │
│  │   5 │ platform.                   │ │  Welcome to Lexichord, the    │  │
│  │   6 │                             │ │  intelligent writing platform.│  │
│  │   7 │ ## Installation             │ │                               │  │
│  │   8 │                             │ │  Installation                 │  │
│  │   9 │ ```bash                     │ │  ────────────                 │  │
│  │  10 │ npm install lexichord       │ │                               │  │
│  │  11 │ ```                         │ │  ┌──────────────────────────┐ │  │
│  │  12 │                             │ │  │ npm install lexichord    │ │  │
│  │  13 │ ## Features                 │ │  └──────────────────────────┘ │  │
│  │  14 │                             │ │                               │  │
│  │  15 │ - [x] AI-powered editing    │ │  Features                     │  │
│  │  16 │ - [x] Real-time preview     │ │  ────────                     │  │
│  │  17 │ - [ ] Collaboration         │ │                               │  │
│  │░░░░░│░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│ │  ☑ AI-powered editing        │  │
│  │░░░░░│░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│ │  ☑ Real-time preview         │  │
│  │░░░░░│░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│ │  ☐ Collaboration             │  │
│  │ ▓▓▓ │ ← Minimap                   │ │                               │  │
│  └─────┴─────────────────────────────┘ └───────────────────────────────┘  │
│                                                                             │
│  Ln 7, Col 4 │ Markdown │ UTF-8 │ LF │ Words: 45 │ Reading: 1 min          │
└────────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic editor only |
| WriterPro | Split view; preview; syntax highlighting |
| Teams | + Custom themes; all chrome options |
| Enterprise | + Theme import; white-label theming |

---

## v0.17.2-EDT: Rich Formatting

**Goal:** Provide a comprehensive formatting toolbar with buttons, keyboard shortcuts, and context menus that apply Markdown syntax intelligently, plus auto-formatting capabilities.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.17.2e | Formatting Toolbar | 12 |
| v0.17.2f | Keyboard Shortcut System | 10 |
| v0.17.2g | Context Menu System | 10 |
| v0.17.2h | Format Painter | 8 |
| v0.17.2i | Auto-Formatting Engine | 8 |
| v0.17.2j | Insert Wizards (Table, Link, Image) | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Formatting toolbar for applying text formatting.
/// </summary>
public interface IFormattingToolbar
{
    /// <summary>
    /// Get available formatting actions.
    /// </summary>
    Task<IReadOnlyList<FormattingAction>> GetActionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Execute a formatting action.
    /// </summary>
    Task ExecuteActionAsync(
        FormattingActionId actionId,
        FormattingContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Get current formatting state at cursor position.
    /// </summary>
    Task<FormattingState> GetStateAsync(
        TextPosition position,
        TextSelection? selection,
        CancellationToken ct = default);

    /// <summary>
    /// Toggle formatting on selection.
    /// </summary>
    Task ToggleFormattingAsync(
        FormattingType type,
        TextSelection selection,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of formatting state changes.
    /// </summary>
    IObservable<FormattingState> StateChanges { get; }
}

public record FormattingAction
{
    public required FormattingActionId Id { get; init; }
    public required string Label { get; init; }
    public required string IconPath { get; init; }
    public string? Tooltip { get; init; }
    public FormattingType Type { get; init; }
    public KeyboardShortcut? Shortcut { get; init; }
    public bool IsToggle { get; init; }
    public FormattingGroup Group { get; init; }
    public int Order { get; init; }
}

public readonly record struct FormattingActionId(string Value);

public enum FormattingType
{
    // Inline
    Bold,
    Italic,
    Strikethrough,
    InlineCode,
    Highlight,
    Subscript,
    Superscript,
    Link,

    // Block
    Heading1,
    Heading2,
    Heading3,
    Heading4,
    Heading5,
    Heading6,
    Paragraph,
    Quote,
    CodeBlock,

    // Lists
    BulletList,
    NumberedList,
    TaskList,
    IndentIncrease,
    IndentDecrease,

    // Other
    HorizontalRule,
    Table,
    Image,
    Footnote,

    // Special
    ClearFormatting,
    FormatPainter
}

public enum FormattingGroup
{
    Text,       // Bold, italic, etc.
    Paragraph,  // Headings, alignment
    Lists,      // Bullet, numbered
    Insert,     // Link, image, table
    Tools       // Clear formatting, etc.
}

public record FormattingState
{
    public bool IsBold { get; init; }
    public bool IsItalic { get; init; }
    public bool IsStrikethrough { get; init; }
    public bool IsCode { get; init; }
    public bool IsLink { get; init; }
    public HeadingLevel? HeadingLevel { get; init; }
    public bool IsQuote { get; init; }
    public bool IsCodeBlock { get; init; }
    public string? CodeBlockLanguage { get; init; }
    public ListType? ListType { get; init; }
    public int IndentLevel { get; init; }
    public bool IsTaskItem { get; init; }
    public bool? TaskChecked { get; init; }
}

public enum HeadingLevel { H1 = 1, H2, H3, H4, H5, H6 }
public enum ListType { Bullet, Numbered, Task }

public record FormattingContext
{
    public required DocumentId DocumentId { get; init; }
    public TextSelection? Selection { get; init; }
    public TextPosition CursorPosition { get; init; }
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Keyboard shortcut system for editor commands.
/// </summary>
public interface IKeyboardShortcuts
{
    /// <summary>
    /// Get all registered shortcuts.
    /// </summary>
    Task<IReadOnlyList<KeyboardShortcut>> GetShortcutsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Register a new shortcut.
    /// </summary>
    Task RegisterAsync(
        KeyboardShortcut shortcut,
        CancellationToken ct = default);

    /// <summary>
    /// Unregister a shortcut.
    /// </summary>
    Task UnregisterAsync(
        string commandId,
        CancellationToken ct = default);

    /// <summary>
    /// Handle a key event.
    /// </summary>
    Task<ShortcutHandleResult> HandleKeyAsync(
        KeyEvent keyEvent,
        CancellationToken ct = default);

    /// <summary>
    /// Get shortcut conflicts.
    /// </summary>
    Task<IReadOnlyList<ShortcutConflict>> GetConflictsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Reset to default shortcuts.
    /// </summary>
    Task ResetToDefaultsAsync(CancellationToken ct = default);
}

public record KeyboardShortcut
{
    public required string CommandId { get; init; }
    public required KeyBinding KeyBinding { get; init; }
    public string? When { get; init; }  // Context condition
    public bool IsDefault { get; init; }
    public string? Description { get; init; }
}

public record KeyBinding
{
    public required KeyCode Key { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    public bool Meta { get; init; }  // Cmd on Mac

    public string ToDisplayString()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Meta) parts.Add("Cmd");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

public enum KeyCode
{
    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Special
    Enter, Escape, Tab, Space, Backspace, Delete,
    Up, Down, Left, Right, Home, End, PageUp, PageDown,

    // Punctuation
    Minus, Equal, BracketLeft, BracketRight,
    Semicolon, Quote, Backquote, Backslash, Comma, Period, Slash
}

/// <summary>
/// Context menu system for right-click actions.
/// </summary>
public interface IContextMenuSystem
{
    /// <summary>
    /// Get context menu for current context.
    /// </summary>
    Task<ContextMenu> GetMenuAsync(
        ContextMenuContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Register a context menu provider.
    /// </summary>
    Task RegisterProviderAsync(
        IContextMenuProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a context menu action.
    /// </summary>
    Task ExecuteActionAsync(
        ContextMenuActionId actionId,
        ContextMenuContext context,
        CancellationToken ct = default);
}

public record ContextMenu
{
    public IReadOnlyList<ContextMenuItem> Items { get; init; } = [];
}

public record ContextMenuItem
{
    public ContextMenuActionId? ActionId { get; init; }
    public string? Label { get; init; }
    public string? IconPath { get; init; }
    public KeyboardShortcut? Shortcut { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool IsSeparator { get; init; }
    public IReadOnlyList<ContextMenuItem>? Submenu { get; init; }
    public ContextMenuGroup Group { get; init; }
}

public readonly record struct ContextMenuActionId(string Value);

public enum ContextMenuGroup
{
    Edit,           // Cut, copy, paste
    Selection,      // Select all, select word
    Formatting,     // Bold, italic, etc.
    Insert,         // Link, image, table
    AI,             // AI actions
    Navigation,     // Go to definition, etc.
    Refactor,       // Rename, extract
    Debug           // Breakpoint, watch
}

public record ContextMenuContext
{
    public required DocumentId DocumentId { get; init; }
    public required TextPosition Position { get; init; }
    public TextSelection? Selection { get; init; }
    public string? Word { get; init; }
    public string? Line { get; init; }
    public SyntaxToken? Token { get; init; }
}

/// <summary>
/// Format painter for copying and applying formatting.
/// </summary>
public interface IFormatPainter
{
    /// <summary>
    /// Whether format painter is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Currently copied format.
    /// </summary>
    CopiedFormat? CopiedFormat { get; }

    /// <summary>
    /// Copy formatting from selection.
    /// </summary>
    Task CopyFormatAsync(
        TextSelection selection,
        CancellationToken ct = default);

    /// <summary>
    /// Apply copied formatting to selection.
    /// </summary>
    Task ApplyFormatAsync(
        TextSelection selection,
        CancellationToken ct = default);

    /// <summary>
    /// Toggle format painter lock (apply multiple times).
    /// </summary>
    Task ToggleLockAsync(CancellationToken ct = default);

    /// <summary>
    /// Clear format painter state.
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Observable stream of format painter state changes.
    /// </summary>
    IObservable<FormatPainterState> StateChanges { get; }
}

public record CopiedFormat
{
    public FormattingState State { get; init; } = new();
    public string? SourceText { get; init; }
    public DateTimeOffset CopiedAt { get; init; }
}

public record FormatPainterState
{
    public bool IsActive { get; init; }
    public bool IsLocked { get; init; }
    public CopiedFormat? Format { get; init; }
}

/// <summary>
/// Auto-formatting engine for automatic text formatting.
/// </summary>
public interface IAutoFormatter
{
    /// <summary>
    /// Get auto-format settings.
    /// </summary>
    Task<AutoFormatSettings> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Update auto-format settings.
    /// </summary>
    Task UpdateSettingsAsync(
        AutoFormatSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Process text change for auto-formatting.
    /// </summary>
    Task<AutoFormatResult?> ProcessChangeAsync(
        TextChange change,
        AutoFormatContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Format entire document.
    /// </summary>
    Task<FormattedDocument> FormatDocumentAsync(
        DocumentId documentId,
        FormatDocumentOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Format selection.
    /// </summary>
    Task<FormattedRange> FormatSelectionAsync(
        DocumentId documentId,
        TextSelection selection,
        CancellationToken ct = default);
}

public record AutoFormatSettings
{
    // On type
    public bool FormatOnType { get; init; } = true;
    public bool AutoCloseBrackets { get; init; } = true;
    public bool AutoCloseQuotes { get; init; } = true;
    public bool AutoCloseMarkdown { get; init; } = true;  // **, __, ``
    public bool AutoIndent { get; init; } = true;
    public bool AutoList { get; init; } = true;  // Continue lists

    // Transformations
    public bool ConvertSmartQuotes { get; init; } = false;  // " → ""
    public bool ConvertDashes { get; init; } = false;       // -- → —
    public bool ConvertEllipsis { get; init; } = false;     // ... → …
    public bool ConvertFractions { get; init; } = false;    // 1/2 → ½
    public bool AutoLinkUrls { get; init; } = true;         // URL → [URL](URL)

    // Cleanup
    public bool TrimTrailingWhitespace { get; init; } = true;
    public bool EnsureFinalNewline { get; init; } = true;
    public bool NormalizeIndentation { get; init; } = true;

    // Table
    public bool AutoFormatTables { get; init; } = true;
    public bool AutoAlignTableColumns { get; init; } = true;
}

public record AutoFormatResult
{
    public required TextEdit Edit { get; init; }
    public AutoFormatType Type { get; init; }
    public string? Description { get; init; }
}

public enum AutoFormatType
{
    AutoClose,
    AutoIndent,
    AutoList,
    SmartQuotes,
    UrlToLink,
    TableFormat,
    WhitespaceTrim,
    Other
}

/// <summary>
/// Insert wizards for complex elements.
/// </summary>
public interface IInsertWizards
{
    /// <summary>
    /// Show link insert wizard.
    /// </summary>
    Task<LinkInsertResult?> InsertLinkAsync(
        LinkInsertOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Show image insert wizard.
    /// </summary>
    Task<ImageInsertResult?> InsertImageAsync(
        ImageInsertOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Show table insert wizard.
    /// </summary>
    Task<TableInsertResult?> InsertTableAsync(
        TableInsertOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Show code block insert wizard.
    /// </summary>
    Task<CodeBlockInsertResult?> InsertCodeBlockAsync(
        CodeBlockInsertOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Show emoji picker.
    /// </summary>
    Task<string?> PickEmojiAsync(CancellationToken ct = default);
}

public record LinkInsertOptions
{
    public string? InitialUrl { get; init; }
    public string? InitialText { get; init; }
    public string? InitialTitle { get; init; }
    public bool AllowSearch { get; init; } = true;
    public bool AllowKnowledgeBase { get; init; } = true;
}

public record LinkInsertResult
{
    public required string Url { get; init; }
    public string? Text { get; init; }
    public string? Title { get; init; }
    public string ToMarkdown() =>
        string.IsNullOrEmpty(Title)
            ? $"[{Text ?? Url}]({Url})"
            : $"[{Text ?? Url}]({Url} \"{Title}\")";
}

public record TableInsertOptions
{
    public int InitialRows { get; init; } = 3;
    public int InitialColumns { get; init; } = 3;
    public bool IncludeHeader { get; init; } = true;
    public TableAlignment DefaultAlignment { get; init; } = TableAlignment.Left;
}

public record TableInsertResult
{
    public required int Rows { get; init; }
    public required int Columns { get; init; }
    public bool HasHeader { get; init; }
    public IReadOnlyList<TableAlignment>? ColumnAlignments { get; init; }
    public IReadOnlyList<IReadOnlyList<string>>? InitialData { get; init; }

    public string ToMarkdown()
    {
        // Generate Markdown table string
        var sb = new StringBuilder();
        // ... implementation
        return sb.ToString();
    }
}

public enum TableAlignment { Left, Center, Right }
```

### Formatting Toolbar UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────────────────┐   │
│ │ [B] [I] [S] [C] │ [H1▾] [¶] │ [•] [1.] [☐] │ [🔗] [🖼] [📊] │ [🎨] [✨] │   │
│ └──────────────────────────────────────────────────────────────────────┘   │
│ ↑ Text formatting  ↑ Headings  ↑ Lists         ↑ Insert     ↑ Tools       │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  When text is selected, buttons show active state:                          │
│                                                                             │
│  Selected: "**Hello World**"                                                │
│  Toolbar:  [B̲] [I] [S] [C] │ ...   ← Bold button is highlighted            │
│                                                                             │
│  Keyboard shortcuts shown in tooltips:                                      │
│  [B] → "Bold (Ctrl+B)"                                                      │
│  [I] → "Italic (Ctrl+I)"                                                    │
│  [🔗] → "Insert Link (Ctrl+K)"                                              │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘

Heading Dropdown:
┌──────────────────┐
│ ¶  Normal text   │
│ H1 Heading 1     │
│ H2 Heading 2     │
│ H3 Heading 3     │
│ H4 Heading 4     │
│ ─────────────────│
│ "  Quote         │
│ <> Code Block    │
└──────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic keyboard shortcuts only |
| WriterPro | Formatting toolbar; context menus |
| Teams | + Auto-formatting; format painter |
| Enterprise | + Custom shortcuts; custom toolbar |

---

## v0.17.3-EDT: Change Tracking

**Goal:** Implement professional change tracking with diff views, version history, track changes mode (like Word/Google Docs), and rich annotations.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.17.3e | Diff Engine | 12 |
| v0.17.3f | Version History | 12 |
| v0.17.3g | Track Changes Mode | 12 |
| v0.17.3h | Annotation System | 10 |
| v0.17.3i | Change Review UI | 6 |
| v0.17.3j | Merge & Conflict Resolution | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Diff engine for comparing document versions.
/// </summary>
public interface IDiffEngine
{
    /// <summary>
    /// Compute diff between two texts.
    /// </summary>
    Task<DiffResult> ComputeDiffAsync(
        string oldText,
        string newText,
        DiffOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Compute diff between document versions.
    /// </summary>
    Task<DiffResult> ComputeVersionDiffAsync(
        DocumentId documentId,
        VersionId oldVersion,
        VersionId newVersion,
        DiffOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get inline diff for display.
    /// </summary>
    Task<InlineDiff> GetInlineDiffAsync(
        DiffResult diff,
        CancellationToken ct = default);

    /// <summary>
    /// Get side-by-side diff for display.
    /// </summary>
    Task<SideBySideDiff> GetSideBySideDiffAsync(
        DiffResult diff,
        CancellationToken ct = default);
}

public record DiffOptions
{
    public DiffGranularity Granularity { get; init; } = DiffGranularity.Word;
    public bool IgnoreWhitespace { get; init; } = false;
    public bool IgnoreCase { get; init; } = false;
    public int ContextLines { get; init; } = 3;
    public bool DetectMoves { get; init; } = true;
    public bool SemanticCleanup { get; init; } = true;
}

public enum DiffGranularity
{
    Character,
    Word,
    Line,
    Sentence,
    Paragraph
}

public record DiffResult
{
    public required string OldText { get; init; }
    public required string NewText { get; init; }
    public IReadOnlyList<DiffHunk> Hunks { get; init; } = [];
    public DiffStatistics Statistics { get; init; } = new();
    public bool HasChanges => Hunks.Count > 0;
}

public record DiffHunk
{
    public required int OldStart { get; init; }
    public required int OldLength { get; init; }
    public required int NewStart { get; init; }
    public required int NewLength { get; init; }
    public IReadOnlyList<DiffLine> Lines { get; init; } = [];
}

public record DiffLine
{
    public required DiffLineType Type { get; init; }
    public required string Content { get; init; }
    public int? OldLineNumber { get; init; }
    public int? NewLineNumber { get; init; }
    public IReadOnlyList<DiffSpan>? InlineChanges { get; init; }
}

public enum DiffLineType
{
    Unchanged,
    Added,
    Removed,
    Modified
}

public record DiffSpan
{
    public required int Start { get; init; }
    public required int Length { get; init; }
    public required DiffSpanType Type { get; init; }
}

public enum DiffSpanType { Unchanged, Added, Removed }

public record DiffStatistics
{
    public int Additions { get; init; }
    public int Deletions { get; init; }
    public int Modifications { get; init; }
    public int MovedBlocks { get; init; }
    public float SimilarityPercent { get; init; }
}

/// <summary>
/// Version history for documents.
/// </summary>
public interface IVersionHistory
{
    /// <summary>
    /// Get version history for a document.
    /// </summary>
    Task<IReadOnlyList<DocumentVersion>> GetHistoryAsync(
        DocumentId documentId,
        VersionHistoryOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific version.
    /// </summary>
    Task<DocumentVersion?> GetVersionAsync(
        DocumentId documentId,
        VersionId versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Get content at a specific version.
    /// </summary>
    Task<string> GetContentAtVersionAsync(
        DocumentId documentId,
        VersionId versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Create a named checkpoint.
    /// </summary>
    Task<DocumentVersion> CreateCheckpointAsync(
        DocumentId documentId,
        string name,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Restore to a previous version.
    /// </summary>
    Task RestoreVersionAsync(
        DocumentId documentId,
        VersionId versionId,
        CancellationToken ct = default);

    /// <summary>
    /// Compare two versions.
    /// </summary>
    Task<DiffResult> CompareVersionsAsync(
        DocumentId documentId,
        VersionId fromVersion,
        VersionId toVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Purge old versions.
    /// </summary>
    Task<int> PurgeOldVersionsAsync(
        DocumentId documentId,
        VersionPurgePolicy policy,
        CancellationToken ct = default);
}

public record DocumentVersion
{
    public required VersionId Id { get; init; }
    public required DocumentId DocumentId { get; init; }
    public required int VersionNumber { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public VersionType Type { get; init; }
    public string? Name { get; init; }           // For checkpoints
    public string? Description { get; init; }
    public int ContentLength { get; init; }
    public string? ContentHash { get; init; }
    public VersionMetadata Metadata { get; init; } = new();
}

public readonly record struct VersionId(Guid Value)
{
    public static VersionId New() => new(Guid.NewGuid());
}

public enum VersionType
{
    Auto,           // Automatic save
    Manual,         // User-initiated save
    Checkpoint,     // Named checkpoint
    AIGenerated,    // Generated by AI
    Import,         // Imported content
    Merge           // Merged from conflict
}

public record VersionMetadata
{
    public int WordCount { get; init; }
    public int CharacterCount { get; init; }
    public int ParagraphCount { get; init; }
    public DiffStatistics? ChangeStats { get; init; }
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }
}

public record VersionPurgePolicy
{
    public TimeSpan? MaxAge { get; init; }
    public int? MaxVersions { get; init; }
    public bool KeepCheckpoints { get; init; } = true;
    public bool KeepDailySnapshots { get; init; } = true;
}

/// <summary>
/// Track changes mode for collaborative editing.
/// </summary>
public interface ITrackChangesManager
{
    /// <summary>
    /// Whether track changes is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enable track changes mode.
    /// </summary>
    Task EnableAsync(CancellationToken ct = default);

    /// <summary>
    /// Disable track changes mode.
    /// </summary>
    Task DisableAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all tracked changes.
    /// </summary>
    Task<IReadOnlyList<TrackedChange>> GetChangesAsync(
        DocumentId documentId,
        TrackedChangeFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Accept a change.
    /// </summary>
    Task AcceptChangeAsync(
        TrackedChangeId changeId,
        CancellationToken ct = default);

    /// <summary>
    /// Reject a change.
    /// </summary>
    Task RejectChangeAsync(
        TrackedChangeId changeId,
        CancellationToken ct = default);

    /// <summary>
    /// Accept all changes.
    /// </summary>
    Task AcceptAllChangesAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Reject all changes.
    /// </summary>
    Task RejectAllChangesAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Navigate to next/previous change.
    /// </summary>
    Task<TrackedChange?> NavigateToChangeAsync(
        DocumentId documentId,
        NavigationDirection direction,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of track changes events.
    /// </summary>
    IObservable<TrackChangesEvent> Events { get; }
}

public record TrackedChange
{
    public required TrackedChangeId Id { get; init; }
    public required DocumentId DocumentId { get; init; }
    public required TrackedChangeType Type { get; init; }
    public required TextRange Range { get; init; }
    public required string OriginalText { get; init; }
    public required string NewText { get; init; }
    public required string Author { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public TrackedChangeStatus Status { get; init; }
    public string? Comment { get; init; }
}

public readonly record struct TrackedChangeId(Guid Value)
{
    public static TrackedChangeId New() => new(Guid.NewGuid());
}

public enum TrackedChangeType
{
    Insertion,
    Deletion,
    Replacement,
    FormatChange,
    Move
}

public enum TrackedChangeStatus
{
    Pending,
    Accepted,
    Rejected
}

public enum NavigationDirection { Next, Previous, First, Last }

/// <summary>
/// Annotation system for comments and highlights.
/// </summary>
public interface IAnnotationSystem
{
    /// <summary>
    /// Add a comment annotation.
    /// </summary>
    Task<Annotation> AddCommentAsync(
        DocumentId documentId,
        TextRange range,
        string comment,
        CancellationToken ct = default);

    /// <summary>
    /// Add a highlight annotation.
    /// </summary>
    Task<Annotation> AddHighlightAsync(
        DocumentId documentId,
        TextRange range,
        HighlightColor color,
        CancellationToken ct = default);

    /// <summary>
    /// Get annotations for a document.
    /// </summary>
    Task<IReadOnlyList<Annotation>> GetAnnotationsAsync(
        DocumentId documentId,
        AnnotationFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reply to a comment.
    /// </summary>
    Task<AnnotationReply> AddReplyAsync(
        AnnotationId annotationId,
        string comment,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve a comment thread.
    /// </summary>
    Task ResolveAsync(
        AnnotationId annotationId,
        CancellationToken ct = default);

    /// <summary>
    /// Delete an annotation.
    /// </summary>
    Task DeleteAsync(
        AnnotationId annotationId,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of annotation changes.
    /// </summary>
    IObservable<AnnotationEvent> Events { get; }
}

public record Annotation
{
    public required AnnotationId Id { get; init; }
    public required DocumentId DocumentId { get; init; }
    public required AnnotationType Type { get; init; }
    public required TextRange Range { get; init; }
    public required string Author { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? Content { get; init; }
    public HighlightColor? Color { get; init; }
    public AnnotationStatus Status { get; init; }
    public IReadOnlyList<AnnotationReply> Replies { get; init; } = [];
}

public readonly record struct AnnotationId(Guid Value)
{
    public static AnnotationId New() => new(Guid.NewGuid());
}

public enum AnnotationType
{
    Comment,
    Highlight,
    Suggestion,     // AI suggestion
    Question,       // Question for AI
    Bookmark
}

public enum HighlightColor
{
    Yellow, Green, Blue, Pink, Purple, Orange
}

public enum AnnotationStatus
{
    Active, Resolved, Deleted
}

public record AnnotationReply
{
    public required Guid Id { get; init; }
    public required string Author { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public record TextRange
{
    public required TextPosition Start { get; init; }
    public required TextPosition End { get; init; }
    public int Length => End.Offset - Start.Offset;
}

public record TextPosition
{
    public required int Line { get; init; }
    public required int Column { get; init; }
    public required int Offset { get; init; }
}
```

### Change Tracking UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Track Changes: ON │ [Accept All] [Reject All] │ Changes: 5 │ [< Prev][Next >]│
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─ Editor ─────────────────────────────────────────────────────────────┐  │
│  │   1 │ # Introduction                                                  │  │
│  │   2 │                                                                 │  │
│  │   3 │ This document describes ̶t̶h̶e̶ ̶o̶l̶d̶ the new architecture...       │  │
│  │     │                         ▲                                       │  │
│  │     │                         └─ Deletion (strikethrough)             │  │
│  │   4 │                           ▲                                     │  │
│  │     │                           └─ Insertion (underlined green)       │  │
│  │   5 │                                                                 │  │
│  │   6 │ We have improved several key components:                        │  │
│  │   7 │                                                                 │  │
│  │   8 │ - The rendering engine                                          │  │
│  │   9 │ - [+] The caching layer    ← New line added                     │  │
│  │  10 │ - The API endpoints                                             │  │
│  │     │                                                                 │  │
│  └─────┴─────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─ Changes Panel ──────────────────────────────────────────────────────┐  │
│  │  ▼ Line 3: Text change                                    [✓] [✗]    │  │
│  │    "the old" → "the new"                                             │  │
│  │    by John Doe • 2 minutes ago                                       │  │
│  │                                                                       │  │
│  │  ▼ Line 9: Insertion                                      [✓] [✗]    │  │
│  │    Added "- The caching layer"                                       │  │
│  │    by AI Assistant • 5 minutes ago                                   │  │
│  │                                                                       │  │
│  │  ▼ Line 15: Comment                                       [Resolve]  │  │
│  │    "Should we add more detail here?"                                 │  │
│  │    by Jane Smith • 10 minutes ago                                    │  │
│  │    └─ Reply: "Yes, let's expand this section"                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘

Version History Panel:
┌─────────────────────────────────────────────────────────────────────────────┐
│ Version History                                              [Compare Mode] │
├─────────────────────────────────────────────────────────────────────────────┤
│ ● Current (unsaved)                                          [Restore]      │
│ │   +15 words, -3 words                                                     │
│ ├─● v12 - Auto-save                              Today, 2:30 PM             │
│ │   +8 words                                                                │
│ ├─● v11 - Auto-save                              Today, 2:15 PM             │
│ │   -2 words                                                                │
│ ├─★ Checkpoint: "Draft Complete"                 Today, 1:00 PM  [Restore]  │
│ │   Major revision milestone                                                │
│ ├─● v9 - AI Generated                            Today, 12:45 PM            │
│ │   +120 words (AI expansion)                                               │
│ ├─● v8 - Auto-save                               Today, 12:30 PM            │
│ │   ...                                                                     │
│ └─● v1 - Created                                 Yesterday, 4:00 PM         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic undo/redo only |
| WriterPro | Diff view; 7-day version history |
| Teams | Track changes; comments; unlimited history |
| Enterprise | + Merge resolution; audit trail |

---

## v0.17.4-EDT: Style & Rules

**Goal:** Implement a comprehensive formatting rules engine with linting, style guides, EditorConfig support, and automatic enforcement.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.17.4e | Formatting Rules Engine | 12 |
| v0.17.4f | Markdown Linter | 10 |
| v0.17.4g | Style Guide System | 10 |
| v0.17.4h | EditorConfig Support | 8 |
| v0.17.4i | Rule Violation Diagnostics | 6 |
| v0.17.4j | Style Settings UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Formatting rules engine for enforcing document style.
/// </summary>
public interface IFormattingRulesEngine
{
    /// <summary>
    /// Get active rules for a document.
    /// </summary>
    Task<IReadOnlyList<FormattingRule>> GetRulesAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Validate document against rules.
    /// </summary>
    Task<ValidationResult> ValidateAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Fix all auto-fixable violations.
    /// </summary>
    Task<FixResult> FixAllAsync(
        DocumentId documentId,
        FixOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Fix a specific violation.
    /// </summary>
    Task<TextEdit> FixViolationAsync(
        RuleViolation violation,
        CancellationToken ct = default);

    /// <summary>
    /// Enable/disable a rule.
    /// </summary>
    Task SetRuleEnabledAsync(
        string ruleId,
        bool enabled,
        RuleScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Configure a rule.
    /// </summary>
    Task ConfigureRuleAsync(
        string ruleId,
        RuleConfiguration config,
        RuleScope scope,
        CancellationToken ct = default);
}

public record FormattingRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required RuleCategory Category { get; init; }
    public required RuleSeverity DefaultSeverity { get; init; }
    public RuleSeverity ConfiguredSeverity { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool IsAutoFixable { get; init; }
    public string? Documentation { get; init; }
    public RuleConfiguration? Configuration { get; init; }
}

public enum RuleCategory
{
    Headings,
    Lists,
    Links,
    Images,
    CodeBlocks,
    Tables,
    Whitespace,
    LineLength,
    Punctuation,
    Consistency,
    Accessibility,
    BestPractices
}

public enum RuleSeverity
{
    Off,
    Hint,
    Info,
    Warning,
    Error
}

public enum RuleScope
{
    Document,   // This document only
    Workspace,  // All documents in workspace
    Global      // All documents
}

public record ValidationResult
{
    public IReadOnlyList<RuleViolation> Violations { get; init; } = [];
    public int ErrorCount => Violations.Count(v => v.Severity == RuleSeverity.Error);
    public int WarningCount => Violations.Count(v => v.Severity == RuleSeverity.Warning);
    public bool IsValid => ErrorCount == 0;
    public TimeSpan ValidationTime { get; init; }
}

public record RuleViolation
{
    public required string RuleId { get; init; }
    public required string RuleName { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required string Message { get; init; }
    public required TextRange Range { get; init; }
    public bool IsAutoFixable { get; init; }
    public string? FixDescription { get; init; }
    public IReadOnlyList<TextEdit>? SuggestedFixes { get; init; }
}

public record FixResult
{
    public int FixedCount { get; init; }
    public int SkippedCount { get; init; }
    public IReadOnlyList<TextEdit> Edits { get; init; } = [];
    public IReadOnlyList<RuleViolation> RemainingViolations { get; init; } = [];
}

/// <summary>
/// Markdown-specific linting rules.
/// </summary>
public interface IMarkdownLinter
{
    /// <summary>
    /// Lint a document.
    /// </summary>
    Task<LintResult> LintAsync(
        string content,
        LintOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get available lint rules.
    /// </summary>
    Task<IReadOnlyList<LintRule>> GetRulesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Configure linter.
    /// </summary>
    Task ConfigureAsync(
        LintConfiguration config,
        CancellationToken ct = default);
}

public record LintRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool IsEnabled { get; init; } = true;
    public RuleSeverity Severity { get; init; } = RuleSeverity.Warning;
}

// Example built-in rules:
public static class MarkdownRules
{
    // Headings
    public const string HeadingIncrement = "MD001";      // Heading levels should increment by 1
    public const string FirstHeadingH1 = "MD002";        // First heading should be H1
    public const string HeadingStyle = "MD003";          // Heading style (atx vs setext)
    public const string NoMultipleH1 = "MD025";          // Single H1 in document

    // Lists
    public const string ListIndent = "MD004";            // Consistent list indentation
    public const string ListMarkerStyle = "MD005";       // Consistent list marker style

    // Whitespace
    public const string NoTrailingSpaces = "MD009";      // No trailing spaces
    public const string NoHardTabs = "MD010";            // No hard tabs
    public const string BlankLinesAroundHeadings = "MD022";
    public const string BlankLinesAroundLists = "MD032";

    // Line length
    public const string LineLength = "MD013";            // Line length limit

    // Links
    public const string NoReversedLinks = "MD011";       // [text](url) not (text)[url]
    public const string NoBareUrls = "MD034";            // No bare URLs

    // Code
    public const string CodeFenceStyle = "MD046";        // Code fence style
    public const string CodeLanguage = "MD040";          // Fenced code should have language

    // Accessibility
    public const string ImageAltText = "MD045";          // Images should have alt text

    // Best practices
    public const string NoDuplicateHeadings = "MD024";   // No duplicate headings
    public const string NoEmptyLinks = "MD042";          // No empty links
}

/// <summary>
/// Style guide system for consistent writing.
/// </summary>
public interface IStyleGuideSystem
{
    /// <summary>
    /// Get available style guides.
    /// </summary>
    Task<IReadOnlyList<StyleGuide>> GetStyleGuidesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get active style guide.
    /// </summary>
    Task<StyleGuide?> GetActiveStyleGuideAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Set active style guide.
    /// </summary>
    Task SetActiveStyleGuideAsync(
        DocumentId documentId,
        string styleGuideId,
        CancellationToken ct = default);

    /// <summary>
    /// Create a custom style guide.
    /// </summary>
    Task<StyleGuide> CreateStyleGuideAsync(
        StyleGuideDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Check document against style guide.
    /// </summary>
    Task<StyleCheckResult> CheckStyleAsync(
        DocumentId documentId,
        CancellationToken ct = default);
}

public record StyleGuide
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsBuiltIn { get; init; }
    public StyleGuideRules Rules { get; init; } = new();
    public IReadOnlyDictionary<string, RuleSeverity> RuleSeverities { get; init; } =
        new Dictionary<string, RuleSeverity>();
}

public record StyleGuideRules
{
    // Document structure
    public bool RequireTitle { get; init; } = true;
    public HeadingLevel MaxHeadingDepth { get; init; } = HeadingLevel.H4;
    public bool AllowMultipleH1 { get; init; } = false;

    // Line and paragraph
    public int MaxLineLength { get; init; } = 120;
    public int MaxParagraphLength { get; init; } = 500;  // words
    public int MaxSentenceLength { get; init; } = 40;    // words

    // Formatting
    public BoldStyle BoldStyle { get; init; } = BoldStyle.Asterisks;
    public ItalicStyle ItalicStyle { get; init; } = ItalicStyle.Asterisks;
    public ListMarkerStyle UnorderedListStyle { get; init; } = ListMarkerStyle.Dash;
    public bool RequireLanguageInCodeBlocks { get; init; } = true;

    // Writing style
    public VoicePreference VoicePreference { get; init; } = VoicePreference.Active;
    public TensePreference TensePreference { get; init; } = TensePreference.Present;
    public bool AvoidPassiveVoice { get; init; } = true;
    public bool AvoidAdverbs { get; init; } = false;
    public int MaxReadingLevel { get; init; } = 12;  // Flesch-Kincaid grade

    // Terminology
    public IReadOnlyDictionary<string, string> TermReplacements { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyList<string> BannedWords { get; init; } = [];
}

public enum BoldStyle { Asterisks, Underscores }
public enum ItalicStyle { Asterisks, Underscores }
public enum ListMarkerStyle { Dash, Asterisk, Plus }
public enum VoicePreference { Any, Active, Passive }
public enum TensePreference { Any, Present, Past, Future }

// Built-in style guides
public static class BuiltInStyleGuides
{
    public const string Technical = "technical";         // Technical documentation
    public const string Academic = "academic";           // Academic writing
    public const string Blog = "blog";                   // Blog posts
    public const string Marketing = "marketing";         // Marketing copy
    public const string Legal = "legal";                 // Legal documents
    public const string Minimal = "minimal";             // Minimal rules
    public const string Strict = "strict";               // Strict enforcement
}

/// <summary>
/// EditorConfig support for cross-editor consistency.
/// </summary>
public interface IEditorConfigProvider
{
    /// <summary>
    /// Get EditorConfig settings for a file.
    /// </summary>
    Task<EditorConfigSettings> GetSettingsAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Create or update .editorconfig file.
    /// </summary>
    Task SaveConfigAsync(
        string directory,
        EditorConfigSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Parse an .editorconfig file.
    /// </summary>
    Task<EditorConfigFile> ParseFileAsync(
        string filePath,
        CancellationToken ct = default);
}

public record EditorConfigSettings
{
    public string? Charset { get; init; }               // utf-8, latin1, etc.
    public EndOfLine? EndOfLine { get; init; }          // lf, crlf
    public bool? InsertFinalNewline { get; init; }
    public bool? TrimTrailingWhitespace { get; init; }
    public IndentStyle? IndentStyle { get; init; }      // space, tab
    public int? IndentSize { get; init; }
    public int? TabWidth { get; init; }
    public int? MaxLineLength { get; init; }

    // Markdown-specific
    public int? MarkdownMaxLineLength { get; init; }
    public bool? MarkdownTrimTrailingWhitespace { get; init; }
}

public enum EndOfLine { Lf, Crlf, Cr }
public enum IndentStyle { Space, Tab }
```

### Diagnostics UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Style: Technical │ Errors: 2 │ Warnings: 5 │ [Fix All] [Configure...]      │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─ Editor ─────────────────────────────────────────────────────────────┐  │
│  │   1 │ # introduction                                                  │  │
│  │     │   ~~~~~~~~~ ⚠ Heading should be capitalized (MD023)             │  │
│  │   2 │                                                                 │  │
│  │   3 │ This is a very long line that exceeds the maximum line length   │  │
│  │     │ ──────────────────────────────────────────────────────────────  │  │
│  │     │ ⚠ Line exceeds 80 characters (MD013)                 [Fix]     │  │
│  │   4 │ that has been configured for this project.                      │  │
│  │   5 │                                                                 │  │
│  │   6 │ ![](image.png)                                                  │  │
│  │     │ ~~~~ ⛔ Image must have alt text (MD045)             [Add alt]  │  │
│  │   7 │                                                                 │  │
│  │   8 │ Visit https://example.com for more info.                        │  │
│  │     │       ~~~~~~~~~~~~~~~~~~~ ⚠ Use link syntax (MD034)  [Fix]     │  │
│  │   9 │                                                                 │  │
│  │  10 │ ``` javascript                                                  │  │
│  │     │ ⚠ Extra space in code fence (MD038)                  [Fix]     │  │
│  └─────┴─────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─ Problems Panel ─────────────────────────────────────────────────────┐  │
│  │ ⛔ Errors (2)                                                         │  │
│  │   Line 6: Image must have alt text for accessibility (MD045)          │  │
│  │   Line 15: Duplicate heading "Installation" (MD024)                   │  │
│  │                                                                       │  │
│  │ ⚠ Warnings (5)                                                        │  │
│  │   Line 1: Heading should be capitalized (MD023)                       │  │
│  │   Line 3: Line exceeds 80 characters - 95 chars (MD013)               │  │
│  │   Line 8: Use link syntax for URLs (MD034)                            │  │
│  │   Line 10: Remove extra space in code fence (MD038)                   │  │
│  │   Line 22: Sentence too long - 45 words (STYLE001)                    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic built-in rules only |
| WriterPro | All rules; auto-fix |
| Teams | + Style guides; EditorConfig |
| Enterprise | + Custom rules; org-wide policies |

---

## v0.17.5-EDT: AI Integration

**Goal:** Deeply integrate AI capabilities into the editor with inline suggestions, selection-based actions, comment-based prompting, ghost text completions, and intelligent assistance.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.17.5e | Selection AI Actions | 14 |
| v0.17.5f | Comment-Based AI Prompts | 12 |
| v0.17.5g | Ghost Text Completions | 12 |
| v0.17.5h | Inline AI Suggestions | 10 |
| v0.17.5i | AI Writing Assistant Panel | 10 |
| v0.17.5j | AI Action History & Undo | 8 |

### Key Interfaces

```csharp
/// <summary>
/// AI actions triggered by text selection.
/// </summary>
public interface ISelectionAIActions
{
    /// <summary>
    /// Get available AI actions for selection.
    /// </summary>
    Task<IReadOnlyList<SelectionAIAction>> GetActionsAsync(
        TextSelection selection,
        SelectionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Execute an AI action on selection.
    /// </summary>
    Task<AIActionResult> ExecuteActionAsync(
        SelectionAIActionId actionId,
        TextSelection selection,
        AIActionOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get quick action for selection (shown in floating button).
    /// </summary>
    Task<SelectionAIAction?> GetQuickActionAsync(
        TextSelection selection,
        CancellationToken ct = default);

    /// <summary>
    /// Chat about selection.
    /// </summary>
    IAsyncEnumerable<AIChatChunk> ChatAboutSelectionAsync(
        TextSelection selection,
        string userMessage,
        CancellationToken ct = default);
}

public record SelectionAIAction
{
    public required SelectionAIActionId Id { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required AIActionCategory Category { get; init; }
    public string? IconPath { get; init; }
    public KeyboardShortcut? Shortcut { get; init; }
    public bool RequiresConfirmation { get; init; }
    public bool SupportsStreaming { get; init; }
    public int Order { get; init; }
}

public readonly record struct SelectionAIActionId(string Value);

public enum AIActionCategory
{
    Improve,        // Improve writing quality
    Transform,      // Change format or structure
    Generate,       // Generate new content
    Analyze,        // Analyze content
    Explain,        // Explain or summarize
    Translate,      // Language translation
    Fix,            // Fix errors
    Custom          // Custom user action
}

// Built-in selection actions
public static class BuiltInAIActions
{
    // Improve
    public static readonly SelectionAIActionId Improve = new("improve");
    public static readonly SelectionAIActionId FixGrammar = new("fix-grammar");
    public static readonly SelectionAIActionId FixSpelling = new("fix-spelling");
    public static readonly SelectionAIActionId Simplify = new("simplify");
    public static readonly SelectionAIActionId Elaborate = new("elaborate");
    public static readonly SelectionAIActionId MakeConciese = new("make-concise");
    public static readonly SelectionAIActionId AdjustTone = new("adjust-tone");

    // Transform
    public static readonly SelectionAIActionId Rephrase = new("rephrase");
    public static readonly SelectionAIActionId ToActiveVoice = new("to-active-voice");
    public static readonly SelectionAIActionId ToBulletPoints = new("to-bullet-points");
    public static readonly SelectionAIActionId ToNumberedList = new("to-numbered-list");
    public static readonly SelectionAIActionId ToTable = new("to-table");
    public static readonly SelectionAIActionId ToHeadings = new("to-headings");

    // Generate
    public static readonly SelectionAIActionId Continue = new("continue");
    public static readonly SelectionAIActionId GenerateExamples = new("generate-examples");
    public static readonly SelectionAIActionId GenerateOutline = new("generate-outline");

    // Analyze
    public static readonly SelectionAIActionId Summarize = new("summarize");
    public static readonly SelectionAIActionId Explain = new("explain");
    public static readonly SelectionAIActionId FindIssues = new("find-issues");
    public static readonly SelectionAIActionId FactCheck = new("fact-check");

    // Translate
    public static readonly SelectionAIActionId Translate = new("translate");
}

public record AIActionOptions
{
    public string? CustomInstruction { get; init; }
    public ToneStyle? TargetTone { get; init; }
    public string? TargetLanguage { get; init; }
    public int? TargetLength { get; init; }
    public bool PreviewOnly { get; init; }
    public string? ModelOverride { get; init; }
}

public enum ToneStyle
{
    Professional, Casual, Friendly, Formal, Academic, Technical, Creative
}

public record AIActionResult
{
    public required string OriginalText { get; init; }
    public required string ResultText { get; init; }
    public required AIActionStatus Status { get; init; }
    public string? Explanation { get; init; }
    public IReadOnlyList<AIChange> Changes { get; init; } = [];
    public AIUsageMetrics Metrics { get; init; } = new();
}

public enum AIActionStatus
{
    Success, PartialSuccess, NoChanges, Error, Cancelled
}

public record AIChange
{
    public required TextRange Range { get; init; }
    public required string OriginalText { get; init; }
    public required string NewText { get; init; }
    public required AIChangeType Type { get; init; }
    public string? Reason { get; init; }
}

public enum AIChangeType
{
    Replacement, Insertion, Deletion, Reorder, StyleChange
}

/// <summary>
/// Comment-based AI prompting system.
/// </summary>
public interface ICommentAIPrompts
{
    /// <summary>
    /// Process a comment as an AI prompt.
    /// </summary>
    Task<CommentPromptResult> ProcessCommentAsync(
        Annotation comment,
        CommentPromptOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Detect AI prompts in comments.
    /// </summary>
    Task<IReadOnlyList<DetectedPrompt>> DetectPromptsAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Execute all pending comment prompts.
    /// </summary>
    Task<IReadOnlyList<CommentPromptResult>> ExecuteAllPromptsAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Get prompt suggestions for a comment.
    /// </summary>
    Task<IReadOnlyList<PromptSuggestion>> GetSuggestionsAsync(
        string partialPrompt,
        CancellationToken ct = default);
}

// Comment prompt syntax:
// @ai: <instruction>           - Inline instruction
// @ai-continue                 - Continue writing
// @ai-improve                  - Improve this section
// @ai-expand                   - Expand on this
// @ai-summarize                - Summarize this
// @ai-translate:<lang>         - Translate to language
// @ai-rewrite:<style>          - Rewrite in style
// @ai-question: <question>     - Ask AI a question

public record DetectedPrompt
{
    public required Annotation Comment { get; init; }
    public required PromptType Type { get; init; }
    public required string Instruction { get; init; }
    public TextRange? TargetRange { get; init; }
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

public enum PromptType
{
    Instruction,    // @ai: custom instruction
    Continue,       // @ai-continue
    Improve,        // @ai-improve
    Expand,         // @ai-expand
    Summarize,      // @ai-summarize
    Translate,      // @ai-translate:lang
    Rewrite,        // @ai-rewrite:style
    Question,       // @ai-question:
    Custom          // Custom prompt type
}

public record CommentPromptResult
{
    public required DetectedPrompt Prompt { get; init; }
    public required CommentPromptStatus Status { get; init; }
    public string? Result { get; init; }
    public TextEdit? SuggestedEdit { get; init; }
    public string? Answer { get; init; }  // For questions
    public string? Error { get; init; }
}

public enum CommentPromptStatus
{
    Success, Applied, Pending, Error, Skipped
}

/// <summary>
/// Ghost text (inline) completions.
/// </summary>
public interface IGhostTextProvider
{
    /// <summary>
    /// Whether ghost text is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enable/disable ghost text.
    /// </summary>
    Task SetEnabledAsync(bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Get ghost text completion for current position.
    /// </summary>
    Task<GhostTextCompletion?> GetCompletionAsync(
        DocumentId documentId,
        TextPosition position,
        GhostTextOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Accept the current ghost text.
    /// </summary>
    Task AcceptAsync(CancellationToken ct = default);

    /// <summary>
    /// Accept partial ghost text (word or line).
    /// </summary>
    Task AcceptPartialAsync(
        GhostTextAcceptMode mode,
        CancellationToken ct = default);

    /// <summary>
    /// Dismiss the current ghost text.
    /// </summary>
    Task DismissAsync(CancellationToken ct = default);

    /// <summary>
    /// Get next alternative completion.
    /// </summary>
    Task<GhostTextCompletion?> GetNextAlternativeAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of ghost text updates.
    /// </summary>
    IObservable<GhostTextUpdate> Updates { get; }
}

public record GhostTextOptions
{
    public int MaxTokens { get; init; } = 100;
    public int MaxAlternatives { get; init; } = 3;
    public float Temperature { get; init; } = 0.7f;
    public int DebounceMs { get; init; } = 500;
    public bool SingleLine { get; init; } = false;
    public GhostTextTrigger Trigger { get; init; } = GhostTextTrigger.OnPause;
}

public enum GhostTextTrigger
{
    OnPause,        // After user stops typing
    OnNewLine,      // After pressing Enter
    OnTab,          // On Tab key
    Manual          // Only on explicit request
}

public record GhostTextCompletion
{
    public required string Text { get; init; }
    public required TextPosition InsertPosition { get; init; }
    public int AlternativeIndex { get; init; }
    public int TotalAlternatives { get; init; }
    public float Confidence { get; init; }
    public string? Explanation { get; init; }
}

public enum GhostTextAcceptMode
{
    All,        // Accept entire completion
    Word,       // Accept next word
    Line,       // Accept to end of line
    Sentence    // Accept to end of sentence
}

/// <summary>
/// Inline AI suggestions (proactive).
/// </summary>
public interface IInlineAISuggestions
{
    /// <summary>
    /// Whether inline suggestions are enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Get suggestions for current document state.
    /// </summary>
    Task<IReadOnlyList<InlineSuggestion>> GetSuggestionsAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Apply a suggestion.
    /// </summary>
    Task ApplySuggestionAsync(
        InlineSuggestionId suggestionId,
        CancellationToken ct = default);

    /// <summary>
    /// Dismiss a suggestion.
    /// </summary>
    Task DismissSuggestionAsync(
        InlineSuggestionId suggestionId,
        CancellationToken ct = default);

    /// <summary>
    /// Configure suggestion types.
    /// </summary>
    Task ConfigureAsync(
        InlineSuggestionConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of new suggestions.
    /// </summary>
    IObservable<InlineSuggestion> NewSuggestions { get; }
}

public record InlineSuggestion
{
    public required InlineSuggestionId Id { get; init; }
    public required InlineSuggestionType Type { get; init; }
    public required TextRange Range { get; init; }
    public required string Message { get; init; }
    public required string SuggestedText { get; init; }
    public string? OriginalText { get; init; }
    public float Confidence { get; init; }
    public string? Rationale { get; init; }
    public bool IsAutoApplicable { get; init; }
}

public readonly record struct InlineSuggestionId(Guid Value)
{
    public static InlineSuggestionId New() => new(Guid.NewGuid());
}

public enum InlineSuggestionType
{
    GrammarFix,
    SpellingFix,
    StyleImprovement,
    ClarityImprovement,
    Simplification,
    Expansion,
    FactCorrection,
    ToneAdjustment,
    StructureSuggestion,
    WordChoice
}

public record InlineSuggestionConfig
{
    public bool EnableGrammar { get; init; } = true;
    public bool EnableSpelling { get; init; } = true;
    public bool EnableStyle { get; init; } = true;
    public bool EnableClarity { get; init; } = true;
    public bool EnableFactCheck { get; init; } = false;
    public int MaxSuggestionsPerParagraph { get; init; } = 3;
    public float MinConfidenceThreshold { get; init; } = 0.7f;
    public bool AutoApplyHighConfidence { get; init; } = false;
}

/// <summary>
/// AI writing assistant panel.
/// </summary>
public interface IAIWritingAssistant
{
    /// <summary>
    /// Start a chat session about the document.
    /// </summary>
    Task<ChatSession> StartSessionAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Send a message to the assistant.
    /// </summary>
    IAsyncEnumerable<AIChatChunk> SendMessageAsync(
        ChatSessionId sessionId,
        string message,
        ChatMessageOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get document insights.
    /// </summary>
    Task<DocumentInsights> GetInsightsAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Get writing suggestions.
    /// </summary>
    Task<IReadOnlyList<WritingSuggestion>> GetWritingSuggestionsAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Generate content based on outline.
    /// </summary>
    IAsyncEnumerable<string> GenerateFromOutlineAsync(
        DocumentId documentId,
        GenerationOptions options,
        CancellationToken ct = default);
}

public record ChatSession
{
    public required ChatSessionId Id { get; init; }
    public required DocumentId DocumentId { get; init; }
    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];
    public DateTimeOffset StartedAt { get; init; }
    public DocumentContext Context { get; init; } = new();
}

public readonly record struct ChatSessionId(Guid Value)
{
    public static ChatSessionId New() => new(Guid.NewGuid());
}

public record DocumentContext
{
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<string> Headings { get; init; } = [];
    public int WordCount { get; init; }
    public string? DetectedTone { get; init; }
    public string? DetectedAudience { get; init; }
}

public record DocumentInsights
{
    public required DocumentId DocumentId { get; init; }
    public ReadabilityMetrics Readability { get; init; } = new();
    public ToneAnalysis Tone { get; init; } = new();
    public StructureAnalysis Structure { get; init; } = new();
    public IReadOnlyList<string> KeyTopics { get; init; } = [];
    public IReadOnlyList<string> SuggestedImprovements { get; init; } = [];
    public int EstimatedReadingTimeMinutes { get; init; }
}

public record ReadabilityMetrics
{
    public float FleschReadingEase { get; init; }
    public float FleschKincaidGrade { get; init; }
    public float GunningFogIndex { get; init; }
    public float AverageWordsPerSentence { get; init; }
    public float AverageSyllablesPerWord { get; init; }
    public ReadabilityLevel Level { get; init; }
}

public enum ReadabilityLevel
{
    VeryEasy,       // 5th grade
    Easy,           // 6th grade
    FairlyEasy,     // 7th grade
    Standard,       // 8th-9th grade
    FairlyDifficult,// 10th-12th grade
    Difficult,      // College
    VeryDifficult   // Graduate
}

public record ToneAnalysis
{
    public string PrimaryTone { get; init; } = "";
    public IReadOnlyList<ToneScore> Tones { get; init; } = [];
    public float Formality { get; init; }       // 0-1
    public float Positivity { get; init; }      // 0-1
    public float Confidence { get; init; }      // 0-1
}

public record ToneScore
{
    public required string Tone { get; init; }
    public float Score { get; init; }
}

/// <summary>
/// AI action history and undo support.
/// </summary>
public interface IAIActionHistory
{
    /// <summary>
    /// Get AI action history for document.
    /// </summary>
    Task<IReadOnlyList<AIActionRecord>> GetHistoryAsync(
        DocumentId documentId,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Undo last AI action.
    /// </summary>
    Task<bool> UndoLastActionAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Undo specific AI action.
    /// </summary>
    Task<bool> UndoActionAsync(
        AIActionRecordId recordId,
        CancellationToken ct = default);

    /// <summary>
    /// Get action details.
    /// </summary>
    Task<AIActionRecord?> GetActionAsync(
        AIActionRecordId recordId,
        CancellationToken ct = default);

    /// <summary>
    /// Compare before/after for an action.
    /// </summary>
    Task<DiffResult> CompareActionAsync(
        AIActionRecordId recordId,
        CancellationToken ct = default);
}

public record AIActionRecord
{
    public required AIActionRecordId Id { get; init; }
    public required DocumentId DocumentId { get; init; }
    public required string ActionType { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string OriginalText { get; init; }
    public required string ResultText { get; init; }
    public TextRange? AffectedRange { get; init; }
    public AIUsageMetrics Metrics { get; init; } = new();
    public bool IsUndone { get; init; }
    public bool CanUndo { get; init; }
}

public readonly record struct AIActionRecordId(Guid Value)
{
    public static AIActionRecordId New() => new(Guid.NewGuid());
}

public record AIUsageMetrics
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public TimeSpan Latency { get; init; }
    public string? ModelUsed { get; init; }
    public bool UsedLocalModel { get; init; }
    public decimal? EstimatedCost { get; init; }
}
```

### AI Integration UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  ┌─ Editor ─────────────────────────────────────────────────────────────┐  │
│  │                                                                       │  │
│  │   The quick brown fox jumps over the lazy dog. This sentence has     │  │
│  │   been used for decades as a pangram for testing fonts and           │  │
│  │   keyboards. ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │  │
│  │              ↑ Ghost text: "It contains every letter of the          │  │
│  │                            English alphabet at least once."          │  │
│  │              [Tab to accept] [Esc to dismiss]                        │  │
│  │                                                                       │  │
│  │   <!-- @ai: Add a section about the history of pangrams -->          │  │
│  │   ↑ Comment prompt detected                              [Execute]   │  │
│  │                                                                       │  │
│  │   ┌─────────────────────────────────────────────────────────────┐    │  │
│  │   │ [Selected: "This sentence has been used for decades"]      │    │  │
│  │   │                                                             │    │  │
│  │   │  🤖 [Improve] [Simplify] [Expand] [Chat...] [More ▾]       │    │  │
│  │   │     ↑ Selection AI action toolbar                          │    │  │
│  │   └─────────────────────────────────────────────────────────────┘    │  │
│  │                                                                       │  │
│  │   The word "synergy" appears frequently in corporate communications. │  │
│  │                ~~~~~~~~                                               │  │
│  │                │ 💡 Consider using "collaboration" instead            │  │
│  │                │    (more accessible language)            [Apply]    │  │
│  │                ↑ Inline AI suggestion                                │  │
│  │                                                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─ AI Assistant Panel ─────────────────────────────────────────────────┐  │
│  │ 📊 Document Insights                                        [Refresh] │  │
│  │ ──────────────────────────────────────────────────────────────────── │  │
│  │ Readability: Standard (8th grade)  │  Words: 1,234  │  Reading: 5 min│  │
│  │ Tone: Professional, Informative    │  Paragraphs: 15                 │  │
│  │ ──────────────────────────────────────────────────────────────────── │  │
│  │ 💬 Chat with AI about this document:                                 │  │
│  │ ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │ │ User: How can I make the introduction more engaging?            │  │  │
│  │ │                                                                  │  │  │
│  │ │ AI: Here are 3 suggestions to make your intro more engaging:    │  │  │
│  │ │ 1. Start with a surprising statistic about pangrams             │  │  │
│  │ │ 2. Open with a rhetorical question                              │  │  │
│  │ │ 3. Begin with a brief anecdote                            [Apply]│  │  │
│  │ └─────────────────────────────────────────────────────────────────┘  │  │
│  │ [Type a message...]                                         [Send 📤]│  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─ AI Action History ──────────────────────────────────────────────────┐  │
│  │ ↩ [Undo Last]                                                        │  │
│  │ • Improved paragraph (2 min ago)                            [Undo]   │  │
│  │   "This sentence has..." → "For decades, this sentence..."           │  │
│  │ • Fixed grammar (5 min ago)                                 [Undo]   │  │
│  │   "their" → "there"                                                  │  │
│  │ • Expanded section (10 min ago)                             [Undo]   │  │
│  │   +45 words added to "History" section                               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘

Selection AI Action Menu (expanded):
┌─────────────────────────────────────┐
│ 📝 Improve                          │
│ ├─ Fix Grammar & Spelling           │
│ ├─ Improve Clarity                  │
│ ├─ Make More Concise                │
│ └─ Adjust Tone...          →        │
│ ─────────────────────────────────── │
│ 🔄 Transform                        │
│ ├─ Rephrase                         │
│ ├─ Convert to Active Voice          │
│ ├─ Convert to Bullet Points         │
│ ├─ Convert to Table                 │
│ └─ Simplify                         │
│ ─────────────────────────────────── │
│ ✨ Generate                         │
│ ├─ Continue Writing                 │
│ ├─ Add Examples                     │
│ └─ Create Outline                   │
│ ─────────────────────────────────── │
│ 🔍 Analyze                          │
│ ├─ Summarize                        │
│ ├─ Explain                          │
│ ├─ Fact Check                       │
│ └─ Find Issues                      │
│ ─────────────────────────────────── │
│ 🌐 Translate...            →        │
│ ─────────────────────────────────── │
│ 💬 Chat About Selection...          │
└─────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | Selection actions; ghost text (limited) |
| Teams | All AI features; comment prompts; history |
| Enterprise | + Custom actions; bulk processing; API |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.17.x |
|:----------|:-------|:-----------------|
| `ILanguageModelProvider` | v0.12.3-AGT | AI inference |
| `ILocalInferenceManager` | v0.16.4-LLM | Local AI |
| `ILlmRouter` | v0.16.5-LLM | Provider routing |
| `IDocumentService` | v0.5.x | Document storage |
| `ISettingsService` | v0.1.6a | User preferences |
| `ILicenseService` | v0.2.1a | Feature gating |
| `IAuditLogger` | v0.11.2-SEC | Action logging |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `PreviewRenderedEvent` | v0.17.1 | Markdown preview updated |
| `LayoutChangedEvent` | v0.17.1 | Editor layout changed |
| `FormattingAppliedEvent` | v0.17.2 | Formatting applied to text |
| `ChangeTrackedEvent` | v0.17.3 | Change tracked |
| `ChangeAcceptedEvent` | v0.17.3 | Change accepted/rejected |
| `AnnotationAddedEvent` | v0.17.3 | Annotation created |
| `RuleViolationDetectedEvent` | v0.17.4 | Rule violation found |
| `StyleCheckCompletedEvent` | v0.17.4 | Style check finished |
| `AIActionExecutedEvent` | v0.17.5 | AI action completed |
| `GhostTextAcceptedEvent` | v0.17.5 | Ghost text accepted |
| `CommentPromptProcessedEvent` | v0.17.5 | Comment prompt executed |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Markdig` | 0.33.x | Markdown parsing |
| `DiffPlex` | 1.7.x | Text diffing |
| `TextMateSharp` | 1.0.x | Syntax highlighting |
| `EditorConfig.Core` | 0.18.x | EditorConfig support |
| `ReadabilityScore` | 1.x | Readability metrics |

---

## Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Preview render | <100ms |
| Incremental preview | <30ms |
| Syntax highlighting | <50ms |
| Diff computation | <200ms |
| Version load | <100ms |
| Rule validation | <500ms |
| Ghost text generation | <1s |
| AI action (small) | <3s |
| Selection action menu | <50ms |

---

## What This Enables

With v0.17.x complete, the Lexichord editor becomes:

- **Professional:** Split views, previews, and full editor chrome
- **Powerful:** Rich formatting with keyboard shortcuts and toolbars
- **Collaborative:** Track changes, comments, and version history
- **Consistent:** Style guides, linting, and formatting rules
- **Intelligent:** Deep AI integration at every level of editing

The editor transforms from a simple text area to a world-class AI-powered authoring environment.

---

## Total Intelligent Editor Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 12: Intelligent Editor | v0.17.1 - v0.17.5 | ~286 |

**Combined with prior phases:** ~2,302 hours (~58 person-months)

---

## Complete Platform Summary

| Phase | Focus | Versions | Hours |
|:------|:------|:---------|:------|
| 1-4 | Foundation (CKVS) | v0.4.x - v0.7.x | ~273 |
| 5 | Advanced Knowledge | v0.10.x | ~214 |
| 6 | Security | v0.11.x | ~209 |
| 7 | Agent Infrastructure | v0.12.x | ~248 |
| 8 | Orchestration | v0.13.x | ~276 |
| 9 | Agent Studio | v0.14.x | ~264 |
| 10 | Marketplace | v0.15.x | ~258 |
| 11 | Local LLM | v0.16.x | ~274 |
| 12 | Intelligent Editor | v0.17.x | ~286 |
| **Total** | | | **~2,302 hours** |

**Approximately 58 person-months** to complete the full platform vision with the intelligent editor.

---
