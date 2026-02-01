# LexiChord Scope Breakdown Document v0.20.1-DOC
# Inline Documentation System

**Document Control**
- **Document ID**: LCS-SBD-v0.20.1-DOC
- **Version**: 1.0
- **Release Date**: 2025-02-01
- **Status**: Active
- **Classification**: Technical Specification
- **Author**: LexiChord Development Team
- **Last Updated**: 2025-02-01
- **Next Review**: 2025-03-01

---

## 1. EXECUTIVE SUMMARY

### 1.1 Overview

v0.20.1-DOC focuses on implementing a comprehensive inline documentation system for the LexiChord language learning platform. This release establishes foundational infrastructure for user guidance, help content, and contextual assistance directly within the application interface.

The Inline Documentation System (IDS) provides users with immediate access to contextual help, feature explanations, and guidance without requiring navigation away from their current workflow. This system reduces cognitive load, improves feature discoverability, and enhances overall user experience.

### 1.2 Strategic Objectives

1. **Reduce User Friction**: Enable users to understand features without leaving their workflow
2. **Improve Feature Adoption**: Increase feature discoverability through intelligent contextual help
3. **Lower Support Burden**: Self-service help content reduces support ticket volume
4. **Enhance Accessibility**: Provide multi-modal help (tooltips, panels, overlays, text)
5. **Support Localization**: Framework for help content in multiple languages
6. **Enable Analytics**: Track help content engagement and user guidance effectiveness

### 1.3 Key Deliverables

- Tooltip System with smart positioning and delay management
- Contextual Help Panels for complex features
- Field Hints and Validation Error Messages
- Feature Explanation Overlays for guided onboarding
- Inline Code Documentation for developers
- Help Content Localization Framework
- PostgreSQL schema for help content storage
- MediatR event system for help tracking
- Comprehensive UI/UX design documentation
- Performance benchmarks and testing framework

### 1.4 Release Scope

**Total Estimated Effort**: 54 hours

**Target Release**: Q1 2025

**Dependencies**:
- LexiChord Core Framework (v0.19.x)
- User Service (v0.19.1)
- Localization Service (v0.19.2)
- MediatR Pipeline (v0.18.x)

---

## 2. SUB-PARTS BREAKDOWN

### 2.1 v0.20.1a: Tooltip System (10 hours)

**Purpose**: Implement rich, intelligent tooltip system with smart positioning, delay management, and content caching.

**Key Features**:
- Smart positioning algorithm (top, bottom, left, right with fallback)
- Configurable show/hide delays
- Mouse and keyboard interaction support
- Tooltip queuing for rapid-fire events
- Content templating and markdown support
- Accessibility support (ARIA labels, keyboard navigation)
- Performance optimization (lazy loading, virtual scrolling)

**Acceptance Criteria**:
- [ ] Tooltips render in < 16ms (P50 latency)
- [ ] Smart positioning works on all viewport edges
- [ ] Show delay configurable per tooltip (default 300ms)
- [ ] Hide delay prevents flicker (default 150ms)
- [ ] ARIA attributes properly set
- [ ] Keyboard navigation (TAB, ENTER, ESC)
- [ ] 100% unit test coverage
- [ ] Works in all modern browsers (Chrome, Firefox, Safari, Edge)

**Deliverables**:
- ITooltipManager interface
- TooltipService implementation
- TooltipPositioner utility
- TooltipRegistry for content management
- Unit tests (30+ test cases)

---

### 2.2 v0.20.1b: Contextual Help Panels (10 hours)

**Purpose**: Implement collapsible, contextual help panels that provide detailed information about specific features without interrupting workflow.

**Key Features**:
- Slide-in/slide-out animation
- Responsive design (mobile, tablet, desktop)
- Multi-section organization with expandable topics
- Rich content support (text, images, video links, code snippets)
- Related links and cross-references
- Context-aware content loading
- Dismissal and persistence preferences

**Acceptance Criteria**:
- [ ] Panel loads in < 200ms
- [ ] Animation frame rate maintained (60 FPS)
- [ ] Responsive design verified on 3+ breakpoints
- [ ] Content hierarchy supports 5+ nested levels
- [ ] Video links configurable per feature
- [ ] Related content auto-loads based on context
- [ ] User dismissal preferences persisted
- [ ] 80+ test coverage

**Deliverables**:
- IContextualHelpService interface
- ContextualHelpPanel component
- HelpContentOrganizer utility
- HelpContentLoader with caching
- Integration tests (20+ test cases)

---

### 2.3 v0.20.1c: Field Hints & Validation Messages (8 hours)

**Purpose**: Implement intelligent field hints that appear below form fields and comprehensive validation error messages with helpful suggestions.

**Key Features**:
- Dynamic hint text based on field value and type
- Validation message templating with parameter substitution
- Severity levels (info, warning, error)
- Inline error correction suggestions
- Character count and format guidance
- Required field indicators
- Accessibility compliance (error associations)

**Acceptance Criteria**:
- [ ] Hints display below all form fields
- [ ] Validation messages appear in < 50ms
- [ ] Error suggestions reduce support tickets by 15%+
- [ ] Character count accuracy (100% match to max length)
- [ ] All form fields have field_hint_id references
- [ ] Error messages localized for 5+ languages
- [ ] 70+ test coverage
- [ ] Accessibility audit passing (WCAG 2.1 AA)

**Deliverables**:
- IFieldHintService interface
- FieldHintRenderer component
- ValidationMessageTemplater utility
- FormFieldHintRegistry
- UI tests (25+ test cases)

---

### 2.4 v0.20.1d: Feature Explanation Overlays (8 hours)

**Purpose**: Implement modal/overlay-based feature explanations for guided onboarding and feature discovery.

**Key Features**:
- Multi-step explanation sequences
- Step navigation (next, previous, skip)
- Highlighted element targeting with spotlight
- Screenshot/video preview support
- Progress indicators
- Customizable styling per feature
- Analytics tracking for completion rates

**Acceptance Criteria**:
- [ ] Overlay loads in < 300ms
- [ ] Spotlight positioning accurate within 2px
- [ ] Multi-step sequences support 10+ steps
- [ ] Skip functionality completes in < 50ms
- [ ] Progress tracking at 95%+ accuracy
- [ ] Mobile responsive (touch-friendly buttons)
- [ ] 65+ test coverage
- [ ] Completion metrics tracked via MediatR events

**Deliverables**:
- IFeatureExplainerService interface
- FeatureExplanationOverlay component
- StepSequenceManager utility
- SpotlightRenderer for element highlighting
- E2E tests (20+ test cases)

---

### 2.5 v0.20.1e: Inline Code Documentation (8 hours)

**Purpose**: Implement developer-focused inline documentation system for code intellisense and hover information.

**Key Features**:
- C# method documentation extraction from XML comments
- Hover information display with syntax highlighting
- Parameter information and return value documentation
- Usage examples in documentation
- Related method suggestions
- Overload documentation
- Deprecated method warnings

**Acceptance Criteria**:
- [ ] XML comment parsing for all public methods
- [ ] Hover information displays in < 100ms
- [ ] Code examples syntax-highlighted correctly
- [ ] Parameter descriptions complete for 95%+ methods
- [ ] 60+ test coverage
- [ ] Works with all major IDEs (VS, VS Code, Rider)
- [ ] Documentation generation from XML comments

**Deliverables**:
- ICodeDocumentationService interface
- XMLCommentParser utility
- CodeHoverInformationProvider
- DocumentationExtractor
- Integration tests (15+ test cases)

---

### 2.6 v0.20.1f: Help Content Localization (10 hours)

**Purpose**: Implement framework for localizing all help content across multiple languages.

**Key Features**:
- Multi-language content storage
- Fallback language support (English default)
- Language-specific formatting (RTL support)
- Translation workflow management
- Content versioning per language
- Pluralization rules per language
- Date/time/number formatting per locale

**Acceptance Criteria**:
- [ ] Content available in 5+ languages on launch
- [ ] Fallback to English when translation missing
- [ ] RTL layout support for Arabic/Hebrew
- [ ] Pluralization rules handle 10+ languages
- [ ] Translation status dashboard available
- [ ] Missing translation detection and alerting
- [ ] 75+ test coverage
- [ ] Performance impact < 5ms per content load

**Deliverables**:
- IHelpLocalizationService interface
- LocalizedContentLoader with caching
- LanguageFallbackProvider
- TranslationStatusMonitor
- Content seeding scripts (SQL)
- Localization tests (30+ test cases)

---

## 3. DETAILED SPECIFICATIONS

### 3.1 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     User Interface Layer                         │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐  │
│  │   Tooltips   │ Help Panels  │ Field Hints  │   Overlays   │  │
│  └──────────────┴──────────────┴──────────────┴──────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                    Help Content Services Layer                   │
│  ┌────────────────┬──────────────┬──────────────────────────┐   │
│  │  Tooltip Mgr   │ Context Help │ Field Hint Service      │   │
│  ├────────────────┼──────────────┼──────────────────────────┤   │
│  │ Feature Expl.  │ Code Docs    │ Localization Service    │   │
│  └────────────────┴──────────────┴──────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                    Data Access Layer                             │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐  │
│  │ Help Content │ Tooltip Data │ Field Hints  │ Localization │  │
│  │ Repository   │ Repository   │ Repository   │ Repository   │  │
│  └──────────────┴──────────────┴──────────────┴──────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                   PostgreSQL Database Layer                      │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ help_content | tooltips | field_hints | localized_content  │ │
│  │ feature_explanations | help_analytics                       │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Tooltip System Architecture

```
User Interaction (Hover/Focus)
            │
            ▼
    ┌─────────────────┐
    │ TooltipDetector │
    └────────┬────────┘
             │
             ▼
    ┌──────────────────────┐
    │ TooltipPositioner    │
    │ - Edge Detection     │
    │ - Viewport Calc      │
    │ - Collision Detection│
    └────────┬─────────────┘
             │
             ▼
    ┌──────────────────────┐
    │ ContentResolver      │
    │ - Registry Lookup    │
    │ - Cache Check        │
    │ - Load if Missing    │
    └────────┬─────────────┘
             │
             ▼
    ┌──────────────────────┐
    │ TooltipRenderer      │
    │ - Position Element   │
    │ - Apply Styles      │
    │ - Show Animation     │
    └────────┬─────────────┘
             │
             ▼
    Tooltip Displayed
```

### 3.3 Contextual Help Flow

```
Feature Context Changed
            │
            ▼
    ┌──────────────────────────┐
    │ ContextChangeDetector    │
    │ - Component mounted      │
    │ - Route changed          │
    │ - State updated          │
    └──────────┬───────────────┘
               │
               ▼
    ┌──────────────────────────┐
    │ ContextAnalyzer          │
    │ - Identify current context
    │ - Query relevant content │
    └──────────┬───────────────┘
               │
               ▼
    ┌──────────────────────────┐
    │ ContentLoader            │
    │ - Database query         │
    │ - Localization check     │
    │ - Cache population       │
    └──────────┬───────────────┘
               │
               ▼
    ┌──────────────────────────┐
    │ PanelRenderer            │
    │ - Slide in animation     │
    │ - Content organization   │
    │ - Section expansion      │
    └──────────┬───────────────┘
               │
               ▼
    Help Panel Visible
```

### 3.4 Field Validation Flow

```
User Input Changed
            │
            ▼
    ┌──────────────────────┐
    │ FieldValidator       │
    │ - Type check         │
    │ - Pattern match      │
    │ - Length validation  │
    └────────┬─────────────┘
             │
             ▼
    ┌──────────────────────┐
    │ HintManager          │
    │ - Determine hint type│
    │ - Generate dynamic   │
    │   hint text          │
    └────────┬─────────────┘
             │
             ▼
    ┌──────────────────────┐
    │ MessageTemplater     │
    │ - Substitute params  │
    │ - Localize message   │
    │ - Apply styling      │
    └────────┬─────────────┘
             │
             ▼
    ┌──────────────────────┐
    │ HintRenderer         │
    │ - Display below field│
    │ - Set error class    │
    │ - Show suggestions   │
    └────────┬─────────────┘
             │
             ▼
    Hint/Error Displayed
```

