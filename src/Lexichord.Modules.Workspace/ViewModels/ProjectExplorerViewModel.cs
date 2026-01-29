namespace Lexichord.Modules.Workspace.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Workspace.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// ViewModel for the Project Explorer tool window.
/// </summary>
/// <remarks>
/// LOGIC: Manages the file tree state and handles:
/// - Workspace lifecycle events (opened/closed)
/// - External file change events (created/deleted/renamed)
/// - User commands (refresh, expand all, collapse all, open file)
/// - Context menu actions (new file/folder, rename, delete, reveal in explorer)
///
/// Threading: All public methods dispatch to UI thread as needed.
/// </remarks>
public partial class ProjectExplorerViewModel : ObservableObject,
    INotificationHandler<WorkspaceOpenedEvent>,
    INotificationHandler<WorkspaceClosedEvent>,
    INotificationHandler<ExternalFileChangesEvent>
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IFileSystemAccess _fileSystemAccess;
    private readonly IFileOperationService _fileOperationService;
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectExplorerViewModel> _logger;

    /// <summary>
    /// Tracks a pending new item node during inline editing.
    /// </summary>
    private FileTreeNode? _pendingNewItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectExplorerViewModel"/> class.
    /// </summary>
    public ProjectExplorerViewModel(
        IWorkspaceService workspaceService,
        IFileSystemAccess fileSystemAccess,
        IFileOperationService fileOperationService,
        IMediator mediator,
        ILogger<ProjectExplorerViewModel> logger)
    {
        _workspaceService = workspaceService;
        _fileSystemAccess = fileSystemAccess;
        _fileOperationService = fileOperationService;
        _mediator = mediator;
        _logger = logger;

        // Initialize with current workspace if already open
        if (_workspaceService.IsWorkspaceOpen && _workspaceService.CurrentWorkspace != null)
        {
            _ = LoadWorkspaceAsync(_workspaceService.CurrentWorkspace.RootPath);
        }
    }

    /// <summary>
    /// The root nodes of the file tree.
    /// </summary>
    public ObservableCollection<FileTreeNode> RootNodes { get; } = new();

    /// <summary>
    /// The currently selected node.
    /// </summary>
    [ObservableProperty]
    private FileTreeNode? _selectedNode;

    /// <summary>
    /// Whether the tree is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Status message shown below the tree.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "No folder open";

    /// <summary>
    /// Whether a workspace is currently open.
    /// </summary>
    public bool HasWorkspace => _workspaceService.IsWorkspaceOpen;

    /// <summary>
    /// Loads a workspace into the tree view.
    /// </summary>
    /// <param name="rootPath">The root path to load.</param>
    [RelayCommand]
    public async Task LoadWorkspaceAsync(string rootPath)
    {
        _logger.LogInformation("Loading workspace tree: {RootPath}", rootPath);
        IsLoading = true;
        StatusMessage = "Loading...";

        try
        {
            RootNodes.Clear();

            var rootNode = new FileTreeNode
            {
                Name = Path.GetFileName(rootPath) ?? rootPath,
                FullPath = rootPath,
                IsDirectory = true,
                Parent = null
            };

            // Subscribe to expansion changes for lazy loading
            rootNode.PropertyChanged += OnNodePropertyChanged;

            // Add placeholder for expand arrow
            rootNode.Children.Add(FileTreeNode.CreateLoadingPlaceholder());

            RootNodes.Add(rootNode);

            // Auto-expand root
            rootNode.IsExpanded = true;
            await rootNode.LoadChildrenAsync(_fileSystemAccess);

            // Subscribe to children property changes
            foreach (var child in rootNode.Children)
            {
                child.PropertyChanged += OnNodePropertyChanged;
            }

            var itemCount = CountItems(rootNode);
            StatusMessage = $"{itemCount} items";

            _logger.LogInformation("Workspace tree loaded: {ItemCount} items", itemCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace tree: {RootPath}", rootPath);
            StatusMessage = "Failed to load";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasWorkspace));
        }
    }

    /// <summary>
    /// Refreshes the current workspace tree.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (!HasWorkspace || _workspaceService.CurrentWorkspace == null)
            return;

        _logger.LogDebug("Refreshing workspace tree");
        await LoadWorkspaceAsync(_workspaceService.CurrentWorkspace.RootPath);
    }

    /// <summary>
    /// Expands all nodes in the tree.
    /// </summary>
    [RelayCommand]
    public async Task ExpandAllAsync()
    {
        _logger.LogDebug("Expanding all nodes");
        IsLoading = true;

        try
        {
            foreach (var root in RootNodes)
            {
                await ExpandNodeRecursiveAsync(root);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Collapses all nodes in the tree.
    /// </summary>
    [RelayCommand]
    public void CollapseAll()
    {
        _logger.LogDebug("Collapsing all nodes");

        foreach (var root in RootNodes)
        {
            CollapseNodeRecursive(root);
        }
    }

    /// <summary>
    /// Opens the currently selected file.
    /// </summary>
    [RelayCommand]
    public async Task OpenSelectedFileAsync()
    {
        if (SelectedNode == null || SelectedNode.IsDirectory || SelectedNode.IsPlaceholder)
            return;

        _logger.LogInformation("Opening file: {FilePath}", SelectedNode.FullPath);

        await _mediator.Publish(new FileOpenRequestedEvent(SelectedNode.FullPath));
    }

    #region Context Menu Commands

    /// <summary>
    /// Creates a new file in the target folder.
    /// </summary>
    [RelayCommand]
    public async Task NewFileAsync()
    {
        var targetFolder = GetTargetFolder();
        if (targetFolder == null)
        {
            _logger.LogWarning("NewFileAsync: no target folder available");
            return;
        }

        _logger.LogDebug("NewFileAsync: target folder = {Path}", targetFolder.FullPath);

        // Generate unique name
        var baseName = _fileOperationService.GenerateUniqueName(targetFolder.FullPath, "untitled.md");

        // Create a pending node for inline editing
        var newNode = new FileTreeNode
        {
            Name = baseName,
            FullPath = Path.Combine(targetFolder.FullPath, baseName),
            IsDirectory = false,
            Parent = targetFolder
        };

        // Ensure parent is expanded and loaded
        if (!targetFolder.IsExpanded)
        {
            targetFolder.IsExpanded = true;
        }
        if (!targetFolder.IsLoaded)
        {
            await targetFolder.LoadChildrenAsync(_fileSystemAccess);
        }

        // Insert node and start editing
        InsertNodeSorted(targetFolder.Children, newNode);
        newNode.PropertyChanged += OnNodePropertyChanged;
        _pendingNewItem = newNode;
        newNode.EditName = baseName;
        newNode.BeginEdit();
        SelectedNode = newNode;
    }

    /// <summary>
    /// Creates a new folder in the target folder.
    /// </summary>
    [RelayCommand]
    public async Task NewFolderAsync()
    {
        var targetFolder = GetTargetFolder();
        if (targetFolder == null)
        {
            _logger.LogWarning("NewFolderAsync: no target folder available");
            return;
        }

        _logger.LogDebug("NewFolderAsync: target folder = {Path}", targetFolder.FullPath);

        // Generate unique name
        var baseName = _fileOperationService.GenerateUniqueName(targetFolder.FullPath, "New Folder");

        // Create a pending node for inline editing
        var newNode = new FileTreeNode
        {
            Name = baseName,
            FullPath = Path.Combine(targetFolder.FullPath, baseName),
            IsDirectory = true,
            Parent = targetFolder
        };
        newNode.Children.Add(FileTreeNode.CreateLoadingPlaceholder());

        // Ensure parent is expanded and loaded
        if (!targetFolder.IsExpanded)
        {
            targetFolder.IsExpanded = true;
        }
        if (!targetFolder.IsLoaded)
        {
            await targetFolder.LoadChildrenAsync(_fileSystemAccess);
        }

        // Insert node and start editing
        InsertNodeSorted(targetFolder.Children, newNode);
        newNode.PropertyChanged += OnNodePropertyChanged;
        _pendingNewItem = newNode;
        newNode.EditName = baseName;
        newNode.BeginEdit();
        SelectedNode = newNode;
    }

    /// <summary>
    /// Starts inline rename mode for the selected item.
    /// </summary>
    [RelayCommand]
    public void Rename()
    {
        if (SelectedNode == null || SelectedNode.IsPlaceholder)
        {
            _logger.LogDebug("Rename: no valid selection");
            return;
        }

        if (IsProtectedPath(SelectedNode.FullPath))
        {
            _logger.LogWarning("Rename: cannot rename protected path {Path}", SelectedNode.FullPath);
            StatusMessage = "Cannot rename this item.";
            return;
        }

        _logger.LogDebug("Starting rename for: {Path}", SelectedNode.FullPath);
        SelectedNode.BeginEdit();
    }

    /// <summary>
    /// Deletes the selected item after confirmation.
    /// </summary>
    /// <param name="confirmed">Whether the user has confirmed deletion.</param>
    [RelayCommand]
    public async Task DeleteAsync(bool confirmed = false)
    {
        if (SelectedNode == null || SelectedNode.IsPlaceholder)
        {
            _logger.LogDebug("DeleteAsync: no valid selection");
            return;
        }

        if (IsProtectedPath(SelectedNode.FullPath))
        {
            _logger.LogWarning("DeleteAsync: cannot delete protected path {Path}", SelectedNode.FullPath);
            StatusMessage = "Cannot delete this item.";
            return;
        }

        // In a real implementation, we would show a confirmation dialog first
        // For now, we proceed with deletion (the 'confirmed' parameter is for future dialog integration)
        _logger.LogInformation("Deleting: {Path}", SelectedNode.FullPath);

        var isDirectory = SelectedNode.IsDirectory;
        var hasContents = isDirectory && SelectedNode.Children.Any(c => !c.IsPlaceholder);

        var result = await _fileOperationService.DeleteAsync(SelectedNode.FullPath, recursive: hasContents);

        if (result.Success)
        {
            StatusMessage = $"Deleted {(isDirectory ? "folder" : "file")}.";
            // Tree update handled by FileDeletedEvent
        }
        else
        {
            StatusMessage = result.ErrorMessage ?? "Delete failed.";
            _logger.LogWarning("Delete failed: {Error} - {Message}", result.Error, result.ErrorMessage);
        }
    }

    /// <summary>
    /// Reveals the selected item in the system file explorer.
    /// </summary>
    [RelayCommand]
    public async Task RevealInExplorerAsync()
    {
        if (SelectedNode == null || SelectedNode.IsPlaceholder)
        {
            _logger.LogDebug("RevealInExplorerAsync: no valid selection");
            return;
        }

        _logger.LogDebug("Revealing in explorer: {Path}", SelectedNode.FullPath);
        await _fileOperationService.RevealInExplorerAsync(SelectedNode.FullPath);
    }

    #endregion

    #region Helper Methods for Context Menu

    /// <summary>
    /// Gets the target folder for new item creation.
    /// </summary>
    /// <returns>The folder to create items in, or null if no valid target.</returns>
    /// <remarks>
    /// LOGIC: Returns the selected node if it's a directory, or the
    /// parent of the selected node if it's a file. Falls back to root.
    /// </remarks>
    public FileTreeNode? GetTargetFolder()
    {
        if (SelectedNode == null)
        {
            return RootNodes.FirstOrDefault();
        }

        if (SelectedNode.IsDirectory && !SelectedNode.IsPlaceholder)
        {
            return SelectedNode;
        }

        return SelectedNode.Parent ?? RootNodes.FirstOrDefault();
    }

    /// <summary>
    /// Checks if a path is protected (cannot be modified).
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is protected.</returns>
    /// <remarks>
    /// LOGIC: Protected paths include:
    /// - Workspace root folder
    /// - .git folder and its contents
    /// </remarks>
    public bool IsProtectedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        // Check if this is the workspace root
        if (_workspaceService.CurrentWorkspace != null &&
            path.Equals(_workspaceService.CurrentWorkspace.RootPath, GetPathComparison()))
        {
            return true;
        }

        // Check for .git folder
        var fileName = Path.GetFileName(path);
        if (fileName.Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if inside .git folder
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(s => s.Equals(".git", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Commits a new file or folder from inline edit mode.
    /// </summary>
    /// <param name="node">The node being edited.</param>
    public async Task CommitNewItemAsync(FileTreeNode node)
    {
        if (node != _pendingNewItem)
        {
            _logger.LogWarning("CommitNewItemAsync: node is not the pending new item");
            return;
        }

        var newName = node.EditName?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            _logger.LogDebug("CommitNewItemAsync: empty name, cancelling");
            HandleRenameCancellation(node);
            return;
        }

        // Validate name
        var validation = _fileOperationService.ValidateName(newName);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CommitNewItemAsync: invalid name '{Name}' - {Error}", newName, validation.ErrorMessage);
            StatusMessage = validation.ErrorMessage ?? "Invalid name.";
            HandleRenameCancellation(node);
            return;
        }

        var parentPath = node.Parent?.FullPath ?? _workspaceService.CurrentWorkspace?.RootPath;
        if (string.IsNullOrEmpty(parentPath))
        {
            HandleRenameCancellation(node);
            return;
        }

        FileOperationResult result;
        if (node.IsDirectory)
        {
            result = await _fileOperationService.CreateFolderAsync(parentPath, newName);
        }
        else
        {
            result = await _fileOperationService.CreateFileAsync(parentPath, newName);
        }

        if (result.Success)
        {
            _logger.LogInformation("Created {Type}: {Path}", node.IsDirectory ? "folder" : "file", result.ResultPath);
            node.CancelEdit();
            _pendingNewItem = null;
            // Tree will be updated by the FileCreatedEvent - remove temp node and let event add the real one
            node.Parent?.Children.Remove(node);
        }
        else
        {
            _logger.LogWarning("Create failed: {Error} - {Message}", result.Error, result.ErrorMessage);
            StatusMessage = result.ErrorMessage ?? "Create failed.";
            HandleRenameCancellation(node);
        }
    }

    /// <summary>
    /// Commits a rename operation from inline edit mode.
    /// </summary>
    /// <param name="node">The node being renamed.</param>
    public async Task CommitRenameAsync(FileTreeNode node)
    {
        // Check if this is a new item being created
        if (node == _pendingNewItem)
        {
            await CommitNewItemAsync(node);
            return;
        }

        var newName = node.EditName?.Trim();
        if (string.IsNullOrEmpty(newName) || newName == node.Name)
        {
            _logger.LogDebug("CommitRenameAsync: name unchanged or empty, cancelling");
            node.CancelEdit();
            return;
        }

        // Validate name
        var validation = _fileOperationService.ValidateName(newName);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CommitRenameAsync: invalid name '{Name}' - {Error}", newName, validation.ErrorMessage);
            StatusMessage = validation.ErrorMessage ?? "Invalid name.";
            node.CancelEdit();
            return;
        }

        var result = await _fileOperationService.RenameAsync(node.FullPath, newName);

        if (result.Success)
        {
            _logger.LogInformation("Renamed: {OldPath} -> {NewPath}", node.FullPath, result.ResultPath);
            node.CancelEdit();
            // Tree will be updated by the FileRenamedEvent
        }
        else
        {
            _logger.LogWarning("Rename failed: {Error} - {Message}", result.Error, result.ErrorMessage);
            StatusMessage = result.ErrorMessage ?? "Rename failed.";
            node.CancelEdit();
        }
    }

    /// <summary>
    /// Handles cancellation of a new item creation.
    /// </summary>
    /// <param name="node">The pending node to remove.</param>
    public void HandleRenameCancellation(FileTreeNode node)
    {
        node.CancelEdit();

        if (node == _pendingNewItem)
        {
            _logger.LogDebug("Cancelling new item creation");
            node.PropertyChanged -= OnNodePropertyChanged;
            node.Parent?.Children.Remove(node);
            _pendingNewItem = null;
        }
    }

    #endregion

    /// <inheritdoc />
    public async Task Handle(WorkspaceOpenedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling WorkspaceOpenedEvent: {RootPath}", notification.WorkspaceRootPath);
        await LoadWorkspaceAsync(notification.WorkspaceRootPath);
    }

    /// <inheritdoc />
    public Task Handle(WorkspaceClosedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling WorkspaceClosedEvent: {RootPath}", notification.WorkspaceRootPath);

        RootNodes.Clear();
        SelectedNode = null;
        StatusMessage = "No folder open";
        OnPropertyChanged(nameof(HasWorkspace));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(ExternalFileChangesEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling ExternalFileChangesEvent: {Count} changes", notification.Changes.Count);

        foreach (var change in notification.Changes)
        {
            try
            {
                ProcessExternalChange(change);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process external change: {ChangeType} {Path}",
                    change.ChangeType, change.FullPath);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles property changes on tree nodes (for lazy loading).
    /// </summary>
    private async void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FileTreeNode node)
            return;

        // Lazy load children when a directory is expanded
        if (e.PropertyName == nameof(FileTreeNode.IsExpanded) && node.IsExpanded && !node.IsLoaded)
        {
            _logger.LogDebug("Lazy loading children for: {Path}", node.FullPath);

            try
            {
                await node.LoadChildrenAsync(_fileSystemAccess);

                // Subscribe to new children
                foreach (var child in node.Children)
                {
                    child.PropertyChanged += OnNodePropertyChanged;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to lazy load: {Path}", node.FullPath);
            }
        }
    }

    /// <summary>
    /// Recursively expands a node and all its children.
    /// </summary>
    private async Task ExpandNodeRecursiveAsync(FileTreeNode node)
    {
        if (!node.IsDirectory)
            return;

        node.IsExpanded = true;

        if (!node.IsLoaded)
        {
            await node.LoadChildrenAsync(_fileSystemAccess);

            foreach (var child in node.Children)
            {
                child.PropertyChanged += OnNodePropertyChanged;
            }
        }

        foreach (var child in node.Children.Where(c => c.IsDirectory && !c.IsPlaceholder))
        {
            await ExpandNodeRecursiveAsync(child);
        }
    }

    /// <summary>
    /// Recursively collapses a node and all its children.
    /// </summary>
    private void CollapseNodeRecursive(FileTreeNode node)
    {
        node.IsExpanded = false;

        foreach (var child in node.Children.Where(c => c.IsDirectory && !c.IsPlaceholder))
        {
            CollapseNodeRecursive(child);
        }
    }

    /// <summary>
    /// Processes an external file system change.
    /// </summary>
    private void ProcessExternalChange(FileSystemChangeInfo change)
    {
        switch (change.ChangeType)
        {
            case FileSystemChangeType.Created:
                HandleFileCreated(change);
                break;

            case FileSystemChangeType.Deleted:
                HandleFileDeleted(change);
                break;

            case FileSystemChangeType.Renamed:
                HandleFileRenamed(change);
                break;

            case FileSystemChangeType.Changed:
                // Content changes don't affect tree structure
                _logger.LogTrace("Ignoring content change: {Path}", change.FullPath);
                break;
        }
    }

    /// <summary>
    /// Handles a file/directory creation.
    /// </summary>
    private void HandleFileCreated(FileSystemChangeInfo change)
    {
        var parentPath = Path.GetDirectoryName(change.FullPath);
        if (string.IsNullOrEmpty(parentPath))
            return;

        // Find parent node
        FileTreeNode? parent = null;
        foreach (var root in RootNodes)
        {
            parent = root.FindByPath(parentPath);
            if (parent != null)
                break;
        }

        // Only add if parent is loaded (avoid adding to unloaded directories)
        if (parent == null || !parent.IsLoaded)
        {
            _logger.LogTrace("Created file parent not loaded, skipping: {Path}", change.FullPath);
            return;
        }

        // Check if already exists
        var existing = parent.Children.FirstOrDefault(c =>
            c.FullPath.Equals(change.FullPath, GetPathComparison()));

        if (existing != null)
        {
            _logger.LogTrace("Created file already exists in tree: {Path}", change.FullPath);
            return;
        }

        var newNode = CreateNodeFromChange(change, parent);
        InsertNodeSorted(parent.Children, newNode);
        newNode.PropertyChanged += OnNodePropertyChanged;

        _logger.LogDebug("Added node for created file: {Path}", change.FullPath);
    }

    /// <summary>
    /// Handles a file/directory deletion.
    /// </summary>
    private void HandleFileDeleted(FileSystemChangeInfo change)
    {
        FileTreeNode? nodeToRemove = null;
        FileTreeNode? parent = null;

        foreach (var root in RootNodes)
        {
            nodeToRemove = root.FindByPath(change.FullPath);
            if (nodeToRemove != null)
            {
                parent = nodeToRemove.Parent;
                break;
            }
        }

        if (nodeToRemove == null)
        {
            _logger.LogTrace("Deleted file not in tree: {Path}", change.FullPath);
            return;
        }

        // Clear selection if deleting selected node
        if (SelectedNode != null && (SelectedNode == nodeToRemove ||
            SelectedNode.FullPath.StartsWith(change.FullPath, GetPathComparison())))
        {
            SelectedNode = null;
        }

        // Unsubscribe from events
        nodeToRemove.PropertyChanged -= OnNodePropertyChanged;

        // Remove from parent or root
        if (parent != null)
        {
            parent.Children.Remove(nodeToRemove);
        }
        else
        {
            RootNodes.Remove(nodeToRemove);
        }

        _logger.LogDebug("Removed node for deleted file: {Path}", change.FullPath);
    }

    /// <summary>
    /// Handles a file/directory rename.
    /// </summary>
    private void HandleFileRenamed(FileSystemChangeInfo change)
    {
        if (string.IsNullOrEmpty(change.OldPath))
        {
            _logger.LogWarning("Rename event missing OldPath: {NewPath}", change.FullPath);
            return;
        }

        // Remove old node
        HandleFileDeleted(new FileSystemChangeInfo(
            FileSystemChangeType.Deleted, change.OldPath, null, _fileSystemAccess.IsDirectory(change.FullPath)));

        // Add new node
        HandleFileCreated(change);

        _logger.LogDebug("Processed rename: {OldPath} -> {NewPath}", change.OldPath, change.FullPath);
    }

    /// <summary>
    /// Creates a tree node from a file system change.
    /// </summary>
    private FileTreeNode CreateNodeFromChange(FileSystemChangeInfo change, FileTreeNode? parent)
    {
        var isDirectory = _fileSystemAccess.IsDirectory(change.FullPath);

        var node = new FileTreeNode
        {
            Name = Path.GetFileName(change.FullPath) ?? change.FullPath,
            FullPath = change.FullPath,
            IsDirectory = isDirectory,
            Parent = parent
        };

        if (isDirectory)
        {
            node.Children.Add(FileTreeNode.CreateLoadingPlaceholder());
        }

        return node;
    }

    /// <summary>
    /// Inserts a node into a collection in sorted order.
    /// </summary>
    private static void InsertNodeSorted(ObservableCollection<FileTreeNode> collection, FileTreeNode node)
    {
        var index = 0;
        while (index < collection.Count &&
               string.Compare(collection[index].SortKey, node.SortKey, StringComparison.Ordinal) < 0)
        {
            index++;
        }

        collection.Insert(index, node);
    }

    /// <summary>
    /// Counts all items under a node recursively.
    /// </summary>
    private static int CountItems(FileTreeNode node)
    {
        var count = 1; // Count this node

        foreach (var child in node.Children.Where(c => !c.IsPlaceholder))
        {
            count += CountItems(child);
        }

        return count;
    }

    /// <summary>
    /// Gets the appropriate string comparison for the current platform.
    /// </summary>
    private static StringComparison GetPathComparison() =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
