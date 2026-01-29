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
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectExplorerViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectExplorerViewModel"/> class.
    /// </summary>
    public ProjectExplorerViewModel(
        IWorkspaceService workspaceService,
        IFileSystemAccess fileSystemAccess,
        IMediator mediator,
        ILogger<ProjectExplorerViewModel> logger)
    {
        _workspaceService = workspaceService;
        _fileSystemAccess = fileSystemAccess;
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