---

## 4. INTERFACE SPECIFICATIONS

### 4.1 ITooltipManager

```csharp
/// <summary>
/// Manages tooltip registration, retrieval, and lifecycle.
/// Handles positioning, styling, and user interaction for tooltips.
/// </summary>
public interface ITooltipManager
{
    /// <summary>
    /// Gets a tooltip by its unique identifier.
    /// </summary>
    /// <param name="tooltipId">The unique identifier of the tooltip</param>
    /// <returns>Tooltip model or null if not found</returns>
    Task<TooltipModel?> GetTooltipAsync(string tooltipId);

    /// <summary>
    /// Gets all tooltips for a specific component or page.
    /// </summary>
    /// <param name="contextKey">Context identifier (component name, page route, etc)</param>
    /// <returns>Collection of tooltips for the context</returns>
    Task<IEnumerable<TooltipModel>> GetTooltipsByContextAsync(string contextKey);

    /// <summary>
    /// Registers a new tooltip in the system.
    /// </summary>
    /// <param name="tooltip">Tooltip model to register</param>
    /// <returns>Registered tooltip with assigned ID</returns>
    Task<TooltipModel> RegisterTooltipAsync(TooltipModel tooltip);

    /// <summary>
    /// Updates an existing tooltip's content or configuration.
    /// </summary>
    /// <param name="tooltipId">The tooltip to update</param>
    /// <param name="updates">Updated tooltip properties</param>
    /// <returns>Updated tooltip model</returns>
    Task<TooltipModel> UpdateTooltipAsync(string tooltipId, TooltipUpdateModel updates);

    /// <summary>
    /// Removes a tooltip from the system.
    /// </summary>
    /// <param name="tooltipId">The tooltip to remove</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveTooltipAsync(string tooltipId);

    /// <summary>
    /// Batch registers multiple tooltips for efficient loading.
    /// </summary>
    /// <param name="tooltips">Collection of tooltips to register</param>
    /// <returns>Registered tooltips</returns>
    Task<IEnumerable<TooltipModel>> RegisterMultipleAsync(IEnumerable<TooltipModel> tooltips);

    /// <summary>
    /// Clears all tooltips from cache for a specific context.
    /// </summary>
    /// <param name="contextKey">Context to clear</param>
    /// <returns>Task completion</returns>
    Task ClearContextCacheAsync(string contextKey);

    /// <summary>
    /// Calculates optimal tooltip position based on target element and viewport.
    /// </summary>
    /// <param name="targetElement">Element the tooltip targets</param>
    /// <param name="tooltipWidth">Width of tooltip in pixels</param>
    /// <param name="tooltipHeight">Height of tooltip in pixels</param>
    /// <returns>Calculated position model</returns>
    TooltipPositionModel CalculatePosition(ElementModel targetElement, int tooltipWidth, int tooltipHeight);

    /// <summary>
    /// Checks if tooltip content should be localized and gets localized version.
    /// </summary>
    /// <param name="tooltipId">Tooltip identifier</param>
    /// <param name="languageCode">Target language code (e.g., "en", "es", "fr")</param>
    /// <returns>Localized tooltip model</returns>
    Task<TooltipModel> GetLocalizedTooltipAsync(string tooltipId, string languageCode);

    /// <summary>
    /// Records that a tooltip was shown for analytics.
    /// </summary>
    /// <param name="tooltipId">Shown tooltip identifier</param>
    /// <param name="metadata">Optional metadata about the show event</param>
    /// <returns>Task completion</returns>
    Task RecordTooltipShownAsync(string tooltipId, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets statistics about tooltip usage.
    /// </summary>
    /// <param name="contextKey">Optional context filter</param>
    /// <param name="timeRange">Optional time range filter</param>
    /// <returns>Tooltip usage statistics</returns>
    Task<TooltipStatisticsModel> GetStatisticsAsync(string? contextKey = null, DateRangeModel? timeRange = null);
}

/// <summary>
/// Represents a tooltip configuration and content.
/// </summary>
public class TooltipModel
{
    /// <summary>Gets or sets the unique tooltip identifier.</summary>
    public string TooltipId { get; set; } = string.Empty;

    /// <summary>Gets or sets the content displayed in the tooltip.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets optional HTML content.</summary>
    public string? HtmlContent { get; set; }

    /// <summary>Gets or sets the context where this tooltip appears.</summary>
    public string ContextKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the selector for the target element.</summary>
    public string TargetSelector { get; set; } = string.Empty;

    /// <summary>Gets or sets the preferred tooltip position.</summary>
    public TooltipPosition Position { get; set; } = TooltipPosition.Auto;

    /// <summary>Gets or sets the delay before showing tooltip (milliseconds).</summary>
    public int ShowDelayMs { get; set; } = 300;

    /// <summary>Gets or sets the delay before hiding tooltip (milliseconds).</summary>
    public int HideDelayMs { get; set; } = 150;

    /// <summary>Gets or sets maximum width of tooltip (pixels).</summary>
    public int MaxWidthPx { get; set; } = 300;

    /// <summary>Gets or sets whether tooltip supports markdown.</summary>
    public bool SupportsMarkdown { get; set; } = true;

    /// <summary>Gets or sets whether tooltip is dismissible by user.</summary>
    public bool Dismissible { get; set; } = true;

    /// <summary>Gets or sets trigger type (hover, focus, click).</summary>
    public TooltipTrigger Trigger { get; set; } = TooltipTrigger.Hover;

    /// <summary>Gets or sets optional CSS classes to apply.</summary>
    public string? CssClasses { get; set; }

    /// <summary>Gets or sets tooltip priority level for queuing.</summary>
    public TooltipPriority Priority { get; set; } = TooltipPriority.Normal;

    /// <summary>Gets or sets when this tooltip expires.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets whether this tooltip is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the target language code if localized.</summary>
    public string? LanguageCode { get; set; }

    /// <summary>Gets or sets creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets last modified timestamp.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Enum for tooltip positioning strategies.</summary>
public enum TooltipPosition
{
    /// <summary>Automatically determine best position</summary>
    Auto = 0,
    /// <summary>Position above target element</summary>
    Top = 1,
    /// <summary>Position below target element</summary>
    Bottom = 2,
    /// <summary>Position to the left of target</summary>
    Left = 3,
    /// <summary>Position to the right of target</summary>
    Right = 4,
    /// <summary>Center over target element</summary>
    Center = 5,
    /// <summary>Follow mouse cursor</summary>
    FollowMouse = 6
}

/// <summary>Enum for tooltip trigger events.</summary>
public enum TooltipTrigger
{
    /// <summary>Show on element hover</summary>
    Hover = 0,
    /// <summary>Show on element focus</summary>
    Focus = 1,
    /// <summary>Show on element click</summary>
    Click = 2,
    /// <summary>Show on focus or hover</summary>
    FocusOrHover = 3,
    /// <summary>Always show</summary>
    Always = 4
}

/// <summary>Enum for tooltip priority levels.</summary>
public enum TooltipPriority
{
    /// <summary>Low priority, can be queued</summary>
    Low = 0,
    /// <summary>Normal priority</summary>
    Normal = 1,
    /// <summary>High priority, shown sooner</summary>
    High = 2,
    /// <summary>Critical, shown immediately</summary>
    Critical = 3
}

/// <summary>Calculated position for a tooltip.</summary>
public class TooltipPositionModel
{
    /// <summary>Gets or sets the X coordinate (pixels).</summary>
    public int X { get; set; }

    /// <summary>Gets or sets the Y coordinate (pixels).</summary>
    public int Y { get; set; }

    /// <summary>Gets or sets the actual position used.</summary>
    public TooltipPosition ActualPosition { get; set; }

    /// <summary>Gets or sets whether position is constrained by viewport.</summary>
    public bool IsConstrained { get; set; }

    /// <summary>Gets or sets recommended CSS transform.</summary>
    public string CssTransform { get; set; } = string.Empty;

    /// <summary>Gets or sets arrow position (relative to tooltip).</summary>
    public string? ArrowPosition { get; set; }
}
```

### 4.2 IContextualHelpService

```csharp
/// <summary>
/// Manages contextual help panels that provide detailed feature information.
/// </summary>
public interface IContextualHelpService
{
    /// <summary>
    /// Gets help panel for current context.
    /// </summary>
    /// <param name="contextKey">Context identifier</param>
    /// <returns>Help panel model or null if not found</returns>
    Task<HelpPanelModel?> GetHelpPanelAsync(string contextKey);

    /// <summary>
    /// Gets help content organized in sections.
    /// </summary>
    /// <param name="contextKey">Context identifier</param>
    /// <returns>Organized help content with sections</returns>
    Task<HelpContentModel> GetOrganizedContentAsync(string contextKey);

    /// <summary>
    /// Searches help content across all panels.
    /// </summary>
    /// <param name="searchQuery">Search terms</param>
    /// <param name="contextKey">Optional context filter</param>
    /// <returns>Search results</returns>
    Task<IEnumerable<HelpSearchResultModel>> SearchHelpAsync(string searchQuery, string? contextKey = null);

    /// <summary>
    /// Gets related help content based on current context.
    /// </summary>
    /// <param name="contextKey">Current context</param>
    /// <returns>Related help content</returns>
    Task<IEnumerable<HelpPanelModel>> GetRelatedHelpAsync(string contextKey);

    /// <summary>
    /// Registers a new help panel in the system.
    /// </summary>
    /// <param name="helpPanel">Help panel to register</param>
    /// <returns>Registered panel with ID</returns>
    Task<HelpPanelModel> RegisterHelpPanelAsync(HelpPanelModel helpPanel);

    /// <summary>
    /// Updates an existing help panel.
    /// </summary>
    /// <param name="panelId">Panel to update</param>
    /// <param name="updates">Updated content</param>
    /// <returns>Updated panel</returns>
    Task<HelpPanelModel> UpdateHelpPanelAsync(string panelId, HelpPanelUpdateModel updates);

    /// <summary>
    /// Records that help content was viewed.
    /// </summary>
    /// <param name="panelId">Viewed panel identifier</param>
    /// <param name="metadata">Metadata about the view event</param>
    /// <returns>Task completion</returns>
    Task RecordHelpViewedAsync(string panelId, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets localized help panel.
    /// </summary>
    /// <param name="contextKey">Context identifier</param>
    /// <param name="languageCode">Target language</param>
    /// <returns>Localized help panel</returns>
    Task<HelpPanelModel?> GetLocalizedHelpPanelAsync(string contextKey, string languageCode);

    /// <summary>
    /// Gets help content statistics.
    /// </summary>
    /// <param name="contextKey">Optional context filter</param>
    /// <returns>Help content usage statistics</returns>
    Task<HelpContentStatisticsModel> GetStatisticsAsync(string? contextKey = null);
}

/// <summary>
/// Represents a help panel with organized content sections.
/// </summary>
public class HelpPanelModel
{
    /// <summary>Gets or sets the unique panel identifier.</summary>
    public string PanelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the panel title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the context this panel applies to.</summary>
    public string ContextKey { get; set; } = string.Empty;

    /// <summary>Gets or sets brief description of the panel content.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets organized sections of content.</summary>
    public List<HelpSectionModel> Sections { get; set; } = new();

    /// <summary>Gets or sets related help panel identifiers.</summary>
    public List<string> RelatedPanelIds { get; set; } = new();

    /// <summary>Gets or sets external links (documentation, videos, etc).</summary>
    public List<HelpLinkModel> ExternalLinks { get; set; } = new();

    /// <summary>Gets or sets whether this panel is visible.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Gets or sets display order priority.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets category/grouping for organization.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets optional thumbnail image URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets estimated reading time in minutes.</summary>
    public int EstimatedReadingTimeMinutes { get; set; }

    /// <summary>Gets or sets creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a section within a help panel.
/// </summary>
public class HelpSectionModel
{
    /// <summary>Gets or sets the section identifier.</summary>
    public string SectionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the section title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the section content (supports markdown).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets child sections for nesting.</summary>
    public List<HelpSectionModel> Subsections { get; set; } = new();

    /// <summary>Gets or sets whether section is expanded by default.</summary>
    public bool ExpandedByDefault { get; set; } = true;

    /// <summary>Gets or sets display order.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets optional code examples.</summary>
    public List<CodeExampleModel> CodeExamples { get; set; } = new();
}

/// <summary>
/// Represents an external link within help content.
/// </summary>
public class HelpLinkModel
{
    /// <summary>Gets or sets the link text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the link URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the link type (documentation, video, etc).</summary>
    public HelpLinkType LinkType { get; set; }

    /// <summary>Gets or sets whether link opens in new tab.</summary>
    public bool OpenInNewTab { get; set; } = true;
}

/// <summary>Enum for help link types.</summary>
public enum HelpLinkType
{
    /// <summary>Link to documentation</summary>
    Documentation = 0,
    /// <summary>Link to video tutorial</summary>
    Video = 1,
    /// <summary>Link to related help content</summary>
    RelatedHelp = 2,
    /// <summary>Link to support ticket</summary>
    Support = 3,
    /// <summary>Link to external resource</summary>
    External = 4
}
```

