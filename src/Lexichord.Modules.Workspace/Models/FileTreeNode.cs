namespace Lexichord.Modules.Workspace.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts;
using System.Collections.ObjectModel;

/// <summary>
/// Represents a node in the project explorer tree.
/// </summary>
/// <remarks>
/// LOGIC: FileTreeNode is the recursive data model for the TreeView.
/// Key features:
/// - Lazy loading via placeholder children
/// - Icon mapping based on file extension
/// - Sort key for directories-first ordering
/// - Observable properties for UI binding
///
/// Threading: UI-thread affinity for ObservableCollection operations.
/// </remarks>
public partial class FileTreeNode : ObservableObject
{
    /// <summary>
    /// Sentinel name for loading placeholder nodes.
    /// </summary>
    public const string LoadingPlaceholderName = "__loading__";

    /// <summary>
    /// The file or directory name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The absolute path to the file or directory.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// True if this node represents a directory; false for files.
    /// </summary>
    public required bool IsDirectory { get; init; }

    /// <summary>
    /// The parent node, or null for root nodes.
    /// </summary>
    public FileTreeNode? Parent { get; init; }

    /// <summary>
    /// The child nodes. For directories, initially contains a loading placeholder.
    /// </summary>
    public ObservableCollection<FileTreeNode> Children { get; } = new();

    /// <summary>
    /// Whether this directory node is expanded in the TreeView.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconKind))]
    private bool _isExpanded;

    /// <summary>
    /// Whether this node is currently selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Whether this node is currently being renamed.
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// The edit buffer for rename operations.
    /// </summary>
    [ObservableProperty]
    private string _editName = string.Empty;

    /// <summary>
    /// Whether the children have been loaded from the file system.
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Whether this is a loading placeholder node.
    /// </summary>
    public bool IsPlaceholder => Name == LoadingPlaceholderName;

    /// <summary>
    /// Gets the appropriate icon name for this node.
    /// </summary>
    /// <remarks>
    /// LOGIC: Icon selection priority:
    /// 1. Placeholders show "Loading"
    /// 2. Directories show "Folder" or "FolderOpen" based on expansion state
    /// 3. Files are mapped by extension via GetFileIcon()
    /// </remarks>
    public string IconKind
    {
        get
        {
            if (IsPlaceholder)
                return "Loading";

            if (IsDirectory)
                return IsExpanded ? "FolderOpen" : "Folder";

            return GetFileIcon(Name);
        }
    }

    /// <summary>
    /// Sort key for ordering nodes (directories first, then alphabetical).
    /// </summary>
    /// <remarks>
    /// LOGIC: Prefixes with "0_" for directories and "1_" for files,
    /// ensuring directories sort before files. The suffix is lowercase
    /// for case-insensitive alphabetical ordering.
    /// </remarks>
    public string SortKey => $"{(IsDirectory ? "0" : "1")}_{Name.ToLowerInvariant()}";

    /// <summary>
    /// Gets the depth of this node in the tree.
    /// </summary>
    public int Depth
    {
        get
        {
            var depth = 0;
            var current = Parent;
            while (current != null)
            {
                depth++;
                current = current.Parent;
            }
            return depth;
        }
    }

    /// <summary>
    /// Loads children from the file system asynchronously.
    /// </summary>
    /// <param name="fileSystem">The file system access service.</param>
    /// <remarks>
    /// LOGIC: Called when a directory is expanded for the first time.
    /// 1. Clears the placeholder child
    /// 2. Fetches entries from file system
    /// 3. Creates sorted FileTreeNode children
    /// 4. Directories get their own loading placeholder
    ///
    /// Thread-safety: Must be called on UI thread due to ObservableCollection.
    /// </remarks>
    public async Task LoadChildrenAsync(IFileSystemAccess fileSystem)
    {
        if (!IsDirectory || IsLoaded)
            return;

        Children.Clear();

        var entries = await fileSystem.GetDirectoryContentsAsync(FullPath);

        var sortedEntries = entries
            .OrderBy(e => e.IsDirectory ? 0 : 1)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in sortedEntries)
        {
            var child = new FileTreeNode
            {
                Name = entry.Name,
                FullPath = entry.FullPath,
                IsDirectory = entry.IsDirectory,
                Parent = this
            };

            // Directories get a placeholder to show the expand arrow
            if (entry.IsDirectory)
            {
                child.Children.Add(CreateLoadingPlaceholder());
            }

            Children.Add(child);
        }

