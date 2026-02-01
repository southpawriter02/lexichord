# LCS-SBD-v0.17.1-EDT: Scope Overview — Editor Foundation

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-v0.17.1-EDT                                          |
| **Version**      | v0.17.1                                                      |
| **Codename**     | Editor Foundation                                            |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-02-01                                                   |
| **Owner**        | Editor Architecture Lead                                     |
| **Depends On**   | v0.17.0-EDC (Core Editor), v0.16.2-ORC (Orchestration), v0.15.1-RNR (Renderer) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.17.1-EDT** delivers **Editor Foundation** — the professional-grade editor infrastructure that transforms LexiChord from a basic text editor into a powerful, multi-pane editing environment. This establishes:

- A split view management system supporting horizontal, vertical, and preview-focused layouts
- A live Markdown preview pane with synchronized scrolling and source-to-preview mapping
- Enhanced syntax highlighting with multi-language support and custom grammar definitions
- Professional editor chrome (line numbers, minimap, breadcrumbs, gutters) for navigational excellence
- Breadcrumb navigation for quick context awareness and document structure traversal
- Comprehensive editor settings and theme system with support for custom themes and white-label theming

This is the cornerstone of professional content authoring — without sophisticated editor infrastructure, users cannot efficiently manage complex documents at scale.

### 1.2 Business Value

- **Productivity:** Split view and live preview accelerate content creation by 40-60% (reduce preview context switch time to zero).
- **Professional Grade:** Chrome elements (minimap, line numbers, breadcrumbs) match industry-standard editors (VS Code, Sublime, JetBrains).
- **Customization:** Theme system enables white-label deployments for enterprise customers.
- **Accessibility:** Multiple navigation aids (breadcrumbs, minimap, outline) support various user workflows.
- **Differentiation:** Feature set positions LexiChord as premium content authoring platform vs. basic markdown editors.
- **Compliance:** Theme persistence and editor settings enable WCAG compliance and accessibility customization.

### 1.3 Success Criteria

1. Split view rendering achieves 60fps with 3 simultaneous panes (P95 frame time <16ms).
2. Live preview updates within 200ms of source change (incremental rendering enabled).
3. Minimap accurately represents document at <10% CPU utilization during idle.
4. Syntax highlighting tokenizes 10,000 lines in <100ms (P95).
5. Breadcrumb navigation responds to cursor movement within <50ms.
6. Editor chrome supports all 5 pane types (Editor, Preview, Diff, Outline, Terminal).
7. Theme system persists user preferences across sessions with zero data loss.
8. License gating enforces tier-based feature restrictions (Core/WriterPro/Teams/Enterprise).
9. Synchronized scrolling maintains <5ms scroll position delta between panes.
10. All UI elements scale correctly from 1024x768 to 4K displays without layout breakage.

---

## 2. Key Deliverables

### 2.1 Sub-Parts with Detailed Descriptions

| Sub-Part | Title | Description | Est. Hours | Dependencies |
|:---------|:------|:------------|:-----------|:-------------|
| v0.17.1e | Split View Manager | Core infrastructure for managing multiple editor/preview panes, layout modes, resizing, and pane lifecycle. Implements ISplitViewManager with support for EditorOnly, PreviewOnly, SplitHorizontal, SplitVertical, and SyncScroll modes. Handles state persistence and RxJS observable streams. | 12 | v0.17.0-EDC |
| v0.17.1f | Markdown Preview Pane | Live Markdown renderer using marked.js/markdown-it with support for syntax highlighting, KaTeX math, Mermaid diagrams, emoji, task lists, footnotes, and TOC generation. Implements incremental rendering for performance. Includes scroll mapping for synchronized scrolling. | 12 | v0.17.1e, v0.17.1g |
| v0.17.1g | Enhanced Syntax Highlighting | Multi-language tokenizer supporting C#, JavaScript, Python, SQL, Markdown, and YAML. Implements ISyntaxHighlighter with TextMate grammar support, custom language registration, and language auto-detection. Integrates with highlight.js and Prism.js. | 10 | v0.17.0-EDC |
| v0.17.1h | Editor Chrome (Line Numbers, Minimap) | Visual components for line numbers (absolute/relative/interval styles), minimap with configurable scale and character rendering, gutter decorations (breakpoints, bookmarks, errors, etc.), scroll indicators, and fold controls. Implements IEditorChrome and EditorChromeConfig. | 10 | v0.17.0-EDC |
| v0.17.1i | Breadcrumb Navigation | Hierarchical breadcrumb trail showing document structure and cursor position context. Supports Markdown headings, code blocks, functions, classes, and namespaces. Implements IBreadcrumbNavigation with dropdown menus for quick navigation. | 6 | v0.17.1g |
| v0.17.1j | Editor Settings & Themes | Persistent editor configuration (font, size, indentation, cursor style, scrolling, etc.) and theme system. Supports built-in themes and custom theme import/export. Implements IEditorSettings and EditorTheme with full WCAG compliance support. | 6 | v0.17.0-EDC |
| **Total** | | | **56 hours** | |

---

## 3. Complete C# Interfaces with XML Documentation

### 3.1 Split View Manager

```csharp
/// <summary>
/// Manages the editor layout with split views and panels.
/// Enables users to view multiple documents simultaneously or edit alongside live preview.
/// Supports multiple layout modes and dynamic pane creation/destruction.
/// </summary>
public interface ISplitViewManager
{
    /// <summary>
    /// Gets the current layout configuration.
    /// Reflects the state of all panes, split modes, and visibility settings.
    /// </summary>
    SplitViewLayout Layout { get; }

    /// <summary>
    /// Splits the editor view horizontally or vertically at the specified ratio.
    /// Creates a new EditorPane with the specified direction and returns it for configuration.
    /// Fires SplitViewLayoutChanged event with the updated layout.
    /// </summary>
    /// <param name="direction">Direction of the split (Horizontal for side-by-side, Vertical for top-bottom).</param>
    /// <param name="ratio">Width/height ratio for the new pane (default 0.5 = equal split).</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The newly created EditorPane.</returns>
    Task<EditorPane> SplitAsync(
        SplitDirection direction,
        float ratio = 0.5f,
        CancellationToken ct = default);

    /// <summary>
    /// Closes a specific pane and removes it from the layout.
    /// If the pane contains a focused editor, focus shifts to an adjacent pane.
    /// If the pane is the last editor pane, operation is rejected with validation error.
    /// </summary>
    /// <param name="paneId">The ID of the pane to close.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task if pane was closed; throws if pane cannot be closed.</returns>
    Task ClosePaneAsync(
        PaneId paneId,
        CancellationToken ct = default);

    /// <summary>
    /// Toggles the visibility of the preview pane.
    /// If preview is hidden, this shows it; if visible, this hides it.
    /// Maintains preview content state while hidden.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when preview visibility has been toggled.</returns>
    Task TogglePreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the layout mode to one of the predefined configurations.
    /// Reconfigures all panes to match the specified mode.
    /// Preserves existing document and cursor positions where applicable.
    /// </summary>
    /// <param name="mode">The layout mode to apply (EditorOnly, PreviewOnly, SplitHorizontal, SplitVertical, SyncScroll).</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when layout mode has been applied.</returns>
    Task SetLayoutAsync(
        SplitViewMode mode,
        CancellationToken ct = default);

    /// <summary>
    /// Resizes panes by adjusting the split ratio.
    /// Affects the size of adjacent panes on either side of the split divider.
    /// Useful for responsive layout adjustments and user-initiated resize operations.
    /// </summary>
    /// <param name="ratio">New split ratio (0.1 to 0.9, where 0.5 = equal split).</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when panes have been resized.</returns>
    Task ResizePanesAsync(
        float ratio,
        CancellationToken ct = default);

    /// <summary>
    /// Swaps the positions of two adjacent panes on either side of the split divider.
    /// Editor and preview panes exchange positions within the layout.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when panes have been swapped.</returns>
    Task SwapPanesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an observable stream of layout change events.
    /// Subscribers receive notifications whenever the layout configuration changes
    /// (splits, pane closures, mode changes, resize operations).
    /// </summary>
    IObservable<SplitViewLayoutChanged> LayoutChanges { get; }
}

/// <summary>
/// Direction of a split operation.
/// </summary>
public enum SplitDirection
{
    /// <summary>Side-by-side layout (editor on left, preview on right).</summary>
    Horizontal,

    /// <summary>Top-and-bottom layout (editor on top, preview below).</summary>
    Vertical
}

/// <summary>
/// Predefined layout modes for the editor.
/// </summary>
public enum SplitViewMode
{
    /// <summary>Full-screen editor, no preview or secondary panes.</summary>
    EditorOnly,

    /// <summary>Full-screen preview, no editor or secondary panes.</summary>
    PreviewOnly,

    /// <summary>Editor on left, preview on right, equal split.</summary>
    SplitHorizontal,

    /// <summary>Editor on top, preview below, equal split.</summary>
    SplitVertical,

    /// <summary>Split layout with synchronized scrolling between editor and preview.</summary>
    SyncScroll
}

/// <summary>
/// Current layout configuration of the editor.
/// </summary>
public record SplitViewLayout
{
    /// <summary>Gets the current layout mode.</summary>
    public SplitViewMode Mode { get; init; }

    /// <summary>Gets the list of active panes in the layout.</summary>
    public IReadOnlyList<EditorPane> Panes { get; init; } = [];

    /// <summary>Gets the split ratio between panes (0.0 to 1.0).</summary>
    public float SplitRatio { get; init; } = 0.5f;

    /// <summary>Gets whether the preview pane is currently visible.</summary>
    public bool PreviewVisible { get; init; }

    /// <summary>Gets whether synchronized scrolling is enabled between panes.</summary>
    public bool SyncScrollEnabled { get; init; }
}

/// <summary>
/// Represents a single pane in the editor layout.
/// A pane is a rectangular region that can contain an editor, preview, diff, outline, or terminal.
/// </summary>
public record EditorPane
{
    /// <summary>Gets the unique identifier for this pane.</summary>
    public PaneId Id { get; init; }

    /// <summary>Gets the type of content this pane displays.</summary>
    public PaneType Type { get; init; }

    /// <summary>Gets the width ratio relative to the total layout width (0.0 to 1.0).</summary>
    public float WidthRatio { get; init; }

    /// <summary>Gets the height ratio relative to the total layout height (0.0 to 1.0).</summary>
    public float HeightRatio { get; init; }

    /// <summary>Gets whether this pane currently has focus.</summary>
    public bool IsFocused { get; init; }

    /// <summary>Gets the ID of the document displayed in this pane (if applicable).</summary>
    public DocumentId? DocumentId { get; init; }

    /// <summary>Gets the current view state (scroll position, cursor position, selection) for this pane.</summary>
    public EditorViewState? ViewState { get; init; }
}

/// <summary>
/// Type of content a pane can display.
/// </summary>
public enum PaneType
{
    /// <summary>Code/text editor pane.</summary>
    Editor,

    /// <summary>Live Markdown preview pane.</summary>
    Preview,

    /// <summary>Diff view pane (for comparing versions).</summary>
    Diff,

    /// <summary>Document outline/structure pane.</summary>
    Outline,

    /// <summary>Terminal/console output pane.</summary>
    Terminal
}

/// <summary>
/// Strongly-typed identifier for an editor pane.
/// </summary>
public readonly record struct PaneId(Guid Value)
{
    /// <summary>Creates a new unique pane identifier.</summary>
    public static PaneId New() => new(Guid.NewGuid());

    /// <summary>Parses a pane ID from a string.</summary>
    public static PaneId Parse(string s) => new(Guid.Parse(s));

    /// <summary>Returns a string representation of the pane ID.</summary>
    public override string ToString() => $"pane:{Value:N}";
}

/// <summary>
/// Event fired when the split view layout changes.
/// </summary>
public record SplitViewLayoutChanged(
    SplitViewLayout OldLayout,
    SplitViewLayout NewLayout,
    DateTimeOffset Timestamp = default) : EditorEvent;
```

