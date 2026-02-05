// ═══════════════════════════════════════════════════════════════════════════════
// LEXICHORD - Template Preview ViewModel (v0.6.3d Specification)
// ViewModel for the Template Preview Dialog
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for the Template Preview Dialog.
/// </summary>
/// <remarks>
/// SPEC: LCS-DES-v0.6.3d - Context Injection Service
/// Provides data and commands for template preview functionality.
/// </remarks>
public partial class TemplatePreviewViewModel : ObservableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    // Observable Properties
    // ─────────────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private TemplateInfo? _selectedTemplate;

    [ObservableProperty]
    private string _renderedPrompt = string.Empty;

    [ObservableProperty]
    private int _tokenCount;

    [ObservableProperty]
    private bool _isValid = true;

    [ObservableProperty]
    private string _validationStatus = "Valid";

    [ObservableProperty]
    private string _validationIcon = "✅";

    [ObservableProperty]
    private bool _hasValidationMessages;

    public ObservableCollection<TemplateInfo> AvailableTemplates { get; } = [];

    public ObservableCollection<TemplateVariable> Variables { get; } = [];

    public ObservableCollection<ValidationMessage> ValidationMessages { get; } = [];

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public TemplatePreviewViewModel()
    {
        LoadSampleTemplates();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ValidateAsync()
    {
        ValidationMessages.Clear();

        // Simulate validation
        await Task.Delay(200);

        if (SelectedTemplate is null)
        {
            AddValidationMessage("⚠️", "No template selected.", "#F59E0B");
            IsValid = false;
            ValidationStatus = "No Template";
            ValidationIcon = "⚠️";
        }
        else if (Variables.Any(v => v.IsRequired && string.IsNullOrEmpty(v.Value)))
        {
            AddValidationMessage("❌", "Missing required variables.", "#EF4444");
            IsValid = false;
            ValidationStatus = "Invalid";
            ValidationIcon = "❌";
        }
        else
        {
            AddValidationMessage("✅", "Template is valid and ready to use.", "#22C55E");
            IsValid = true;
            ValidationStatus = "Valid";
            ValidationIcon = "✅";
        }

        HasValidationMessages = ValidationMessages.Count > 0;
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(RenderedPrompt);
            }
        }
    }

    [RelayCommand]
    private void OpenDocumentation()
    {
        // TODO: Open documentation URL
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property Change Handlers
    // ─────────────────────────────────────────────────────────────────────────

    partial void OnSelectedTemplateChanged(TemplateInfo? value)
    {
        if (value is null)
        {
            RenderedPrompt = string.Empty;
            TokenCount = 0;
            Variables.Clear();
            return;
        }

        // Load template variables
        Variables.Clear();
        foreach (var variable in value.Variables)
        {
            Variables.Add(variable);
        }

        // Render the template
        RenderTemplate();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private void RenderTemplate()
    {
        if (SelectedTemplate is null)
            return;

        var prompt = SelectedTemplate.Content;

        // Replace variables with their values or placeholders
        foreach (var variable in Variables)
        {
            var placeholder = "{{" + variable.Name + "}}";
            var replacement = !string.IsNullOrEmpty(variable.Value)
                ? variable.Value
                : $"[{variable.Name}]";
            prompt = prompt.Replace(placeholder, replacement);
        }

        RenderedPrompt = prompt;

        // Estimate token count (rough: ~4 chars per token)
        TokenCount = (int)Math.Ceiling(prompt.Length / 4.0);
    }

    private void LoadSampleTemplates()
    {
        AvailableTemplates.Add(new TemplateInfo
        {
            Icon = "✍️",
            Name = "Writing Assistant",
            Content = """
                You are a professional writing assistant for Lexichord.

                {{#style_rules}}
                STYLE GUIDELINES ({{style_rule_count}} rules):
                {{style_rules}}
                {{/style_rules}}

                {{#context}}
                REFERENCE CONTEXT ({{context_source_count}} sources):
                {{context}}
                {{/context}}

                {{#selected_text}}
                The user has selected the following text ({{selection_word_count}} words):
                ---
                {{selected_text}}
                ---
                {{/selected_text}}

                Please help the user improve their writing while following the style guidelines.
                """,
            Variables =
            [
                new() { Name = "style_rules", Description = "Formatted style guidelines", IsRequired = false },
                new() { Name = "style_rule_count", Description = "Number of active rules", IsRequired = false },
                new() { Name = "context", Description = "RAG context chunks", IsRequired = false },
                new() { Name = "context_source_count", Description = "Number of context sources", IsRequired = false },
                new() { Name = "selected_text", Description = "User's selected text", IsRequired = true },
                new() { Name = "selection_word_count", Description = "Word count of selection", IsRequired = false }
            ]
        });

        AvailableTemplates.Add(new TemplateInfo
        {
            Icon = "📝",
            Name = "Summarization",
            Content = """
                You are a document summarization assistant.

                {{#document_name}}
                Document: {{document_name}}
                {{/document_name}}

                Please provide a concise summary of the following text:

                {{selected_text}}

                Keep the summary to {{max_length}} words or fewer.
                """,
            Variables =
            [
                new() { Name = "document_name", Description = "Name of the source document", IsRequired = false },
                new() { Name = "selected_text", Description = "Text to summarize", IsRequired = true },
                new() { Name = "max_length", Description = "Maximum summary length", IsRequired = true, Value = "150" }
            ]
        });

        AvailableTemplates.Add(new TemplateInfo
        {
            Icon = "🎨",
            Name = "Tone Adjustment",
            Content = """
                You are a writing tone specialist.

                Adjust the tone of the following text to be more {{target_tone}}:

                ---
                {{selected_text}}
                ---

                Maintain the original meaning while adjusting the tone.
                Output only the rewritten text without explanations.
                """,
            Variables =
            [
                new() { Name = "selected_text", Description = "Text to adjust", IsRequired = true },
                new() { Name = "target_tone", Description = "Target tone (formal, casual, professional)", IsRequired = true, Value = "professional" }
            ]
        });
    }

    private void AddValidationMessage(string icon, string message, string colorHex)
    {
        ValidationMessages.Add(new ValidationMessage
        {
            Icon = icon,
            Message = message,
            Color = new SolidColorBrush(Color.Parse(colorHex))
        });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Supporting Types
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Information about a prompt template.
/// </summary>
public class TemplateInfo
{
    public string Icon { get; set; } = "📄";
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<TemplateVariable> Variables { get; set; } = [];
}

/// <summary>
/// A variable within a template.
/// </summary>
public partial class TemplateVariable : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    [ObservableProperty]
    private string _value = string.Empty;
}

/// <summary>
/// A validation message for display.
/// </summary>
public class ValidationMessage
{
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IBrush Color { get; set; } = Brushes.White;
}
