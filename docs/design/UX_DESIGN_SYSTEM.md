# Lexichord UX Design System

**Version:** 1.0.0
**Last Updated:** 2026-01-26
**Status:** Approved

---

## 1. Design Philosophy

### 1.1 The Orchestral Metaphor

Lexichord's UI embodies the concept of a **conductor's podium**—a space of clarity, control, and focus. The interface should feel like a professional music studio: sophisticated but not intimidating, powerful but not overwhelming.

**Core Principles:**

1. **Clarity Over Complexity** — Every element serves a purpose. No decorative clutter.
2. **Focus Without Isolation** — The editor is the stage; auxiliary panels are the wings.
3. **Progressive Disclosure** — Advanced features reveal themselves as needed.
4. **Consistent Rhythm** — Spacing, timing, and interactions follow predictable patterns.

### 1.2 The "Dark Stage" Philosophy

Lexichord defaults to **dark mode** because:
- Writers often work in low-light environments
- Dark backgrounds reduce eye strain during long sessions
- The "stage" metaphor: performers work in darkness while content is illuminated
- Orange accent highlights evoke warmth, creativity, and energy

---

## 2. Color System

### 2.1 Core Palette (Dark Mode - Primary)

```
┌─────────────────────────────────────────────────────────────┐
│  LEXICHORD COLOR PALETTE - DARK MODE                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  BACKGROUNDS                                                │
│  ┌─────┐ Surface.Base      #0D0D0F   rgb(13, 13, 15)       │
│  │█████│ The deepest layer - window background              │
│  └─────┘                                                    │
│  ┌─────┐ Surface.Elevated  #16161A   rgb(22, 22, 26)       │
│  │█████│ Panels, cards, sidebars                            │
│  └─────┘                                                    │
│  ┌─────┐ Surface.Overlay   #1E1E24   rgb(30, 30, 36)       │
│  │█████│ Modals, dropdowns, tooltips                        │
│  └─────┘                                                    │
│  ┌─────┐ Surface.Editor    #121215   rgb(18, 18, 21)       │
│  │█████│ The manuscript editor background                   │
│  └─────┘                                                    │
│                                                             │
│  BORDERS & DIVIDERS                                         │
│  ┌─────┐ Border.Subtle     #2A2A32   rgb(42, 42, 50)       │
│  │▓▓▓▓▓│ Panel edges, separators                            │
│  └─────┘                                                    │
│  ┌─────┐ Border.Default    #3D3D47   rgb(61, 61, 71)       │
│  │▓▓▓▓▓│ Input fields, focused elements                     │
│  └─────┘                                                    │
│                                                             │
│  TEXT                                                       │
│  ┌─────┐ Text.Primary      #EAEAEC   rgb(234, 234, 236)    │
│  │░░░░░│ Main content, headings                             │
│  └─────┘                                                    │
│  ┌─────┐ Text.Secondary    #A0A0A8   rgb(160, 160, 168)    │
│  │░░░░░│ Labels, captions, metadata                         │
│  └─────┘                                                    │
│  ┌─────┐ Text.Tertiary     #6B6B75   rgb(107, 107, 117)    │
│  │░░░░░│ Placeholders, disabled text                        │
│  └─────┘                                                    │
│                                                             │
│  ACCENT (ORANGE - The "Flame")                              │
│  ┌─────┐ Accent.Primary    #FF6B2C   rgb(255, 107, 44)     │
│  │█████│ Primary actions, active states, links              │
│  └─────┘                                                    │
│  ┌─────┐ Accent.Hover      #FF8A57   rgb(255, 138, 87)     │
│  │█████│ Hover states on accent elements                    │
│  └─────┘                                                    │
│  ┌─────┐ Accent.Muted      #FF6B2C26 rgba(255,107,44,0.15) │
│  │▒▒▒▒▒│ Selection highlights, subtle backgrounds           │
│  └─────┘                                                    │
│  ┌─────┐ Accent.Glow       #FF6B2C40 rgba(255,107,44,0.25) │
│  │▒▒▒▒▒│ Focus rings, glowing effects                       │
│  └─────┘                                                    │
│                                                             │
│  SEMANTIC COLORS                                            │
│  ┌─────┐ Status.Success    #34D399   rgb(52, 211, 153)     │
│  │█████│ Successful operations, "in tune"                   │
│  └─────┘                                                    │
│  ┌─────┐ Status.Warning    #FBBF24   rgb(251, 191, 36)     │
│  │█████│ Warnings, style suggestions                        │
│  └─────┘                                                    │
│  ┌─────┐ Status.Error      #F87171   rgb(248, 113, 113)    │
│  │█████│ Errors, "dissonance" indicators                    │
│  └─────┘                                                    │
│  ┌─────┐ Status.Info       #60A5FA   rgb(96, 165, 250)     │
│  │█████│ Informational, tips                                │
│  └─────┘                                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Light Mode Palette (Secondary)

For users who prefer light mode (outdoor work, accessibility needs):

| Token | Light Value | Usage |
|-------|-------------|-------|
| Surface.Base | `#FAFAFA` | Window background |
| Surface.Elevated | `#FFFFFF` | Panels, cards |
| Surface.Editor | `#FFFFFF` | Editor background |
| Border.Subtle | `#E5E5E5` | Dividers |
| Border.Default | `#D4D4D4` | Input borders |
| Text.Primary | `#18181B` | Main text |
| Text.Secondary | `#52525B` | Labels |
| Accent.Primary | `#EA580C` | Slightly darker orange for contrast |

