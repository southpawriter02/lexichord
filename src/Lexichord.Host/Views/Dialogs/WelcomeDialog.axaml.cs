// ═══════════════════════════════════════════════════════════════════════════════
// LEXICHORD - Welcome/Onboarding Dialog (P0 - Critical Missing Component)
// Multi-step onboarding wizard for first-time users
// ═══════════════════════════════════════════════════════════════════════════════

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.Views.Dialogs;

/// <summary>
/// Welcome dialog for first-time users.
/// Guides users through initial setup.
/// </summary>
/// <remarks>
/// SPEC: SCREEN_WIREFRAMES.md Section 3.1 - Welcome/Onboarding Dialog
/// This was identified as a P0 critical missing component.
///
/// Steps:
/// 1. Welcome - Product overview and features
/// 2. Style Guide Selection - Choose preset or custom
/// 3. Ready - Final setup confirmation
/// </remarks>
public partial class WelcomeDialog : Window
{
    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    public static readonly StyledProperty<int> CurrentStepProperty =
        AvaloniaProperty.Register<WelcomeDialog, int>(nameof(CurrentStep), 0);

    public int CurrentStep
    {
        get => GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public static readonly StyledProperty<bool> IsGoogleSelectedProperty =
        AvaloniaProperty.Register<WelcomeDialog, bool>(nameof(IsGoogleSelected), true);

    public bool IsGoogleSelected
    {
        get => GetValue(IsGoogleSelectedProperty);
        set => SetValue(IsGoogleSelectedProperty, value);
    }

    public static readonly StyledProperty<bool> IsMicrosoftSelectedProperty =
        AvaloniaProperty.Register<WelcomeDialog, bool>(nameof(IsMicrosoftSelected), false);

    public bool IsMicrosoftSelected
    {
        get => GetValue(IsMicrosoftSelectedProperty);
        set => SetValue(IsMicrosoftSelectedProperty, value);
    }

    public static readonly StyledProperty<bool> IsCustomSelectedProperty =
        AvaloniaProperty.Register<WelcomeDialog, bool>(nameof(IsCustomSelected), false);

    public bool IsCustomSelected
    {
        get => GetValue(IsCustomSelectedProperty);
        set => SetValue(IsCustomSelectedProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    public bool CanGoBack => CurrentStep > 0;
    public bool IsStep1 => CurrentStep == 0;
    public bool IsStep2 => CurrentStep == 1;
    public bool IsStep3 => CurrentStep == 2;
    public bool Step1Completed => CurrentStep > 0;
    public bool Step2Completed => CurrentStep > 1;

    public string NextButtonText => CurrentStep switch
    {
        0 => "Get Started →",
        1 => "Continue →",
        _ => "Start Writing ✨"
    };

    // Direct properties for binding
    public static readonly DirectProperty<WelcomeDialog, bool> CanGoBackProperty =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, bool>(nameof(CanGoBack), o => o.CanGoBack);

    public static readonly DirectProperty<WelcomeDialog, bool> IsStep1Property =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, bool>(nameof(IsStep1), o => o.IsStep1);

    public static readonly DirectProperty<WelcomeDialog, bool> IsStep2Property =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, bool>(nameof(IsStep2), o => o.IsStep2);

    public static readonly DirectProperty<WelcomeDialog, bool> IsStep3Property =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, bool>(nameof(IsStep3), o => o.IsStep3);

    public static readonly DirectProperty<WelcomeDialog, bool> Step1CompletedProperty =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, bool>(nameof(Step1Completed), o => o.Step1Completed);

    public static readonly DirectProperty<WelcomeDialog, bool> Step2CompletedProperty =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, bool>(nameof(Step2Completed), o => o.Step2Completed);

    public static readonly DirectProperty<WelcomeDialog, string?> NextButtonTextProperty =
        AvaloniaProperty.RegisterDirect<WelcomeDialog, string?>(nameof(NextButtonText), o => o.NextButtonText);

    /// <summary>
    /// The selected style guide preset.
    /// </summary>
    public string SelectedStyleGuide { get; private set; } = "google";

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public WelcomeDialog()
    {
        InitializeComponent();
        DataContext = this;

        // Update computed properties when step changes
        this.GetObservable(CurrentStepProperty).Subscribe(_ => UpdateComputedProperties());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGoogleSelected(object? sender, PointerPressedEventArgs e)
    {
        IsGoogleSelected = true;
        IsMicrosoftSelected = false;
        IsCustomSelected = false;
        SelectedStyleGuide = "google";
    }

    private void OnMicrosoftSelected(object? sender, PointerPressedEventArgs e)
    {
        IsGoogleSelected = false;
        IsMicrosoftSelected = true;
        IsCustomSelected = false;
        SelectedStyleGuide = "microsoft";
    }

    private void OnCustomSelected(object? sender, PointerPressedEventArgs e)
    {
        IsGoogleSelected = false;
        IsMicrosoftSelected = false;
        IsCustomSelected = true;
        SelectedStyleGuide = "custom";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep < 2)
        {
            CurrentStep++;
        }
        else
        {
            // Final step - close dialog and start the app
            Close(true);
        }
    }

    [RelayCommand]
    private void ConfigureAI()
    {
        // TODO: Open AI configuration dialog
        // For now, just close and let user configure via Settings
        Close(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateComputedProperties()
    {
        RaisePropertyChanged(CanGoBackProperty, default, CanGoBack);
        RaisePropertyChanged(IsStep1Property, default, IsStep1);
        RaisePropertyChanged(IsStep2Property, default, IsStep2);
        RaisePropertyChanged(IsStep3Property, default, IsStep3);
        RaisePropertyChanged(Step1CompletedProperty, default, Step1Completed);
        RaisePropertyChanged(Step2CompletedProperty, default, Step2Completed);
        RaisePropertyChanged(NextButtonTextProperty, default, NextButtonText);
    }
}
