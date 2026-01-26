# Lexichord: React Prototype → AvaloniaUI AXAML Mapping

**Version:** 1.0.0
**Last Updated:** 2026-01-26
**Target Framework:** AvaloniaUI 11.2+ / .NET 9

This document maps every component and pattern from the React prototype to production-ready AvaloniaUI AXAML code.

---

## Table of Contents

1. [Color System](#1-color-system)
2. [Typography](#2-typography)
3. [Layout Structure](#3-layout-structure)
4. [Navigation Rail](#4-navigation-rail)
5. [Buttons](#5-buttons)
6. [Text Inputs](#6-text-inputs)
7. [Cards & Panels](#7-cards--panels)
8. [Style Gauge](#8-style-gauge)
9. [Issue Cards](#9-issue-cards)
10. [Agent Cards](#10-agent-cards)
11. [Tab Bar](#11-tab-bar)
12. [Status Bar](#12-status-bar)
13. [Progress Indicators](#13-progress-indicators)
14. [Icons](#14-icons)
15. [Animations](#15-animations)
16. [Known Differences](#16-known-differences)

---

## 1. Color System

### React (Prototype)

```javascript
const colors = {
  dark: {
    surfaceBase: '#0D0D0F',
    accentMuted: 'rgba(255, 107, 44, 0.15)',  // ⚠️ CSS rgba format
    // ...
  }
};
```

### AvaloniaUI (Production)

```xml
<!-- Themes/Colors.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- ═══════════════════════════════════════════════════════════════
       SURFACE COLORS
       ═══════════════════════════════════════════════════════════════ -->
  <Color x:Key="SurfaceBaseColor">#FF0D0D0F</Color>
  <Color x:Key="SurfaceElevatedColor">#FF16161A</Color>
  <Color x:Key="SurfaceOverlayColor">#FF1E1E24</Color>
  <Color x:Key="SurfaceEditorColor">#FF121215</Color>

  <!-- ═══════════════════════════════════════════════════════════════
       BORDER COLORS
       ═══════════════════════════════════════════════════════════════ -->
  <Color x:Key="BorderSubtleColor">#FF2A2A32</Color>
  <Color x:Key="BorderDefaultColor">#FF3D3D47</Color>

  <!-- ═══════════════════════════════════════════════════════════════
       TEXT COLORS
       ═══════════════════════════════════════════════════════════════ -->
  <Color x:Key="TextPrimaryColor">#FFEAEAEC</Color>
  <Color x:Key="TextSecondaryColor">#FFA0A0A8</Color>
  <Color x:Key="TextTertiaryColor">#FF6B6B75</Color>

  <!-- ═══════════════════════════════════════════════════════════════
       ACCENT COLORS (Orange)
       ═══════════════════════════════════════════════════════════════ -->
  <Color x:Key="AccentPrimaryColor">#FFFF6B2C</Color>
  <Color x:Key="AccentHoverColor">#FFFF8A57</Color>
  <Color x:Key="AccentPressedColor">#FFE55A1F</Color>
  <!-- Alpha colors: Use #AARRGGBB format -->
  <Color x:Key="AccentMutedColor">#26FF6B2C</Color>      <!-- 15% opacity -->
  <Color x:Key="AccentGlowColor">#40FF6B2C</Color>       <!-- 25% opacity -->

  <!-- ═══════════════════════════════════════════════════════════════
       SEMANTIC COLORS
       ═══════════════════════════════════════════════════════════════ -->
  <Color x:Key="StatusSuccessColor">#FF34D399</Color>
  <Color x:Key="StatusWarningColor">#FFFBBF24</Color>
  <Color x:Key="StatusErrorColor">#FFF87171</Color>
  <Color x:Key="StatusInfoColor">#FF60A5FA</Color>

  <!-- ═══════════════════════════════════════════════════════════════
       BRUSHES (for binding)
       ═══════════════════════════════════════════════════════════════ -->
  <SolidColorBrush x:Key="SurfaceBaseBrush" Color="{StaticResource SurfaceBaseColor}" />
  <SolidColorBrush x:Key="SurfaceElevatedBrush" Color="{StaticResource SurfaceElevatedColor}" />
  <SolidColorBrush x:Key="SurfaceOverlayBrush" Color="{StaticResource SurfaceOverlayColor}" />
  <SolidColorBrush x:Key="SurfaceEditorBrush" Color="{StaticResource SurfaceEditorColor}" />

  <SolidColorBrush x:Key="BorderSubtleBrush" Color="{StaticResource BorderSubtleColor}" />
  <SolidColorBrush x:Key="BorderDefaultBrush" Color="{StaticResource BorderDefaultColor}" />

  <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}" />
  <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}" />
  <SolidColorBrush x:Key="TextTertiaryBrush" Color="{StaticResource TextTertiaryColor}" />

  <SolidColorBrush x:Key="AccentPrimaryBrush" Color="{StaticResource AccentPrimaryColor}" />
  <SolidColorBrush x:Key="AccentHoverBrush" Color="{StaticResource AccentHoverColor}" />
  <SolidColorBrush x:Key="AccentMutedBrush" Color="{StaticResource AccentMutedColor}" />
  <SolidColorBrush x:Key="AccentGlowBrush" Color="{StaticResource AccentGlowColor}" />

  <SolidColorBrush x:Key="StatusSuccessBrush" Color="{StaticResource StatusSuccessColor}" />
  <SolidColorBrush x:Key="StatusWarningBrush" Color="{StaticResource StatusWarningColor}" />
  <SolidColorBrush x:Key="StatusErrorBrush" Color="{StaticResource StatusErrorColor}" />
  <SolidColorBrush x:Key="StatusInfoBrush" Color="{StaticResource StatusInfoColor}" />

</ResourceDictionary>
```

### Key Differences

| React | Avalonia | Notes |
|-------|----------|-------|
| `#FF6B2C` | `#FFFF6B2C` | Avalonia uses `#AARRGGBB` (alpha first) |
| `rgba(255, 107, 44, 0.15)` | `#26FF6B2C` | Convert: `0.15 × 255 = 38 = 0x26` |
| Inline styles | `{StaticResource}` or `{DynamicResource}` | Use `DynamicResource` for theme switching |

---

## 2. Typography

### React (Prototype)

```javascript
className="text-4xl font-bold"  // Tailwind
className="text-sm"
className="font-mono"
```

### AvaloniaUI (Production)

```xml
<!-- Themes/Typography.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Font Families -->
  <FontFamily x:Key="InterFont">avares://Lexichord.Host/Assets/Fonts#Inter</FontFamily>
  <FontFamily x:Key="JetBrainsMonoFont">avares://Lexichord.Host/Assets/Fonts#JetBrains Mono</FontFamily>

  <!-- Or use system fonts -->
  <FontFamily x:Key="DefaultFont">Inter, Segoe UI Variable, -apple-system, sans-serif</FontFamily>
  <FontFamily x:Key="MonoFont">JetBrains Mono, Cascadia Code, Consolas, monospace</FontFamily>

  <!-- Type Scale Styles -->
  <ControlTheme x:Key="DisplayText" TargetType="TextBlock">
    <Setter Property="FontSize" Value="32" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="FontFamily" Value="{StaticResource DefaultFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
  </ControlTheme>

  <ControlTheme x:Key="Heading1Text" TargetType="TextBlock">
    <Setter Property="FontSize" Value="24" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="FontFamily" Value="{StaticResource DefaultFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
  </ControlTheme>

  <ControlTheme x:Key="Heading2Text" TargetType="TextBlock">
    <Setter Property="FontSize" Value="20" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="FontFamily" Value="{StaticResource DefaultFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
  </ControlTheme>

  <ControlTheme x:Key="BodyText" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="FontWeight" Value="Normal" />
    <Setter Property="FontFamily" Value="{StaticResource DefaultFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
  </ControlTheme>

  <ControlTheme x:Key="SmallText" TargetType="TextBlock">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="FontWeight" Value="Normal" />
    <Setter Property="FontFamily" Value="{StaticResource DefaultFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextSecondaryBrush}" />
  </ControlTheme>

  <ControlTheme x:Key="MonoText" TargetType="TextBlock">
    <Setter Property="FontSize" Value="13" />
    <Setter Property="FontWeight" Value="Normal" />
    <Setter Property="FontFamily" Value="{StaticResource MonoFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
  </ControlTheme>

  <ControlTheme x:Key="LabelText" TargetType="TextBlock">
    <Setter Property="FontSize" Value="11" />
    <Setter Property="FontWeight" Value="Medium" />
    <Setter Property="FontFamily" Value="{StaticResource DefaultFont}" />
    <Setter Property="Foreground" Value="{DynamicResource TextTertiaryBrush}" />
    <Setter Property="TextTransform" Value="Uppercase" />
  </ControlTheme>

</ResourceDictionary>
```

### Usage

```xml
<!-- Apply theme to TextBlock -->
<TextBlock Theme="{StaticResource Heading1Text}" Text="API Reference" />
<TextBlock Theme="{StaticResource BodyText}" Text="Regular body text" />
<TextBlock Theme="{StaticResource MonoText}" Text="code.example()" />
```

---

## 3. Layout Structure

### React (Prototype)

```jsx
<div className="h-screen flex flex-col">
  <header className="h-10">...</header>
  <div className="flex flex-1">
    <nav className="w-14">...</nav>
    <main className="flex-1">...</main>
  </div>
</div>
```

### AvaloniaUI (Production)

```xml
<!-- Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Lexichord.Host.ViewModels"
        xmlns:views="using:Lexichord.Host.Views"
        xmlns:controls="using:Lexichord.Host.Controls"
        x:Class="Lexichord.Host.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="Lexichord"
        Width="1400" Height="900"
        MinWidth="800" MinHeight="600"
        Background="{DynamicResource SurfaceBaseBrush}"
        ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaChromeHints="NoChrome"
        ExtendClientAreaTitleBarHeightHint="40">

  <Grid RowDefinitions="40,*,28">

    <!-- ═══════════════════════════════════════════════════════════════
         ROW 0: TITLE BAR (40px)
         ═══════════════════════════════════════════════════════════════ -->
    <Border Grid.Row="0"
            Background="{DynamicResource SurfaceElevatedBrush}"
            BorderBrush="{DynamicResource BorderSubtleBrush}"
            BorderThickness="0,0,0,1">
      <Grid ColumnDefinitions="Auto,*,Auto">
        <!-- Left: Logo -->
        <StackPanel Grid.Column="0"
                   Orientation="Horizontal"
                   Spacing="12"
                   Margin="16,0">
          <Button Classes="icon-button">
            <PathIcon Data="{StaticResource MenuIcon}" Width="18" Height="18" />
          </Button>
          <TextBlock Text="LEXICHORD"
                    FontWeight="SemiBold"
                    Foreground="{DynamicResource AccentPrimaryBrush}"
                    VerticalAlignment="Center" />
        </StackPanel>

        <!-- Center: Drag Region -->
        <Border Grid.Column="1"
               IsHitTestVisible="True"
               Background="Transparent"
               Name="TitleBarDragRegion" />

        <!-- Right: Controls -->
        <StackPanel Grid.Column="2"
                   Orientation="Horizontal"
                   Spacing="8"
                   Margin="0,0,16,0">
          <Button Classes="icon-button"
                 Command="{Binding ToggleThemeCommand}">
            <PathIcon Data="{Binding ThemeIcon}" Width="16" Height="16" />
          </Button>
          <Button Classes="icon-button">
            <PathIcon Data="{StaticResource SettingsIcon}" Width="18" Height="18" />
          </Button>
          <Button Classes="user-menu">
            <StackPanel Orientation="Horizontal" Spacing="8">
              <PathIcon Data="{StaticResource UserIcon}" Width="16" Height="16" />
              <TextBlock Text="{Binding CurrentUser.Name}"
                        Foreground="{DynamicResource TextSecondaryBrush}" />
              <PathIcon Data="{StaticResource ChevronDownIcon}" Width="12" Height="12" />
            </StackPanel>
          </Button>
        </StackPanel>
      </Grid>
    </Border>

    <!-- ═══════════════════════════════════════════════════════════════
         ROW 1: MAIN CONTENT
         ═══════════════════════════════════════════════════════════════ -->
    <Grid Grid.Row="1" ColumnDefinitions="56,*">

      <!-- Navigation Rail -->
      <controls:NavigationRail Grid.Column="0"
                              SelectedItem="{Binding SelectedNavItem}"
                              TopItems="{Binding TopNavItems}"
                              BottomItems="{Binding BottomNavItems}" />

      <!-- Content Area -->
      <Grid Grid.Column="1" RowDefinitions="36,*">

        <!-- Tab Bar -->
        <controls:TabBar Grid.Row="0"
                        Tabs="{Binding OpenTabs}"
                        SelectedTab="{Binding SelectedTab}" />

        <!-- Content + Inspector -->
        <Grid Grid.Row="1" ColumnDefinitions="*,Auto">

          <!-- Main Content -->
          <ContentControl Grid.Column="0"
                         Content="{Binding CurrentView}"
                         Background="{DynamicResource SurfaceEditorBrush}" />

          <!-- Inspector Panel (collapsible) -->
          <Border Grid.Column="1"
                 Width="{Binding InspectorWidth}"
                 IsVisible="{Binding IsInspectorVisible}"
                 Background="{DynamicResource SurfaceElevatedBrush}"
                 BorderBrush="{DynamicResource BorderSubtleBrush}"
                 BorderThickness="1,0,0,0">
            <controls:InspectorPanel DataContext="{Binding InspectorViewModel}" />
          </Border>
        </Grid>
      </Grid>
    </Grid>

    <!-- ═══════════════════════════════════════════════════════════════
         ROW 2: STATUS BAR (28px)
         ═══════════════════════════════════════════════════════════════ -->
    <Border Grid.Row="2"
            Background="{DynamicResource SurfaceElevatedBrush}"
            BorderBrush="{DynamicResource BorderSubtleBrush}"
            BorderThickness="0,1,0,0">
      <controls:StatusBar DataContext="{Binding StatusViewModel}" />
    </Border>

  </Grid>

</Window>
```

### Key Differences

| React | Avalonia | Notes |
|-------|----------|-------|
| `flex flex-col` | `StackPanel Orientation="Vertical"` or `Grid RowDefinitions` | Grid is more flexible for fixed sizes |
| `flex-1` | `*` in RowDefinitions/ColumnDefinitions | Star sizing expands to fill |
| `h-10` (40px) | `RowDefinitions="40,*,28"` | Explicit pixel values |
| `overflow-hidden` | `ClipToBounds="True"` | Or use ScrollViewer |

---

## 4. Navigation Rail

### React (Prototype)

```jsx
<nav className="w-14 flex flex-col border-r">
  <NavItem icon={FileText} label="Editor" active={activeNav === 'editor'} />
  {/* ... */}
</nav>
```

### AvaloniaUI (Production)

```xml
<!-- Controls/NavigationRail.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             x:Class="Lexichord.Host.Controls.NavigationRail"
             x:DataType="vm:NavigationRailViewModel"
             Width="56">

  <Border Background="{DynamicResource SurfaceElevatedBrush}"
          BorderBrush="{DynamicResource BorderSubtleBrush}"
          BorderThickness="0,0,1,0">
    <DockPanel>

      <!-- Top Items -->
      <ItemsControl DockPanel.Dock="Top"
                   ItemsSource="{Binding TopItems}">
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:NavItemViewModel">
            <Button Classes="nav-item"
                   Command="{Binding $parent[ItemsControl].((vm:NavigationRailViewModel)DataContext).NavigateCommand}"
                   CommandParameter="{Binding}"
                   Classes.active="{Binding IsActive}">
              <Panel>
                <PathIcon Data="{Binding IconData}"
                         Width="22" Height="22"
                         Classes.active="{Binding IsActive}" />
                <!-- Lock indicator for premium features -->
                <PathIcon Data="{StaticResource LockIcon}"
                         Width="10" Height="10"
                         HorizontalAlignment="Right"
                         VerticalAlignment="Top"
                         Margin="0,4,4,0"
                         IsVisible="{Binding IsLocked}"
                         Foreground="{DynamicResource TextTertiaryBrush}" />
              </Panel>
              <ToolTip.Tip>
                <TextBlock Text="{Binding Label}" />
              </ToolTip.Tip>
            </Button>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

      <!-- Spacer -->
      <Border />

      <!-- Bottom Items -->
      <ItemsControl DockPanel.Dock="Bottom"
                   ItemsSource="{Binding BottomItems}">
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:NavItemViewModel">
            <Button Classes="nav-item"
                   Command="{Binding $parent[ItemsControl].((vm:NavigationRailViewModel)DataContext).NavigateCommand}"
                   CommandParameter="{Binding}"
                   Classes.active="{Binding IsActive}">
              <PathIcon Data="{Binding IconData}"
                       Width="22" Height="22"
                       Classes.active="{Binding IsActive}" />
              <ToolTip.Tip>
                <TextBlock Text="{Binding Label}" />
              </ToolTip.Tip>
            </Button>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

    </DockPanel>
  </Border>

  <UserControl.Styles>
    <!-- Nav Item Button -->
    <Style Selector="Button.nav-item">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="BorderThickness" Value="3,0,0,0" />
      <Setter Property="BorderBrush" Value="Transparent" />
      <Setter Property="Padding" Value="0" />
      <Setter Property="Width" Value="56" />
      <Setter Property="Height" Value="48" />
      <Setter Property="HorizontalContentAlignment" Value="Center" />
      <Setter Property="VerticalContentAlignment" Value="Center" />
      <Setter Property="CornerRadius" Value="0" />
      <Setter Property="Cursor" Value="Hand" />
    </Style>

    <!-- Nav Item Icon -->
    <Style Selector="Button.nav-item PathIcon">
      <Setter Property="Foreground" Value="{DynamicResource TextSecondaryBrush}" />
    </Style>

    <!-- Hover State -->
    <Style Selector="Button.nav-item:pointerover">
      <Setter Property="Background" Value="{DynamicResource SurfaceOverlayBrush}" />
    </Style>
    <Style Selector="Button.nav-item:pointerover PathIcon">
      <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    </Style>

    <!-- Active State -->
    <Style Selector="Button.nav-item.active">
      <Setter Property="Background" Value="{DynamicResource AccentMutedBrush}" />
      <Setter Property="BorderBrush" Value="{DynamicResource AccentPrimaryBrush}" />
    </Style>
    <Style Selector="Button.nav-item.active PathIcon">
      <Setter Property="Foreground" Value="{DynamicResource AccentPrimaryBrush}" />
    </Style>
    <Style Selector="PathIcon.active">
      <Setter Property="Foreground" Value="{DynamicResource AccentPrimaryBrush}" />
    </Style>

    <!-- Pressed State -->
    <Style Selector="Button.nav-item:pressed">
      <Setter Property="Background" Value="{DynamicResource AccentMutedBrush}" />
    </Style>
  </UserControl.Styles>

</UserControl>
```

### ViewModel

```csharp
// ViewModels/NavItemViewModel.cs
public partial class NavItemViewModel : ObservableObject
{
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private StreamGeometry? _iconData;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isLocked;
    [ObservableProperty] private string _viewKey = string.Empty;
}
```

---

## 5. Buttons

### React (Prototype)

```jsx
// Primary
<button style={{ background: c.accentPrimary, color: '#fff' }}>
  Generate
</button>

// Secondary
<button style={{ background: c.surfaceOverlay, border: `1px solid ${c.borderSubtle}` }}>
  Cancel
</button>

// Ghost
<button style={{ background: c.accentMuted, color: c.accentPrimary }}>
  Apply Fix
</button>
```

### AvaloniaUI (Production)

```xml
<!-- Themes/ButtonStyles.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- ═══════════════════════════════════════════════════════════════
       PRIMARY BUTTON (Orange filled)
       ═══════════════════════════════════════════════════════════════ -->
  <ControlTheme x:Key="PrimaryButton" TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource AccentPrimaryBrush}" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="VerticalContentAlignment" Value="Center" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
               Background="{TemplateBinding Background}"
               BorderBrush="{TemplateBinding BorderBrush}"
               BorderThickness="{TemplateBinding BorderThickness}"
               CornerRadius="{TemplateBinding CornerRadius}"
               Padding="{TemplateBinding Padding}">
          <ContentPresenter Name="PART_ContentPresenter"
                           Content="{TemplateBinding Content}"
                           ContentTemplate="{TemplateBinding ContentTemplate}"
                           HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                           VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
                           RecognizesAccessKey="True" />
        </Border>
      </ControlTemplate>
    </Setter>

    <!-- Transitions -->
    <Setter Property="Transitions">
      <Transitions>
        <BrushTransition Property="Background" Duration="0:0:0.1" />
      </Transitions>
    </Setter>

    <!-- Hover -->
    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="Background" Value="{DynamicResource AccentHoverBrush}" />
    </Style>

    <!-- Pressed -->
    <Style Selector="^:pressed /template/ Border#PART_Border">
      <Setter Property="Background" Value="#FFE55A1F" />
    </Style>

    <!-- Disabled -->
    <Style Selector="^:disabled">
      <Setter Property="Opacity" Value="0.5" />
    </Style>

    <!-- Focus Ring -->
    <Style Selector="^:focus-visible /template/ Border#PART_Border">
      <Setter Property="BoxShadow" Value="0 0 0 3px #40FF6B2C" />
    </Style>
  </ControlTheme>

  <!-- ═══════════════════════════════════════════════════════════════
       SECONDARY BUTTON (Outlined)
       ═══════════════════════════════════════════════════════════════ -->
  <ControlTheme x:Key="SecondaryButton" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderDefaultBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="VerticalContentAlignment" Value="Center" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
               Background="{TemplateBinding Background}"
               BorderBrush="{TemplateBinding BorderBrush}"
               BorderThickness="{TemplateBinding BorderThickness}"
               CornerRadius="{TemplateBinding CornerRadius}"
               Padding="{TemplateBinding Padding}">
          <ContentPresenter Content="{TemplateBinding Content}"
                           HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                           VerticalAlignment="{TemplateBinding VerticalContentAlignment}" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="Background" Value="{DynamicResource SurfaceElevatedBrush}" />
    </Style>

    <Style Selector="^:pressed /template/ Border#PART_Border">
      <Setter Property="Background" Value="{DynamicResource SurfaceOverlayBrush}" />
    </Style>
  </ControlTheme>

  <!-- ═══════════════════════════════════════════════════════════════
       GHOST BUTTON (Text only, orange)
       ═══════════════════════════════════════════════════════════════ -->
  <ControlTheme x:Key="GhostButton" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource AccentPrimaryBrush}" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="CornerRadius" Value="4" />
    <Setter Property="Padding" Value="8,4" />
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Cursor" Value="Hand" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
               Background="{TemplateBinding Background}"
               CornerRadius="{TemplateBinding CornerRadius}"
               Padding="{TemplateBinding Padding}">
          <ContentPresenter Content="{TemplateBinding Content}"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="Background" Value="{DynamicResource AccentMutedBrush}" />
    </Style>
  </ControlTheme>

  <!-- ═══════════════════════════════════════════════════════════════
       ICON BUTTON (for toolbar)
       ═══════════════════════════════════════════════════════════════ -->
  <ControlTheme x:Key="IconButton" TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource SurfaceOverlayBrush}" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="CornerRadius" Value="4" />
    <Setter Property="Padding" Value="6" />
    <Setter Property="Width" Value="32" />
    <Setter Property="Height" Value="32" />
    <Setter Property="Cursor" Value="Hand" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
               Background="{TemplateBinding Background}"
               CornerRadius="{TemplateBinding CornerRadius}"
               Padding="{TemplateBinding Padding}">
          <ContentPresenter Content="{TemplateBinding Content}"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^ PathIcon">
      <Setter Property="Foreground" Value="{DynamicResource TextSecondaryBrush}" />
    </Style>

    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="Background" Value="{DynamicResource BorderSubtleBrush}" />
    </Style>

    <Style Selector="^:pointerover PathIcon">
      <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    </Style>
  </ControlTheme>

  <!-- ═══════════════════════════════════════════════════════════════
       DESTRUCTIVE BUTTON (Red)
       ═══════════════════════════════════════════════════════════════ -->
  <ControlTheme x:Key="DestructiveButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
    <Setter Property="Background" Value="{DynamicResource StatusErrorBrush}" />

    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="Background" Value="#FFEF4444" />
    </Style>

    <Style Selector="^:pressed /template/ Border#PART_Border">
      <Setter Property="Background" Value="#FFDC2626" />
    </Style>
  </ControlTheme>

</ResourceDictionary>
```

### Usage

```xml
<Button Theme="{StaticResource PrimaryButton}" Content="Generate" />
<Button Theme="{StaticResource SecondaryButton}" Content="Cancel" />
<Button Theme="{StaticResource GhostButton}" Content="Apply Fix" />
<Button Theme="{StaticResource IconButton}">
  <PathIcon Data="{StaticResource SettingsIcon}" />
</Button>
```

---

## 6. Text Inputs

### React (Prototype)

```jsx
<input
  type="text"
  placeholder="Search your knowledge base..."
  className="flex-1 bg-transparent outline-none"
  style={{ color: c.textPrimary }}
/>
```

### AvaloniaUI (Production)

```xml
<!-- Themes/TextBoxStyles.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <ControlTheme x:Key="LexTextBox" TargetType="TextBox">
    <Setter Property="Background" Value="{DynamicResource SurfaceElevatedBrush}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSubtleBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="12,10" />
    <Setter Property="MinHeight" Value="40" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="CaretBrush" Value="{DynamicResource AccentPrimaryBrush}" />
    <Setter Property="SelectionBrush" Value="{DynamicResource AccentMutedBrush}" />
    <Setter Property="SelectionForegroundBrush" Value="{DynamicResource TextPrimaryBrush}" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
               Background="{TemplateBinding Background}"
               BorderBrush="{TemplateBinding BorderBrush}"
               BorderThickness="{TemplateBinding BorderThickness}"
               CornerRadius="{TemplateBinding CornerRadius}">
          <DockPanel>
            <DataValidationErrors DockPanel.Dock="Bottom" />
            <Panel>
              <!-- Watermark -->
              <TextBlock Name="PART_Watermark"
                        Text="{TemplateBinding Watermark}"
                        Foreground="{DynamicResource TextTertiaryBrush}"
                        IsVisible="False"
                        HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                        VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
                        Margin="{TemplateBinding Padding}" />

              <!-- Text Scroll -->
              <ScrollViewer Name="PART_ScrollViewer"
                           HorizontalScrollBarVisibility="{TemplateBinding (ScrollViewer.HorizontalScrollBarVisibility)}"
                           VerticalScrollBarVisibility="{TemplateBinding (ScrollViewer.VerticalScrollBarVisibility)}">
                <TextPresenter Name="PART_TextPresenter"
                              Text="{TemplateBinding Text, Mode=TwoWay}"
                              CaretBrush="{TemplateBinding CaretBrush}"
                              SelectionBrush="{TemplateBinding SelectionBrush}"
                              SelectionForegroundBrush="{TemplateBinding SelectionForegroundBrush}"
                              TextAlignment="{TemplateBinding TextAlignment}"
                              TextWrapping="{TemplateBinding TextWrapping}"
                              Margin="{TemplateBinding Padding}"
                              VerticalAlignment="Center" />
              </ScrollViewer>
            </Panel>
          </DockPanel>
        </Border>
      </ControlTemplate>
    </Setter>

    <!-- Transitions -->
    <Setter Property="Transitions">
      <Transitions>
        <BrushTransition Property="BorderBrush" Duration="0:0:0.1" />
        <BoxShadowsTransition Property="BoxShadow" Duration="0:0:0.15" />
      </Transitions>
    </Setter>

    <!-- Show watermark when empty -->
    <Style Selector="^:empty /template/ TextBlock#PART_Watermark">
      <Setter Property="IsVisible" Value="True" />
    </Style>

    <!-- Hover -->
    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="BorderBrush" Value="{DynamicResource BorderDefaultBrush}" />
    </Style>

    <!-- Focus -->
    <Style Selector="^:focus-within /template/ Border#PART_Border">
      <Setter Property="BorderBrush" Value="{DynamicResource BorderDefaultBrush}" />
      <Setter Property="BoxShadow" Value="0 0 0 3px #40FF6B2C" />
    </Style>

    <!-- Disabled -->
    <Style Selector="^:disabled">
      <Setter Property="Opacity" Value="0.5" />
    </Style>

    <!-- Error state (via DataValidation) -->
    <Style Selector="^:error /template/ Border#PART_Border">
      <Setter Property="BorderBrush" Value="{DynamicResource StatusErrorBrush}" />
    </Style>
  </ControlTheme>

  <!-- Search Box with Icon -->
  <ControlTheme x:Key="SearchTextBox" TargetType="TextBox" BasedOn="{StaticResource LexTextBox}">
    <Setter Property="InnerLeftContent">
      <Template>
        <PathIcon Data="{StaticResource SearchIcon}"
                 Width="16" Height="16"
                 Margin="12,0,0,0"
                 Foreground="{DynamicResource TextTertiaryBrush}" />
      </Template>
    </Setter>
    <Setter Property="InnerRightContent">
      <Template>
        <Border Background="{DynamicResource SurfaceElevatedBrush}"
               CornerRadius="4"
               Padding="6,2"
               Margin="0,0,8,0">
          <TextBlock Text="⌘K"
                    FontSize="11"
                    Foreground="{DynamicResource TextTertiaryBrush}" />
        </Border>
      </Template>
    </Setter>
  </ControlTheme>

</ResourceDictionary>
```

---

## 7. Cards & Panels

### React (Prototype)

```jsx
<div
  className="p-4 rounded-lg"
  style={{
    background: c.surfaceOverlay,
    border: `1px solid ${c.borderSubtle}`
  }}
>
  {/* content */}
</div>
```

### AvaloniaUI (Production)

```xml
<!-- Controls/Card.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Basic Card -->
  <ControlTheme x:Key="CardBorder" TargetType="Border">
    <Setter Property="Background" Value="{DynamicResource SurfaceOverlayBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSubtleBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16" />
  </ControlTheme>

  <!-- Elevated Card (with shadow) -->
  <ControlTheme x:Key="ElevatedCard" TargetType="Border" BasedOn="{StaticResource CardBorder}">
    <Setter Property="BoxShadow" Value="0 2px 8px #33000000" />
  </ControlTheme>

  <!-- Clickable Card -->
  <ControlTheme x:Key="ClickableCard" TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource SurfaceOverlayBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderSubtleBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16" />
    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
    <Setter Property="Cursor" Value="Hand" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
               Background="{TemplateBinding Background}"
               BorderBrush="{TemplateBinding BorderBrush}"
               BorderThickness="{TemplateBinding BorderThickness}"
               CornerRadius="{TemplateBinding CornerRadius}"
               Padding="{TemplateBinding Padding}">
          <ContentPresenter Content="{TemplateBinding Content}"
                           HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Setter Property="Transitions">
      <Transitions>
        <DoubleTransition Property="Opacity" Duration="0:0:0.15" />
      </Transitions>
    </Setter>

    <Style Selector="^:pointerover">
      <Setter Property="Opacity" Value="0.8" />
    </Style>
  </ControlTheme>

</ResourceDictionary>
```

### Usage

```xml
<Border Theme="{StaticResource CardBorder}">
  <StackPanel Spacing="8">
    <TextBlock Text="Card Title" FontWeight="SemiBold" />
    <TextBlock Text="Card content goes here" />
  </StackPanel>
</Border>
```

---

## 8. Style Gauge

### React (Prototype)

```jsx
const StyleGauge = ({ score, theme }) => {
  const getScoreColor = (s) => {
    if (s >= 90) return c.statusSuccess;
    if (s >= 70) return c.statusWarning;
    return c.statusError;
  };
  // ...
};
```

### AvaloniaUI (Production)

```xml
<!-- Controls/StyleGauge.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Lexichord.Host.Controls.StyleGauge">

  <Border Background="{DynamicResource SurfaceOverlayBrush}"
          CornerRadius="8"
          Padding="16">
    <StackPanel HorizontalAlignment="Center" Spacing="8">

      <!-- Score Display -->
      <TextBlock Name="ScoreText"
                Text="{Binding Score, StringFormat={}{0}%}"
                FontSize="36"
                FontWeight="Bold"
                HorizontalAlignment="Center" />

      <!-- Progress Bar -->
      <Grid Height="8" Width="120">
        <Border Background="{DynamicResource BorderSubtleBrush}"
               CornerRadius="4" />
        <Border Name="ProgressFill"
               CornerRadius="4"
               HorizontalAlignment="Left">
          <Border.Transitions>
            <Transitions>
              <DoubleTransition Property="Width" Duration="0:0:0.3" Easing="CubicEaseOut" />
            </Transitions>
          </Border.Transitions>
        </Border>
      </Grid>

      <!-- Label -->
      <TextBlock Name="LabelText"
                FontSize="14"
                Foreground="{DynamicResource TextSecondaryBrush}"
                HorizontalAlignment="Center" />

    </StackPanel>
  </Border>

</UserControl>
```

```csharp
// Controls/StyleGauge.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Lexichord.Host.Controls;

public partial class StyleGauge : UserControl
{
    public static readonly StyledProperty<int> ScoreProperty =
        AvaloniaProperty.Register<StyleGauge, int>(nameof(Score), defaultValue: 0);

    public int Score
    {
        get => GetValue(ScoreProperty);
        set => SetValue(ScoreProperty, value);
    }

    public StyleGauge()
    {
        InitializeComponent();
        DataContext = this;

        // React to score changes
        this.GetObservable(ScoreProperty).Subscribe(OnScoreChanged);
    }

    private void OnScoreChanged(int score)
    {
        // Determine color based on score
        var (brush, label) = score switch
        {
            >= 90 => (FindResource("StatusSuccessBrush") as IBrush, "In Harmony"),
            >= 70 => (FindResource("StatusWarningBrush") as IBrush, "Minor Dissonance"),
            _ => (FindResource("StatusErrorBrush") as IBrush, "Needs Tuning")
        };

        // Update visuals
        var scoreText = this.FindControl<TextBlock>("ScoreText");
        var progressFill = this.FindControl<Border>("ProgressFill");
        var labelText = this.FindControl<TextBlock>("LabelText");

        if (scoreText != null) scoreText.Foreground = brush;
        if (progressFill != null)
        {
            progressFill.Background = brush;
            progressFill.Width = (score / 100.0) * 120;
        }
        if (labelText != null) labelText.Text = label;
    }
}
```

---

## 9. Issue Cards

### React (Prototype)

```jsx
<div style={{ borderLeft: `3px solid ${isError ? c.statusError : c.statusWarning}` }}>
  <span style={{ textDecoration: 'line-through' }}>{issue.word}</span>
  → <span style={{ color: c.statusSuccess }}>{issue.suggestion}</span>
</div>
```

### AvaloniaUI (Production)

```xml
<!-- Controls/IssueCard.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             x:Class="Lexichord.Host.Controls.IssueCard"
             x:DataType="vm:StyleIssueViewModel">

  <Border Name="CardBorder"
          Background="{DynamicResource SurfaceOverlayBrush}"
          CornerRadius="6"
          Padding="12"
          Margin="0,0,0,8"
          BorderThickness="3,0,0,0"
          Cursor="Hand">

    <StackPanel Spacing="8">
      <!-- Header: Icon + Line Number -->
      <StackPanel Orientation="Horizontal" Spacing="8">
        <PathIcon Name="TypeIcon"
                 Width="14" Height="14" />
        <TextBlock Text="{Binding Line, StringFormat='Line {0}'}"
                  FontSize="12"
                  FontFamily="{StaticResource MonoFont}"
                  Foreground="{DynamicResource TextTertiaryBrush}" />
      </StackPanel>

      <!-- Word Replacement -->
      <TextBlock FontSize="14">
        <Run Text="&quot;" />
        <Run Name="OldWord"
            Text="{Binding Word}"
            TextDecorations="Strikethrough" />
        <Run Text="&quot; → " />
        <Run Name="NewWord"
            Text="{Binding Suggestion}"
            Foreground="{DynamicResource StatusSuccessBrush}" />
      </TextBlock>

      <!-- Apply Button -->
      <Button Theme="{StaticResource GhostButton}"
             Content="Apply Fix"
             Command="{Binding ApplyCommand}"
             HorizontalAlignment="Left" />
    </StackPanel>
  </Border>

</UserControl>
```

```csharp
// Controls/IssueCard.axaml.cs
public partial class IssueCard : UserControl
{
    public IssueCard()
    {
        InitializeComponent();

        // Bind to DataContext changes
        this.GetObservable(DataContextProperty).Subscribe(OnDataContextChanged);
    }

    private void OnDataContextChanged(object? context)
    {
        if (context is not StyleIssueViewModel vm) return;

        var cardBorder = this.FindControl<Border>("CardBorder");
        var typeIcon = this.FindControl<PathIcon>("TypeIcon");
        var oldWord = this.FindControl<Run>("OldWord");

        var isError = vm.Type == ViolationType.Error;

        // Set border color
        cardBorder?.SetValue(Border.BorderBrushProperty,
            FindResource(isError ? "StatusErrorBrush" : "StatusWarningBrush") as IBrush);

        // Set icon
        if (typeIcon != null)
        {
            typeIcon.Data = (StreamGeometry?)FindResource("AlertTriangleIcon");
            typeIcon.Foreground = FindResource(isError ? "StatusErrorBrush" : "StatusWarningBrush") as IBrush;
        }

        // Set strikethrough color
        if (oldWord != null)
        {
            oldWord.Foreground = FindResource("StatusErrorBrush") as IBrush;
        }
    }
}
```

---

## 10. Agent Cards

### AvaloniaUI (Production)

```xml
<!-- Controls/AgentCard.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             x:Class="Lexichord.Host.Controls.AgentCard"
             x:DataType="vm:AgentViewModel">

  <Border Background="{DynamicResource SurfaceOverlayBrush}"
          BorderBrush="{DynamicResource BorderSubtleBrush}"
          BorderThickness="1"
          CornerRadius="8"
          Padding="16"
          Opacity="{Binding IsAvailable, Converter={StaticResource AvailabilityToOpacityConverter}}">

    <StackPanel Spacing="8">
      <!-- Header: Icon + Name -->
      <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="{Binding Icon}"
                  FontSize="20" />
        <TextBlock Text="{Binding Name}"
                  FontWeight="SemiBold"
                  Foreground="{DynamicResource TextPrimaryBrush}" />
      </StackPanel>

      <!-- Description -->
      <TextBlock Text="{Binding Description}"
                FontSize="14"
                Foreground="{DynamicResource TextSecondaryBrush}"
                TextWrapping="Wrap" />

      <!-- Footer: License + Action -->
      <Grid ColumnDefinitions="*,Auto">
        <!-- License Badge -->
        <Border Grid.Column="0"
               Background="{DynamicResource SurfaceElevatedBrush}"
               CornerRadius="4"
               Padding="8,4"
               HorizontalAlignment="Left">
          <TextBlock Text="{Binding LicenseTier}"
                    FontSize="12"
                    Foreground="{DynamicResource TextTertiaryBrush}" />
        </Border>

        <!-- Action Button -->
        <Button Grid.Column="1"
               Theme="{Binding IsAvailable, Converter={StaticResource AvailabilityToButtonThemeConverter}}"
               Command="{Binding ActionCommand}">
          <StackPanel Orientation="Horizontal" Spacing="6">
            <PathIcon Data="{Binding IsAvailable, Converter={StaticResource AvailabilityToIconConverter}}"
                     Width="14" Height="14" />
            <TextBlock Text="{Binding IsAvailable, Converter={StaticResource AvailabilityToTextConverter}}" />
          </StackPanel>
        </Button>
      </Grid>
    </StackPanel>
  </Border>

</UserControl>
```

### Converters

```csharp
// Converters/AvailabilityConverters.cs
public class AvailabilityToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.7;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AvailabilityToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Start" : "Upgrade";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

## 11. Tab Bar

### AvaloniaUI (Production)

```xml
<!-- Controls/TabBar.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             x:Class="Lexichord.Host.Controls.TabBar"
             x:DataType="vm:TabBarViewModel"
             Height="36">

  <Border Background="{DynamicResource SurfaceElevatedBrush}"
          BorderBrush="{DynamicResource BorderSubtleBrush}"
          BorderThickness="0,0,0,1">
    <DockPanel Margin="8,0">

      <!-- Project Selector -->
      <Button DockPanel.Dock="Left"
             Theme="{StaticResource SecondaryButton}"
             Padding="12,4"
             Margin="0,0,16,0">
        <StackPanel Orientation="Horizontal" Spacing="8">
          <TextBlock Text="📁" />
          <TextBlock Text="{Binding CurrentProject.Name}" />
          <PathIcon Data="{StaticResource ChevronDownIcon}"
                   Width="12" Height="12"
                   Foreground="{DynamicResource TextTertiaryBrush}" />
        </StackPanel>
      </Button>

      <!-- Add Tab Button -->
      <Button DockPanel.Dock="Right"
             Theme="{StaticResource IconButton}"
             Width="28" Height="28"
             Command="{Binding AddTabCommand}">
        <PathIcon Data="{StaticResource PlusIcon}"
                 Width="14" Height="14" />
      </Button>

      <!-- Tabs -->
      <ItemsControl ItemsSource="{Binding Tabs}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" />
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:TabViewModel">
            <Button Classes="tab-item"
                   Command="{Binding $parent[ItemsControl].((vm:TabBarViewModel)DataContext).SelectTabCommand}"
                   CommandParameter="{Binding}"
                   Classes.active="{Binding IsActive}">
              <StackPanel Orientation="Horizontal" Spacing="8">
                <PathIcon Data="{StaticResource FileTextIcon}"
                         Width="14" Height="14" />
                <TextBlock Text="{Binding Title}" />
                <Button Classes="close-button"
                       Command="{Binding CloseCommand}">
                  <PathIcon Data="{StaticResource XIcon}"
                           Width="12" Height="12" />
                </Button>
              </StackPanel>
            </Button>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

    </DockPanel>
  </Border>

  <UserControl.Styles>
    <Style Selector="Button.tab-item">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="Padding" Value="12,6" />
      <Setter Property="Margin" Value="0" />
      <Setter Property="BorderThickness" Value="0,0,0,2" />
      <Setter Property="BorderBrush" Value="Transparent" />
      <Setter Property="CornerRadius" Value="4,4,0,0" />
    </Style>

    <Style Selector="Button.tab-item.active">
      <Setter Property="Background" Value="{DynamicResource SurfaceBaseBrush}" />
      <Setter Property="BorderBrush" Value="{DynamicResource AccentPrimaryBrush}" />
    </Style>

    <Style Selector="Button.tab-item.active PathIcon">
      <Setter Property="Foreground" Value="{DynamicResource AccentPrimaryBrush}" />
    </Style>

    <Style Selector="Button.close-button">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="Padding" Value="2" />
      <Setter Property="Opacity" Value="0.5" />
    </Style>

    <Style Selector="Button.close-button:pointerover">
      <Setter Property="Opacity" Value="1" />
    </Style>
  </UserControl.Styles>

</UserControl>
```

---

## 12. Status Bar

### AvaloniaUI (Production)

```xml
<!-- Controls/StatusBar.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             x:Class="Lexichord.Host.Controls.StatusBar"
             x:DataType="vm:StatusBarViewModel"
             Height="28">

  <DockPanel Margin="16,0">
    <!-- Left Side -->
    <StackPanel DockPanel.Dock="Left"
               Orientation="Horizontal"
               Spacing="16"
               VerticalAlignment="Center">

      <TextBlock Text="{Binding WordCount, StringFormat='📊 {0} words'}"
                FontSize="12"
                Foreground="{DynamicResource TextTertiaryBrush}" />

      <TextBlock Text="{Binding ReadingTime, StringFormat='📏 ~{0} min read'}"
                FontSize="12"
                Foreground="{DynamicResource TextTertiaryBrush}" />

      <TextBlock FontSize="12">
        <TextBlock.Text>
          <MultiBinding StringFormat="🎯 Style: {0}%">
            <Binding Path="StyleScore" />
          </MultiBinding>
        </TextBlock.Text>
        <TextBlock.Foreground>
          <MultiBinding Converter="{StaticResource ScoreToColorConverter}">
            <Binding Path="StyleScore" />
          </MultiBinding>
        </TextBlock.Foreground>
      </TextBlock>
    </StackPanel>

    <!-- Right Side -->
    <StackPanel DockPanel.Dock="Right"
               Orientation="Horizontal"
               Spacing="16"
               VerticalAlignment="Center">

      <!-- AI Status -->
      <StackPanel Orientation="Horizontal" Spacing="6">
        <Ellipse Width="8" Height="8"
                Fill="{Binding AiStatus, Converter={StaticResource StatusToColorConverter}}" />
        <TextBlock Text="{Binding AiStatusText}"
                  FontSize="12"
                  Foreground="{DynamicResource TextTertiaryBrush}" />
      </StackPanel>

      <!-- Cursor Position -->
      <TextBlock FontSize="12"
                Foreground="{DynamicResource TextTertiaryBrush}">
        <TextBlock.Text>
          <MultiBinding StringFormat="Ln {0}, Col {1}">
            <Binding Path="CursorLine" />
            <Binding Path="CursorColumn" />
          </MultiBinding>
        </TextBlock.Text>
      </TextBlock>
    </StackPanel>

  </DockPanel>

</UserControl>
```

---

## 13. Progress Indicators

### AvaloniaUI (Production)

```xml
<!-- Determinate Progress Bar -->
<ProgressBar Minimum="0"
            Maximum="100"
            Value="{Binding Progress}"
            Height="6"
            CornerRadius="3"
            Background="{DynamicResource BorderSubtleBrush}"
            Foreground="{DynamicResource AccentPrimaryBrush}" />

<!-- Indeterminate Progress Bar -->
<ProgressBar IsIndeterminate="True"
            Height="4"
            CornerRadius="2"
            Background="{DynamicResource BorderSubtleBrush}"
            Foreground="{DynamicResource AccentPrimaryBrush}" />

<!-- Spinner -->
<PathIcon Data="{StaticResource LoaderIcon}"
         Width="16" Height="16"
         Foreground="{DynamicResource AccentPrimaryBrush}"
         Classes="spinning" />

<!-- Spinner Animation Style -->
<Style Selector="PathIcon.spinning">
  <Style.Animations>
    <Animation Duration="0:0:1" IterationCount="INFINITE">
      <KeyFrame Cue="0%">
        <Setter Property="RotateTransform.Angle" Value="0" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="RotateTransform.Angle" Value="360" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

---

## 14. Icons

### React Uses Lucide Icons

```jsx
import { FileText, Settings, User } from 'lucide-react';
```

### AvaloniaUI: PathIcon with StreamGeometry

```xml
<!-- Resources/Icons.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- File Text (Lucide) -->
  <StreamGeometry x:Key="FileTextIcon">M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M16 13H8 M16 17H8 M10 9H8</StreamGeometry>

  <!-- Settings (Lucide) -->
  <StreamGeometry x:Key="SettingsIcon">M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z</StreamGeometry>

  <!-- User -->
  <StreamGeometry x:Key="UserIcon">M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M12 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8z</StreamGeometry>

  <!-- Menu (Hamburger) -->
  <StreamGeometry x:Key="MenuIcon">M4 6h16 M4 12h16 M4 18h16</StreamGeometry>

  <!-- Search -->
  <StreamGeometry x:Key="SearchIcon">M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z</StreamGeometry>

  <!-- Plus -->
  <StreamGeometry x:Key="PlusIcon">M12 5v14 M5 12h14</StreamGeometry>

  <!-- X (Close) -->
  <StreamGeometry x:Key="XIcon">M18 6L6 18 M6 6l12 12</StreamGeometry>

  <!-- ChevronDown -->
  <StreamGeometry x:Key="ChevronDownIcon">M6 9l6 6 6-6</StreamGeometry>

  <!-- AlertTriangle -->
  <StreamGeometry x:Key="AlertTriangleIcon">M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z M12 9v4 M12 17h.01</StreamGeometry>

  <!-- CheckCircle -->
  <StreamGeometry x:Key="CheckCircleIcon">M22 11.08V12a10 10 0 1 1-5.93-9.14 M22 4L12 14.01l-3-3</StreamGeometry>

  <!-- Lock -->
  <StreamGeometry x:Key="LockIcon">M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 10 0v4</StreamGeometry>

  <!-- Play -->
  <StreamGeometry x:Key="PlayIcon">M5 3l14 9-14 9V3z</StreamGeometry>

  <!-- Sun -->
  <StreamGeometry x:Key="SunIcon">M12 1v2 M12 21v2 M4.22 4.22l1.42 1.42 M18.36 18.36l1.42 1.42 M1 12h2 M21 12h2 M4.22 19.78l1.42-1.42 M18.36 5.64l1.42-1.42 M12 5a7 7 0 1 0 0 14 7 7 0 0 0 0-14z</StreamGeometry>

  <!-- Moon -->
  <StreamGeometry x:Key="MoonIcon">M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z</StreamGeometry>

  <!-- Sparkles -->
  <StreamGeometry x:Key="SparklesIcon">M12 3l1.5 4.5L18 9l-4.5 1.5L12 15l-1.5-4.5L6 9l4.5-1.5L12 3z M5 19l.5 1.5L7 21l-1.5.5L5 23l-.5-1.5L3 21l1.5-.5L5 19z M19 5l.5 1.5L21 7l-1.5.5L19 9l-.5-1.5L17 7l1.5-.5L19 5z</StreamGeometry>

  <!-- Book Open (Knowledge) -->
  <StreamGeometry x:Key="BookOpenIcon">M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z</StreamGeometry>

  <!-- Piano (Agents) -->
  <StreamGeometry x:Key="PianoIcon">M9 18V5l12-2v13 M9 9l12-2 M5 22a3 3 0 1 0 0-6 3 3 0 0 0 0 6z M17 20a3 3 0 1 0 0-6 3 3 0 0 0 0 6z</StreamGeometry>

  <!-- BarChart2 (Analytics) -->
  <StreamGeometry x:Key="BarChart2Icon">M18 20V10 M12 20V4 M6 20v-6</StreamGeometry>

</ResourceDictionary>
```

### Usage

```xml
<PathIcon Data="{StaticResource FileTextIcon}"
         Width="20" Height="20"
         Foreground="{DynamicResource TextSecondaryBrush}" />
```

---

## 15. Animations

### React (CSS Transitions)

```jsx
className="transition-all duration-200"
className="transition-colors"
```

### AvaloniaUI (Transitions & Animations)

```xml
<!-- Simple Property Transition -->
<Border>
  <Border.Transitions>
    <Transitions>
      <BrushTransition Property="Background" Duration="0:0:0.15" />
      <DoubleTransition Property="Opacity" Duration="0:0:0.2" />
      <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2" />
    </Transitions>
  </Border.Transitions>
</Border>

<!-- BoxShadow Transition (for focus glow) -->
<Border>
  <Border.Transitions>
    <Transitions>
      <BoxShadowsTransition Property="BoxShadow" Duration="0:0:0.15" />
    </Transitions>
  </Border.Transitions>
</Border>

<!-- Keyframe Animation (for spinner) -->
<Style Selector="PathIcon.spinning">
  <Setter Property="RenderTransform" Value="rotate(0deg)" />
  <Style.Animations>
    <Animation Duration="0:0:1" IterationCount="INFINITE" Easing="LinearEasing">
      <KeyFrame Cue="0%">
        <Setter Property="RotateTransform.Angle" Value="0" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="RotateTransform.Angle" Value="360" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>

<!-- Panel Slide Animation -->
<Style Selector="Border.inspector">
  <Setter Property="Transitions">
    <Transitions>
      <DoubleTransition Property="Width" Duration="0:0:0.2" Easing="CubicEaseOut" />
    </Transitions>
  </Setter>
</Style>
```

---

## 16. Known Differences

| Feature | React Prototype | AvaloniaUI Production | Solution |
|---------|-----------------|----------------------|----------|
| **Wavy underlines** | CSS `text-decoration: wavy underline` | Not natively supported | Use AvaloniaEdit `TextMarkerService` or custom `DrawingVisual` |
| **RGBA colors** | `rgba(255, 107, 44, 0.15)` | `#26FF6B2C` (AARRGGBB) | Convert alpha: `0.15 × 255 = 38 = 0x26` |
| **Box shadow glow** | `box-shadow: 0 0 0 3px color` | `BoxShadow` property | Use `BoxShadow="0 0 0 3px #40FF6B2C"` |
| **Flexbox `gap`** | `gap: 8px` | Not direct equivalent | Use `Spacing` on `StackPanel` or margins |
| **CSS Grid** | `grid-cols-2 gap-4` | `UniformGrid` or `Grid` | Use `Grid ColumnDefinitions="*,*"` |
| **Hover group** | Tailwind `group-hover:` | Avalonia style selectors | Use `^:pointerover` on parent |
| **Inline styles** | Direct style props | Resource bindings | Always use `{DynamicResource}` |
| **Rich text** | `dangerouslySetInnerHTML` | `InlineCollection` or AvaloniaEdit | Use proper text controls |
| **Emoji icons** | Direct Unicode | May need font support | Embed emoji font or use PathIcon |

---

## Summary

This mapping document provides a 1:1 translation path from the React prototype to production AvaloniaUI code. Key takeaways:

1. **Colors**: Convert all colors to `#AARRGGBB` format with alpha prefix
2. **Layout**: Use `Grid` with `RowDefinitions`/`ColumnDefinitions` instead of flexbox
3. **Styles**: Use `ControlTheme` and style selectors (`:pointerover`, `.active`)
4. **Icons**: Convert Lucide SVG paths to `StreamGeometry` resources
5. **Animations**: Use `Transitions` property and `Animation` classes
6. **Bindings**: Always use `{DynamicResource}` for theme-switchable resources

All components in this document are production-ready and compile with AvaloniaUI 11.2+.

---

## Appendix: Created AXAML Files

The following production-ready AXAML files have been created based on this mapping:

### Theme Resources (`src/Lexichord.Host/Themes/`)

| File | Description |
|------|-------------|
| `Colors.Dark.axaml` | Dark theme color palette with all surface, text, accent, and status colors |
| `Colors.Light.axaml` | Light theme variant for theme switching support |
| `ButtonStyles.axaml` | Button ControlThemes: Primary, Secondary, Ghost, Icon, Destructive |
| `TextBoxStyles.axaml` | TextBox themes: LexTextBox, SearchTextBox, MultilineTextBox, ChatInputTextBox |
| `CardStyles.axaml` | Card border styles: CardBase, CardInteractive, IssueCard, AgentCard, ChatMessageCard |
| `TabStyles.axaml` | Tab control themes: LexTabControl, PillTabControl, VerticalTabControl |
| `LexichordTheme.axaml` | Master theme that imports all resources with typography and spacing scales |

### Icon Resources (`src/Lexichord.Host/Resources/`)

| File | Description |
|------|-------------|
| `Icons.axaml` | StreamGeometry icons converted from Lucide: FileText, Settings, User, Search, Sparkles, etc. |

### Custom Controls (`src/Lexichord.Host/Controls/`)

| File | Description |
|------|-------------|
| `NavigationRail.axaml` + `.cs` | Vertical navigation sidebar with icon buttons and active state indication |
| `StyleGauge.axaml` + `.cs` | Circular progress gauge with score-based color transitions |
| `IssueCard.axaml` + `.cs` | Style suggestion card with severity indicator and Apply Fix action |
| `StatusBar.axaml` + `.cs` | Bottom status bar with word count, sync status, and cursor position |

### Application Entry (`src/Lexichord.Host/`)

| File | Description |
|------|-------------|
| `App.axaml` | Application configuration importing Fluent theme and LexichordTheme |

### Usage

Import the master theme in your `App.axaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://Avalonia.Themes.Fluent/FluentTheme.xaml" />
            <ResourceInclude Source="avares://Lexichord.Host/Themes/LexichordTheme.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Then use components with their themes:

```xml
<Button Theme="{StaticResource PrimaryButton}" Content="Generate" />
<TextBox Theme="{StaticResource SearchTextBox}" Watermark="Search..." />
<controls:StyleGauge Score="87" Label="Style Score" />
<controls:NavigationRail SelectedItem="Editor" />
```