### 4.3 IFieldHintService

```csharp
/// <summary>
/// Manages field hints, validation messages, and form guidance.
/// </summary>
public interface IFieldHintService
{
    /// <summary>
    /// Gets hint for a specific form field.
    /// </summary>
    /// <param name="fieldId">Field identifier</param>
    /// <returns>Field hint model or null if not found</returns>
    Task<FieldHintModel?> GetFieldHintAsync(string fieldId);

    /// <summary>
    /// Gets all hints for a form.
    /// </summary>
    /// <param name="formId">Form identifier</param>
    /// <returns>Collection of field hints for the form</returns>
    Task<IEnumerable<FieldHintModel>> GetFormHintsAsync(string formId);

    /// <summary>
    /// Generates validation error message for a validation failure.
    /// </summary>
    /// <param name="fieldId">Field that failed validation</param>
    /// <param name="validationType">Type of validation that failed</param>
    /// <param name="parameters">Parameters to substitute in message</param>
    /// <returns>Generated error message</returns>
    Task<ValidationMessageModel> GenerateValidationMessageAsync(
        string fieldId,
        ValidationType validationType,
        Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Gets validation error suggestion for helping user correct input.
    /// </summary>
    /// <param name="fieldId">Field with invalid input</param>
    /// <param name="invalidValue">The invalid value</param>
    /// <param name="validationType">Validation type that failed</param>
    /// <returns>Helpful correction suggestion</returns>
    Task<string?> GetValidationSuggestionAsync(
        string fieldId,
        string invalidValue,
        ValidationType validationType);

    /// <summary>
    /// Gets dynamic hint based on current field value.
    /// </summary>
    /// <param name="fieldId">Field identifier</param>
    /// <param name="currentValue">Current field value</param>
    /// <returns>Dynamic hint based on value</returns>
    Task<string> GetDynamicHintAsync(string fieldId, string? currentValue);

    /// <summary>
    /// Registers a new field hint in the system.
    /// </summary>
    /// <param name="fieldHint">Field hint to register</param>
    /// <returns>Registered field hint with ID</returns>
    Task<FieldHintModel> RegisterFieldHintAsync(FieldHintModel fieldHint);

    /// <summary>
    /// Updates field hint content.
    /// </summary>
    /// <param name="fieldId">Field to update</param>
    /// <param name="updates">Updated field hint</param>
    /// <returns>Updated field hint</returns>
    Task<FieldHintModel> UpdateFieldHintAsync(string fieldId, FieldHintUpdateModel updates);

    /// <summary>
    /// Gets localized field hint.
    /// </summary>
    /// <param name="fieldId">Field identifier</param>
    /// <param name="languageCode">Target language</param>
    /// <returns>Localized field hint</returns>
    Task<FieldHintModel?> GetLocalizedFieldHintAsync(string fieldId, string languageCode);

    /// <summary>
    /// Gets character count guidance for field.
    /// </summary>
    /// <param name="fieldId">Field identifier</param>
    /// <returns>Character count guidance model</returns>
    Task<CharacterCountGuidanceModel?> GetCharacterCountGuidanceAsync(string fieldId);

    /// <summary>
    /// Gets format guidance for field.
    /// </summary>
    /// <param name="fieldId">Field identifier</param>
    /// <returns>Format guidance (e.g., "example@email.com")</returns>
    Task<string?> GetFormatGuidanceAsync(string fieldId);

    /// <summary>
    /// Records validation error for analytics.
    /// </summary>
    /// <param name="fieldId">Field with error</param>
    /// <param name="validationType">Error type</param>
    /// <returns>Task completion</returns>
    Task RecordValidationErrorAsync(string fieldId, ValidationType validationType);
}

/// <summary>
/// Represents a hint for a form field.
/// </summary>
public class FieldHintModel
{
    /// <summary>Gets or sets the unique field identifier.</summary>
    public string FieldId { get; set; } = string.Empty;

    /// <summary>Gets or sets the form this field belongs to.</summary>
    public string FormId { get; set; } = string.Empty;

    /// <summary>Gets or sets the field label.</summary>
    public string FieldLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the hint text displayed to user.</summary>
    public string HintText { get; set; } = string.Empty;

    /// <summary>Gets or sets detailed help for this field.</summary>
    public string? DetailedHelp { get; set; }

    /// <summary>Gets or sets example text for guidance.</summary>
    public string? ExampleText { get; set; }

    /// <summary>Gets or sets whether field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the field data type.</summary>
    public FieldDataType FieldType { get; set; }

    /// <summary>Gets or sets maximum length if applicable.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets minimum length if applicable.</summary>
    public int? MinLength { get; set; }

    /// <summary>Gets or sets validation rules for this field.</summary>
    public List<ValidationRuleModel> ValidationRules { get; set; } = new();

    /// <summary>Gets or sets display order on form.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets whether hint should show character count.</summary>
    public bool ShowCharacterCount { get; set; }

    /// <summary>Gets or sets whether hint should show format guidance.</summary>
    public bool ShowFormatGuidance { get; set; }

    /// <summary>Gets or sets creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a validation error message.
/// </summary>
public class ValidationMessageModel
{
    /// <summary>Gets or sets the error message text.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets severity level of error.</summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>Gets or sets the validation type that failed.</summary>
    public ValidationType ValidationType { get; set; }

    /// <summary>Gets or sets helpful suggestion for correction.</summary>
    public string? Suggestion { get; set; }

    /// <summary>Gets or sets related documentation link.</summary>
    public string? DocumentationLink { get; set; }

    /// <summary>Gets or sets error code for internationalization.</summary>
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>Enum for validation types.</summary>
public enum ValidationType
{
    /// <summary>Field is required but empty</summary>
    Required = 0,
    /// <summary>Input length exceeds maximum</summary>
    MaxLength = 1,
    /// <summary>Input length below minimum</summary>
    MinLength = 2,
    /// <summary>Input format is invalid</summary>
    Format = 3,
    /// <summary>Input does not match pattern</summary>
    Pattern = 4,
    /// <summary>Input value is invalid</summary>
    InvalidValue = 5,
    /// <summary>Duplicate value not allowed</summary>
    Duplicate = 6,
    /// <summary>Custom validation failed</summary>
    Custom = 7
}

/// <summary>Enum for validation severity levels.</summary>
public enum ValidationSeverity
{
    /// <summary>Information message</summary>
    Info = 0,
    /// <summary>Warning that should be addressed</summary>
    Warning = 1,
    /// <summary>Error that prevents submission</summary>
    Error = 2,
    /// <summary>Critical error requiring immediate action</summary>
    Critical = 3
}

/// <summary>Enum for field data types.</summary>
public enum FieldDataType
{
    /// <summary>Plain text input</summary>
    Text = 0,
    /// <summary>Numeric input</summary>
    Number = 1,
    /// <summary>Email address input</summary>
    Email = 2,
    /// <summary>Password input</summary>
    Password = 3,
    /// <summary>URL input</summary>
    Url = 4,
    /// <summary>Date input</summary>
    Date = 5,
    /// <summary>Time input</summary>
    Time = 6,
    /// <summary>Textarea for longer text</summary>
    Textarea = 7,
    /// <summary>Select dropdown</summary>
    Select = 8,
    /// <summary>Checkbox input</summary>
    Checkbox = 9,
    /// <summary>Radio button group</summary>
    Radio = 10
}

/// <summary>
/// Represents a validation rule for a field.
/// </summary>
public class ValidationRuleModel
{
    /// <summary>Gets or sets the validation type.</summary>
    public ValidationType RuleType { get; set; }

    /// <summary>Gets or sets validation parameter (e.g., max length).</summary>
    public string? Parameter { get; set; }

    /// <summary>Gets or sets error message template for this rule.</summary>
    public string ErrorMessageTemplate { get; set; } = string.Empty;

    /// <summary>Gets or sets error code key.</summary>
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// Represents character count guidance for a field.
/// </summary>
public class CharacterCountGuidanceModel
{
    /// <summary>Gets or sets the maximum allowed characters.</summary>
    public int MaxCharacters { get; set; }

    /// <summary>Gets or sets the minimum required characters.</summary>
    public int MinCharacters { get; set; }

    /// <summary>Gets or sets recommended character count for optimal content.</summary>
    public int? RecommendedCharacters { get; set; }

    /// <summary>Gets or sets guidance text about character requirements.</summary>
    public string GuidanceText { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to show live character counter.</summary>
    public bool ShowLiveCounter { get; set; } = true;

    /// <summary>Gets or sets threshold percentage to warn user (e.g., 80%).</summary>
    public int WarningThresholdPercent { get; set; } = 80;
}
```

### 4.4 IFeatureExplainerService

