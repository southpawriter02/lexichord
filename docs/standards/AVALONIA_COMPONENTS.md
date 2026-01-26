# Lexichord AvaloniaUI Component Library

**Version:** 1.0.0
**Last Updated:** 2026-01-26
**Framework:** AvaloniaUI 11.x / .NET 9

---

## 1. Architecture Overview

### 1.1 MVVM Structure

```
src/Lexichord.Host/
├── App.axaml                    # Application resources
├── App.axaml.cs                 # Application startup
├── Themes/
│   ├── Lexichord.Dark.axaml     # Dark theme resources
│   ├── Lexichord.Light.axaml    # Light theme resources
│   └── Shared.axaml             # Shared styles & templates
├── Controls/
│   ├── LexButton.axaml          # Custom button control
│   ├── LexTextBox.axaml         # Custom text input
│   ├── StyleGauge.axaml         # Style score visualization
│   └── NavigationRail.axaml     # Side navigation
├── Views/
│   ├── MainWindow.axaml         # Application shell
│   ├── EditorView.axaml         # Document editor
│   ├── KnowledgeView.axaml      # RAG interface
│   └── AgentsView.axaml         # Agent management
└── ViewModels/
    ├── MainWindowViewModel.cs
    ├── EditorViewModel.cs
    ├── KnowledgeViewModel.cs
    └── AgentsViewModel.cs
```

### 1.2 Dependencies

```xml
<!-- Lexichord.Host.csproj -->
<ItemGroup>
  <PackageReference Include="Avalonia" Version="11.2.*" />
  <PackageReference Include="Avalonia.Desktop" Version="11.2.*" />
  <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.*" />
  <PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.*" />
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
  <PackageReference Include="Avalonia.Xaml.Behaviors" Version="11.*" />
  <PackageReference Include="AvaloniaEdit" Version="11.*" />
</ItemGroup>
```

---

## 2. Theme System

### 2.1 Color Resources

```xml
<!-- Themes/Lexichord.Dark.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Surface Colors -->
  <Color x:Key="Surface.Base">#0D0D0F</Color>
  <Color x:Key="Surface.Elevated">#16161A</Color>
  <Color x:Key="Surface.Overlay">#1E1E24</Color>
  <Color x:Key="Surface.Editor">#121215</Color>

  <!-- Border Colors -->
  <Color x:Key="Border.Subtle">#2A2A32</Color>
  <Color x:Key="Border.Default">#3D3D47</Color>

  <!-- Text Colors -->
  <Color x:Key="Text.Primary">#EAEAEC</Color>
  <Color x:Key="Text.Secondary">#A0A0A8</Color>
  <Color x:Key="Text.Tertiary">#6B6B75</Color>

  <!-- Accent Colors -->
  <Color x:Key="Accent.Primary">#FF6B2C</Color>
  <Color x:Key="Accent.Hover">#FF8A57</Color>
  <Color x:Key="Accent.Muted">#FF6B2C26</Color>
  <Color x:Key="Accent.Glow">#FF6B2C40</Color>

  <!-- Semantic Colors -->
  <Color x:Key="Status.Success">#34D399</Color>
  <Color x:Key="Status.Warning">#FBBF24</Color>
  <Color x:Key="Status.Error">#F87171</Color>
  <Color x:Key="Status.Info">#60A5FA</Color>

  <!-- Brushes -->
  <SolidColorBrush x:Key="Brush.Surface.Base" Color="{StaticResource Surface.Base}" />
  <SolidColorBrush x:Key="Brush.Surface.Elevated" Color="{StaticResource Surface.Elevated}" />
  <SolidColorBrush x:Key="Brush.Surface.Overlay" Color="{StaticResource Surface.Overlay}" />
  <SolidColorBrush x:Key="Brush.Surface.Editor" Color="{StaticResource Surface.Editor}" />

  <SolidColorBrush x:Key="Brush.Border.Subtle" Color="{StaticResource Border.Subtle}" />
  <SolidColorBrush x:Key="Brush.Border.Default" Color="{StaticResource Border.Default}" />

  <SolidColorBrush x:Key="Brush.Text.Primary" Color="{StaticResource Text.Primary}" />
  <SolidColorBrush x:Key="Brush.Text.Secondary" Color="{StaticResource Text.Secondary}" />
  <SolidColorBrush x:Key="Brush.Text.Tertiary" Color="{StaticResource Text.Tertiary}" />

  <SolidColorBrush x:Key="Brush.Accent.Primary" Color="{StaticResource Accent.Primary}" />
  <SolidColorBrush x:Key="Brush.Accent.Hover" Color="{StaticResource Accent.Hover}" />
  <SolidColorBrush x:Key="Brush.Accent.Muted" Color="{StaticResource Accent.Muted}" />

  <SolidColorBrush x:Key="Brush.Status.Success" Color="{StaticResource Status.Success}" />
  <SolidColorBrush x:Key="Brush.Status.Warning" Color="{StaticResource Status.Warning}" />
  <SolidColorBrush x:Key="Brush.Status.Error" Color="{StaticResource Status.Error}" />
  <SolidColorBrush x:Key="Brush.Status.Info" Color="{StaticResource Status.Info}" />

</ResourceDictionary>
```