### 3.2 Markdown Preview Renderer

```csharp
/// <summary>
/// Renders Markdown content as live preview HTML.
/// Supports advanced Markdown features including math, diagrams, emoji, task lists, footnotes, and TOC.
/// Provides incremental rendering for performance and scroll mapping for synchronized scrolling.
/// </summary>
public interface IMarkdownPreviewRenderer
{
    /// <summary>
    /// Renders the entire Markdown document to HTML for preview display.
    /// Processes all Markdown syntax and applies the specified rendering options.
    /// Returns timing information for performance monitoring.
    /// </summary>
    /// <param name="markdown">The Markdown source text to render.</param>
    /// <param name="options">Rendering options controlling features like math, diagrams, emoji.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A RenderedPreview containing HTML, metadata, and timing information.</returns>
    Task<RenderedPreview> RenderAsync(
        string markdown,
        PreviewRenderOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Renders Markdown incrementally based on a text change.
    /// Only re-renders the affected sections of the document, significantly improving performance
    /// for large documents with frequent edits.
    /// Falls back to full render if change affects document structure.
    /// </summary>
    /// <param name="markdown">The updated Markdown source text.</param>
    /// <param name="change">The TextChange describing what was modified.</param>
    /// <param name="previousRender">The previous RenderedPreview to use as a base.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An updated RenderedPreview with only affected sections re-rendered.</returns>
    Task<RenderedPreview> RenderIncrementalAsync(
        string markdown,
        TextChange change,
        RenderedPreview previousRender,
        CancellationToken ct = default);

    /// <summary>
    /// Computes the scroll position mapping between the Markdown source and the HTML preview.
    /// Enables synchronized scrolling where scrolling the editor scrolls the preview at the correct position.
    /// Anchor points map source lines to preview HTML elements.
    /// </summary>
    /// <param name="markdown">The Markdown source text.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A ScrollMapping containing anchor points for synchronization.</returns>
    Task<ScrollMapping> GetScrollMappingAsync(
        string markdown,
        CancellationToken ct = default);

    /// <summary>
    /// Gets an observable stream of preview update events.
    /// Subscribers are notified whenever the preview is re-rendered, enabling real-time UI updates.
    /// </summary>
    IObservable<PreviewUpdate> Updates { get; }
}

/// <summary>
/// Options controlling Markdown preview rendering behavior.
/// </summary>
public record PreviewRenderOptions
{
    /// <summary>Gets whether to enable syntax highlighting in code blocks. Default: true.</summary>
    public bool SyntaxHighlighting { get; init; } = true;

    /// <summary>Gets whether to enable LaTeX/KaTeX math rendering. Default: true.</summary>
    public bool MathRendering { get; init; } = true;

    /// <summary>Gets whether to enable Mermaid diagram rendering. Default: true.</summary>
    public bool DiagramRendering { get; init; } = true;

    /// <summary>Gets whether to enable emoji rendering. Default: true.</summary>
    public bool EmojiRendering { get; init; } = true;

    /// <summary>Gets whether to enable Markdown task list rendering. Default: true.</summary>
    public bool TaskListRendering { get; init; } = true;

    /// <summary>Gets whether to generate and display table of contents. Default: true.</summary>
    public bool TableOfContents { get; init; } = true;

    /// <summary>Gets whether to enable footnote rendering. Default: true.</summary>
    public bool FootnoteRendering { get; init; } = true;

    /// <summary>Gets optional custom CSS to apply to the preview. Default: null.</summary>
    public string? CustomCss { get; init; }

    /// <summary>Gets the theme to apply to the preview. Default: PreviewTheme.Auto.</summary>
    public PreviewTheme Theme { get; init; } = PreviewTheme.Auto;
}

/// <summary>
/// Theme options for the Markdown preview.
/// </summary>
public enum PreviewTheme
{
    /// <summary>Automatically match the editor theme.</summary>
    Auto,

    /// <summary>Light theme.</summary>
    Light,

    /// <summary>Dark theme.</summary>
    Dark,

    /// <summary>Sepia/warm theme (reduced blue light).</summary>
    Sepia,

    /// <summary>High-contrast theme for accessibility.</summary>
    HighContrast
}

/// <summary>
/// The result of rendering a Markdown document to HTML.
/// Contains the HTML output, metadata about the rendering, and timing information.
/// </summary>
public record RenderedPreview
{
    /// <summary>Gets the rendered HTML output ready for display in the browser.</summary>
    public required string Html { get; init; }

    /// <summary>Gets a hash of the source Markdown for change detection.</summary>
    public required string SourceHash { get; init; }

    /// <summary>Gets metadata about sections in the preview for scroll mapping.</summary>
    public IReadOnlyList<PreviewSection> Sections { get; init; } = [];

    /// <summary>Gets all hyperlinks found in the preview.</summary>
    public IReadOnlyList<PreviewLink> Links { get; init; } = [];

    /// <summary>Gets all images found in the preview.</summary>
    public IReadOnlyList<PreviewImage> Images { get; init; } = [];

    /// <summary>Gets all headings found in the preview (for TOC).</summary>
    public IReadOnlyList<PreviewHeading> Headings { get; init; } = [];

    /// <summary>Gets the time taken to render this preview.</summary>
    public TimeSpan RenderTime { get; init; }
}

/// <summary>
/// Metadata about a section in the preview for scroll mapping.
/// Maps a range of source lines to an HTML element in the preview.
/// </summary>
public record PreviewSection
{
    /// <summary>Gets the starting line number in the source Markdown.</summary>
    public required int SourceStartLine { get; init; }

    /// <summary>Gets the ending line number in the source Markdown.</summary>
    public required int SourceEndLine { get; init; }

    /// <summary>Gets the HTML element ID for this section in the preview.</summary>
    public required string HtmlElementId { get; init; }
}

/// <summary>
/// Scroll position mapping between source and preview.
/// Contains anchor points that map source lines to preview scroll positions.
/// </summary>
public record ScrollMapping
{
    /// <summary>Gets the list of scroll anchors mapping source to preview positions.</summary>
    public IReadOnlyList<ScrollAnchor> Anchors { get; init; } = [];
}

/// <summary>
/// A single scroll anchor mapping a source line to a preview position.
/// </summary>
public record ScrollAnchor
{
    /// <summary>Gets the source line number.</summary>
    public int SourceLine { get; init; }

    /// <summary>Gets the preview scroll position as a percentage (0.0 to 1.0).</summary>
    public float PreviewScrollPercent { get; init; }

    /// <summary>Gets the HTML element ID this anchor corresponds to.</summary>
    public string? ElementId { get; init; }
}

/// <summary>
/// Event fired when the preview is updated.
/// </summary>
public record PreviewUpdate(
    RenderedPreview Preview,
    bool IsIncremental,
    DateTimeOffset Timestamp = default) : EditorEvent;
```

### 3.3 Syntax Highlighter

```csharp
/// <summary>
/// Provides enhanced syntax highlighting for multiple programming languages and markup formats.
/// Supports custom grammar definitions, language auto-detection, and extensible tokenization.
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>
    /// Tokenizes the provided text and returns syntax tokens for highlighting.
    /// Each token specifies its type (keyword, comment, string, etc.) and scope for styling.
    /// </summary>
    /// <param name="text">The source text to tokenize.</param>
    /// <param name="languageId">The language identifier (e.g., "csharp", "javascript", "markdown").</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A list of syntax tokens describing the highlighted regions.</returns>
    Task<IReadOnlyList<SyntaxToken>> TokenizeAsync(
        string text,
        string languageId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all supported language definitions available in the highlighter.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A list of all available LanguageDefinitions.</returns>
    Task<IReadOnlyList<LanguageDefinition>> GetLanguagesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Detects the likely programming language of the provided text.
    /// Uses content analysis and optional filename hints to determine the language.
    /// </summary>
    /// <param name="text">The source text to analyze.</param>
    /// <param name="filename">Optional filename for additional context (e.g., "config.yaml").</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A LanguageDetection result with detected language and confidence score.</returns>
    Task<LanguageDetection> DetectLanguageAsync(
        string text,
        string? filename = null,
        CancellationToken ct = default);

    /// <summary>
    /// Registers a custom language definition with the highlighter.
    /// Allows users to define syntax highlighting for domain-specific languages.
    /// </summary>
    /// <param name="language">The LanguageDefinition to register.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when the language has been registered.</returns>
    Task RegisterLanguageAsync(
        LanguageDefinition language,
        CancellationToken ct = default);
}

/// <summary>
/// A syntax token representing a highlighted region in source text.
/// </summary>
public record SyntaxToken
{
    /// <summary>Gets the character offset in the source where this token begins.</summary>
    public required int StartOffset { get; init; }

    /// <summary>Gets the character length of this token.</summary>
    public required int Length { get; init; }

    /// <summary>Gets the type of syntax element this token represents.</summary>
    public required SyntaxTokenType Type { get; init; }

    /// <summary>Gets optional modifiers that refine the token type (e.g., "readonly", "deprecated").</summary>
    public IReadOnlyList<string>? Modifiers { get; init; }

    /// <summary>Gets the TextMate scope for this token, enabling fine-grained styling.</summary>
    public string? Scope { get; init; }
}

/// <summary>
/// Types of syntax elements for highlighting.
/// </summary>
public enum SyntaxTokenType
{
    // Basic tokens
    Comment, String, Number, Keyword, Operator, Punctuation,

    // Identifiers
    Variable, Function, Class, Interface, Namespace, Property, Parameter,

    // Markdown specific
    Heading, Bold, Italic, Link, Image, Code, CodeBlock, Quote, List, Table,

    // Special
    Error, Warning, Info, Deprecated
}

/// <summary>
/// Definition of a programming language or markup format.
/// </summary>
public record LanguageDefinition
{
    /// <summary>Gets the unique identifier for this language (e.g., "csharp", "javascript").</summary>
    public required string Id { get; init; }

    /// <summary>Gets the human-readable name of the language.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the file extensions associated with this language.</summary>
    public IReadOnlyList<string> Extensions { get; init; } = [];

    /// <summary>Gets alternative names or aliases for this language.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Gets the MIME type for this language (e.g., "application/json").</summary>
    public string? MimeType { get; init; }

    /// <summary>Gets the TextMate grammar JSON for this language.</summary>
    public string? Grammar { get; init; }
}

/// <summary>
/// Result of language detection analysis.
/// </summary>
public record LanguageDetection
{
    /// <summary>Gets the detected language identifier.</summary>
    public required string LanguageId { get; init; }

    /// <summary>Gets the confidence score (0.0 to 1.0) of the detection.</summary>
    public float Confidence { get; init; }
}
```