```csharp
/// <summary>
/// Manages feature explanation overlays and guided tours.
/// </summary>
public interface IFeatureExplainerService
{
    /// <summary>
    /// Gets explanation sequence for a feature.
    /// </summary>
    /// <param name="featureKey">Feature identifier</param>
    /// <returns>Explanation sequence or null if not found</returns>
    Task<FeatureExplanationModel?> GetFeatureExplanationAsync(string featureKey);

    /// <summary>
    /// Starts a guided tour for a feature.
    /// </summary>
    /// <param name="featureKey">Feature to explain</param>
    /// <param name="userId">User viewing explanation</param>
    /// <returns>Tour session model</returns>
    Task<TourSessionModel> StartTourAsync(string featureKey, string userId);

    /// <summary>
    /// Advances to next step in tour.
    /// </summary>
    /// <param name="sessionId">Tour session identifier</param>
    /// <returns>Next step model</returns>
    Task<TourStepModel> GoToNextStepAsync(string sessionId);

    /// <summary>
    /// Goes to previous step in tour.
    /// </summary>
    /// <param name="sessionId">Tour session identifier</param>
    /// <returns>Previous step model</returns>
    Task<TourStepModel> GoToPreviousStepAsync(string sessionId);

    /// <summary>
    /// Skips tour and marks as completed.
    /// </summary>
    /// <param name="sessionId">Tour session identifier</param>
    /// <returns>Task completion</returns>
    Task SkipTourAsync(string sessionId);

    /// <summary>
    /// Completes tour successfully.
    /// </summary>
    /// <param name="sessionId">Tour session identifier</param>
    /// <returns>Completion report</returns>
    Task<TourCompletionModel> CompleteTourAsync(string sessionId);

    /// <summary>
    /// Registers a new feature explanation.
    /// </summary>
    /// <param name="explanation">Feature explanation to register</param>
    /// <returns>Registered explanation with ID</returns>
    Task<FeatureExplanationModel> RegisterFeatureExplanationAsync(FeatureExplanationModel explanation);

    /// <summary>
    /// Gets tour completion rate for analytics.
    /// </summary>
    /// <param name="featureKey">Feature identifier</param>
    /// <returns>Completion statistics</returns>
    Task<TourStatisticsModel> GetTourStatisticsAsync(string featureKey);

    /// <summary>
    /// Checks if user has already completed tour.
    /// </summary>
    /// <param name="featureKey">Feature identifier</param>
    /// <param name="userId">User identifier</param>
    /// <returns>True if tour completed, false otherwise</returns>
    Task<bool> HasUserCompletedTourAsync(string featureKey, string userId);

    /// <summary>
    /// Gets localized feature explanation.
    /// </summary>
    /// <param name="featureKey">Feature identifier</param>
    /// <param name="languageCode">Target language</param>
    /// <returns>Localized explanation</returns>
    Task<FeatureExplanationModel?> GetLocalizedExplanationAsync(string featureKey, string languageCode);
}

/// <summary>
/// Represents a feature explanation with multiple steps.
/// </summary>
public class FeatureExplanationModel
{
    /// <summary>Gets or sets the feature identifier.</summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the feature title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets feature description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the explanation steps.</summary>
    public List<TourStepModel> Steps { get; set; } = new();

    /// <summary>Gets or sets whether explanation is currently enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets target user roles for this explanation.</summary>
    public List<string> TargetRoles { get; set; } = new();

    /// <summary>Gets or sets whether to show on first visit.</summary>
    public bool ShowOnFirstVisit { get; set; } = false;

    /// <summary>Gets or sets estimated completion time in minutes.</summary>
    public int EstimatedTimeMinutes { get; set; }

    /// <summary>Gets or sets difficulty level.</summary>
    public DifficultyLevel DifficultyLevel { get; set; }

    /// <summary>Gets or sets optional video URL for the feature.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Gets or sets related feature explanations.</summary>
    public List<string> RelatedFeatures { get; set; } = new();

    /// <summary>Gets or sets creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a single step in a guided tour.
/// </summary>
public class TourStepModel
{
    /// <summary>Gets or sets the step identifier.</summary>
    public string StepId { get; set; } = string.Empty;

    /// <summary>Gets or sets the step number (1-based).</summary>
    public int StepNumber { get; set; }

    /// <summary>Gets or sets the total number of steps.</summary>
    public int TotalSteps { get; set; }

    /// <summary>Gets or sets the step title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the step content (supports markdown).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the CSS selector for the element to highlight.</summary>
    public string? TargetSelector { get; set; }

    /// <summary>Gets or sets position of tooltip relative to target.</summary>
    public TooltipPosition TooltipPosition { get; set; } = TooltipPosition.Auto;

    /// <summary>Gets or sets optional screenshot/image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets whether spotlight is shown around target element.</summary>
    public bool ShowSpotlight { get; set; } = true;

    /// <summary>Gets or sets spotlight padding around target element (pixels).</summary>
    public int SpotlightPaddingPx { get; set; } = 8;

    /// <summary>Gets or sets whether user must interact with element to proceed.</summary>
    public bool RequiresInteraction { get; set; } = false;

    /// <summary>Gets or sets what interaction is required (click, hover, type, etc).</summary>
    public InteractionType? RequiredInteraction { get; set; }

    /// <summary>Gets or sets button text for next action.</summary>
    public string NextButtonText { get; set; } = "Next";

    /// <summary>Gets or sets whether to show skip button on this step.</summary>
    public bool ShowSkipButton { get; set; } = true;

    /// <summary>Gets or sets whether to show previous button on this step.</summary>
    public bool ShowPreviousButton { get; set; } = true;

    /// <summary>Gets or sets estimated time for this step in seconds.</summary>
    public int EstimatedTimeSeconds { get; set; }

    /// <summary>Gets or sets custom CSS classes for styling.</summary>
    public string? CssClasses { get; set; }
}

/// <summary>
/// Represents an active tour session.
/// </summary>
public class TourSessionModel
{
    /// <summary>Gets or sets the session identifier.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the feature being explained.</summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the user viewing the tour.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the current step number.</summary>
    public int CurrentStepNumber { get; set; } = 1;

    /// <summary>Gets or sets whether tour is completed.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets whether tour was skipped.</summary>
    public bool WasSkipped { get; set; }

    /// <summary>Gets or sets when session started.</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when session completed or skipped.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets number of interactions in this step.</summary>
    public int InteractionCount { get; set; }
}

/// <summary>
/// Represents tour completion information.
/// </summary>
public class TourCompletionModel
{
    /// <summary>Gets or sets whether tour was completed successfully.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets whether tour was skipped.</summary>
    public bool WasSkipped { get; set; }

    /// <summary>Gets or sets total time spent on tour (seconds).</summary>
    public int TotalTimeSeconds { get; set; }

    /// <summary>Gets or sets number of steps completed.</summary>
    public int StepsCompleted { get; set; }

    /// <summary>Gets or sets total number of steps in tour.</summary>
    public int TotalSteps { get; set; }

    /// <summary>Gets or sets completion percentage.</summary>
    public decimal CompletionPercentage { get; set; }

    /// <summary>Gets or sets timestamp of completion.</summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Enum for interaction types required in tour steps.</summary>
public enum InteractionType
{
    /// <summary>User must click the element</summary>
    Click = 0,
    /// <summary>User must hover over the element</summary>
    Hover = 1,
    /// <summary>User must type in the element</summary>
    Type = 2,
    /// <summary>User must select from the element</summary>
    Select = 3,
    /// <summary>User must scroll to the element</summary>
    Scroll = 4
}

/// <summary>Enum for feature difficulty levels.</summary>
public enum DifficultyLevel
{
    /// <summary>Beginner level explanation</summary>
    Beginner = 0,
    /// <summary>Intermediate level explanation</summary>
    Intermediate = 1,
    /// <summary>Advanced level explanation</summary>
    Advanced = 2
}

/// <summary>
/// Represents tour usage statistics.
/// </summary>
public class TourStatisticsModel
{
    /// <summary>Gets or sets the feature key.</summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>Gets or sets total number of tours started.</summary>
    public int ToursStarted { get; set; }

    /// <summary>Gets or sets number of tours completed.</summary>
    public int ToursCompleted { get; set; }

    /// <summary>Gets or sets number of tours skipped.</summary>
    public int ToursSkipped { get; set; }

    /// <summary>Gets or sets completion rate percentage.</summary>
    public decimal CompletionRatePercent { get; set; }

    /// <summary>Gets or sets average time spent on tour.</summary>
    public TimeSpan AverageTimeSpent { get; set; }

    /// <summary>Gets or sets drop-off rate by step.</summary>
    public Dictionary<int, decimal> DropOffRateByStep { get; set; } = new();
}
```

### 4.5 ICodeDocumentationService

```csharp
/// <summary>
/// Manages inline code documentation from XML comments.
/// </summary>
public interface ICodeDocumentationService
{
    /// <summary>
    /// Gets documentation for a method.
    /// </summary>
    /// <param name="methodFullName">Fully qualified method name</param>
    /// <returns>Method documentation or null if not found</returns>
    Task<MethodDocumentationModel?> GetMethodDocumentationAsync(string methodFullName);

    /// <summary>
    /// Gets documentation for a type (class, interface, etc).
    /// </summary>
    /// <param name="typeFullName">Fully qualified type name</param>
    /// <returns>Type documentation</returns>
    Task<TypeDocumentationModel?> GetTypeDocumentationAsync(string typeFullName);

    /// <summary>
    /// Gets parameter information for a method.
    /// </summary>
    /// <param name="methodFullName">Fully qualified method name</param>
    /// <returns>Parameter documentation</returns>
    Task<IEnumerable<ParameterDocumentationModel>> GetParameterDocumentationAsync(string methodFullName);

    /// <summary>
    /// Gets return value documentation for a method.
    /// </summary>
    /// <param name="methodFullName">Fully qualified method name</param>
    /// <returns>Return documentation</returns>
    Task<ReturnDocumentationModel?> GetReturnDocumentationAsync(string methodFullName);

    /// <summary>
    /// Gets examples for a method.
    /// </summary>
    /// <param name="methodFullName">Fully qualified method name</param>
    /// <returns>Code examples</returns>
    Task<IEnumerable<CodeExampleModel>> GetMethodExamplesAsync(string methodFullName);

    /// <summary>
    /// Searches documentation by keyword.
    /// </summary>
    /// <param name="searchQuery">Search terms</param>
    /// <returns>Matching documentation</returns>
    Task<IEnumerable<DocumentationSearchResultModel>> SearchDocumentationAsync(string searchQuery);

    /// <summary>
    /// Gets all overloads for a method.
    /// </summary>
    /// <param name="methodName">Method name to find overloads for</param>
    /// <returns>All method overloads</returns>
    Task<IEnumerable<MethodDocumentationModel>> GetMethodOverloadsAsync(string methodName);

    /// <summary>
    /// Checks if a method is deprecated.
    /// </summary>
    /// <param name="methodFullName">Fully qualified method name</param>
    /// <returns>Deprecation information or null</returns>
    Task<DeprecationWarningModel?> GetDeprecationWarningAsync(string methodFullName);

    /// <summary>
    /// Indexes documentation for search capability.
    /// </summary>
    /// <returns>Task completion</returns>
    Task IndexDocumentationAsync();
}

/// <summary>
/// Represents documentation for a method.
/// </summary>
public class MethodDocumentationModel
{
    /// <summary>Gets or sets the fully qualified method name.</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>Gets or sets brief summary of the method.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets detailed remarks about the method.</summary>
    public string? Remarks { get; set; }

    /// <summary>Gets or sets the containing type.</summary>
    public string ContainingType { get; set; } = string.Empty;

    /// <summary>Gets or sets the method signature.</summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>Gets or sets parameter documentation.</summary>
    public List<ParameterDocumentationModel> Parameters { get; set; } = new();

    /// <summary>Gets or sets return value documentation.</summary>
    public ReturnDocumentationModel? ReturnDocumentation { get; set; }

    /// <summary>Gets or sets exceptions that can be thrown.</summary>
    public List<ExceptionDocumentationModel> Exceptions { get; set; } = new();

    /// <summary>Gets or sets code examples.</summary>
    public List<CodeExampleModel> Examples { get; set; } = new();

    /// <summary>Gets or sets visibility (public, private, internal, etc).</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets whether method is static.</summary>
    public bool IsStatic { get; set; }

    /// <summary>Gets or sets whether method is async.</summary>
    public bool IsAsync { get; set; }

    /// <summary>Gets or sets deprecation warning if applicable.</summary>
    public DeprecationWarningModel? DeprecationWarning { get; set; }

    /// <summary>Gets or sets when method was added to the API.</summary>
    public string? SinceVersion { get; set; }

    /// <summary>Gets or sets related methods.</summary>
    public List<string> RelatedMethods { get; set; } = new();
}

/// <summary>
/// Represents parameter documentation.
/// </summary>
public class ParameterDocumentationModel
{
    /// <summary>Gets or sets the parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets parameter description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether parameter is optional.</summary>
    public bool IsOptional { get; set; }

    /// <summary>Gets or sets default value if optional.</summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Represents return value documentation.
/// </summary>
public class ReturnDocumentationModel
{
    /// <summary>Gets or sets the return type.</summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>Gets or sets return value description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets possible return values/ranges.</summary>
    public List<string>? PossibleValues { get; set; }
}

/// <summary>
/// Represents exception documentation.
/// </summary>
public class ExceptionDocumentationModel
{
    /// <summary>Gets or sets the exception type.</summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>Gets or sets when this exception is thrown.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code example.
/// </summary>
public class CodeExampleModel
{
    /// <summary>Gets or sets the example code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the programming language.</summary>
    public string Language { get; set; } = "csharp";

    /// <summary>Gets or sets example description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether example shows expected output.</summary>
    public string? ExpectedOutput { get; set; }
}

/// <summary>
/// Represents type documentation.
/// </summary>
public class TypeDocumentationModel
{
    /// <summary>Gets or sets the fully qualified type name.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets type category (class, interface, struct, etc).</summary>
    public string TypeCategory { get; set; } = string.Empty;

    /// <summary>Gets or sets type summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets detailed remarks.</summary>
    public string? Remarks { get; set; }

    /// <summary>Gets or sets public members of the type.</summary>
    public List<MemberDocumentationModel> Members { get; set; } = new();

    /// <summary>Gets or sets base type.</summary>
    public string? BaseType { get; set; }

    /// <summary>Gets or sets implemented interfaces.</summary>
    public List<string> ImplementedInterfaces { get; set; } = new();
}

/// <summary>
/// Represents documentation for a type member.
/// </summary>
public class MemberDocumentationModel
{
    /// <summary>Gets or sets member name.</summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>Gets or sets member type (method, property, field, event).</summary>
    public string MemberType { get; set; } = string.Empty;

    /// <summary>Gets or sets member summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets visibility level.</summary>
    public string Visibility { get; set; } = string.Empty;
}

/// <summary>
/// Represents a deprecation warning.
/// </summary>
public class DeprecationWarningModel
{
    /// <summary>Gets or sets deprecation message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets recommended alternative.</summary>
    public string? RecommendedAlternative { get; set; }

    /// <summary>Gets or sets version when item was deprecated.</summary>
    public string? DeprecatedInVersion { get; set; }

    /// <summary>Gets or sets version when item will be removed.</summary>
    public string? RemovedInVersion { get; set; }
}

/// <summary>
/// Represents documentation search result.
/// </summary>
public class DocumentationSearchResultModel
{
    /// <summary>Gets or sets the item name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets the item type (method, class, etc).</summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>Gets or sets the item's containing type/namespace.</summary>
    public string ContainingType { get; set; } = string.Empty;

    /// <summary>Gets or sets the matched content snippet.</summary>
    public string MatchedContent { get; set; } = string.Empty;

    /// <summary>Gets or sets relevance score (0-100).</summary>
    public int RelevanceScore { get; set; }
}
```