### 2.2 Theme Service

```csharp
// Services/ThemeService.cs
public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    void SetTheme(ThemeMode mode);
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

public enum ThemeMode { Dark, Light, System }

public class ThemeService(Application app) : IThemeService
{
    private ThemeMode _currentTheme = ThemeMode.Dark;

    public ThemeMode CurrentTheme => _currentTheme;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public void SetTheme(ThemeMode mode)
    {
        _currentTheme = mode;

        var actualTheme = mode == ThemeMode.System
            ? GetSystemTheme()
            : mode;

        var uri = actualTheme == ThemeMode.Dark
            ? new Uri("avares://Lexichord.Host/Themes/Lexichord.Dark.axaml")
            : new Uri("avares://Lexichord.Host/Themes/Lexichord.Light.axaml");

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(
            (ResourceDictionary)AvaloniaXamlLoader.Load(uri));

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(actualTheme));
    }

    private static ThemeMode GetSystemTheme()
    {
        // Platform-specific implementation
        return ThemeMode.Dark;
    }
}

public record ThemeChangedEventArgs(ThemeMode NewTheme);
```

---

## 3. Core Components

### 3.1 LexButton

```xml
<!-- Controls/LexButton.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Button Styles -->
  <ControlTheme x:Key="LexButtonPrimary" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource Brush.Accent.Primary}" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Template">
      <ControlTemplate>
        <Border Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="{TemplateBinding BorderThickness}"
                CornerRadius="{TemplateBinding CornerRadius}"
                Padding="{TemplateBinding Padding}">
          <ContentPresenter Content="{TemplateBinding Content}"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
        </Border>
      </ControlTemplate>
    </Setter>

    <!-- Hover State -->
    <Style Selector="^:pointerover">
      <Setter Property="Background" Value="{StaticResource Brush.Accent.Hover}" />
    </Style>

    <!-- Pressed State -->
    <Style Selector="^:pressed">
      <Setter Property="Background" Value="#E55A1F" />
    </Style>

    <!-- Disabled State -->
    <Style Selector="^:disabled">
      <Setter Property="Opacity" Value="0.5" />
    </Style>

    <!-- Focus State -->
    <Style Selector="^:focus-visible">
      <Setter Property="BoxShadow" Value="0 0 0 3px #FF6B2C40" />
    </Style>
  </ControlTheme>

  <ControlTheme x:Key="LexButtonSecondary" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{StaticResource Brush.Text.Primary}" />
    <Setter Property="BorderBrush" Value="{StaticResource Brush.Border.Default}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Template">
      <ControlTemplate>
        <Border Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="{TemplateBinding BorderThickness}"
                CornerRadius="{TemplateBinding CornerRadius}"
                Padding="{TemplateBinding Padding}">
          <ContentPresenter Content="{TemplateBinding Content}"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:pointerover">
      <Setter Property="Background" Value="{StaticResource Brush.Surface.Elevated}" />
    </Style>
  </ControlTheme>

  <ControlTheme x:Key="LexButtonGhost" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{StaticResource Brush.Accent.Primary}" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Padding" Value="8,4" />
    <Setter Property="Cursor" Value="Hand" />

    <Style Selector="^:pointerover">
      <Setter Property="Background" Value="{StaticResource Brush.Accent.Muted}" />
    </Style>
  </ControlTheme>

</ResourceDictionary>
```

### 3.2 LexTextBox