### 3.4 Editor Chrome

```csharp
/// <summary>
/// Manages editor chrome components including line numbers, minimap, gutter decorations, and visual indicators.
/// Provides a professional editing experience with navigational aids and visual feedback.
/// </summary>
public interface IEditorChrome
{
    /// <summary>
    /// Gets the current chrome configuration controlling what elements are visible and how they appear.
    /// </summary>
    EditorChromeConfig Config { get; }

    /// <summary>
    /// Updates the chrome configuration, changing visibility and appearance of chrome elements.
    /// </summary>
    /// <param name="config">The new chrome configuration to apply.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when the configuration has been applied.</returns>
    Task UpdateConfigAsync(
        EditorChromeConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a decoration to the gutter (e.g., breakpoint, bookmark, error indicator) on the specified line.
    /// Decorations are visible in the gutter margin and can have click handlers.
    /// </summary>
    /// <param name="line">The line number (1-based) to decorate.</param>
    /// <param name="type">The type of decoration (breakpoint, bookmark, error, warning, etc.).</param>
    /// <param name="options">Configuration options for the decoration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The created GutterDecoration with a unique ID for later removal.</returns>
    Task<GutterDecoration> AddGutterDecorationAsync(
        int line,
        GutterDecorationType type,
        GutterDecorationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a gutter decoration by ID.
    /// </summary>
    /// <param name="decorationId">The unique ID of the decoration to remove.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when the decoration has been removed.</returns>
    Task RemoveGutterDecorationAsync(
        Guid decorationId,
        CancellationToken ct = default);

    /// <summary>
    /// Sets the highlight style for a specific line (e.g., current line, selection, error).
    /// </summary>
    /// <param name="line">The line number to highlight.</param>
    /// <param name="type">The type of highlight to apply.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when the highlight has been applied.</returns>
    Task SetLineHighlightAsync(
        int line,
        LineHighlightType type,
        CancellationToken ct = default);

    /// <summary>
    /// Gets minimap rendering data for the entire document.
    /// The minimap provides a bird's-eye view of the document for quick navigation.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>MinimapData containing character grid and color information for rendering.</returns>
    Task<MinimapData> GetMinimapDataAsync(CancellationToken ct = default);
}

/// <summary>
/// Configuration options for editor chrome elements.
/// Controls visibility, style, and behavior of all chrome components.
/// </summary>
public record EditorChromeConfig
{
    // Line Numbers
    /// <summary>Gets whether line numbers are displayed. Default: true.</summary>
    public bool ShowLineNumbers { get; init; } = true;

    /// <summary>Gets the style for line numbers (absolute, relative, or interval). Default: Absolute.</summary>
    public LineNumberStyle LineNumberStyle { get; init; } = LineNumberStyle.Absolute;

    // Minimap
    /// <summary>Gets whether the minimap is displayed. Default: true.</summary>
    public bool ShowMinimap { get; init; } = true;

    /// <summary>Gets the position of the minimap (left or right). Default: Right.</summary>
    public MinimapPosition MinimapPosition { get; init; } = MinimapPosition.Right;

    /// <summary>Gets the scale factor for minimap rendering (0.05 to 0.2). Default: 0.1.</summary>
    public float MinimapScale { get; init; } = 0.1f;

    /// <summary>Gets whether the minimap renders actual characters or just colored blocks. Default: true.</summary>
    public bool MinimapRenderCharacters { get; init; } = true;

    // Gutter
    /// <summary>Gets whether code folding controls are displayed. Default: true.</summary>
    public bool ShowFoldingControls { get; init; } = true;

    /// <summary>Gets whether the glyph margin is displayed for decorations. Default: true.</summary>
    public bool ShowGlyphMargin { get; init; } = true;

    /// <summary>Gets the width of the gutter in pixels. Default: 60.</summary>
    public int GutterWidth { get; init; } = 60;

    // Scrollbar
    /// <summary>Gets whether the scrollbar is displayed. Default: true.</summary>
    public bool ShowScrollbar { get; init; } = true;

    /// <summary>Gets the scrollbar style (visible, overlay, or hidden). Default: Overlay.</summary>
    public ScrollbarStyle ScrollbarStyle { get; init; } = ScrollbarStyle.Overlay;

    // Other
    /// <summary>Gets whether breadcrumbs are displayed. Default: true.</summary>
    public bool ShowBreadcrumbs { get; init; } = true;

    /// <summary>Gets whether indent guides are displayed. Default: true.</summary>
    public bool ShowIndentGuides { get; init; } = true;

    /// <summary>Gets whether the current line is highlighted. Default: true.</summary>
    public bool HighlightCurrentLine { get; init; } = true;

    /// <summary>Gets whether whitespace characters are displayed. Default: false.</summary>
    public bool ShowWhitespace { get; init; } = false;

    /// <summary>Gets whether vertical rulers are displayed. Default: false.</summary>
    public bool ShowRulers { get; init; } = false;

    /// <summary>Gets the column positions for rulers (if ShowRulers is true). Default: null.</summary>
    public IReadOnlyList<int>? RulerColumns { get; init; }
}

/// <summary>
/// Style for rendering line numbers.
/// </summary>
public enum LineNumberStyle
{
    /// <summary>Absolute line numbers (1, 2, 3, ...).</summary>
    Absolute,

    /// <summary>Relative to cursor position (useful for Vim-like editors).</summary>
    Relative,

    /// <summary>Every Nth line (e.g., every 5 or 10 lines).</summary>
    Interval
}

/// <summary>
/// Position of the minimap within the editor.
/// </summary>
public enum MinimapPosition { Left, Right }

/// <summary>
/// Visual style of the scrollbar.
/// </summary>
public enum ScrollbarStyle { Visible, Overlay, Hidden }

/// <summary>
/// A decoration applied to the gutter (margin) of the editor.
/// </summary>
public record GutterDecoration
{
    /// <summary>Gets the unique identifier for this decoration.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the line number (1-based) where this decoration appears.</summary>
    public int Line { get; init; }

    /// <summary>Gets the type of decoration.</summary>
    public GutterDecorationType Type { get; init; }

    /// <summary>Gets the tooltip text displayed on hover.</summary>
    public string? Tooltip { get; init; }

    /// <summary>Gets the path to the icon image to display.</summary>
    public string? IconPath { get; init; }

    /// <summary>Gets the color of the decoration (hex code or color name).</summary>
    public string? Color { get; init; }

    /// <summary>Gets the action to invoke when the decoration is clicked.</summary>
    public Action? OnClick { get; init; }
}

/// <summary>
/// Types of gutter decorations.
/// </summary>
public enum GutterDecorationType
{
    /// <summary>Debugger breakpoint indicator.</summary>
    Breakpoint,

    /// <summary>User bookmark indicator.</summary>
    Bookmark,

    /// <summary>Syntax or runtime error indicator.</summary>
    Error,

    /// <summary>Warning indicator.</summary>
    Warning,

    /// <summary>Informational indicator.</summary>
    Info,

    /// <summary>Hint or suggestion indicator.</summary>
    Hint,

    /// <summary>Fold open control.</summary>
    FoldOpen,

    /// <summary>Fold closed control.</summary>
    FoldClosed,

    /// <summary>Diff addition indicator.</summary>
    Diff,

    /// <summary>Git change indicator.</summary>
    GitChange,

    /// <summary>AI suggestion indicator.</summary>
    AISuggestion,

    /// <summary>Comment indicator.</summary>
    Comment,

    /// <summary>Custom user-defined decoration.</summary>
    Custom
}

/// <summary>
/// Types of line highlighting styles.
/// </summary>
public enum LineHighlightType
{
    /// <summary>No highlight.</summary>
    None,

    /// <summary>Current line highlight.</summary>
    CurrentLine,

    /// <summary>Selected text highlight.</summary>
    Selection,

    /// <summary>Search result highlight.</summary>
    Search,

    /// <summary>Error line highlight.</summary>
    Error,

    /// <summary>Warning line highlight.</summary>
    Warning,

    /// <summary>Added line highlight (diff).</summary>
    Added,

    /// <summary>Removed line highlight (diff).</summary>
    Removed,

    /// <summary>Modified line highlight (diff).</summary>
    Modified
}

/// <summary>
/// Data for rendering the minimap.
/// </summary>
public record MinimapData
{
    /// <summary>Gets the total height of the minimap in pixels.</summary>
    public int Height { get; init; }

    /// <summary>Gets the width of the minimap in pixels.</summary>
    public int Width { get; init; }

    /// <summary>Gets the character grid for rendering (if RenderCharacters is true).</summary>
    public string? CharacterGrid { get; init; }

    /// <summary>Gets the color grid for rendering (RGBA values).</summary>
    public byte[]? ColorGrid { get; init; }

    /// <summary>Gets the scroll position indicator height (as a percentage of minimap height).</summary>
    public float ViewportHeight { get; init; }
}
```

### 3.5 Breadcrumb Navigation