### 4.6 IHelpLocalizationService

```csharp
/// <summary>
/// Manages localization of help content across multiple languages.
/// </summary>
public interface IHelpLocalizationService
{
    /// <summary>
    /// Gets localized help content.
    /// </summary>
    /// <param name="contentId">Content identifier</param>
    /// <param name="languageCode">Target language code (e.g., "en", "es")</param>
    /// <returns>Localized content or null if not found</returns>
    Task<LocalizedHelpContentModel?> GetLocalizedContentAsync(string contentId, string languageCode);

    /// <summary>
    /// Gets all available languages for help content.
    /// </summary>
    /// <returns>List of supported language codes</returns>
    Task<IEnumerable<SupportedLanguageModel>> GetSupportedLanguagesAsync();

    /// <summary>
    /// Gets language settings for current user.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>User's language preferences</returns>
    Task<UserLanguageSettingsModel> GetUserLanguageSettingsAsync(string userId);

    /// <summary>
    /// Sets preferred language for a user.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="languageCode">Preferred language code</param>
    /// <returns>Updated language settings</returns>
    Task<UserLanguageSettingsModel> SetUserLanguageAsync(string userId, string languageCode);

    /// <summary>
    /// Gets fallback language for a language (default is English).
    /// </summary>
    /// <param name="languageCode">Language code</param>
    /// <returns>Fallback language code</returns>
    string GetFallbackLanguage(string languageCode);

    /// <summary>
    /// Checks if content is translated to a language.
    /// </summary>
    /// <param name="contentId">Content identifier</param>
    /// <param name="languageCode">Language code to check</param>
    /// <returns>True if translated, false otherwise</returns>
    Task<bool> IsContentTranslatedAsync(string contentId, string languageCode);

    /// <summary>
    /// Gets translation status for content.
    /// </summary>
    /// <param name="contentId">Content identifier</param>
    /// <returns>Translation status for all languages</returns>
    Task<Dictionary<string, TranslationStatusModel>> GetTranslationStatusAsync(string contentId);

    /// <summary>
    /// Registers a translation for help content.
    /// </summary>
    /// <param name="translation">Translation to register</param>
    /// <returns>Registered translation</returns>
    Task<LocalizedHelpContentModel> RegisterTranslationAsync(LocalizedHelpContentModel translation);

    /// <summary>
    /// Gets missing translations (content not translated to language).
    /// </summary>
    /// <param name="languageCode">Language to check</param>
    /// <returns>Untranslated content identifiers</returns>
    Task<IEnumerable<string>> GetMissingTranslationsAsync(string languageCode);

    /// <summary>
    /// Formats date/time according to locale.
    /// </summary>
    /// <param name="dateTime">Date/time to format</param>
    /// <param name="languageCode">Language/locale code</param>
    /// <returns>Formatted date/time string</returns>
    string FormatDateTime(DateTime dateTime, string languageCode);

    /// <summary>
    /// Formats number according to locale.
    /// </summary>
    /// <param name="number">Number to format</param>
    /// <param name="languageCode">Language/locale code</param>
    /// <returns>Formatted number string</returns>
    string FormatNumber(decimal number, string languageCode);

    /// <summary>
    /// Gets pluralization rule for language.
    /// </summary>
    /// <param name="count">Count of items</param>
    /// <param name="languageCode">Language code</param>
    /// <returns>Plural rule form (singular, plural, zero, few, many, other)</returns>
    string GetPluralForm(int count, string languageCode);

    /// <summary>
    /// Applies RTL (right-to-left) layout for languages that require it.
    /// </summary>
    /// <param name="languageCode">Language code</param>
    /// <returns>True if RTL, false if LTR</returns>
    bool IsRightToLeftLanguage(string languageCode);
}

/// <summary>
/// Represents localized help content.
/// </summary>
public class LocalizedHelpContentModel
{
    /// <summary>Gets or sets the original content identifier.</summary>
    public string ContentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the language code for this translation.</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the translated title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the translated content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the translated summary.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets translator name.</summary>
    public string? TranslatedBy { get; set; }

    /// <summary>Gets or sets translation completion percentage (0-100).</summary>
    public int CompletionPercentage { get; set; } = 100;

    /// <summary>Gets or sets when translation was created.</summary>
    public DateTime TranslatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when translation was last reviewed.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Gets or sets reviewer name if reviewed.</summary>
    public string? ReviewedBy { get; set; }

    /// <summary>Gets or sets translation notes or comments.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets whether this is an official translation.</summary>
    public bool IsOfficial { get; set; }

    /// <summary>Gets or sets localized metadata (RTL, date format, etc).</summary>
    public LocalizationMetadataModel? Metadata { get; set; }
}

/// <summary>
/// Represents metadata for localized content.
/// </summary>
public class LocalizationMetadataModel
{
    /// <summary>Gets or sets whether content uses right-to-left layout.</summary>
    public bool IsRightToLeft { get; set; }

    /// <summary>Gets or sets locale-specific date format.</summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>Gets or sets locale-specific time format.</summary>
    public string TimeFormat { get; set; } = "HH:mm:ss";

    /// <summary>Gets or sets decimal separator for numbers.</summary>
    public char DecimalSeparator { get; set; } = '.';

    /// <summary>Gets or sets thousands separator for numbers.</summary>
    public char ThousandsSeparator { get; set; } = ',';

    /// <summary>Gets or sets currency code if applicable.</summary>
    public string? CurrencyCode { get; set; }

    /// <summary>Gets or sets custom CSS overrides for this locale.</summary>
    public Dictionary<string, string> CssOverrides { get; set; } = new();
}

/// <summary>
/// Represents a supported language.
/// </summary>
public class SupportedLanguageModel
{
    /// <summary>Gets or sets the language code (e.g., "en", "es", "fr").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name in English.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the native name in the language itself.</summary>
    public string NativeName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is a built-in supported language.</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>Gets or sets whether this language is RTL.</summary>
    public bool IsRightToLeft { get; set; }

    /// <summary>Gets or sets the number of translations available.</summary>
    public int TranslationCount { get; set; }

    /// <summary>Gets or sets overall translation completion percentage.</summary>
    public int CompletionPercentage { get; set; }
}

/// <summary>
/// Represents user's language settings.
/// </summary>
public class UserLanguageSettingsModel
{
    /// <summary>Gets or sets the user identifier.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's preferred language code.</summary>
    public string PreferredLanguageCode { get; set; } = "en";

    /// <summary>Gets or sets the fallback language if preferred not available.</summary>
    public string FallbackLanguageCode { get; set; } = "en";

    /// <summary>Gets or sets whether user explicitly set language or auto-detected.</summary>
    public bool IsExplicitlySet { get; set; }

    /// <summary>Gets or sets when settings were last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents translation status for a specific language.
/// </summary>
public class TranslationStatusModel
{
    /// <summary>Gets or sets the language code.</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Gets or sets whether content is translated to this language.</summary>
    public bool IsTranslated { get; set; }

    /// <summary>Gets or sets translation completion percentage.</summary>
    public int CompletionPercentage { get; set; }

    /// <summary>Gets or sets when translation was last updated.</summary>
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>Gets or sets translator name.</summary>
    public string? TranslatedBy { get; set; }

    /// <summary>Gets or sets whether translation has been reviewed.</summary>
    public bool IsReviewed { get; set; }

    /// <summary>Gets or sets notes about the translation status.</summary>
    public string? Notes { get; set; }
}
```

---

## 5. DATABASE SCHEMA

### 5.1 PostgreSQL Tables