```xml
<!-- Controls/LexTextBox.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <ControlTheme x:Key="LexTextBox" TargetType="TextBox">
    <Setter Property="Background" Value="{StaticResource Brush.Surface.Elevated}" />
    <Setter Property="Foreground" Value="{StaticResource Brush.Text.Primary}" />
    <Setter Property="BorderBrush" Value="{StaticResource Brush.Border.Subtle}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="12,10" />
    <Setter Property="MinHeight" Value="40" />
    <Setter Property="CaretBrush" Value="{StaticResource Brush.Accent.Primary}" />
    <Setter Property="SelectionBrush" Value="{StaticResource Brush.Accent.Muted}" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
                Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="{TemplateBinding BorderThickness}"
                CornerRadius="{TemplateBinding CornerRadius}">
          <DockPanel>
            <!-- Watermark -->
            <TextBlock Name="PART_Watermark"
                      Text="{TemplateBinding Watermark}"
                      Foreground="{StaticResource Brush.Text.Tertiary}"
                      IsVisible="{Binding Text.Length, RelativeSource={RelativeSource TemplatedParent}, Converter={x:Static StringConverters.IsNullOrEmpty}}"
                      Margin="{TemplateBinding Padding}"
                      VerticalAlignment="Center" />

            <!-- Text Presenter -->
            <ScrollViewer Name="PART_ScrollViewer"
                         HorizontalScrollBarVisibility="{TemplateBinding (ScrollViewer.HorizontalScrollBarVisibility)}"
                         VerticalScrollBarVisibility="{TemplateBinding (ScrollViewer.VerticalScrollBarVisibility)}">
              <TextPresenter Name="PART_TextPresenter"
                            Text="{TemplateBinding Text, Mode=TwoWay}"
                            CaretBrush="{TemplateBinding CaretBrush}"
                            SelectionBrush="{TemplateBinding SelectionBrush}"
                            TextAlignment="{TemplateBinding TextAlignment}"
                            TextWrapping="{TemplateBinding TextWrapping}"
                            Margin="{TemplateBinding Padding}" />
            </ScrollViewer>
          </DockPanel>
        </Border>
      </ControlTemplate>
    </Setter>

    <!-- Hover -->
    <Style Selector="^:pointerover /template/ Border#PART_Border">
      <Setter Property="BorderBrush" Value="{StaticResource Brush.Border.Default}" />
    </Style>

    <!-- Focus -->
    <Style Selector="^:focus-within /template/ Border#PART_Border">
      <Setter Property="BorderBrush" Value="{StaticResource Brush.Border.Default}" />
      <Setter Property="BoxShadow" Value="0 0 0 3px #FF6B2C40" />
    </Style>

    <!-- Error -->
    <Style Selector="^.error /template/ Border#PART_Border">
      <Setter Property="BorderBrush" Value="{StaticResource Brush.Status.Error}" />
    </Style>

    <!-- Disabled -->
    <Style Selector="^:disabled">
      <Setter Property="Opacity" Value="0.5" />
    </Style>
  </ControlTheme>

</ResourceDictionary>
```

### 3.3 Navigation Rail