```csharp
/// <summary>
/// Provides breadcrumb navigation within documents.
/// Displays the hierarchical path from document root to the current cursor position,
/// enabling quick context awareness and navigation to parent/sibling elements.
/// </summary>
public interface IBreadcrumbNavigation
{
    /// <summary>
    /// Gets the breadcrumb trail for the current cursor position in the specified document.
    /// Shows the complete path from document root to the cursor location.
    /// </summary>
    /// <param name="documentId">The document to get breadcrumbs for.</param>
    /// <param name="position">The cursor position in the document.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A BreadcrumbTrail showing the path to the current position.</returns>
    Task<BreadcrumbTrail> GetBreadcrumbsAsync(
        DocumentId documentId,
        TextPosition position,
        CancellationToken ct = default);

    /// <summary>
    /// Navigates to the position represented by a breadcrumb item.
    /// Scrolls the editor to make the item visible and positions the cursor appropriately.
    /// </summary>
    /// <param name="item">The breadcrumb item to navigate to.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when navigation is complete.</returns>
    Task NavigateToAsync(
        BreadcrumbItem item,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the children of a breadcrumb item for display in dropdown menus.
    /// Allows users to quickly select from sibling elements.
    /// </summary>
    /// <param name="parent">The parent breadcrumb item.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A list of child breadcrumb items.</returns>
    Task<IReadOnlyList<BreadcrumbItem>> GetChildrenAsync(
        BreadcrumbItem parent,
        CancellationToken ct = default);

    /// <summary>
    /// Gets an observable stream of breadcrumb updates.
    /// Subscribers are notified when the cursor moves to a new context.
    /// </summary>
    IObservable<BreadcrumbTrail> BreadcrumbUpdates { get; }
}

/// <summary>
/// A hierarchical trail of breadcrumb items from document root to current position.
/// </summary>
public record BreadcrumbTrail
{
    /// <summary>Gets the list of breadcrumb items from root to current position.</summary>
    public IReadOnlyList<BreadcrumbItem> Items { get; init; } = [];
}

/// <summary>
/// A single breadcrumb item in the navigation trail.
/// </summary>
public record BreadcrumbItem
{
    /// <summary>Gets the display label for this breadcrumb.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the type of element this breadcrumb represents.</summary>
    public required BreadcrumbItemType Type { get; init; }

    /// <summary>Gets the text range this breadcrumb corresponds to in the source.</summary>
    public TextRange? Range { get; init; }

    /// <summary>Gets the icon path for visual representation of the item type.</summary>
    public string? IconPath { get; init; }

    /// <summary>Gets whether this breadcrumb has children (for dropdown expansion).</summary>
    public bool HasChildren { get; init; }

    /// <summary>Gets optional custom data associated with this breadcrumb.</summary>
    public object? Data { get; init; }
}

/// <summary>
/// Types of breadcrumb items based on document structure.
/// </summary>
public enum BreadcrumbItemType
{
    /// <summary>File or document.</summary>
    File,

    /// <summary>Directory or folder.</summary>
    Folder,

    /// <summary>Markdown heading (H1-H6).</summary>
    Section,

    /// <summary>Code block or fenced code.</summary>
    CodeBlock,

    /// <summary>Function definition.</summary>
    Function,

    /// <summary>Class definition.</summary>
    Class,

    /// <summary>Namespace or module.</summary>
    Namespace,

    /// <summary>Generic symbol.</summary>
    Symbol
}
```

### 3.6 Editor Settings & Themes

```csharp
/// <summary>
/// Manages editor settings and theming system.
/// Provides persistent editor configuration, theme management, and support for custom themes.
/// </summary>
public interface IEditorSettings
{
    /// <summary>
    /// Gets the current editor configuration including font, indentation, cursor style, and chrome settings.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The current EditorConfig.</returns>
    Task<EditorConfig> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the editor configuration with new settings.
    /// Changes are persisted and applied to all current editor instances.
    /// </summary>
    /// <param name="config">The new EditorConfig to apply.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when the configuration has been updated.</returns>
    Task UpdateConfigAsync(
        EditorConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all available editor themes (built-in and custom).
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A list of available EditorTheme objects.</returns>
    Task<IReadOnlyList<EditorTheme>> GetThemesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Applies a theme by ID, updating the editor's visual appearance.
    /// The selected theme is persisted for future sessions.
    /// </summary>
    /// <param name="themeId">The ID of the theme to apply.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Completed task when the theme has been applied.</returns>
    Task ApplyThemeAsync(
        string themeId,
        CancellationToken ct = default);

    /// <summary>
    /// Imports a VS Code theme from JSON format.
    /// Validates the theme structure and registers it as a custom theme.
    /// Requires WriterPro license or higher for custom theme import.
    /// </summary>
    /// <param name="themeJson">The VS Code theme JSON to import.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The imported EditorTheme object.</returns>
    Task<EditorTheme> ImportVSCodeThemeAsync(
        string themeJson,
        CancellationToken ct = default);

    /// <summary>
    /// Gets an observable stream of editor configuration changes.
    /// Subscribers are notified when settings are updated.
    /// </summary>
    IObservable<EditorConfig> ConfigChanges { get; }
}

/// <summary>
/// Complete editor configuration including font, editing behavior, cursor, scrolling, and chrome settings.
/// </summary>
public record EditorConfig
{
    // Font Settings
    /// <summary>Gets the font family for the editor. Default: "JetBrains Mono, Consolas, monospace".</summary>
    public string FontFamily { get; init; } = "JetBrains Mono, Consolas, monospace";

    /// <summary>Gets the font size in pixels. Default: 14.</summary>
    public int FontSize { get; init; } = 14;

    /// <summary>Gets the line height in pixels. Default: 22.</summary>
    public int LineHeight { get; init; } = 22;

    /// <summary>Gets the font weight (thin, light, normal, medium, bold). Default: Normal.</summary>
    public FontWeight FontWeight { get; init; } = FontWeight.Normal;

    /// <summary>Gets whether font ligatures are enabled. Default: true.</summary>
    public bool FontLigatures { get; init; } = true;

    // Editing Behavior
    /// <summary>Gets the number of spaces per tab or tab width. Default: 4.</summary>
    public int TabSize { get; init; } = 4;

    /// <summary>Gets whether to insert spaces instead of tab characters. Default: true.</summary>
    public bool InsertSpaces { get; init; } = true;

    /// <summary>Gets whether word wrapping is enabled. Default: true.</summary>
    public bool WordWrap { get; init; } = true;

    /// <summary>Gets the column at which word wrapping occurs. Default: 80.</summary>
    public int WordWrapColumn { get; init; } = 80;

    /// <summary>Gets whether automatic indentation is enabled. Default: true.</summary>
    public bool AutoIndent { get; init; } = true;

    /// <summary>Gets whether brackets are automatically closed. Default: true.</summary>
    public bool AutoClosingBrackets { get; init; } = true;

    /// <summary>Gets whether quotes are automatically closed. Default: true.</summary>
    public bool AutoClosingQuotes { get; init; } = true;

    /// <summary>Gets whether automatic surround is enabled. Default: true.</summary>
    public bool AutoSurround { get; init; } = true;

    // Cursor Settings
    /// <summary>Gets the cursor style (line, block, or underline). Default: Line.</summary>
    public CursorStyle CursorStyle { get; init; } = CursorStyle.Line;

    /// <summary>Gets the cursor blinking style. Default: Blink.</summary>
    public CursorBlinking CursorBlinking { get; init; } = CursorBlinking.Blink;

    /// <summary>Gets whether smooth caret animation is enabled. Default: true.</summary>
    public bool SmoothCaretAnimation { get; init; } = true;

    // Scrolling Settings
    /// <summary>Gets whether smooth scrolling is enabled. Default: true.</summary>
    public bool SmoothScrolling { get; init; } = true;

    /// <summary>Gets how many lines beyond the last line can be scrolled. Default: 5.</summary>
    public int ScrollBeyondLastLine { get; init; } = 5;

    /// <summary>Gets whether scrolling past the document end is allowed. Default: false.</summary>
    public bool ScrollPastEnd { get; init; } = false;

    // Theme
    /// <summary>Gets the current theme ID. Default: "lexichord-dark".</summary>
    public string ThemeId { get; init; } = "lexichord-dark";

    // Chrome Configuration
    /// <summary>Gets the editor chrome configuration for line numbers, minimap, etc.</summary>
    public EditorChromeConfig Chrome { get; init; } = new();
}

/// <summary>
/// Font weight options.
/// </summary>
public enum CursorStyle { Line, Block, Underline }
public enum CursorBlinking { Blink, Smooth, Phase, Expand, Solid }
public enum FontWeight { Thin, Light, Normal, Medium, Bold }

/// <summary>
/// A complete editor theme definition.
/// </summary>
public record EditorTheme
{
    /// <summary>Gets the unique identifier for this theme.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the human-readable name of the theme.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the type of theme (light, dark, or high-contrast).</summary>
    public ThemeType Type { get; init; }

    /// <summary>Gets the color palette for the theme.</summary>
    public required ThemeColors Colors { get; init; }

    /// <summary>Gets the token-specific color rules for syntax highlighting.</summary>
    public IReadOnlyList<TokenColorRule> TokenColors { get; init; } = [];

    /// <summary>Gets a description of the theme.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the author or creator of the theme.</summary>
    public string? Author { get; init; }
}

/// <summary>
/// Type of color theme.
/// </summary>
public enum ThemeType { Light, Dark, HighContrast }

/// <summary>
/// Color palette for a theme.
/// </summary>
public record ThemeColors
{
    // Editor Colors
    /// <summary>Gets the editor background color.</summary>
    public required string EditorBackground { get; init; }

    /// <summary>Gets the editor foreground (text) color.</summary>
    public required string EditorForeground { get; init; }

    /// <summary>Gets the current line highlight color (optional).</summary>
    public string? EditorLineHighlight { get; init; }

    /// <summary>Gets the selection highlight color (optional).</summary>
    public string? EditorSelection { get; init; }

    /// <summary>Gets the cursor color (optional).</summary>
    public string? EditorCursor { get; init; }

    // Gutter Colors
    /// <summary>Gets the gutter background color (optional).</summary>
    public string? GutterBackground { get; init; }

    /// <summary>Gets the line number foreground color (optional).</summary>
    public string? LineNumberForeground { get; init; }

    /// <summary>Gets the active line number foreground color (optional).</summary>
    public string? LineNumberActiveForeground { get; init; }
}

/// <summary>
/// A rule for coloring syntax tokens based on their type.
/// </summary>
public record TokenColorRule
{
    /// <summary>Gets the syntax token types this rule applies to.</summary>
    public required IReadOnlyList<SyntaxTokenType> TokenTypes { get; init; }

    /// <summary>Gets the foreground color for matched tokens.</summary>
    public string? Foreground { get; init; }

    /// <summary>Gets the background color for matched tokens (optional).</summary>
    public string? Background { get; init; }

    /// <summary>Gets font style attributes (bold, italic, underline).</summary>
    public string? FontStyle { get; init; }
}
```

---

## 4. Architecture Overview