```sql
-- Help content base table
CREATE TABLE help_content (
    help_content_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_key VARCHAR(255) NOT NULL UNIQUE,
    content_type VARCHAR(50) NOT NULL CHECK (content_type IN ('tooltip', 'contextual', 'field_hint', 'feature_explanation', 'code_doc')),
    title VARCHAR(500) NOT NULL,
    description TEXT,
    content_template TEXT NOT NULL,
    supports_markdown BOOLEAN DEFAULT TRUE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    INDEX idx_help_key (help_key),
    INDEX idx_content_type (content_type),
    INDEX idx_is_active (is_active)
);

-- Tooltips
CREATE TABLE tooltips (
    tooltip_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID NOT NULL REFERENCES help_content(help_content_id),
    context_key VARCHAR(255) NOT NULL,
    target_selector VARCHAR(500),
    position VARCHAR(20) DEFAULT 'auto' CHECK (position IN ('auto', 'top', 'bottom', 'left', 'right', 'center', 'follow_mouse')),
    show_delay_ms INT DEFAULT 300,
    hide_delay_ms INT DEFAULT 150,
    max_width_px INT DEFAULT 300,
    trigger_type VARCHAR(50) DEFAULT 'hover' CHECK (trigger_type IN ('hover', 'focus', 'click', 'focus_or_hover', 'always')),
    is_dismissible BOOLEAN DEFAULT TRUE,
    priority VARCHAR(20) DEFAULT 'normal' CHECK (priority IN ('low', 'normal', 'high', 'critical')),
    css_classes VARCHAR(500),
    expires_at TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_context_key (context_key),
    INDEX idx_help_content_id (help_content_id),
    INDEX idx_is_active (is_active)
);

-- Contextual help panels
CREATE TABLE help_panels (
    panel_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID NOT NULL REFERENCES help_content(help_content_id),
    context_key VARCHAR(255) NOT NULL UNIQUE,
    panel_title VARCHAR(500) NOT NULL,
    panel_description TEXT,
    thumbnail_url VARCHAR(1000),
    estimated_reading_time_minutes INT,
    display_order INT DEFAULT 0,
    category VARCHAR(100),
    is_visible BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_context_key (context_key),
    INDEX idx_category (category)
);

-- Help panel sections
CREATE TABLE help_sections (
    section_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    panel_id UUID NOT NULL REFERENCES help_panels(panel_id),
    section_title VARCHAR(500),
    section_content TEXT NOT NULL,
    parent_section_id UUID REFERENCES help_sections(section_id),
    expanded_by_default BOOLEAN DEFAULT TRUE,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_panel_id (panel_id),
    INDEX idx_parent_section_id (parent_section_id)
);

-- Field hints
CREATE TABLE field_hints (
    field_hint_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID NOT NULL REFERENCES help_content(help_content_id),
    field_id VARCHAR(255) NOT NULL UNIQUE,
    form_id VARCHAR(255) NOT NULL,
    field_label VARCHAR(500),
    hint_text TEXT NOT NULL,
    detailed_help TEXT,
    example_text VARCHAR(500),
    is_required BOOLEAN DEFAULT FALSE,
    field_type VARCHAR(50) DEFAULT 'text' CHECK (field_type IN ('text', 'number', 'email', 'password', 'url', 'date', 'time', 'textarea', 'select', 'checkbox', 'radio')),
    max_length INT,
    min_length INT,
    display_order INT,
    show_character_count BOOLEAN DEFAULT FALSE,
    show_format_guidance BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_field_id (field_id),
    INDEX idx_form_id (form_id)
);

-- Validation rules for fields
CREATE TABLE field_validation_rules (
    validation_rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    field_hint_id UUID NOT NULL REFERENCES field_hints(field_hint_id),
    rule_type VARCHAR(50) NOT NULL CHECK (rule_type IN ('required', 'max_length', 'min_length', 'format', 'pattern', 'invalid_value', 'duplicate', 'custom')),
    parameter VARCHAR(255),
    error_message_template TEXT,
    error_code VARCHAR(100),
    display_order INT DEFAULT 0,
    INDEX idx_field_hint_id (field_hint_id)
);

-- Feature explanations with steps
CREATE TABLE feature_explanations (
    explanation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID NOT NULL REFERENCES help_content(help_content_id),
    feature_key VARCHAR(255) NOT NULL UNIQUE,
    feature_title VARCHAR(500) NOT NULL,
    feature_description TEXT,
    is_enabled BOOLEAN DEFAULT TRUE,
    show_on_first_visit BOOLEAN DEFAULT FALSE,
    estimated_time_minutes INT,
    difficulty_level VARCHAR(20) DEFAULT 'beginner' CHECK (difficulty_level IN ('beginner', 'intermediate', 'advanced')),
    video_url VARCHAR(1000),
    target_roles TEXT, -- JSON array of role names
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_feature_key (feature_key),
    INDEX idx_is_enabled (is_enabled)
);

-- Tour steps
CREATE TABLE tour_steps (
    step_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    explanation_id UUID NOT NULL REFERENCES feature_explanations(explanation_id),
    step_number INT NOT NULL,
    step_title VARCHAR(500) NOT NULL,
    step_content TEXT NOT NULL,
    target_selector VARCHAR(500),
    tooltip_position VARCHAR(20) DEFAULT 'auto',
    image_url VARCHAR(1000),
    show_spotlight BOOLEAN DEFAULT TRUE,
    spotlight_padding_px INT DEFAULT 8,
    requires_interaction BOOLEAN DEFAULT FALSE,
    required_interaction VARCHAR(50) CHECK (required_interaction IN ('click', 'hover', 'type', 'select', 'scroll')),
    next_button_text VARCHAR(100) DEFAULT 'Next',
    show_skip_button BOOLEAN DEFAULT TRUE,
    show_previous_button BOOLEAN DEFAULT TRUE,
    estimated_time_seconds INT,
    css_classes VARCHAR(500),
    display_order INT,
    UNIQUE(explanation_id, step_number),
    INDEX idx_explanation_id (explanation_id)
);

-- Tour sessions for tracking
CREATE TABLE tour_sessions (
    session_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    explanation_id UUID NOT NULL REFERENCES feature_explanations(explanation_id),
    user_id UUID NOT NULL,
    current_step_number INT DEFAULT 1,
    is_completed BOOLEAN DEFAULT FALSE,
    was_skipped BOOLEAN DEFAULT FALSE,
    interaction_count INT DEFAULT 0,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP,
    INDEX idx_user_id (user_id),
    INDEX idx_explanation_id (explanation_id),
    INDEX idx_is_completed (is_completed)
);

-- Code documentation
CREATE TABLE code_documentation (
    doc_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID REFERENCES help_content(help_content_id),
    item_type VARCHAR(50) NOT NULL CHECK (item_type IN ('method', 'type', 'property', 'field', 'event')),
    full_name VARCHAR(500) NOT NULL UNIQUE,
    namespace VARCHAR(500),
    summary TEXT,
    remarks TEXT,
    signature TEXT,
    return_type VARCHAR(255),
    return_description TEXT,
    visibility VARCHAR(50),
    is_static BOOLEAN DEFAULT FALSE,
    is_async BOOLEAN DEFAULT FALSE,
    since_version VARCHAR(50),
    deprecated_in_version VARCHAR(50),
    removed_in_version VARCHAR(50),
    deprecation_message TEXT,
    recommended_alternative VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_full_name (full_name),
    INDEX idx_namespace (namespace),
    INDEX idx_item_type (item_type)
);

-- Parameters for code documentation
CREATE TABLE code_parameters (
    param_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doc_id UUID NOT NULL REFERENCES code_documentation(doc_id),
    param_name VARCHAR(255) NOT NULL,
    param_type VARCHAR(255),
    description TEXT,
    is_optional BOOLEAN DEFAULT FALSE,
    default_value VARCHAR(500),
    display_order INT,
    INDEX idx_doc_id (doc_id)
);

-- Code examples
CREATE TABLE code_examples (
    example_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doc_id UUID REFERENCES code_documentation(doc_id),
    panel_id UUID REFERENCES help_panels(panel_id),
    code_text TEXT NOT NULL,
    language VARCHAR(50) DEFAULT 'csharp',
    description TEXT,
    expected_output TEXT,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Localized content
CREATE TABLE localized_content (
    localized_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID NOT NULL REFERENCES help_content(help_content_id),
    language_code VARCHAR(10) NOT NULL,
    title VARCHAR(500),
    content TEXT,
    summary TEXT,
    translated_by UUID,
    completion_percentage INT DEFAULT 100,
    translated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reviewed_at TIMESTAMP,
    reviewed_by UUID,
    notes TEXT,
    is_official BOOLEAN DEFAULT FALSE,
    UNIQUE(help_content_id, language_code),
    INDEX idx_language_code (language_code),
    INDEX idx_help_content_id (help_content_id)
);

-- Localization metadata
CREATE TABLE localization_metadata (
    metadata_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    localized_id UUID NOT NULL REFERENCES localized_content(localized_id),
    is_right_to_left BOOLEAN DEFAULT FALSE,
    date_format VARCHAR(50),
    time_format VARCHAR(50),
    decimal_separator CHAR(1) DEFAULT '.',
    thousands_separator CHAR(1) DEFAULT ',',
    currency_code VARCHAR(3),
    css_overrides JSONB,
    UNIQUE(localized_id)
);

-- Help statistics
CREATE TABLE help_statistics (
    stat_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_content_id UUID NOT NULL REFERENCES help_content(help_content_id),
    user_id UUID NOT NULL,
    event_type VARCHAR(50) NOT NULL CHECK (event_type IN ('shown', 'viewed', 'completed', 'skipped', 'clicked')),
    context_key VARCHAR(255),
    metadata JSONB,
    event_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_help_content_id (help_content_id),
    INDEX idx_user_id (user_id),
    INDEX idx_event_type (event_type),
    INDEX idx_event_timestamp (event_timestamp)
);

-- Supported languages
CREATE TABLE supported_languages (
    language_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    language_code VARCHAR(10) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    native_name VARCHAR(100),
    is_built_in BOOLEAN DEFAULT FALSE,
    is_right_to_left BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User language preferences
CREATE TABLE user_language_preferences (
    preference_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE,
    preferred_language_code VARCHAR(10) NOT NULL REFERENCES supported_languages(language_code),
    fallback_language_code VARCHAR(10) DEFAULT 'en',
    is_explicitly_set BOOLEAN DEFAULT FALSE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX idx_help_content_content_type ON help_content(content_type);
CREATE INDEX idx_tooltips_context_key_active ON tooltips(context_key, is_active);
CREATE INDEX idx_help_panels_visible ON help_panels(is_visible);
CREATE INDEX idx_feature_explanations_enabled ON feature_explanations(is_enabled);
CREATE INDEX idx_help_statistics_timestamp ON help_statistics(event_timestamp DESC);
CREATE INDEX idx_tour_sessions_user_completed ON tour_sessions(user_id, is_completed);
```

---

## 6. TOOLTIP CONTENT SCHEMA

### 6.1 Tooltip Content Format

```json
{
  "tooltipId": "tooltip_001",
  "contextKey": "dashboard.metrics",
  "targetSelector": "[data-tooltip-id='daily-revenue']",
  "content": "Total revenue generated today",
  "htmlContent": "<strong>Daily Revenue</strong><br/>Total earnings from all sales",
  "position": "top",
  "showDelayMs": 300,
  "hideDelayMs": 150,
  "maxWidthPx": 250,
  "supportsMarkdown": true,
  "dismissible": true,
  "trigger": "hover",
  "cssClasses": "tooltip-primary",
  "priority": "normal",
  "expiresAt": null,
  "isActive": true,
  "languageCode": "en",
  "createdAt": "2025-02-01T10:00:00Z",
  "updatedAt": "2025-02-01T10:00:00Z"
}
```

### 6.2 Help Content Format Specification

```json
{
  "helpContent": {
    "helpId": "help_feature_001",
    "type": "contextual",
    "title": "Understanding Dashboard Metrics",
    "description": "Learn how to interpret and use dashboard metrics",
    "sections": [
      {
        "sectionId": "sec_001",
        "title": "Key Metrics Overview",
        "content": "Dashboard metrics provide real-time insights into your business performance...",
        "subsections": [
          {
            "sectionId": "subsec_001",
            "title": "Revenue Metrics",
            "content": "Revenue metrics track..."
          }
        ],
        "codeExamples": [
          {
            "language": "javascript",
            "code": "const metric = dashboard.getMetric('daily-revenue');",
            "description": "Retrieve a metric value"
          }
        ]
      }
    ],
    "externalLinks": [
      {
        "text": "Dashboard Documentation",
        "url": "https://docs.example.com/dashboard",
        "type": "documentation",
        "openInNewTab": true
      }
    ],
    "metadata": {
      "version": "1.0",
      "lastUpdated": "2025-02-01T10:00:00Z",
      "author": "Help Team",
      "tags": ["metrics", "dashboard", "analytics"]
    }
  }
}
```

---

## 7. INLINE HELP COVERAGE MAP

### 7.1 UI Elements with Help Content

| Component | Element | Tooltip | Contextual Help | Field Hint | Feature Explanation | Coverage % |
|-----------|---------|---------|-----------------|------------|---------------------|-----------|
| Dashboard | Revenue Card | Yes | Yes | N/A | No | 67% |
| Dashboard | Metrics Summary | Yes | Yes | N/A | Yes | 100% |
| Dashboard | Chart Controls | Yes | No | N/A | Yes | 67% |
| Forms | Login Email | Yes | Yes | Yes | No | 100% |
| Forms | Password Field | Yes | Yes | Yes | No | 100% |
| Forms | Settings | Yes | Yes | Yes | Yes | 100% |
| Navigation | Main Menu | Yes | Yes | N/A | Yes | 100% |
| Navigation | Breadcrumbs | Yes | No | N/A | No | 33% |
| Reports | Date Range Picker | Yes | Yes | Yes | No | 75% |
| Reports | Export Button | Yes | Yes | N/A | Yes | 100% |
| Profile | Avatar Upload | Yes | Yes | Yes | No | 75% |
| Profile | Settings Options | Yes | Yes | Yes | No | 75% |

**Target**: 95%+ component coverage for v0.20.1

---

## 8. PERFORMANCE TARGETS

### 8.1 Performance Benchmarks

| Metric | Target | Acceptance | Priority |
|--------|--------|-----------|----------|
| Tooltip render latency (P50) | < 16ms | < 20ms | Critical |
| Tooltip render latency (P95) | < 30ms | < 50ms | Critical |
| Help panel load time | < 200ms | < 300ms | High |
| Field hint display | < 50ms | < 100ms | High |
| Feature explanation overlay load | < 300ms | < 500ms | High |
| Tooltip positioning algorithm | < 10ms | < 15ms | Medium |
| Contextual help content lookup | < 100ms | < 200ms | Medium |
| Localization lookup latency | < 50ms | < 100ms | Medium |
| Code documentation hover display | < 100ms | < 150ms | Medium |