```xml
<!-- Controls/NavigationRail.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Host.ViewModels"
             x:Class="Lexichord.Host.Controls.NavigationRail"
             Width="56">

  <Border Background="{StaticResource Brush.Surface.Elevated}"
          BorderBrush="{StaticResource Brush.Border.Subtle}"
          BorderThickness="0,0,1,0">
    <DockPanel>
      <!-- Top Navigation Items -->
      <ItemsControl DockPanel.Dock="Top"
                   ItemsSource="{Binding TopNavItems}">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Button Classes="nav-item"
                   Command="{Binding NavigateCommand}"
                   Classes.active="{Binding IsActive}">
              <StackPanel>
                <PathIcon Data="{Binding IconPath}"
                         Width="22" Height="22" />
              </StackPanel>
              <ToolTip.Tip>
                <TextBlock Text="{Binding Label}" />
              </ToolTip.Tip>
            </Button>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

      <!-- Spacer -->
      <Border />

      <!-- Bottom Navigation Items -->
      <ItemsControl DockPanel.Dock="Bottom"
                   ItemsSource="{Binding BottomNavItems}">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Button Classes="nav-item"
                   Command="{Binding NavigateCommand}"
                   Classes.active="{Binding IsActive}">
              <PathIcon Data="{Binding IconPath}"
                       Width="22" Height="22" />
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
    <Style Selector="Button.nav-item">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="BorderThickness" Value="3,0,0,0" />
      <Setter Property="BorderBrush" Value="Transparent" />
      <Setter Property="Padding" Value="0" />
      <Setter Property="Width" Value="56" />
      <Setter Property="Height" Value="48" />
      <Setter Property="HorizontalContentAlignment" Value="Center" />
      <Setter Property="VerticalContentAlignment" Value="Center" />
    </Style>

    <Style Selector="Button.nav-item PathIcon">
      <Setter Property="Foreground" Value="{StaticResource Brush.Text.Secondary}" />
    </Style>

    <Style Selector="Button.nav-item:pointerover">
      <Setter Property="Background" Value="{StaticResource Brush.Surface.Overlay}" />
    </Style>

    <Style Selector="Button.nav-item:pointerover PathIcon">
      <Setter Property="Foreground" Value="{StaticResource Brush.Text.Primary}" />
    </Style>

    <Style Selector="Button.nav-item.active">
      <Setter Property="Background" Value="{StaticResource Brush.Accent.Muted}" />
      <Setter Property="BorderBrush" Value="{StaticResource Brush.Accent.Primary}" />
    </Style>

    <Style Selector="Button.nav-item.active PathIcon">
      <Setter Property="Foreground" Value="{StaticResource Brush.Accent.Primary}" />
    </Style>
  </UserControl.Styles>

</UserControl>
```

### 3.4 Style Gauge

```xml
<!-- Controls/StyleGauge.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Lexichord.Host.Controls.StyleGauge">

  <Border Background="{StaticResource Brush.Surface.Overlay}"
          CornerRadius="8"
          Padding="16">
    <StackPanel HorizontalAlignment="Center">
      <!-- Score Display -->
      <TextBlock Name="ScoreText"
                FontSize="36"
                FontWeight="Bold"
                HorizontalAlignment="Center"
                Margin="0,0,0,8">
        <TextBlock.Text>
          <MultiBinding StringFormat="{}{0}%">
            <Binding Path="Score" />
          </MultiBinding>
        </TextBlock.Text>
      </TextBlock>

      <!-- Progress Bar -->
      <Border Height="8"
              Width="120"
              CornerRadius="4"
              Background="{StaticResource Brush.Border.Subtle}">
        <Border Name="ProgressFill"
                HorizontalAlignment="Left"
                CornerRadius="4">
          <Border.Width>
            <MultiBinding>
              <Binding Path="Score" />
              <Binding ElementName="ProgressFill" Path="Bounds.Width" />
            </MultiBinding>
          </Border.Width>
        </Border>
      </Border>

      <!-- Label -->
      <TextBlock Name="LabelText"
                FontSize="14"
                Foreground="{StaticResource Brush.Text.Secondary}"
                HorizontalAlignment="Center"
                Margin="0,8,0,0" />
    </StackPanel>
  </Border>

</UserControl>
```

```csharp
// Controls/StyleGauge.axaml.cs
public partial class StyleGauge : UserControl
{
    public static readonly StyledProperty<int> ScoreProperty =
        AvaloniaProperty.Register<StyleGauge, int>(nameof(Score), 0);

    public int Score
    {
        get => GetValue(ScoreProperty);
        set => SetValue(ScoreProperty, value);
    }

    public StyleGauge()
    {
        InitializeComponent();
        this.GetObservable(ScoreProperty).Subscribe(UpdateVisuals);
    }

    private void UpdateVisuals(int score)
    {
        var (color, label) = score switch
        {
            >= 90 => (Brushes.Find("Brush.Status.Success"), "In Harmony"),
            >= 70 => (Brushes.Find("Brush.Status.Warning"), "Minor Dissonance"),
            _ => (Brushes.Find("Brush.Status.Error"), "Needs Tuning")
        };

        ScoreText.Foreground = color;
        ProgressFill.Background = color;
        LabelText.Text = label;
    }
}
```

---

## 4. View Models

### 4.1 Base ViewModel

