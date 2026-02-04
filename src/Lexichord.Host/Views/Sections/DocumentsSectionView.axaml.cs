using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Views.Sections;

/// <summary>
/// Documents section view - main content area for document editing.
/// </summary>
/// <remarks>
/// LOGIC: Provides the container for document content when the Documents
/// section is selected. Coordinates with the EditorModule for actual
/// document editing functionality.
/// </remarks>
public partial class DocumentsSectionView : UserControl
{
    public DocumentsSectionView()
    {
        InitializeComponent();
    }

    private async void OnOpenFileClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Show file picker and open selected file via IEditorService
        if (VisualRoot is not Window window)
            return;

        var storageProvider = window.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Document",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown Files") { Patterns = ["*.md", "*.markdown"] },
                new FilePickerFileType("Text Files") { Patterns = ["*.txt"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count == 0)
            return;

        var filePath = files[0].Path.LocalPath;

        var editorService = App.Services.GetService<IEditorService>();
        if (editorService is not null)
        {
            await editorService.OpenDocumentAsync(filePath);
        }
    }

    private async void OnNewDocumentClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Create a new untitled document via IEditorService
        var editorService = App.Services.GetService<IEditorService>();
        if (editorService is not null)
        {
            await editorService.CreateDocumentAsync();
        }
    }
}