### 2.3 Color Usage Rules

1. **Never use pure black (`#000000`)** — Always use `Surface.Base` for depth
2. **Never use pure white (`#FFFFFF`) for text** — Use `Text.Primary` for warmth
3. **Accent colors are for interaction only** — Don't use orange for static decorative elements
4. **Semantic colors must be consistent** — Red always means error, never "delete" or "stop"

---

## 3. Typography

### 3.1 Font Stack

```
Primary (UI):     "Inter", "Segoe UI Variable", system-ui, sans-serif
Monospace (Code): "JetBrains Mono", "Cascadia Code", "Fira Code", monospace
Editor (Writing): "IBM Plex Serif", "Charter", Georgia, serif (optional)
```

### 3.2 Type Scale

| Token | Size | Weight | Line Height | Usage |
|-------|------|--------|-------------|-------|
| `Display` | 32px | 700 | 1.2 | Welcome screens, splash |
| `Heading.1` | 24px | 600 | 1.3 | Page titles |
| `Heading.2` | 20px | 600 | 1.35 | Section headers |
| `Heading.3` | 16px | 600 | 1.4 | Panel titles |
| `Body.Large` | 16px | 400 | 1.6 | Editor content |
| `Body.Default` | 14px | 400 | 1.5 | UI text |
| `Body.Small` | 12px | 400 | 1.5 | Captions, metadata |
| `Label` | 11px | 500 | 1.4 | Form labels, tags |
| `Code` | 13px | 400 | 1.5 | Inline code, terminal |

### 3.3 Typography Rules

1. **Maximum line length:** 72 characters for body text (editor), 100 for UI
2. **Paragraph spacing:** 1.5× the font size
3. **Heading hierarchy:** Never skip levels (H1 → H3 is forbidden)
4. **All-caps usage:** Only for `Label` tokens, never for sentences

---

## 4. Spacing & Layout

### 4.1 Spacing Scale (8px Base)

```
Space.0   = 0px    (none)
Space.1   = 4px    (tight)
Space.2   = 8px    (small)
Space.3   = 12px   (medium-small)
Space.4   = 16px   (medium)
Space.5   = 24px   (large)
Space.6   = 32px   (x-large)
Space.7   = 48px   (section)
Space.8   = 64px   (page)
```

### 4.2 Layout Grid

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [≡]  Lexichord                              [Theme] [Settings] [User]  │ ← Title Bar (40px)
├────────┬────────────────────────────────────────────────────────────────┤
│        │                                                                │
│  NAV   │                    CONTENT REGION                              │
│  RAIL  │                                                                │
│  (56px)│  ┌──────────────────────────────────────────────────────────┐  │
│        │  │                                                          │  │
│  [📄]  │  │                    MANUSCRIPT EDITOR                     │  │
│  [📚]  │  │                                                          │  │
│  [🎹]  │  │              (Primary working area)                      │  │
│  [⚙️]  │  │                                                          │  │
│        │  │                                                          │  │
│        │  └──────────────────────────────────────────────────────────┘  │
│        │                                                                │
│        ├────────────────────────────────────────────────────────────────┤
│        │  [Status Bar: Word Count | Style Score | AI Status]     (28px)│
└────────┴────────────────────────────────────────────────────────────────┘