### 4.1 Component Interactions Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Editor Foundation (v0.17.1-EDT)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     ISplitViewManager                                   │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐ │ │
│  │  │         Split View Layout Management                            │ │ │
│  │  │                                                                  │ │ │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │ │ │
│  │  │  │   SplitAsync │  │ TogglePreview│  │SetLayoutMode │          │ │ │
│  │  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │ │ │
│  │  │         └──────────────────┼───────────────┘                   │ │ │
│  │  │                            ▼                                   │ │ │
│  │  │                   SplitViewLayout                              │ │ │
│  │  │                   (Mode, Panes[], Ratio)                       │ │ │
│  │  └──────────────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                   │                                         │
│          ┌────────────────────────┼────────────────────────┐               │
│          ▼                        ▼                        ▼               │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐    │
│  │ Editor Pane      │  │ Preview Pane     │  │ Chrome Components    │    │
│  │ (Code Editor)    │  │ (Live Preview)   │  │                      │    │
│  │                  │  │                  │  │ ┌────────────────────┐   │
│  │ ISyntaxHighlight │  │IMarkdownPreview │  │ │ IEditorChrome      │   │
│  │    Renderer      │  │   Renderer      │  │ │                    │   │
│  │                  │  │                 │  │ │ • Line Numbers     │   │
│  │ • Tokenization   │  │ • KaTeX Math    │  │ │ • Minimap          │   │
│  │ • Language Defs  │  │ • Mermaid Diag  │  │ │ • Gutter Decor.    │   │
│  │ • Tree-sitter    │  │ • Emoji Render  │  │ │ • Breadcrumbs      │   │
│  └──────────────────┘  │ • Task Lists    │  │ │ • Scrollbar        │   │
│                        │ • Footnotes     │  │ └────────────────────┘   │
│                        └──────────────────┘  │                          │
│  ┌──────────────────────────────────────────┼──────────────────────┐    │
│  │                                          ▼                      │    │
│  │                      ScrollMapping (Sync Scroll)               │    │
│  │                      SourceLine ↔ PreviewScrollPercent         │    │
│  └──────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                 IEditorSettings & Theme System                          │ │
│  │                                                                         │ │
│  │  EditorConfig (Font, Indentation, Cursor, Scrolling)                  │ │
│  │  ↓                                                                      │ │
│  │  EditorTheme (Colors, TokenColorRules, WCAG Compliance)               │ │
│  │  ↓                                                                      │ │
│  │  Theme Persistence (PostgreSQL, User Settings)                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │              IBreadcrumbNavigation                                      │ │
│  │                                                                         │ │
│  │  GetBreadcrumbsAsync() → BreadcrumbTrail                             │ │
│  │                                                                         │ │
│  │  File → Section → SubSection (Markdown)                              │ │
│  │  File → Namespace → Class → Method (Code)                            │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                        ┌────────────────────────────┐
                        │   License Gating Layer     │
                        │                            │
                        │ Core: Editor only          │
                        │ WriterPro: +Preview        │
                        │ Teams: +Custom Themes      │
                        │ Enterprise: +White-label   │
                        └────────────────────────────┘
```

### 4.2 Data Flow: Live Preview Update

```
Editor Content Changed
     │
     ▼
ISyntaxHighlighter.TokenizeAsync()
     │
     ├─→ Token Stream (for editor display)
     │
     ▼
IMarkdownPreviewRenderer.RenderIncrementalAsync()
     │
     ├─→ Determine change scope (block-level or character-level)
     │
     ├─→ If character-level: Re-render only affected block
     │
     ├─→ If block-level: Full document re-render
     │
     ▼
RenderedPreview (HTML + Metadata)
     │
     ├─→ ScrollMapping (source lines → preview scroll %)
     │
     ├─→ PreviewSections (source ranges → HTML elements)
     │
     ├─→ PreviewHeadings (for TOC)
     │
     ▼
UI Update
     │
     ├─→ Update preview HTML
     │
     ├─→ Update breadcrumbs (if heading changed)
     │
     └─→ Update minimap
```

### 4.3 Split View State Management

```
SplitViewMode Selection
     │
     ├─→ EditorOnly
     │   └─→ [       EDITOR        ]
     │
     ├─→ PreviewOnly
     │   └─→ [      PREVIEW        ]
     │
     ├─→ SplitHorizontal (mode:0.5)
     │   └─→ [  EDITOR  |  PREVIEW ]
     │
     ├─→ SplitVertical (mode:0.5)
     │   └─→ ┌────────────────────┐
     │       │      EDITOR        │
     │       ├────────────────────┤
     │       │     PREVIEW        │
     │       └────────────────────┘
     │
     └─→ SyncScroll
         └─→ [  EDITOR  |  PREVIEW ]
             (scroll positions synchronized)
```

---

## 5. PostgreSQL Database Schema

### 5.1 Editor State Persistence

```sql
-- Editor pane configurations and state
CREATE TABLE editor_panes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    pane_type VARCHAR(50) NOT NULL, -- 'Editor', 'Preview', 'Diff', 'Outline', 'Terminal'
    document_id UUID,
    width_ratio REAL NOT NULL DEFAULT 0.5,
    height_ratio REAL NOT NULL DEFAULT 0.5,
    is_focused BOOLEAN NOT NULL DEFAULT FALSE,
    view_state_json JSONB, -- scroll position, cursor position, selection
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_editor_panes_user ON editor_panes(user_id),
    INDEX idx_editor_panes_document ON editor_panes(document_id),
    CONSTRAINT fk_editor_panes_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Split view layouts (per user or per document)
CREATE TABLE split_view_layouts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    document_id UUID,
    mode VARCHAR(50) NOT NULL, -- 'EditorOnly', 'PreviewOnly', 'SplitHorizontal', 'SplitVertical', 'SyncScroll'
    layout_json JSONB NOT NULL, -- complete SplitViewLayout serialized
    split_ratio REAL NOT NULL DEFAULT 0.5,
    preview_visible BOOLEAN NOT NULL DEFAULT TRUE,
    sync_scroll_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_split_view_layouts_user ON split_view_layouts(user_id),
    INDEX idx_split_view_layouts_document ON split_view_layouts(document_id, user_id),
    CONSTRAINT fk_split_view_layouts_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_split_view_layouts_document FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE
);

-- Editor configurations (font, indentation, cursor style, etc.)
CREATE TABLE editor_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE,
    font_family VARCHAR(200) NOT NULL DEFAULT 'JetBrains Mono, Consolas, monospace',
    font_size INT NOT NULL DEFAULT 14,
    line_height INT NOT NULL DEFAULT 22,
    font_weight VARCHAR(50) NOT NULL DEFAULT 'Normal',
    font_ligatures BOOLEAN NOT NULL DEFAULT TRUE,
    tab_size INT NOT NULL DEFAULT 4,
    insert_spaces BOOLEAN NOT NULL DEFAULT TRUE,
    word_wrap BOOLEAN NOT NULL DEFAULT TRUE,
    word_wrap_column INT NOT NULL DEFAULT 80,
    auto_indent BOOLEAN NOT NULL DEFAULT TRUE,
    auto_closing_brackets BOOLEAN NOT NULL DEFAULT TRUE,
    auto_closing_quotes BOOLEAN NOT NULL DEFAULT TRUE,
    auto_surround BOOLEAN NOT NULL DEFAULT TRUE,
    cursor_style VARCHAR(50) NOT NULL DEFAULT 'Line', -- 'Line', 'Block', 'Underline'
    cursor_blinking VARCHAR(50) NOT NULL DEFAULT 'Blink',
    smooth_caret_animation BOOLEAN NOT NULL DEFAULT TRUE,
    smooth_scrolling BOOLEAN NOT NULL DEFAULT TRUE,
    scroll_beyond_last_line INT NOT NULL DEFAULT 5,
    scroll_past_end BOOLEAN NOT NULL DEFAULT FALSE,
    theme_id VARCHAR(100) NOT NULL DEFAULT 'lexichord-dark',
    chrome_config_json JSONB, -- EditorChromeConfig serialized
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_editor_configs_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_editor_configs_user ON editor_configs(user_id)
);

-- Editor themes (built-in and custom)
CREATE TABLE editor_themes (
    id VARCHAR(100) PRIMARY KEY,
    user_id UUID, -- NULL for built-in themes
    name VARCHAR(200) NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'Light', 'Dark', 'HighContrast'
    colors_json JSONB NOT NULL, -- ThemeColors serialized
    token_colors_json JSONB, -- TokenColorRule[] serialized
    description TEXT,
    author VARCHAR(200),
    is_built_in BOOLEAN NOT NULL DEFAULT TRUE,
    is_shared BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_editor_themes_user ON editor_themes(user_id) WHERE user_id IS NOT NULL,
    INDEX idx_editor_themes_built_in ON editor_themes(is_built_in) WHERE is_built_in = TRUE,
    INDEX idx_editor_themes_shared ON editor_themes(is_shared) WHERE is_shared = TRUE,
    CONSTRAINT fk_editor_themes_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Gutter decorations
CREATE TABLE gutter_decorations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL,
    line_number INT NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'Breakpoint', 'Bookmark', 'Error', 'Warning', etc.
    tooltip TEXT,
    icon_path VARCHAR(500),
    color VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_gutter_decorations_document ON gutter_decorations(document_id),
    INDEX idx_gutter_decorations_line ON gutter_decorations(document_id, line_number),
    CONSTRAINT fk_gutter_decorations_document FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE
);

-- Breadcrumb cache (for performance)
CREATE TABLE breadcrumb_cache (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL,
    content_hash VARCHAR(64) NOT NULL,
    breadcrumbs_json JSONB NOT NULL, -- Cached BreadcrumbTrail[]
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(document_id, content_hash),
    INDEX idx_breadcrumb_cache_document ON breadcrumb_cache(document_id),
    CONSTRAINT fk_breadcrumb_cache_document FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE
);