### 8.2 Memory and Resource Limits

| Resource | Limit | Note |
|----------|-------|------|
| Tooltip content cache size | 50MB | Per browser session |
| Help panel content cache | 100MB | Per browser session |
| Concurrent tooltip displays | 5 | Maximum simultaneous |
| Code documentation index size | 500MB | In-memory search index |
| Localization cache entries | 10,000 | Per language |

---

## 9. TESTING STRATEGY

### 9.1 Unit Testing

- **Tooltip Manager**: 30+ test cases
  - Position calculation edge cases
  - Content caching behavior
  - Locale-specific formatting
  - Priority queuing logic

- **Field Hint Service**: 25+ test cases
  - Dynamic hint generation
  - Validation message templating
  - Character count accuracy
  - Format guidance generation

- **Contextual Help Service**: 20+ test cases
  - Content organization
  - Section nesting
  - Related content detection
  - Search functionality

### 9.2 Integration Testing

- **Help Content Repository**: 15+ test cases
  - Database persistence
  - Query performance
  - Transaction handling
  - Cascade operations

- **Localization Service**: 20+ test cases
  - Multi-language content retrieval
  - Fallback behavior
  - Language-specific formatting
  - Date/time/number formatting

### 9.3 UI/Component Testing

- **Tooltip Component**: 20+ test cases
  - Rendering with various content
  - Position changes on viewport resize
  - Keyboard accessibility
  - Animation performance

- **Help Panel Component**: 15+ test cases
  - Section expansion/collapse
  - Scroll behavior
  - Link navigation
  - Responsive layout

### 9.4 E2E Testing

- **Feature Explanation Flow**: 10+ test cases
  - Multi-step navigation
  - Spotlight accuracy
  - Step progression
  - Tour completion tracking

- **Field Validation Flow**: 8+ test cases
  - Real-time validation
  - Error message display
  - Correction suggestions
  - Form submission behavior

### 9.5 Performance Testing

- **Tooltip Rendering**: Benchmark against performance targets
  - Measure P50, P95, P99 latencies
  - Profile memory usage
  - Test with 100+ tooltips on page

- **Help Content Loading**: Test cache effectiveness
  - First load vs. cached loads
  - Large content files
  - Concurrent requests

### 9.6 Accessibility Testing

- **WCAG 2.1 AA Compliance**
  - Keyboard navigation (TAB, SHIFT+TAB, ENTER, ESC)
  - Screen reader support
  - Color contrast (4.5:1 minimum)
  - Focus indicators

- **Internationalization Testing**
  - RTL language support
  - Special character rendering
  - Pluralization rules
  - Date/time formatting

### 9.7 User Experience Testing

- **Tooltip Usability**
  - User can find and activate tooltips
  - Tooltip timing feels natural
  - Content is clear and helpful
  - Dismissal is intuitive

- **Help Content Effectiveness**
  - Users can understand content
  - Navigation is logical
  - Search finds relevant results
  - Related links are helpful

---

## 10. MEDIATR EVENTS

### 10.1 Event Definitions

```csharp
/// <summary>
/// Event raised when tooltip is shown to user.
/// </summary>
public class TooltipShownEvent : INotification
{
    /// <summary>Gets the tooltip identifier.</summary>
    public string TooltipId { get; set; }

    /// <summary>Gets the context where tooltip was shown.</summary>
    public string ContextKey { get; set; }

    /// <summary>Gets the user who saw the tooltip.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets metadata about the event.</summary>
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>Gets timestamp of event.</summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when help content is viewed.
/// </summary>
public class HelpContentViewedEvent : INotification
{
    /// <summary>Gets the help content identifier.</summary>
    public string HelpContentId { get; set; }

    /// <summary>Gets the content type (tooltip, contextual, etc).</summary>
    public string ContentType { get; set; }

    /// <summary>Gets the user who viewed content.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets time spent viewing in seconds.</summary>
    public int? TimeSpentSeconds { get; set; }

    /// <summary>Gets metadata about the view event.</summary>
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>Gets timestamp of event.</summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when feature explanation is completed.
/// </summary>
public class FeatureExplainedEvent : INotification
{
    /// <summary>Gets the feature key.</summary>
    public string FeatureKey { get; set; }

    /// <summary>Gets the user who completed explanation.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets whether explanation was completed fully.</summary>
    public bool WasCompleted { get; set; }

    /// <summary>Gets whether explanation was skipped.</summary>
    public bool WasSkipped { get; set; }

    /// <summary>Gets total time spent in seconds.</summary>
    public int TimeSpentSeconds { get; set; }

    /// <summary>Gets number of steps completed.</summary>
    public int StepsCompleted { get; set; }

    /// <summary>Gets total number of steps.</summary>
    public int TotalSteps { get; set; }

    /// <summary>Gets metadata about the event.</summary>
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>Gets timestamp of event.</summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when validation error occurs.
/// </summary>
public class ValidationErrorEvent : INotification
{
    /// <summary>Gets the field that failed validation.</summary>
    public string FieldId { get; set; }

    /// <summary>Gets the validation type that failed.</summary>
    public ValidationType ValidationType { get; set; }

    /// <summary>Gets the user attempting to validate.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets the user's input that failed.</summary>
    public string? InvalidInput { get; set; }

    /// <summary>Gets error code for localization.</summary>
    public string ErrorCode { get; set; }

    /// <summary>Gets metadata about the error.</summary>
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>Gets timestamp of event.</summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when help content search is performed.
/// </summary>
public class HelpSearchPerformedEvent : INotification
{
    /// <summary>Gets the search query.</summary>
    public string SearchQuery { get; set; }

    /// <summary>Gets the user performing search.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets number of results found.</summary>
    public int ResultsCount { get; set; }

    /// <summary>Gets whether user clicked a result.</summary>
    public bool ClickedResult { get; set; }

    /// <summary>Gets which result was clicked (if any).</summary>
    public string? ClickedResultId { get; set; }

    /// <summary>Gets context where search occurred.</summary>
    public string? ContextKey { get; set; }

    /// <summary>Gets timestamp of event.</summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when localized content is accessed.
/// </summary>
public class LocalizedContentAccessedEvent : INotification
{
    /// <summary>Gets the content identifier.</summary>
    public string ContentId { get; set; }

    /// <summary>Gets the requested language code.</summary>
    public string LanguageCode { get; set; }

    /// <summary>Gets whether translation was available.</summary>
    public bool TranslationAvailable { get; set; }

    /// <summary>Gets the user accessing content.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets timestamp of event.</summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}
```

### 10.2 Event Handlers

```csharp
/// <summary>
/// Handles tooltip shown events for analytics tracking.
/// </summary>
public class TooltipShownEventHandler : INotificationHandler<TooltipShownEvent>
{
    private readonly IHelpAnalyticsService _analyticsService;

    public TooltipShownEventHandler(IHelpAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task Handle(TooltipShownEvent notification, CancellationToken cancellationToken)
    {
        await _analyticsService.RecordTooltipShownAsync(
            notification.TooltipId,
            notification.UserId,
            notification.Metadata,
            cancellationToken);
    }
}

/// <summary>
/// Handles help content viewed events.
/// </summary>
public class HelpContentViewedEventHandler : INotificationHandler<HelpContentViewedEvent>
{
    private readonly IHelpAnalyticsService _analyticsService;

    public HelpContentViewedEventHandler(IHelpAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task Handle(HelpContentViewedEvent notification, CancellationToken cancellationToken)
    {
        await _analyticsService.RecordHelpViewedAsync(
            notification.HelpContentId,
            notification.UserId,
            notification.TimeSpentSeconds,
            notification.Metadata,
            cancellationToken);
    }
}
```

---

## 11. DEPENDENCY CHAIN

### 11.1 Sub-Part Dependencies

```
v0.20.1a (Tooltip System)
    ├─ Core Framework (v0.19.x)
    ├─ UI Component Library (v0.19.3)
    └─ No other sub-parts

v0.20.1b (Contextual Help Panels)
    ├─ v0.20.1a (Tooltip System)
    ├─ Core Framework (v0.19.x)
    ├─ UI Component Library (v0.19.3)
    └─ Content Repository (new)

v0.20.1c (Field Hints & Validation)
    ├─ Core Framework (v0.19.x)
    ├─ Form Validation Service (v0.19.4)
    ├─ UI Component Library (v0.19.3)
    └─ No other sub-parts

v0.20.1d (Feature Explanation Overlays)
    ├─ v0.20.1a (Tooltip System)
    ├─ Core Framework (v0.19.x)
    ├─ UI Component Library (v0.19.3)
    └─ Analytics Service (v0.19.5)

v0.20.1e (Inline Code Documentation)
    ├─ Core Framework (v0.19.x)
    ├─ Documentation Repository (new)
    └─ Search Service (v0.18.2)

v0.20.1f (Help Content Localization)
    ├─ v0.20.1a through v0.20.1e (all sub-parts)
    ├─ Localization Service (v0.19.2)
    ├─ Core Framework (v0.19.x)
    └─ Content Repository (from v0.20.1b)
```

### 11.2 Critical Path

The critical path for v0.20.1 release:

```
Start
  ├─ v0.20.1a (Tooltip System) - 10 hours
  │   └─ v0.20.1b (Contextual Help) - 10 hours
  │       └─ v0.20.1d (Feature Explanations) - 8 hours
  │           └─ v0.20.1f (Localization) - 10 hours
  │
  ├─ v0.20.1c (Field Hints) - 8 hours [parallel]
  │   └─ v0.20.1f (Localization) - 10 hours
  │
  └─ v0.20.1e (Code Documentation) - 8 hours [parallel]
      └─ v0.20.1f (Localization) - 10 hours

Total Critical Path: 46 hours (parallelizable to 54 total with 8 hour buffer)
```

---

## 12. LICENSE GATING TABLE

| Feature | Community | Professional | Enterprise | Notes |
|---------|-----------|--------------|-----------|-------|
| Tooltip System | ✓ | ✓ | ✓ | Basic tooltips |
| Smart Positioning | ✓ | ✓ | ✓ | All positions supported |
| Contextual Help Panels | Limited | ✓ | ✓ | 3 panels per feature |
| Help Content Creation | ✗ | ✓ | ✓ | Admin only |
| Feature Explanations | ✗ | ✓ | ✓ | Guided tours |
| Field Hints & Validation | ✓ | ✓ | ✓ | All validation types |
| Code Documentation | ✓ | ✓ | ✓ | Basic docs |
| Advanced Code Docs | ✗ | ✗ | ✓ | Examples, deprecation |
| Multi-Language Support | Limited | ✓ | ✓ | 2 languages max |
| 5+ Language Support | ✗ | ✗ | ✓ | All languages |
| Analytics Tracking | ✗ | Limited | ✓ | 3 month retention |
| Custom Help Content | ✗ | ✓ | ✓ | Unlimited |
| Help Content Search | ✓ | ✓ | ✓ | Full-text search |
| Advanced Search | ✗ | ✗ | ✓ | Faceted search |
| Mobile Optimization | ✓ | ✓ | ✓ | Touch-friendly UI |
| Accessibility (WCAG AA) | ✓ | ✓ | ✓ | Full compliance |

---

## 13. RISKS & MITIGATIONS

### 13.1 Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Content localization delays | Medium | High | Pre-prepare translation templates, hire translators early |
| Performance regressions | Medium | Critical | Continuous performance testing, automated benchmarks |
| Accessibility compliance issues | Low | High | WCAG audit from start, automated testing |
| Content schema migration issues | Low | Critical | Comprehensive schema versioning, rollback plan |
| Browser compatibility issues | Low | Medium | Cross-browser testing matrix, polyfills |
| Help content accuracy | Medium | Medium | Content review process, user feedback loop |
| Tooltip positioning conflicts | Low | Medium | Comprehensive edge case testing, fallback strategies |
| Search index performance degradation | Low | High | Index optimization, caching strategy, pagination |