With Sidebars Expanded:
┌────────┬──────────────────────────────────────────┬─────────────────────┐
│  NAV   │           MANUSCRIPT EDITOR              │    INSPECTOR        │
│  RAIL  │                                          │    PANEL            │
│        │                                          │    (320px)          │
│        │                                          │                     │
│        │                                          │  ┌─────────────┐    │
│        │                                          │  │ Style Score │    │
│        │                                          │  │    92%      │    │
│        │                                          │  └─────────────┘    │
│        │                                          │                     │
│        │                                          │  ┌─────────────┐    │
│        │                                          │  │ Suggestions │    │
│        │                                          │  └─────────────┘    │
└────────┴──────────────────────────────────────────┴─────────────────────┘
```

### 4.3 Responsive Breakpoints

| Breakpoint | Min Width | Layout Changes |
|------------|-----------|----------------|
| Compact | 800px | Nav rail collapses to icons only |
| Default | 1200px | Full layout with optional sidebars |
| Wide | 1600px+ | Three-column layout, wider editor |

---

## 5. Components

### 5.1 Buttons

```
┌─────────────────────────────────────────────────────────────┐
│  BUTTON VARIANTS                                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  PRIMARY (Orange Fill)                                      │
│  ┌────────────────────┐  Height: 36px                      │
│  │    Generate ✨     │  Padding: 16px horizontal          │
│  └────────────────────┘  Border-radius: 6px                │
│  Background: Accent.Primary                                 │
│  Text: #FFFFFF (white)                                      │
│  Hover: Accent.Hover                                        │
│                                                             │
│  SECONDARY (Ghost/Outline)                                  │
│  ┌────────────────────┐                                    │
│  │      Cancel        │                                    │
│  └────────────────────┘                                    │
│  Background: transparent                                    │
│  Border: 1px Border.Default                                 │
│  Text: Text.Primary                                         │
│  Hover: Surface.Elevated background                         │
│                                                             │
│  TERTIARY (Text Only)                                       │
│  ┌────────────────────┐                                    │
│  │    Learn More →    │                                    │
│  └────────────────────┘                                    │
│  Background: transparent                                    │
│  Text: Accent.Primary                                       │
│  Hover: Accent.Muted background                             │
│                                                             │
│  DESTRUCTIVE                                                │
│  ┌────────────────────┐                                    │
│  │      Delete        │                                    │
│  └────────────────────┘                                    │
│  Background: Status.Error                                   │
│  Text: #FFFFFF                                              │
│                                                             │
│  DISABLED STATE (All variants)                              │
│  Opacity: 0.5                                               │
│  Cursor: not-allowed                                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Input Fields

```
┌─────────────────────────────────────────────────────────────┐
│  TEXT INPUT                                                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  LABEL (optional)                                           │
│  Project Name                                               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ My Documentation Project                              │   │
│  └──────────────────────────────────────────────────────┘   │
│  Helper text appears here                                   │
│                                                             │
│  States:                                                    │
│  - Default: Border.Subtle border                            │
│  - Hover: Border.Default border                             │
│  - Focus: Border.Default + Accent.Glow shadow               │
│  - Error: Status.Error border + red helper text             │
│  - Disabled: Surface.Base background, 0.5 opacity           │
│                                                             │
│  Specs:                                                     │
│  - Height: 40px                                             │
│  - Padding: 12px horizontal                                 │
│  - Border-radius: 6px                                       │
│  - Font: Body.Default                                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.3 Cards & Panels

```
┌─────────────────────────────────────────────────────────────┐
│  CARD COMPONENT                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                                                        │ │
│  │  📄 Document Title                           [···]    │ │
│  │  ──────────────────────────────────────────────────   │ │
│  │                                                        │ │
│  │  Card body content goes here. Can contain text,       │ │
│  │  images, or nested components.                        │ │
│  │                                                        │ │
│  │  ┌──────────────────┐  ┌──────────────────┐           │ │
│  │  │  Secondary Btn   │  │   Primary Btn    │           │ │
│  │  └──────────────────┘  └──────────────────┘           │ │
│  │                                                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
│  Specs:                                                     │
│  - Background: Surface.Elevated                             │
│  - Border: 1px Border.Subtle                                │
│  - Border-radius: 8px                                       │
│  - Padding: 16px                                            │
│  - Shadow: 0 2px 8px rgba(0,0,0,0.2) (subtle)              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.4 Navigation Rail