-- Preview cache (for incremental rendering)
CREATE TABLE preview_cache (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL,
    content_hash VARCHAR(64) NOT NULL,
    rendered_html TEXT NOT NULL,
    sections_json JSONB, -- PreviewSection[] serialized
    headings_json JSONB, -- PreviewHeading[] serialized
    scroll_mapping_json JSONB, -- ScrollMapping serialized
    render_time_ms INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(document_id, content_hash),
    INDEX idx_preview_cache_document ON preview_cache(document_id),
    CONSTRAINT fk_preview_cache_document FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE
);
```

### 5.2 License Gating Schema

```sql
-- Feature access by license tier
CREATE TABLE license_feature_access (
    feature_id VARCHAR(100) PRIMARY KEY,
    feature_name VARCHAR(200) NOT NULL,
    description TEXT,
    minimum_tier VARCHAR(50) NOT NULL, -- 'Core', 'WriterPro', 'Teams', 'Enterprise'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Insert feature access rules
INSERT INTO license_feature_access VALUES
    ('feature.editor.splitview', 'Split View', 'Multiple pane layouts', 'WriterPro'),
    ('feature.editor.preview', 'Live Markdown Preview', 'Real-time preview pane', 'WriterPro'),
    ('feature.editor.syntax', 'Enhanced Syntax Highlighting', 'Multi-language tokenization', 'WriterPro'),
    ('feature.editor.minimap', 'Minimap', 'Visual document overview', 'WriterPro'),
    ('feature.editor.custom_themes', 'Custom Themes', 'Import custom editor themes', 'Teams'),
    ('feature.editor.white_label', 'White-label Theming', 'Custom branding themes', 'Enterprise'),
    ('feature.editor.chrome_advanced', 'Advanced Chrome', 'All editor chrome features', 'Teams');
```

---

## 6. UI Mockups and Layout Diagrams

### 6.1 Split View Horizontal Layout

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Document: "analysis.md" [WriterPro]        [Maximize] [Close]               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│ Breadcrumb: Root > Analysis > Methodology                                    │
│                                                                               │
├─────────────────────────────────────────────────────────────┬────────────────┤
│                                                              │                │
│ 1  # Market Analysis                                        │ # Market Analy │
│ 2                                                           │   │            │
│ 3  ## Methodology                                           │ ## Methodology │
│ 4  We analyzed competitor pricing using:                    │ We analyzed c  │
│ 5                                                           │ competitor pri │
│ 6  - Price Point Analysis                                  │                │
│ 7  - Feature Comparison                                    │ - Price Point  │
│ 8  - Customer Segment Targeting                            │ - Feature      │
│ 9                                                           │ - Customer     │
│10  ### Price Points                                        │ ### Price      │
│11  Competitor A: $49/month                                 │ Competitor A:  │
│12  Competitor B: $79/month                                 │ Competitor B:  │
│13  Competitor C: $129/month                                │ Competitor C:  │
│14                                                           │                │
│15  ## Key Findings                                         │ ## Key Findings│
│16                                                           │                │
│   └─ Fold indicator                  [Minimap ░░░] ▲       │ Scrollbar ▲    │
│                                                       │       │        ▲│     │
│                                                       ▼       │        ││     │
│                                                               │        ▼     │
│ [Line numbers] | [Editor pane] | [Divider] | [Preview pane]│        ▼     │
├────────────────────────────────────────────────────────────────────────────┤
│ Gutter (60px)      │ Editor                     │ Preview                   │
└────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Split View Vertical Layout (Sync Scroll)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Document: "guide.md"  [SyncScroll Mode]       [Layouts ▼]                  │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│ File > Documentation > Installation Guide  [Breadcrumb Navigation]          │
│                                                                               │
│┌─────────────────────────────────────────────────────────────────────────────┐
││ EDITOR                                                                       │
│├─────────────────────────────────────────────────────────────────────────────┤
││ 1  # Installation Guide                                                     │
││ 2                                                                           │
││ 3  ## Prerequisites                                                         │
││ 4  - Node.js 16+                                                           │
││ 5  - npm or yarn                                                           │
││ 6                                                                           │
││ 7  ## Installation Steps                                                    │
││ 8                                                                           │
││ 9  ```bash                                                                 │
││10  npm install lexichord-editor                                            │
││11  ```                                                                     │
││ 12                                                                          │
││ 13  ## Configuration                                                        │
││ 14                                                                          │
│   └─ [Minimap]                              [Scroll Position Indicator]     │
│├─────────────────────────────────────────────────────────────────────────────┤
││ LIVE PREVIEW                                                                │
│├─────────────────────────────────────────────────────────────────────────────┤
││                                                                              │
││ Installation Guide                                                          │
││                                                                              │
││ Prerequisites                                                               │
││ • Node.js 16+                                                              │
││ • npm or yarn                                                              │
││                                                                              │
││ Installation Steps                                                          │
││                                                                              │
││ npm install lexichord-editor                                               │
││                                                                              │
││ Configuration                                                               │
││                                                                              │
│   [Scroll synchronized with editor above]   [Minimap]                      │
│└─────────────────────────────────────────────────────────────────────────────┘
└──────────────────────────────────────────────────────────────────────────────┘
```

### 6.3 Editor Chrome Elements Detail

```
┌─────┬───────────────────────────────────────────────────────────────┬──────────┐
│ GUT │ LINE NUMBERS                                                  │ MINIMAP  │
│ TER │                                                               │          │
│ (60 │ ┌─────────────────────────────────────────────────────────┐  │ ┌──────┐ │
│ px) │ │  1  # Document Heading                                  │  │ │░░░░░░│ │
│     │ │     ▪ [bookmark icon]                                   │  │ │░░░░░░│ │
│     │ │                                                         │  │ │░░░░░░│ │
│     │ │  2  This is a paragraph with content.                  │  │ │░░░░░░│ │
│     │ │                                                         │  │ │░░░░░░│ │
│     │ │  3  ## Section Heading                                 │  │ │░░░░░░│ │
│     │ │     ⚠  [warning icon]                                   │  │ │░░░░░░│ │
│     │ │                                                         │  │ │░░░░░░│ │
│     │ │  4  Code example:                                       │  │ │░░░░░░│ │
│     │ │     ┌──                                                 │  │ │░░░░░░│ │
│     │ │  5  │ const x = 42;                                    │  │ │░░░░░░│ │
│     │ │     └──  [fold controls]                               │  │ │░░░░░░│ │
│     │ │                                                         │  │ │░░░░░░│ │
│     │ │ 10  ### Subsection                                     │  │ │░░░░░░│ │
│     │ │                                                         │  │ │░░░░░░│ │
│     │ └─────────────────────────────────────────────────────────┘  │ │░░░░░░│ │
│     │ [Indent guides]                          [Scroll position]   │ └──────┘ │
│     │                                            indicator ▓ ▓    │ [Mini   │
│     │                                                           │ │  map]   │
└─────┴───────────────────────────────────────────────────────────────┴──────────┘
         [Decoration]      [Editor Content]                [Scrollbar]   [Scale]
         Icons:                                             [Always      10%
         • Breakpoint (●)                                   visible or
         • Bookmark (★)                                     overlay]
         • Error (✕)
         • Warning (⚠)
         • Info (ℹ)
         • Fold (▶/▼)
```

### 6.4 Breadcrumb Navigation with Dropdown

```
File > Documentation > Installation > Prerequisites

     ▼ File dropdown
     ├─ README.md
     ├─ CHANGELOG.md
     └─ guide.md (current)

                ▼ Documentation dropdown
                ├─ overview.md
                ├─ installation.md (current)
                ├─ configuration.md
                └─ api-reference.md

                            ▼ Installation dropdown (subsections)
                            ├─ Prerequisites (current)
                            ├─ Installation Steps
                            └─ Configuration

                                          ▼ Prerequisites dropdown (subheadings)
                                          ├─ Required Software
                                          └─ System Requirements (current)
```

### 6.5 Settings Panel (Editor Configuration)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Editor Settings                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ APPEARANCE                                                                   │
│ ├─ Theme: [lexichord-dark ▼]  [Import Custom Theme...]  [Preview]         │
│ ├─ Font Family: [JetBrains Mono ▼]                                         │
│ ├─ Font Size: [14 ▼]  px                                                   │
│ ├─ Line Height: [22 ▼]  px                                                 │
│ ├─ Font Weight: [Normal ▼]                                                 │
│ └─ Font Ligatures: [✓]                                                     │
│                                                                              │
│ EDITING BEHAVIOR                                                             │
│ ├─ Tab Size: [4 ▼]                                                         │
│ ├─ Insert Spaces: [✓]                                                      │
│ ├─ Word Wrap: [✓]                                                          │
│ ├─ Word Wrap Column: [80]                                                  │
│ ├─ Auto Indent: [✓]                                                        │
│ ├─ Auto-closing Brackets: [✓]                                              │
│ ├─ Auto-closing Quotes: [✓]                                                │
│ └─ Auto Surround: [✓]                                                      │
│                                                                              │
│ CURSOR & SCROLLING                                                           │
│ ├─ Cursor Style: [Line ▼]                                                  │
│ ├─ Cursor Blinking: [Blink ▼]                                              │
│ ├─ Smooth Caret Animation: [✓]                                             │
│ ├─ Smooth Scrolling: [✓]                                                   │
│ ├─ Scroll Beyond Last Line: [5 ▼]  lines                                   │
│ └─ Scroll Past End: [ ]                                                    │
│                                                                              │
│ CHROME ELEMENTS                                                              │
│ ├─ Show Line Numbers: [✓]                                                  │
│ ├─ Line Number Style: [Absolute ▼]                                         │
│ ├─ Show Minimap: [✓]                                                       │
│ ├─ Minimap Position: [Right ▼]                                             │
│ ├─ Minimap Scale: [0.1 ▼]                                                  │
│ ├─ Minimap Render Characters: [✓]                                          │
│ ├─ Show Folding Controls: [✓]                                              │
│ ├─ Show Glyph Margin: [✓]                                                  │
│ ├─ Gutter Width: [60 ▼]  px                                                │
│ ├─ Show Breadcrumbs: [✓]                                                   │
│ ├─ Show Indent Guides: [✓]                                                 │
│ ├─ Highlight Current Line: [✓]                                             │
│ ├─ Show Whitespace: [ ]                                                    │
│ ├─ Show Rulers: [ ]                                                        │
│ └─ Ruler Columns: [80, 120]                                                │
│                                                                              │
│ LAYOUT & PREVIEW                                                             │
│ ├─ Default Layout: [SplitHorizontal ▼]                                     │
│ ├─ Preview Theme: [Auto ▼]                                                 │
│ ├─ Sync Scroll: [✓]                                                        │
│ └─ Preview Options:                                                        │
│    ├─ Syntax Highlighting: [✓]                                             │
│    ├─ Math Rendering: [✓]                                                  │
│    ├─ Diagram Rendering: [✓]                                               │
│    ├─ Emoji Rendering: [✓]                                                 │
│    ├─ Task Lists: [✓]                                                      │
│    ├─ Table of Contents: [✓]                                               │
│    └─ Footnotes: [✓]                                                       │
│                                                                              │
│                                                  [Save Changes] [Reset]     │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Dependencies on Prior Versions

| Dependency | Version | Usage | Impact if Missing |
|:-----------|:--------|:------|:------------------|
| Core Editor | v0.17.0-EDC | Base editor component, text manipulation, document model | Critical: Cannot render editor panes |
| Orchestration API | v0.16.2-ORC | Workflow execution context for editor events | Medium: Preview updates may not sync |
| Renderer | v0.15.1-RNR | Canvas/viewport rendering, scrolling synchronization | High: Split view rendering fails |
| License Service | v0.2.1a | Feature access gating by license tier | High: Cannot enforce feature restrictions |
| Settings Service | v0.1.6a | User preferences persistence | Medium: Settings not persisted between sessions |
| MediatR/Events | v0.0.7a | Event publication for state changes | Medium: Real-time updates unavailable |
| Markdown-it/marked.js | 13.x | Markdown parsing for preview rendering | Critical: Preview pane non-functional |
| highlight.js/Prism.js | 11.x/1.x | Syntax highlighting library | High: Syntax highlighting unavailable |
| KaTeX | 0.16.x | Math equation rendering | Medium: Math rendering unavailable |
| Mermaid | 10.x | Diagram rendering | Medium: Diagram rendering unavailable |

---

## 8. License Gating

### 8.1 Feature Access Table

| Feature | Core | WriterPro | Teams | Enterprise | Notes |
|:--------|:-----|:----------|:------|:-----------|:------|
| Editor (Basic) | ✓ | ✓ | ✓ | ✓ | Read-only in Core tier |
| Split View (Multiple Panes) | ✗ | ✓ | ✓ | ✓ | Requires WriterPro or higher |
| Live Markdown Preview | ✗ | ✓ | ✓ | ✓ | Real-time rendering |
| Enhanced Syntax Highlighting | ✗ | ✓ | ✓ | ✓ | Multi-language support |
| Editor Chrome (Line Numbers, Minimap) | ✗ | ✓ | ✓ | ✓ | All gutter decorations |
| Breadcrumb Navigation | ✗ | ✓ | ✓ | ✓ | Document structure navigation |
| Custom Editor Themes (Import) | ✗ | ✗ | ✓ | ✓ | Import VS Code themes |
| Built-in Themes | ✗ | ✓ | ✓ | ✓ | 5+ pre-built themes |
| White-label Theming | ✗ | ✗ | ✗ | ✓ | Custom branding themes |
| Advanced Chrome Options | ✗ | ✓ | ✓ | ✓ | All configuration options |
| Synchronized Scrolling | ✗ | ✓ | ✓ | ✓ | Editor ↔ Preview sync |
| Preview Theme Customization | ✗ | ✓ | ✓ | ✓ | Auto/Light/Dark/Sepia/HighContrast |

---

## 9. Performance Targets

### 9.1 Rendering Performance

| Metric | Target | P95 SLA | Measurement Method |
|:-------|:-------|:--------|:-------------------|
| Split view render frame time | <16ms | P95 <20ms | Frame time profiler; 60fps target |
| Preview incremental update | <200ms | P95 <250ms | Benchmark: 10,000 line doc, 1-line change |
| Syntax tokenization (10K lines) | <100ms | P95 <150ms | Benchmark: C#, JavaScript, Markdown |
| Minimap generation | <50ms | P95 <75ms | Benchmark: 50,000 lines, 10% scale |
| Breadcrumb computation | <50ms | P95 <75ms | Benchmark: deeply nested structure |
| Scroll mapping calculation | <30ms | P95 <50ms | Benchmark: full document with anchors |

### 9.2 Interactive Performance

| Metric | Target | P95 SLA | Notes |
|:-------|:-------|:--------|:------|
| Pane resize response | <16ms | P95 <25ms | Immediate visual feedback during drag |
| Zoom/pan response | <16ms | P95 <25ms | Smooth 60fps interaction |
| Split layout switch | <100ms | P95 <150ms | Complete layout recalculation |
| Theme application | <200ms | P95 <300ms | All elements re-styled |
| Settings save | <50ms | P95 <100ms | Database persistence |

### 9.3 Memory Performance

| Metric | Target | Notes |
|:-------|:-------|:------|
| Editor pane memory | <50MB | Single pane with syntax highlighting |
| Preview cache | <100MB | Cached renders for last 50 documents |
| Minimap memory | <10MB | Single instance for 100K lines |
| Breadcrumb cache | <5MB | 1000 documents cached |
| Total editor footprint | <200MB | All components combined, idle state |

---

## 10. Comprehensive Testing Strategy

### 10.1 Unit Tests

**Split View Manager Tests:**
- ✓ SplitAsync creates pane with correct direction
- ✓ SplitAsync respects ratio parameter
- ✓ ClosePaneAsync removes pane from layout
- ✓ ClosePaneAsync prevents closing last editor pane
- ✓ SetLayoutAsync transitions between modes
- ✓ ResizePanesAsync updates split ratio
- ✓ SwapPanesAsync exchanges pane positions
- ✓ LayoutChanges observable emits events

**Preview Renderer Tests:**
- ✓ RenderAsync produces valid HTML
- ✓ RenderIncrementalAsync detects block-level changes
- ✓ RenderIncrementalAsync falls back to full render for structure changes
- ✓ Math rendering converts LaTeX to HTML
- ✓ Diagram rendering processes Mermaid syntax
- ✓ ScrollMapping produces correct anchors
- ✓ Preview sections map to correct HTML elements

**Syntax Highlighter Tests:**
- ✓ TokenizeAsync produces correct token types
- ✓ TokenizeAsync handles all supported languages
- ✓ DetectLanguageAsync correctly identifies language
- ✓ RegisterLanguageAsync persists custom languages
- ✓ Token modifiers applied correctly
- ✓ TextMate scope calculation correct

**Editor Chrome Tests:**
- ✓ AddGutterDecorationAsync creates decoration with ID
- ✓ RemoveGutterDecorationAsync removes decoration
- ✓ SetLineHighlightAsync applies highlight style
- ✓ GetMinimapDataAsync returns correct dimensions
- ✓ MinimapScale parameter affects output

**Breadcrumb Navigation Tests:**
- ✓ GetBreadcrumbsAsync returns correct trail
- ✓ BreadcrumbTrail shows full path from root
- ✓ GetChildrenAsync returns sibling items
- ✓ NavigateToAsync positions cursor correctly
- ✓ BreadcrumbUpdates emits on cursor movement

**Editor Settings Tests:**
- ✓ GetConfigAsync returns current config
- ✓ UpdateConfigAsync persists changes
- ✓ ApplyThemeAsync switches theme
- ✓ ImportVSCodeThemeAsync validates theme JSON
- ✓ ConfigChanges observable emits on update

### 10.2 Integration Tests

**Split View + Preview Rendering:**
- ✓ Content edits trigger preview updates
- ✓ Scroll in editor updates preview scroll mapping
- ✓ Switching layout modes preserves document content
- ✓ Pane focus changes update breadcrumbs

**Preview + Syntax Highlighting:**
- ✓ Code blocks in preview are syntax highlighted
- ✓ Language auto-detection works in preview
- ✓ Theme changes update code block colors

**Settings + Theme Application:**
- ✓ Theme colors override syntax token colors
- ✓ Font settings apply to all panes
- ✓ Chrome configuration applies correctly
- ✓ Settings persist across editor reload

**License Gating:**
- ✓ Core tier users cannot access split view
- ✓ WriterPro users can access preview
- ✓ Teams users can import custom themes
- ✓ Enterprise users can white-label

### 10.3 Performance Tests

**Rendering Performance:**
- ✓ Split view renders 3 panes at 60fps
- ✓ Preview incremental render <200ms for typical edits
- ✓ Minimap renders 100K-line document in <50ms
- ✓ Syntax highlighting 10K lines in <100ms

**Stress Tests:**
- ✓ Render 50,000 line document without lag
- ✓ Rapid pane resizing maintains 60fps
- ✓ 100+ gutter decorations render without slowdown
- ✓ Memory usage stays <200MB for typical workflow

**Concurrency Tests:**
- ✓ Simultaneous edits in multiple panes
- ✓ Preview rendering while user is typing
- ✓ Settings changes don't block UI
- ✓ Scroll synchronization with high-frequency updates

### 10.4 UI/E2E Tests

**Split View Interactions:**
- ✓ Drag split divider to resize panes
- ✓ Double-click divider to swap panes
- ✓ Toolbar button toggles preview pane
- ✓ Layout menu switches between preset layouts
- ✓ Keyboard shortcut (Ctrl+K Ctrl+E) opens split menu

**Editor Chrome Interactions:**
- ✓ Click line number to go to line
- ✓ Click minimap to scroll document
- ✓ Hover over gutter decoration shows tooltip
- ✓ Click fold indicator expands/collapses
- ✓ Breadcrumb dropdown shows siblings

**Settings UI:**
- ✓ Theme selector applies theme immediately
- ✓ Font size slider adjusts editor
- ✓ Checkbox toggles enable/disable features
- ✓ Save button persists all changes
- ✓ Reset button restores defaults

**Mobile/Responsive:**
- ✓ Editor scales correctly at 1024x768
- ✓ Touch gestures work on split divider
- ✓ Minimap hidden on small screens
- ✓ Breadcrumbs collapse on mobile
- ✓ 4K display renders without scaling issues

---

## 11. Risks & Mitigations

### 11.1 Technical Risks

| Risk | Probability | Impact | Severity | Mitigation |
|:-----|:-----------|:-------|:---------|:-----------|
| **Split view rendering performance degradation** | Medium (40%) | Users experience lag with 3+ panes or 50K+ lines | High | Implement virtual rendering; level-of-detail culling; test with real-world documents |
| **Preview render timeout on very large documents** | Low (20%) | Preview fails to render, user sees blank pane | High | Implement rendering timeout with graceful fallback; chunked rendering; abort signals |
| **Scroll mapping misalignment between editor and preview** | Medium (35%) | Editor and preview scroll out of sync, confusing UX | Medium | Comprehensive scroll anchor testing; automated scroll diff validation; visual regression tests |
| **Memory leaks in incremental rendering** | Low (15%) | Memory grows unbounded; editor becomes unusable | High | Profile with heap snapshots; implement aggressive garbage collection; cycle detection |
| **Syntax highlighting grammar parsing errors** | Medium (30%) | Tokens malformed, rendering artifacts appear | Medium | Validation layer for grammar JSON; fallback to default highlighting; error logging |
| **Theme color conflicts with accessibility** | Low (25%) | Low-contrast colors violate WCAG AA; compliance issues | Medium | WCAG AA validation layer; contrast ratio checking; accessibility testing suite |
| **Font ligatures not supported in all browsers** | Low (20%) | Editor renders incorrectly on Safari/Firefox | Low | Feature detection; ligature fallback; browser-specific CSS |
| **Breadcrumb cache invalidation bugs** | Low (15%) | Stale breadcrumbs shown after document edits | Medium | Content hash validation; cache TTL limits; manual invalidation on structure change |
| **Pane layout state corruption** | Low (10%) | User's layout preferences lost; panes in invalid state | Medium | Layout validation on load; versioned schema; state recovery mechanism |
| **License gating bypass vulnerability** | Low (5%) | Users access paid features without license | Critical | Server-side enforcement; watermark token validation; audit logging |

### 11.2 Mitigation Strategies

**1. Performance Degradation**
- Implement Progressive Enhancement: Start with basic editor-only view, add panes as needed
- Use Web Workers for syntax highlighting and preview rendering
- Implement virtual scrolling in minimap for large documents
- Profile and benchmark with real-world large documents (500K+ lines)

**2. Preview Render Timeout**
- Set 5-second timeout with user notification
- Implement chunked rendering: render first 1000 lines immediately, rest in background
- Add abort signal support to allow user cancellation
- Queue preview renders with priority queue (edited section first)

**3. Scroll Mapping Misalignment**
- Automated tests comparing editor scroll position to preview element position
- Visual regression tests with screenshots
- Implement bidirectional scroll handlers to test both directions
- Add scroll position diff logging for debugging

**4. Memory Leaks**
- Monthly heap snapshot analysis in performance tests
- WeakMap usage for element references
- Explicit cleanup in component unmount
- Memory profiling CI checks with memory regression detection

**5. Syntax Highlighting Errors**
- JSON Schema validation for grammar definitions
- Fallback to plain text if tokenization fails
- Comprehensive unit tests for each language grammar
- Error boundary component to prevent full app crash

**6. Accessibility Compliance**
- WCAG AA contrast ratio checking in theme validation
- Automated accessibility tests (axe-core)
- High-contrast theme as mandatory built-in option
- Annual accessibility audit

**7. Browser Compatibility**
- Feature detection: `CSS.supports('font-feature-settings')`
- Fallback CSS without ligatures
- Comprehensive browser compatibility matrix testing
- Polyfill strategy for older browsers

**8. Breadcrumb Cache Invalidation**
- Content hash SHA-256 based cache key
- 24-hour TTL with manual invalidation on structure change
- Periodic cache validation (compare cached vs. computed)
- Cache versioning for schema changes

**9. Layout State Corruption**
- Validate layout JSON schema on load
- Version layout schema; implement migration logic
- Detect invalid pane states; reset to default if invalid
- Auto-save layout state every 30 seconds

**10. License Gating**
- Server-side feature access check before rendering
- Watermark token validation with expiration
- Audit logging of all feature access attempts
- Monthly license compliance audit

---

## 12. MediatR Events and Domain Events

### 12.1 Event Definitions

```csharp
/// <summary>
/// Base class for all editor domain events.
/// </summary>
public abstract record EditorEvent
{
    /// <summary>Gets the timestamp when the event was raised.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

// Split View Events
public record SplitViewCreatedEvent(
    PaneId PaneId,
    SplitDirection Direction,
    float Ratio) : EditorEvent;

public record SplitViewClosedEvent(
    PaneId PaneId) : EditorEvent;

public record SplitViewLayoutChangedEvent(
    SplitViewMode Mode,
    IReadOnlyList<EditorPane> Panes,
    float SplitRatio) : EditorEvent;

public record PreviewVisibilityToggledEvent(
    bool IsVisible) : EditorEvent;

public record PaneResizedEvent(
    PaneId PaneId,
    float OldRatio,
    float NewRatio) : EditorEvent;

public record PanesSwappedEvent(
    PaneId Pane1Id,
    PaneId Pane2Id) : EditorEvent;

// Preview Rendering Events
public record PreviewRenderStartedEvent(
    DocumentId DocumentId) : EditorEvent;

public record PreviewRenderCompletedEvent(
    DocumentId DocumentId,
    string SourceHash,
    TimeSpan RenderTime,
    bool IsIncremental) : EditorEvent;

public record PreviewRenderFailedEvent(
    DocumentId DocumentId,
    string ErrorMessage) : EditorEvent;

public record ScrollMappingUpdatedEvent(
    DocumentId DocumentId,
    ScrollMapping Mapping) : EditorEvent;

public record SyncScrollEnabledEvent(bool Enabled) : EditorEvent;

// Syntax Highlighting Events
public record LanguageDetectedEvent(
    DocumentId DocumentId,
    string LanguageId,
    float Confidence) : EditorEvent;

public record SyntaxHighlightingAppliedEvent(
    DocumentId DocumentId,
    int TokenCount) : EditorEvent;

public record CustomLanguageRegisteredEvent(
    string LanguageId,
    string Name) : EditorEvent;

// Editor Chrome Events
public record GutterDecorationAddedEvent(
    DocumentId DocumentId,
    int Line,
    GutterDecorationType Type,
    Guid DecorationId) : EditorEvent;

public record GutterDecorationRemovedEvent(
    Guid DecorationId) : EditorEvent;

public record MinimapGeneratedEvent(
    DocumentId DocumentId,
    int Width,
    int Height) : EditorEvent;

public record LineHighlightAppliedEvent(
    DocumentId DocumentId,
    int Line,
    LineHighlightType Type) : EditorEvent;

// Breadcrumb Events
public record BreadcrumbTrailUpdatedEvent(
    DocumentId DocumentId,
    BreadcrumbTrail Trail) : EditorEvent;

public record BreadcrumbNavigatedEvent(
    BreadcrumbItem Item,
    TextPosition NewPosition) : EditorEvent;

// Settings & Theme Events
public record EditorConfigUpdatedEvent(
    EditorConfig NewConfig) : EditorEvent;

public record ThemeAppliedEvent(
    string ThemeId,
    EditorTheme Theme) : EditorEvent;

public record CustomThemeImportedEvent(
    string ThemeId,
    string ThemeName) : EditorEvent;

public record SettingsPersisted(
    EditorConfig Config) : EditorEvent;

// License Gating Events
public record FeatureAccessCheckedEvent(
    string FeatureId,
    bool HasAccess,
    LicenseTier Tier) : EditorEvent;

public record FeatureAccessDeniedEvent(
    string FeatureId,
    LicenseTier RequiredTier,
    LicenseTier CurrentTier) : EditorEvent;
```

### 12.2 Event Handlers (Sample)

```csharp
/// <summary>
/// Handles preview render completion and updates breadcrumbs if needed.
/// </summary>
public class PreviewRenderCompletedEventHandler : INotificationHandler<PreviewRenderCompletedEvent>
{
    private readonly IBreadcrumbNavigation _breadcrumbs;

    public async Task Handle(PreviewRenderCompletedEvent notification, CancellationToken ct)
    {
        // Update breadcrumbs if document structure may have changed
        if (!notification.IsIncremental)
        {
            await _breadcrumbs.UpdateAsync(notification.DocumentId, ct);
        }
    }
}

/// <summary>
/// Persists editor settings whenever configuration changes.
/// </summary>
public class EditorConfigUpdatedEventHandler : INotificationHandler<EditorConfigUpdatedEvent>
{
    private readonly IEditorSettingsRepository _repository;

    public async Task Handle(EditorConfigUpdatedEvent notification, CancellationToken ct)
    {
        await _repository.SaveAsync(notification.NewConfig, ct);
    }
}

/// <summary>
/// Validates feature access when features are accessed.
/// </summary>
public class FeatureAccessCheckedEventHandler : INotificationHandler<FeatureAccessCheckedEvent>
{
    private readonly IAuditLogger _auditLogger;

    public async Task Handle(FeatureAccessCheckedEvent notification, CancellationToken ct)
    {
        await _auditLogger.LogFeatureAccessAsync(
            notification.FeatureId,
            notification.HasAccess,
            notification.Tier,
            ct);
    }
}
```

---

## 13. Implementation Phases

### Phase 1: Core Foundation (Weeks 1-2, 12 hours)
- **v0.17.1e: Split View Manager**
  - Basic pane creation, closing, resizing
  - Layout mode switching
  - Observable event streams
  - Database schema implementation

### Phase 2: Preview & Rendering (Weeks 3-4, 12 hours)
- **v0.17.1f: Markdown Preview Pane**
  - Basic Markdown to HTML rendering
  - Incremental rendering optimization
  - Scroll mapping calculation
  - Preview theme support

### Phase 3: Syntax & Styling (Weeks 5-6, 10 hours)
- **v0.17.1g: Enhanced Syntax Highlighting**
  - Multi-language tokenization
  - Language detection
  - Custom language registration
  - Grammar validation

### Phase 4: Chrome & Navigation (Weeks 7-8, 10 hours)
- **v0.17.1h: Editor Chrome (Line Numbers, Minimap)**
  - Line number rendering
  - Minimap implementation
  - Gutter decorations
  - Fold controls

### Phase 5: Navigation & Settings (Weeks 9-10, 12 hours)
- **v0.17.1i: Breadcrumb Navigation** (6 hours)
  - Breadcrumb computation
  - Dropdown menus
  - Navigation handlers
- **v0.17.1j: Editor Settings & Themes** (6 hours)
  - Configuration UI
  - Theme persistence
  - Custom theme import
  - License gating

### Final Phase: Integration & Testing (Week 11, 8 hours)
- E2E testing
- Performance benchmarking
- License gating validation
- Documentation

---

## 14. Success Metrics

### Quantitative Metrics
1. **P95 Frame Time:** <20ms with 3 panes (60fps target)
2. **Preview Update Latency:** <250ms for typical edits
3. **Syntax Highlighting Speed:** 10K lines in <150ms (P95)
4. **Feature Adoption:** 70%+ of WriterPro+ users enable split view within 30 days
5. **User Satisfaction:** >4.5/5 rating in editor features survey
6. **Performance Compliance:** 100% of performance targets met in production

### Qualitative Metrics
1. **Code Quality:** Zero critical bugs in first month; <2 high-severity bugs
2. **Documentation:** All interfaces documented with XML comments; API guide published
3. **Test Coverage:** >90% code coverage for core components
4. **Accessibility:** WCAG AA compliance on all themes and UI elements
5. **User Feedback:** Positive sentiment from early adopters; feature requests logged

---

## 15. Appendix: Example Workflows

### Workflow 1: Split View with Live Preview

**User Goal:** Edit Markdown while seeing live preview

1. Open document in editor (full screen)
2. Use Layout menu → Select "SplitHorizontal"
3. Editor pane appears on left; preview on right
4. Edit content in editor pane
5. Preview updates in real-time (incremental rendering)
6. Scroll in editor; preview scrolls to matching position (sync scroll)
7. Close preview pane by clicking split divider close button

### Workflow 2: Navigate Using Breadcrumbs

**User Goal:** Quickly navigate to subsection in long document

1. View breadcrumb trail: "File > Documentation > API Reference > Methods"
2. Click "Documentation" in breadcrumb
3. Dropdown appears showing siblings: "Overview", "Installation", "Documentation"
4. Click "Overview"
5. Editor scrolls to Overview section
6. Breadcrumb updates: "File > Documentation > Overview"

### Workflow 3: Customize Editor Theme

**User Goal:** Apply dark theme with custom colors

1. Open Settings panel (Ctrl+,)
2. Find "Theme" dropdown; select "lexichord-dark"
3. Theme applies immediately; colors change
4. Open "Font" settings; change font size to 16px
5. All panes update with new settings
6. Close settings; changes persisted
7. Reload page; settings retained

---

## 16. Related Documentation

- Editor Architecture: See v0.17.0-EDC specification
- Markdown Rendering: See v0.15.1-RNR (Renderer) documentation
- License System: See v0.2.1a (License Service) API reference
- Database Design: See /docs/db/editor-schema.sql

---

## Document Approval

| Role | Name | Date | Signature |
|:-----|:-----|:-----|:----------|
| Technical Lead | [To be assigned] | 2026-02-01 | _____ |
| Product Manager | [To be assigned] | 2026-02-01 | _____ |
| Architecture Lead | [To be assigned] | 2026-02-01 | _____ |

---

**Document End**