```csharp
// ViewModels/ViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    protected void ClearError() => ErrorMessage = null;

    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        try
        {
            IsLoading = true;
            ClearError();
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 4.2 Editor ViewModel

```csharp
// ViewModels/EditorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class EditorViewModel : ViewModelBase
{
    private readonly IStyleEngine _styleEngine;
    private readonly ILogger<EditorViewModel> _logger;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private int _styleScore;

    [ObservableProperty]
    private ObservableCollection<StyleViolation> _violations = [];

    [ObservableProperty]
    private string _documentTitle = "Untitled";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _cursorLine = 1;

    [ObservableProperty]
    private int _cursorColumn = 1;

    public EditorViewModel(
        IStyleEngine styleEngine,
        ILogger<EditorViewModel> logger)
    {
        _styleEngine = styleEngine;
        _logger = logger;
    }

    partial void OnContentChanged(string value)
    {
        HasUnsavedChanges = true;
        WordCount = CountWords(value);
        AnalyzeContentDebounced();
    }

    private readonly Timer _debounceTimer = new(500);

    private void AnalyzeContentDebounced()
    {
        _debounceTimer.Dispose();
        _debounceTimer = new Timer(_ => AnalyzeContentAsync().ConfigureAwait(false), null, 500, Timeout.Infinite);
    }

    [RelayCommand]
    private async Task AnalyzeContentAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _styleEngine.AnalyzeAsync(new Document(Content));
            StyleScore = result.Score;
            Violations = new ObservableCollection<StyleViolation>(result.Violations);

            _logger.LogInformation(
                "Analysis complete: Score={Score}, Violations={Count}",
                result.Score,
                result.Violations.Count);
        });
    }

    [RelayCommand]
    private void ApplyFix(StyleViolation violation)
    {
        Content = Content.Replace(violation.Word, violation.Suggestion);
        Violations.Remove(violation);

        _logger.LogInformation(
            "Applied fix: {Word} → {Suggestion}",
            violation.Word,
            violation.Suggestion);
    }

    [RelayCommand]
    private void ApplyAllFixes()
    {
        foreach (var violation in Violations.ToList())
        {
            ApplyFix(violation);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Save implementation
        HasUnsavedChanges = false;
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
```

### 4.3 Main Window ViewModel

```csharp
// ViewModels/MainWindowViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private NavigationItem _selectedNavItem;

    [ObservableProperty]
    private ObservableCollection<NavigationItem> _topNavItems;

    [ObservableProperty]
    private ObservableCollection<NavigationItem> _bottomNavItems;

    [ObservableProperty]
    private ThemeMode _currentTheme = ThemeMode.Dark;

    [ObservableProperty]
    private bool _isInspectorVisible = true;

    [ObservableProperty]
    private UserProfile _currentUser;

    public MainWindowViewModel(
        IThemeService themeService,
        INavigationService navigationService,
        EditorViewModel editorViewModel)
    {
        _themeService = themeService;
        _navigationService = navigationService;

        // Initialize navigation
        TopNavItems =
        [
            new NavigationItem("Editor", "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z", editorViewModel),
            new NavigationItem("Knowledge", "M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253", null),
            new NavigationItem("Agents", "M9 19V6l12-3v13", null),
            new NavigationItem("Analytics", "M18 20V10m-6 10V4M6 20v-6", null),
        ];

        BottomNavItems =
        [
            new NavigationItem("Settings", "M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8zM2 12a10 10 0 1 1 20 0 10 10 0 0 1-20 0z", null),
            new NavigationItem("Profile", "M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2", null),
        ];

        CurrentView = editorViewModel;
        SelectedNavItem = TopNavItems[0];
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        if (item.ViewModel is not null)
        {
            CurrentView = item.ViewModel;
            SelectedNavItem = item;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        _themeService.SetTheme(CurrentTheme);
    }

    [RelayCommand]
    private void ToggleInspector()
    {
        IsInspectorVisible = !IsInspectorVisible;
    }
}

public record NavigationItem(string Label, string IconPath, ViewModelBase? ViewModel)
{
    public bool IsActive { get; set; }
}
```

---

## 5. Main Window Layout

```xml
<!-- Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Lexichord.Host.ViewModels"
        xmlns:controls="using:Lexichord.Host.Controls"
        x:Class="Lexichord.Host.Views.MainWindow"
        Title="Lexichord"
        Width="1400" Height="900"
        MinWidth="800" MinHeight="600"
        Background="{StaticResource Brush.Surface.Base}"
        ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaChromeHints="NoChrome"
        x:DataType="vm:MainWindowViewModel">

  <Grid RowDefinitions="40,*,28">
    <!-- Title Bar -->
    <Border Grid.Row="0"
            Background="{StaticResource Brush.Surface.Elevated}"
            BorderBrush="{StaticResource Brush.Border.Subtle}"
            BorderThickness="0,0,0,1">
      <DockPanel Margin="8,0">
        <!-- Left: Logo & Title -->
        <StackPanel DockPanel.Dock="Left"
                   Orientation="Horizontal"
                   Spacing="12"
                   VerticalAlignment="Center">
          <Button Classes="icon-button">
            <PathIcon Data="M4 6h16M4 12h16M4 18h16" Width="18" Height="18" />
          </Button>
          <TextBlock Text="LEXICHORD"
                    FontWeight="SemiBold"
                    Foreground="{StaticResource Brush.Accent.Primary}"
                    VerticalAlignment="Center" />
          <Border Background="{StaticResource Brush.Accent.Muted}"
                  CornerRadius="4"
                  Padding="6,2">
            <TextBlock Text="PROTOTYPE"
                      FontSize="10"
                      Foreground="{StaticResource Brush.Accent.Primary}" />
          </Border>
        </StackPanel>

        <!-- Right: Controls -->
        <StackPanel DockPanel.Dock="Right"
                   Orientation="Horizontal"
                   Spacing="8"
                   VerticalAlignment="Center">
          <Button Classes="icon-button"
                 Command="{Binding ToggleThemeCommand}">
            <PathIcon Data="{Binding CurrentTheme, Converter={StaticResource ThemeToIconConverter}}"
                     Width="16" Height="16" />
          </Button>
          <Button Classes="icon-button">
            <PathIcon Data="M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z" Width="18" Height="18" />
          </Button>
          <Button Classes="user-menu">
            <StackPanel Orientation="Horizontal" Spacing="8">
              <PathIcon Data="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"
                       Width="16" Height="16" />
              <TextBlock Text="{Binding CurrentUser.Name}"
                        Foreground="{StaticResource Brush.Text.Secondary}" />
              <PathIcon Data="M6 9l6 6 6-6" Width="12" Height="12" />
            </StackPanel>
          </Button>
        </StackPanel>

        <!-- Drag Region -->
        <Border IsHitTestVisible="True"
               Name="TitleBarDragRegion" />
      </DockPanel>
    </Border>

    <!-- Main Content -->
    <Grid Grid.Row="1" ColumnDefinitions="56,*">
      <!-- Navigation Rail -->
      <controls:NavigationRail Grid.Column="0"
                              DataContext="{Binding}" />

      <!-- Content Area -->
      <ContentControl Grid.Column="1"
                     Content="{Binding CurrentView}" />
    </Grid>

    <!-- Status Bar -->
    <Border Grid.Row="2"
            Background="{StaticResource Brush.Surface.Elevated}"
            BorderBrush="{StaticResource Brush.Border.Subtle}"
            BorderThickness="0,1,0,0">
      <DockPanel Margin="16,0">
        <!-- Left Status -->
        <StackPanel DockPanel.Dock="Left"
                   Orientation="Horizontal"
                   Spacing="16"
                   VerticalAlignment="Center">
          <TextBlock Text="{Binding CurrentView.WordCount, StringFormat='📊 {0} words'}"
                    FontSize="12"
                    Foreground="{StaticResource Brush.Text.Tertiary}" />
          <TextBlock Text="📏 ~2 min read"
                    FontSize="12"
                    Foreground="{StaticResource Brush.Text.Tertiary}" />
          <TextBlock FontSize="12">
            <TextBlock.Text>
              <MultiBinding StringFormat="🎯 Style: {0}%">
                <Binding Path="CurrentView.StyleScore" />
              </MultiBinding>
            </TextBlock.Text>
          </TextBlock>
        </StackPanel>

        <!-- Right Status -->
        <StackPanel DockPanel.Dock="Right"
                   Orientation="Horizontal"
                   Spacing="16"
                   VerticalAlignment="Center">
          <StackPanel Orientation="Horizontal" Spacing="4">
            <Ellipse Width="8" Height="8"
                    Fill="{StaticResource Brush.Status.Success}" />
            <TextBlock Text="AI: Ready"
                      FontSize="12"
                      Foreground="{StaticResource Brush.Text.Tertiary}" />
          </StackPanel>
          <TextBlock FontSize="12"
                    Foreground="{StaticResource Brush.Text.Tertiary}">
            <TextBlock.Text>
              <MultiBinding StringFormat="Ln {0}, Col {1}">
                <Binding Path="CurrentView.CursorLine" />
                <Binding Path="CurrentView.CursorColumn" />
              </MultiBinding>
            </TextBlock.Text>
          </TextBlock>
        </StackPanel>
      </DockPanel>
    </Border>
  </Grid>

  <Window.Styles>
    <Style Selector="Button.icon-button">
      <Setter Property="Background" Value="{StaticResource Brush.Surface.Overlay}" />
      <Setter Property="BorderThickness" Value="0" />
      <Setter Property="CornerRadius" Value="4" />
      <Setter Property="Padding" Value="6" />
      <Setter Property="Cursor" Value="Hand" />
    </Style>

    <Style Selector="Button.icon-button:pointerover">
      <Setter Property="Background" Value="{StaticResource Brush.Border.Subtle}" />
    </Style>

    <Style Selector="Button.icon-button PathIcon">
      <Setter Property="Foreground" Value="{StaticResource Brush.Text.Secondary}" />
    </Style>

    <Style Selector="Button.user-menu">
      <Setter Property="Background" Value="{StaticResource Brush.Surface.Overlay}" />
      <Setter Property="BorderThickness" Value="0" />
      <Setter Property="CornerRadius" Value="4" />
      <Setter Property="Padding" Value="8,4" />
      <Setter Property="Cursor" Value="Hand" />
    </Style>
  </Window.Styles>

</Window>
```

---

## 6. Accessibility (A11y)

### 6.1 Automation Properties

```xml
<!-- Always set automation properties for interactive elements -->
<Button Content="Generate"
       AutomationProperties.Name="Generate Release Notes"
       AutomationProperties.HelpText="Analyzes git history and generates changelog" />

<TextBox Watermark="Search..."
        AutomationProperties.Name="Search knowledge base"
        AutomationProperties.LabeledBy="{Binding ElementName=SearchLabel}" />

<controls:StyleGauge Score="{Binding StyleScore}"
                    AutomationProperties.Name="Style compliance score"
                    AutomationProperties.LiveSetting="Polite" />
```

### 6.2 Keyboard Navigation

```xml
<!-- Ensure logical tab order -->
<StackPanel KeyboardNavigation.TabNavigation="Local">
  <TextBox TabIndex="0" />
  <Button TabIndex="1" Content="Submit" />
  <Button TabIndex="2" Content="Cancel" />
</StackPanel>

<!-- Focus management -->
<TextBox Name="SearchBox"
        KeyDown="OnSearchKeyDown" />
```

```csharp
private void OnSearchKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Escape)
    {
        SearchBox.Clear();
        FocusManager.Instance?.Focus(MainContent);
    }
}
```

---

## 7. Performance Guidelines

### 7.1 Virtualization

```xml
<!-- Use virtualization for long lists -->
<ListBox ItemsSource="{Binding Violations}"
        VirtualizationMode="Simple">
  <ListBox.ItemsPanel>
    <ItemsPanelTemplate>
      <VirtualizingStackPanel />
    </ItemsPanelTemplate>
  </ListBox.ItemsPanel>
</ListBox>
```

### 7.2 Lazy Loading

```csharp
// Defer heavy operations
public class KnowledgeViewModel : ViewModelBase
{
    private bool _isInitialized;

    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await LoadSourcesAsync();
        _isInitialized = true;
    }
}
```

### 7.3 Compiled Bindings

```xml
<!-- Enable compiled bindings for performance -->
<UserControl xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:DataType="vm:EditorViewModel"
             x:CompileBindings="True">
  <!-- Bindings are now compile-time checked -->
  <TextBlock Text="{Binding DocumentTitle}" />
</UserControl>
```

---

## Appendix: Component Checklist

When creating a new component:

- [ ] Follows naming convention (`Lex{Name}.axaml`)
- [ ] Uses semantic color tokens (not hardcoded colors)
- [ ] Supports both Dark and Light themes
- [ ] Has proper focus states
- [ ] Has hover/pressed states
- [ ] Has disabled state
- [ ] Sets `AutomationProperties.Name`
- [ ] Supports keyboard navigation
- [ ] Uses compiled bindings where possible
- [ ] Has XML documentation comments
- [ ] Has corresponding ViewModel (if needed)
- [ ] Has unit tests for ViewModel