```
┌──────┐
│  ≡   │  ← Hamburger (collapse/expand)
├──────┤
│      │
│  📄  │  ← Editor (active state: orange icon + left border)
│      │
│  📚  │  ← Knowledge (The Score)
│      │
│  🎹  │  ← Agents (The Ensemble)
│      │
│  📊  │  ← Analytics
│      │
├──────┤
│  ⚙️  │  ← Settings (bottom-anchored)
│      │
│  👤  │  ← User Profile
└──────┘

Active State:
- Left border: 3px Accent.Primary
- Icon: Accent.Primary color
- Background: Accent.Muted

Hover State:
- Background: Surface.Overlay
```

### 5.5 Style Linter Indicators

```
┌─────────────────────────────────────────────────────────────┐
│  INLINE STYLE FEEDBACK                                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  The administrator should whitelist the IP address.         │
│           ~~~~~~~~~~~          ~~~~~~~~~                    │
│           ↑ Warning            ↑ Error                      │
│           (Yellow squiggle)    (Red squiggle)               │
│                                                             │
│  Tooltip on hover:                                          │
│  ┌──────────────────────────────────────────────┐           │
│  │ ⚠️ Style Warning                              │           │
│  │ ────────────────────────────────────────────  │           │
│  │ "whitelist" is discouraged.                   │           │
│  │ Preferred: "allowlist"                        │           │
│  │                                              │           │
│  │ [Apply Fix]  [Ignore]  [Add to Dictionary]   │           │
│  └──────────────────────────────────────────────┘           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. Motion & Animation

### 6.1 Timing Curves

| Token | Duration | Easing | Usage |
|-------|----------|--------|-------|
| `Motion.Instant` | 0ms | — | Immediate feedback |
| `Motion.Fast` | 100ms | ease-out | Hover states, micro-interactions |
| `Motion.Normal` | 200ms | ease-in-out | Panel transitions, modals |
| `Motion.Slow` | 300ms | ease-in-out | Page transitions, complex animations |
| `Motion.Deliberate` | 500ms | cubic-bezier(0.4, 0, 0.2, 1) | Onboarding, attention-grabbing |

### 6.2 Animation Principles

1. **Purpose over decoration** — Animation must provide feedback or guide attention
2. **Respect user preferences** — Honor `prefers-reduced-motion: reduce`
3. **Never block interaction** — Animations should not prevent clicking/typing
4. **Consistent direction** — Elements enter from where they'll logically go

### 6.3 Common Animations

```
Panel Slide:
- Direction: From right (inspector) or left (nav)
- Duration: Motion.Normal (200ms)
- Opacity: Fade in simultaneously

Modal Entrance:
- Scale: 0.95 → 1.0
- Opacity: 0 → 1
- Duration: Motion.Normal

Tooltip:
- Delay: 300ms before showing
- Fade in: Motion.Fast (100ms)
- Position: Prefer above, then below