### 13.2 Mitigation Strategies

1. **Localization Delays**
   - Start translation sprints early (by hour 20)
   - Use professional translation service
   - Implement in-context translation tool
   - Create translation memory for reusable content

2. **Performance Regressions**
   - Set up continuous performance monitoring
   - Implement automated performance tests in CI/CD
   - Create performance budgets per feature
   - Monitor production with real user metrics

3. **Accessibility Issues**
   - Conduct WCAG audit at 50% completion
   - Include a11y reviewer on development team
   - Use automated accessibility testing tools
   - Plan for remediation time (2-3 hours)

4. **Schema Migration Issues**
   - Test migrations with production-sized datasets
   - Create comprehensive rollback procedures
   - Version all schema changes
   - Stage migrations in test environment first

---

## 14. UI MOCKUPS & WIREFRAMES

### 14.1 Tooltip Display States

```
Initial State:
┌─────────────────────────────────┐
│ Hover over element              │
│  [Button] ← User hovers here    │
└─────────────────────────────────┘

After Delay (300ms):
┌─────────────────────────────────┐
│         ╔═══════════════════╗   │
│ [Button]║ This is a tooltip ║   │
│         ║ with helpful info ║   │
│         ╚═══════════════════╝   │
└─────────────────────────────────┘

Position Options:
     ╔════════╗              ╔════════╗
     ║ Tooltip║              ║Tooltip ║
     ╚════════╝              ╚════════╝
      TOP   [Button]    [Button]  BOTTOM

╔════════╗              ╔════════╗
║Tooltip ║ [Button]     [Button] ║Tooltip ║
╚════════╝              ╚════════╝
 LEFT                      RIGHT
```

### 14.2 Help Panel Display

```
Main Content Area          Help Panel
┌──────────────────┐      ┌──────────────────┐
│                  │      │ Help: Dashboard  │
│   Dashboard      │   ▶  │ ─────────────────│
│                  │      │ Quick Overview:  │
│   [Content]      │      │ ┌──────────────┐ │
│   [More Content] │      │ │ Key metrics  │ │
│                  │      │ │ are displayed│ │
│   [Widgets]      │      │ │ here.        │ │
│                  │      │ └──────────────┘ │
│                  │      │ ▼ Expand Section │
│                  │      │                  │
│                  │      │ Related Help:    │
│                  │      │ • Setting Alerts │
│                  │      │ • Export Data    │
│                  │      │                  │
└──────────────────┘      └──────────────────┘

Close (✕) or Minimize (−)
```

### 14.3 Field Hint Display

```
Form Input:
┌─────────────────────────────────────┐
│ Email Address                       │
│ ┌───────────────────────────────┐   │
│ │ user@example.com              │   │
│ └───────────────────────────────┘   │
│ ℹ Enter a valid email address      │
│   (example: john@company.com)       │
│   Maximum 255 characters            │
│ Characters: 18 / 255               │
└─────────────────────────────────────┘

With Error:
┌─────────────────────────────────────┐
│ Email Address                       │
│ ┌───────────────────────────────┐   │
│ │ invalid-email                 │   │ ✗
│ └───────────────────────────────┘   │
│ ⚠ Invalid email format              │
│   Did you mean: invalid-email@?    │
│   Format: example@domain.com        │
└─────────────────────────────────────┘
```

### 14.4 Feature Explanation Overlay

```
┌─────────────────────────────────────────┐
│        Dashboard Feature Tour      ✕    │
├─────────────────────────────────────────┤
│                                         │
│  Step 1 of 5: Overview                  │
│  ═══════════════════════════════════    │
│                                         │
│  ╔═══════════════════════════════════╗  │
│  ║  The dashboard provides an        ║  │
│  ║  at-a-glance overview of your     ║  │
│  ║  key metrics and activities.      ║  │
│  ║                                   ║  │
│  ║  [Spotlight highlights button]    ║  │
│  ╚═══════════════════════════════════╝  │
│                                         │
│  [◄ Previous]  [Progress: ████░░░░░░░]  │
│  [Next ►]      [Skip]                   │
└─────────────────────────────────────────┘
```

---

## 15. ACCEPTANCE CRITERIA SUMMARY

### 15.1 Overall Release Criteria

- [ ] All sub-parts (v0.20.1a through v0.20.1f) completed
- [ ] 100+ hours of testing performed
- [ ] Performance targets met (95%+ of benchmarks)
- [ ] WCAG 2.1 AA accessibility compliance achieved
- [ ] Documentation complete and reviewed
- [ ] Security audit passed
- [ ] 5+ languages localized and reviewed
- [ ] User acceptance testing completed
- [ ] Deployment to staging successful
- [ ] Production deployment plan finalized

### 15.2 Sub-Part Level Criteria

**v0.20.1a: Tooltip System**
- 30+ unit tests with 100% pass rate
- Tooltip render latency < 20ms (P50)
- Positioning accuracy within 2px
- All browsers tested (Chrome, Firefox, Safari, Edge)

**v0.20.1b: Contextual Help Panels**
- 20+ integration tests
- Panel load time < 300ms
- Content organization supports 5+ nesting levels
- Responsive design tested on 3+ breakpoints

**v0.20.1c: Field Hints & Validation**
- 25+ UI tests
- Validation error display < 100ms
- All validation types tested
- Error message accuracy 100%

**v0.20.1d: Feature Explanation Overlays**
- 20+ E2E tests
- Tour completion tracking working
- Spotlight accuracy within 2px
- Mobile responsive verified

**v0.20.1e: Inline Code Documentation**
- 15+ integration tests
- XML comment extraction working for 95%+ methods
- Search functionality indexed
- Code example rendering tested

**v0.20.1f: Help Content Localization**
- 30+ localization tests
- Content available in 5+ languages
- Fallback language working correctly
- RTL layout verified for applicable languages

---

## 16. SCHEDULE & ESTIMATES

### 16.1 Detailed Time Breakdown

```
Week 1 (20 hours):
  v0.20.1a (Tooltip System) - 10 hours
    - Design and interface definitions
    - Core positioning algorithm
    - Content registry
    - Caching strategy

  v0.20.1c (Field Hints) - 10 hours [parallel]
    - Field hint data model
    - Validation message templating
    - Dynamic hint generation
    - UI component design

Week 2 (20 hours):
  v0.20.1a (Tooltip System) - completion - 4 hours
    - Component implementation
    - Unit tests
    - Browser compatibility

  v0.20.1b (Contextual Help) - 10 hours
    - Panel component design
    - Content organization
    - Section expansion/collapse
    - Related content linking

  v0.20.1d (Feature Explanations) - 6 hours [parallel]
    - Tour step model design
    - Session management
    - Spotlight algorithm

Week 3 (14 hours):
  v0.20.1b (Contextual Help) - completion - 6 hours
    - Integration with content repository
    - Search functionality
    - Integration tests

  v0.20.1d (Feature Explanations) - completion - 8 hours [parallel]
    - Component implementation
    - Tour progression logic
    - E2E tests

Week 4+ (Parallel):
  v0.20.1c completion - final 2 hours
  v0.20.1e (Code Documentation) - 8 hours
    - XML comment parsing
    - Documentation extraction
    - Search index creation
    - Integration tests

  v0.20.1f (Localization) - 10 hours
    - Localization service implementation
    - Content translation seeding
    - Language fallback testing
    - RTL support implementation
    - Multi-language testing
```

---

## 17. DELIVERABLES CHECKLIST

### 17.1 Code Deliverables

- [ ] ITooltipManager interface and implementation
- [ ] IContextualHelpService interface and implementation
- [ ] IFieldHintService interface and implementation
- [ ] IFeatureExplainerService interface and implementation
- [ ] ICodeDocumentationService interface and implementation
- [ ] IHelpLocalizationService interface and implementation
- [ ] TooltipPositioner utility class
- [ ] TooltipRegistry for content management
- [ ] HelpContentOrganizer utility
- [ ] ValidationMessageTemplater utility
- [ ] XMLCommentParser utility
- [ ] LocalizedContentLoader with caching
- [ ] LanguageFallbackProvider
- [ ] TranslationStatusMonitor

### 17.2 UI/Component Deliverables

- [ ] Tooltip component (Vue/React/Angular compatible)
- [ ] ContextualHelpPanel component
- [ ] FieldHintRenderer component
- [ ] FeatureExplanationOverlay component
- [ ] SpotlightRenderer for element highlighting
- [ ] StepSequenceManager component
- [ ] TooltipPositioner visual tests

### 17.3 Database Deliverables

- [ ] PostgreSQL schema (13 tables)
- [ ] Database migration scripts
- [ ] Index optimization scripts
- [ ] Data seed scripts for default content
- [ ] Backup and recovery procedures

### 17.4 Documentation Deliverables

- [ ] API documentation (Swagger/OpenAPI)
- [ ] Developer integration guide
- [ ] Content creation guide
- [ ] Localization workflow guide
- [ ] Performance tuning guide
- [ ] Troubleshooting guide
- [ ] Architecture documentation

### 17.5 Test Deliverables

- [ ] 100+ unit tests
- [ ] 50+ integration tests
- [ ] 30+ E2E tests
- [ ] Accessibility test report (WCAG 2.1 AA)
- [ ] Performance benchmark report
- [ ] Cross-browser compatibility matrix
- [ ] Load testing results

### 17.6 Configuration Deliverables

- [ ] Feature flags for gradual rollout
- [ ] Performance tuning configuration
- [ ] Language configuration
- [ ] Content caching policy
- [ ] Analytics tracking configuration

---

## 18. SUCCESS METRICS

### 18.1 Adoption Metrics

- 75%+ of users interact with tooltips within first week
- 50%+ of users view contextual help panels
- Help content search used by 30%+ of users
- Feature explanation tours completed by 40%+ of new users

### 18.2 Support Impact

- 20% reduction in support tickets related to feature questions
- 15% increase in first-time feature discovery
- 10% reduction in average resolution time

### 18.3 Quality Metrics

- < 1% of help content contains errors
- 95%+ of help content is accurate and up-to-date
- 95%+ of links are functional
- 99.5% help system uptime

### 18.4 Performance Metrics

- 99%+ of tooltip renders under 50ms
- 95%+ of help panel loads under 500ms
- Zero performance regressions
- 98%+ search query completion < 200ms

---

## 19. ROLLOUT STRATEGY

### 19.1 Phased Release

**Phase 1 (Week 1-2)**: Internal Alpha
- Available to development team only
- Feature flags disabled for production
- Performance testing and optimization

**Phase 2 (Week 3-4)**: Beta to 10% of Users
- Enable for 10% of user base
- Monitor performance and error rates
- Gather user feedback
- Iterate on UI/UX

**Phase 3 (Week 5-6)**: Beta to 50% of Users
- Expand to 50% of user base
- Continue monitoring
- Final content localization

**Phase 4 (Week 7)**: Full Release
- Enable for 100% of users
- Full monitoring and support
- Content refinement based on feedback

### 19.2 Rollback Plan

If critical issues found:
- Disable feature flags in < 1 minute
- Revert database schema changes
- Restore from backup if necessary
- Post-mortem analysis within 24 hours

---

## 20. CONCLUSION

v0.20.1-DOC represents a comprehensive implementation of inline documentation and help systems for LexiChord. By implementing tooltips, contextual help, field hints, feature explanations, code documentation, and localization in a coordinated manner, we provide users with immediate access to guidance and reduce support burden.

The estimated 54-hour effort, distributed across 6 sub-parts, allows for parallel development and efficient resource utilization. Performance targets ensure the help system enhances rather than detracts from the user experience, while comprehensive testing and localization strategies ensure quality across all supported languages and platforms.

Success metrics track both adoption and impact, ensuring we understand how effectively the help system serves user needs and improves the overall LexiChord experience.

---

**Document Version**: 1.0
**Last Updated**: 2025-02-01
**Next Review**: 2025-03-01
**Status**: Active - Ready for Implementation