        IsLoaded = true;
    }

    /// <summary>
    /// Refreshes the children from the file system.
    /// </summary>
    /// <param name="fileSystem">The file system access service.</param>
    public async Task RefreshAsync(IFileSystemAccess fileSystem)
    {
        IsLoaded = false;
        await LoadChildrenAsync(fileSystem);
    }

    /// <summary>
    /// Finds a node by its full path.
    /// </summary>
    /// <param name="path">The path to search for.</param>
    /// <returns>The matching node, or null if not found.</returns>
    /// <remarks>
    /// LOGIC: Recursively searches this node and all loaded children.
    /// Does not trigger lazy loading - only searches already-loaded nodes.
    /// Path comparison is case-insensitive on Windows, case-sensitive on Unix.
    /// </remarks>
    public FileTreeNode? FindByPath(string path)
    {
        if (FullPath.Equals(path, PathComparison))
            return this;

        foreach (var child in Children)
        {
            if (child.IsPlaceholder)
                continue;

            // Optimization: only recurse if path starts with child's path
            if (path.StartsWith(child.FullPath, PathComparison))
            {
                var found = child.FindByPath(path);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Begins an in-place rename operation.
    /// </summary>
    public void BeginEdit()
    {
        EditName = Name;
        IsEditing = true;
    }

    /// <summary>
    /// Cancels an in-place rename operation.
    /// </summary>
    public void CancelEdit()
    {
        IsEditing = false;
        EditName = string.Empty;
    }

    /// <summary>
    /// Creates a loading placeholder node.
    /// </summary>
    /// <returns>A placeholder FileTreeNode.</returns>
    public static FileTreeNode CreateLoadingPlaceholder()
    {
        return new FileTreeNode
        {
            Name = LoadingPlaceholderName,
            FullPath = string.Empty,
            IsDirectory = false,
            Parent = null
        };
    }

    /// <summary>
    /// Gets the Material icon name for a file based on its extension.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The Material icon name.</returns>
    private static string GetFileIcon(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            // Programming languages
            ".cs" => "LanguageCsharp",
            ".fs" => "LanguageFsharp",
            ".vb" => "LanguageVb",
            ".js" => "LanguageJavascript",
            ".ts" => "LanguageTypescript",
            ".jsx" or ".tsx" => "React",
            ".py" => "LanguagePython",
            ".java" => "LanguageJava",
            ".go" => "LanguageGo",
            ".rs" => "LanguageRust",
            ".c" or ".h" => "LanguageC",
            ".cpp" or ".hpp" => "LanguageCpp",
            ".swift" => "LanguageSwift",
            ".kt" => "LanguageKotlin",
            ".rb" => "LanguageRuby",
            ".php" => "LanguagePhp",
            ".lua" => "LanguageLua",

            // Web
            ".html" or ".htm" => "LanguageHtml5",
            ".css" => "LanguageCss3",
            ".scss" or ".sass" => "Sass",

            // Data formats
            ".json" => "CodeJson",
            ".xml" => "Xml",
            ".yaml" or ".yml" => "FileDocumentOutline",
            ".toml" => "FileDocumentOutline",
            ".csv" => "FileDelimitedOutline",

            // Documentation
            ".md" or ".markdown" => "LanguageMarkdown",
            ".txt" => "FileDocumentOutline",
            ".pdf" => "FilePdfBox",

            // Images
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".ico" or ".svg" or ".webp" =>
                "FileImageOutline",

            // Config / Project
            ".csproj" or ".fsproj" or ".vbproj" => "MicrosoftVisualStudio",
            ".sln" => "MicrosoftVisualStudio",
            ".gitignore" or ".gitattributes" => "Git",
            ".editorconfig" => "CogOutline",
            ".env" => "Key",

            // Scripts
            ".sh" or ".bash" => "Console",
            ".ps1" => "Powershell",
            ".bat" or ".cmd" => "Console",

            // Archives
            ".zip" or ".tar" or ".gz" or ".7z" or ".rar" => "ZipBox",

            // Databases
            ".db" or ".sqlite" or ".sqlite3" => "Database",
            ".sql" => "DatabaseOutline",

            // Lock files
            ".lock" => "Lock",

            // Avalonia / XAML
            ".axaml" or ".xaml" => "Xml",

            // Default
            _ => "FileOutline"
        };
    }

    /// <summary>
    /// Platform-appropriate string comparison for path matching.
    /// </summary>
    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