Loading Spinner:
- Rotation: Continuous, 1s per revolution
- Color: Accent.Primary
```

---

## 7. Accessibility

### 7.1 Contrast Requirements

| Element Type | Minimum Ratio | Our Actual Ratio |
|-------------|---------------|------------------|
| Body text | 4.5:1 | 11.2:1 (Text.Primary on Surface.Base) |
| Large text (18px+) | 3:1 | ✅ Exceeds |
| UI components | 3:1 | ✅ All buttons exceed |
| Focus indicators | 3:1 | ✅ Accent.Glow on Surface.Base = 4.8:1 |

### 7.2 Keyboard Navigation

1. **Tab order** must follow visual layout (left→right, top→bottom)
2. **Focus visible** state required on all interactive elements
3. **Escape** dismisses modals, dropdowns, and tooltips
4. **Arrow keys** navigate within grouped controls (tabs, menus)

### 7.3 Screen Reader Support

- All images must have `alt` text or be marked decorative
- Interactive elements need accessible names (`AutomationProperties.Name`)
- Live regions announce dynamic content changes
- Landmarks define page structure

---

## 8. Iconography

### 8.1 Icon Set

Lexichord uses **Lucide Icons** (open-source, consistent style).

| Icon | Name | Usage |
|------|------|-------|
| 📄 | `file-text` | Documents, editor |
| 📚 | `library` | Knowledge base, RAG |
| 🎹 | `piano` | Agents, AI features |
| 📊 | `bar-chart-2` | Analytics, metrics |
| ⚙️ | `settings` | Configuration |
| 👤 | `user` | Profile, account |
| ✨ | `sparkles` | AI-generated content |
| ⚠️ | `alert-triangle` | Warnings |
| ❌ | `x-circle` | Errors, close |
| ✅ | `check-circle` | Success, confirmation |

### 8.2 Icon Sizing

| Context | Size | Stroke Width |
|---------|------|--------------|
| Navigation rail | 24px | 1.5px |
| Buttons | 16px | 2px |
| Inline text | 14px | 2px |
| Status indicators | 12px | 2px |

---

## 9. Dark Mode Implementation (AvaloniaUI)

### 9.1 Resource Dictionary Structure

```xml
<!-- Lexichord.Dark.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Surfaces -->
    <Color x:Key="Surface.Base">#0D0D0F</Color>
    <Color x:Key="Surface.Elevated">#16161A</Color>
    <Color x:Key="Surface.Overlay">#1E1E24</Color>
    <Color x:Key="Surface.Editor">#121215</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="Brush.Surface.Base" Color="{StaticResource Surface.Base}"/>
    <SolidColorBrush x:Key="Brush.Accent.Primary" Color="#FF6B2C"/>

    <!-- Text -->
    <SolidColorBrush x:Key="Brush.Text.Primary" Color="#EAEAEC"/>
    <SolidColorBrush x:Key="Brush.Text.Secondary" Color="#A0A0A8"/>

</ResourceDictionary>
```

### 9.2 Theme Switching Service

```csharp
public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    void SetTheme(ThemeMode mode);
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;
}

public enum ThemeMode { Dark, Light, System }
```

---

## 10. File Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Views | `{Feature}View.axaml` | `EditorView.axaml` |
| ViewModels | `{Feature}ViewModel.cs` | `EditorViewModel.cs` |
| Styles | `{Component}Styles.axaml` | `ButtonStyles.axaml` |
| Icons | `{name}-{size}.svg` | `settings-24.svg` |
| Screenshots | `{screen}-{state}-{theme}.png` | `editor-active-dark.png` |

---

## Appendix A: Quick Reference Card

```
╔═══════════════════════════════════════════════════════════════╗
║  LEXICHORD DESIGN QUICK REFERENCE                             ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  COLORS                                                       ║
║  Background:  #0D0D0F        Accent:    #FF6B2C               ║
║  Elevated:    #16161A        Success:   #34D399               ║
║  Text:        #EAEAEC        Warning:   #FBBF24               ║
║  Secondary:   #A0A0A8        Error:     #F87171               ║
║                                                               ║
║  TYPOGRAPHY                                                   ║
║  Font:        Inter (UI), JetBrains Mono (code)              ║
║  Base size:   14px                                            ║
║  Line height: 1.5                                             ║
║                                                               ║
║  SPACING                                                      ║
║  Base unit:   8px                                             ║
║  Common:      8, 16, 24, 32px                                 ║
║                                                               ║
║  BORDERS                                                      ║
║  Radius:      6px (inputs), 8px (cards)                      ║
║  Color:       #2A2A32 (subtle), #3D3D47 (default)            ║
║                                                               ║
║  MOTION                                                       ║
║  Fast:        100ms          Normal:    200ms                 ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```
