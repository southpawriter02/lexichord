using Dock.Model.Controls;
using Dock.Model.Core;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Layout;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

// Alias to distinguish Lexichord's interfaces from Dock's
using ILexichordDocument = Lexichord.Abstractions.Layout.IDocument;
using ILexichordTool = Lexichord.Abstractions.Layout.ITool;

namespace Lexichord.Host.Layout;

/// <summary>
/// Implementation of IRegionManager using Dock.Avalonia.
/// </summary>
/// <remarks>
/// LOGIC: Bridges the abstraction layer (IRegionManager) with the concrete
/// Dock.Avalonia implementation. Handles:
/// - UI thread marshalling via Dispatcher.UIThread
/// - View factory execution with service provider
/// - MediatR event publishing for region changes
/// - Hidden dockable tracking for Show/Hide
///
/// All public methods are thread-safe and can be called from any thread.
/// </remarks>
public sealed class DockRegionManager : IRegionManager
{
    private readonly IDockFactory _dockFactory;
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<DockRegionManager> _logger;

    // LOGIC: Track hidden dockables by ID for Show/Hide functionality
    private readonly Dictionary<string, (IDockable Dockable, IDock Parent)> _hiddenDockables = [];
    private readonly object _hiddenLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DockRegionManager"/> class.
    /// </summary>
    /// <param name="dockFactory">Factory for creating and managing dock layouts.</param>
    /// <param name="mediator">MediatR for publishing notifications.</param>
    /// <param name="serviceProvider">Service provider for view factory execution.</param>
    /// <param name="dispatcher">UI thread dispatcher for thread-safe operations.</param>
    /// <param name="logger">Logger for recording operations.</param>
    public DockRegionManager(
        IDockFactory dockFactory,
        IMediator mediator,
        IServiceProvider serviceProvider,
        IUiDispatcher dispatcher,
        ILogger<DockRegionManager> logger)
    {
        _dockFactory = dockFactory;
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        _dispatcher = dispatcher;
        _logger = logger;

        _logger.LogDebug("DockRegionManager initialized");
    }

    /// <inheritdoc />
    public event EventHandler<RegionChangedEventArgs>? RegionChanged;

    /// <inheritdoc />
    public event EventHandler<RegionNavigationRequestedEventArgs>? NavigationRequested;

    /// <inheritdoc />
    public async Task<ILexichordTool?> RegisterToolAsync(
        ShellRegion region,
        string id,
        string title,
        Func<IServiceProvider, object> viewFactory,
        ToolRegistrationOptions? options = null)
    {
        _logger.LogDebug(
            "Registering tool: {Id} in region {Region}",
            id,
            region);

        options ??= new ToolRegistrationOptions();

        // LOGIC: Validate region is valid for tools
        if (region == ShellRegion.Center || region == ShellRegion.Top)
        {
            _logger.LogWarning(
                "Invalid region {Region} for tool registration. Use Left, Right, or Bottom.",
                region);
            return null;
        }

        var toolDock = _dockFactory.GetToolDock(region);
        if (toolDock is null)
        {
            _logger.LogWarning(
                "Tool dock not found for region {Region}",
                region);
            return null;
        }

        try
        {
            // LOGIC: Execute view factory and create tool on UI thread
            var tool = await _dispatcher.InvokeAsync(() =>
            {
                var content = viewFactory(_serviceProvider);
                var createdTool = _dockFactory.CreateTool(region, id, title, content);

                // Apply options
                if (createdTool is LexichordTool lexTool)
                {
                    lexTool.CanClose = options.CanClose;
                    if (options.MinWidth.HasValue)
                        lexTool.MinWidth = options.MinWidth.Value;
                    if (options.MinHeight.HasValue)
                        lexTool.MinHeight = options.MinHeight.Value;
                }

                // Add to dock
                if (toolDock is IDock dock && dock.VisibleDockables is not null)
                {
                    dock.VisibleDockables.Add((IDockable)createdTool);

                    if (options.ActivateOnRegister)
                    {
                        dock.ActiveDockable = (IDockable)createdTool;
                    }
                }

                return createdTool;
            });

            // LOGIC: Fire events and notifications
            await RaiseRegionChangedAsync(region, id, RegionChangeType.Added);

            _logger.LogInformation(
                "Tool registered: {Id} in region {Region}",
                id,
                region);

            return tool;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to register tool: {Id} in region {Region}",
                id,
                region);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ILexichordDocument?> RegisterDocumentAsync(
        string id,
        string title,
        Func<IServiceProvider, object> viewFactory,
        DocumentRegistrationOptions? options = null)
    {
        _logger.LogDebug("Registering document: {Id}", id);

        options ??= new DocumentRegistrationOptions();

        var documentDock = _dockFactory.DocumentDock;
        if (documentDock is null)
        {
            _logger.LogWarning("Document dock not available");
            return null;
        }

        try
        {
            // LOGIC: Execute view factory and create document on UI thread
            var document = await _dispatcher.InvokeAsync(() =>
            {
                var content = viewFactory(_serviceProvider);
                var createdDoc = _dockFactory.CreateDocument(id, title, content);

                // Apply options
                if (createdDoc is LexichordDocument lexDoc)
                {
                    lexDoc.IsPinned = options.IsPinned;
                }

                // Add to dock
                if (documentDock is IDock dock && dock.VisibleDockables is not null)
                {
                    dock.VisibleDockables.Add((IDockable)createdDoc);

                    if (options.ActivateOnRegister)
                    {
                        dock.ActiveDockable = (IDockable)createdDoc;
                    }
                }

                return createdDoc;
            });

            // LOGIC: Fire events and notifications
            await RaiseRegionChangedAsync(ShellRegion.Center, id, RegionChangeType.Added);

            _logger.LogInformation("Document registered: {Id}", id);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register document: {Id}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> NavigateToAsync(string id)
    {
        _logger.LogDebug("Navigating to dockable: {Id}", id);

        // LOGIC: First, try to find existing dockable
        var existing = _dockFactory.FindDockable(id);

        if (existing is not null)
        {
            // LOGIC: Activate the existing dockable
            await _dispatcher.InvokeAsync(() =>
            {
                if (existing.Owner is IDock parentDock)
                {
                    parentDock.ActiveDockable = existing;
                }
            });

            var region = GetRegionForDockable(existing);
            await RaiseRegionChangedAsync(region, id, RegionChangeType.Activated);

            _logger.LogDebug("Navigated to existing dockable: {Id}", id);
            return true;
        }

        // LOGIC: Check hidden dockables
        bool wasHidden;
        lock (_hiddenLock)
        {
            wasHidden = _hiddenDockables.ContainsKey(id);
        }

        if (wasHidden)
        {
            return await ShowAsync(id);
        }

        // LOGIC: Raise navigation requested event for dynamic creation
        var args = new RegionNavigationRequestedEventArgs(id);
        NavigationRequested?.Invoke(this, args);

        // LOGIC: Publish MediatR notification
        await _mediator.Publish(new RegionNavigationRequestNotification(id));

        if (!args.Handled)
        {
            _logger.LogWarning("Navigation requested for unknown dockable: {Id}", id);
        }

        return args.Handled;
    }

    /// <inheritdoc />
    public async Task<bool> CloseAsync(string id, bool force = false)
    {
        _logger.LogDebug("Closing dockable: {Id}, force: {Force}", id, force);

        var dockable = _dockFactory.FindDockable(id);
        if (dockable is null)
        {
            _logger.LogWarning("Dockable not found for close: {Id}", id);
            return false;
        }

        // LOGIC: Check CanCloseAsync for documents unless force is true
        if (!force && dockable is ILexichordDocument document)
        {
            var canClose = await document.CanCloseAsync();
            if (!canClose)
            {
                _logger.LogDebug("Document {Id} declined close request", id);
                return false;
            }
        }

        var region = GetRegionForDockable(dockable);

        // LOGIC: Remove from parent dock on UI thread
        var removed = await _dispatcher.InvokeAsync(() =>
        {
            if (dockable.Owner is IDock parentDock &&
                parentDock.VisibleDockables is not null)
            {
                return parentDock.VisibleDockables.Remove(dockable);
            }
            return false;
        });

        if (removed)
        {
            // LOGIC: Also remove from hidden if it was there
            lock (_hiddenLock)
            {
                _hiddenDockables.Remove(id);
            }

            await RaiseRegionChangedAsync(region, id, RegionChangeType.Removed);
            _logger.LogInformation("Dockable closed: {Id}", id);
        }

        return removed;
    }

    /// <inheritdoc />
    public async Task<bool> HideAsync(string id)
    {
        _logger.LogDebug("Hiding dockable: {Id}", id);

        var dockable = _dockFactory.FindDockable(id);
        if (dockable is null)
        {
            _logger.LogWarning("Dockable not found for hide: {Id}", id);
            return false;
        }

        var region = GetRegionForDockable(dockable);

        // LOGIC: Remove from visible and store for later restoration
        var hidden = await _dispatcher.InvokeAsync(() =>
        {
            if (dockable.Owner is IDock parentDock &&
                parentDock.VisibleDockables is not null)
            {
                var removed = parentDock.VisibleDockables.Remove(dockable);
                if (removed)
                {
                    lock (_hiddenLock)
                    {
                        _hiddenDockables[id] = (dockable, parentDock);
                    }
                }
                return removed;
            }
            return false;
        });

        if (hidden)
        {
            await RaiseRegionChangedAsync(region, id, RegionChangeType.Removed);
            _logger.LogDebug("Dockable hidden: {Id}", id);
        }

        return hidden;
    }

    /// <inheritdoc />
    public async Task<bool> ShowAsync(string id)
    {
        _logger.LogDebug("Showing dockable: {Id}", id);

        (IDockable Dockable, IDock Parent)? hidden;
        lock (_hiddenLock)
        {
            if (!_hiddenDockables.TryGetValue(id, out var entry))
            {
                _logger.LogWarning("Hidden dockable not found: {Id}", id);
                return false;
            }
            hidden = entry;
            _hiddenDockables.Remove(id);
        }

        var region = GetRegionForDockable(hidden.Value.Dockable);

        // LOGIC: Restore to parent dock on UI thread
        await _dispatcher.InvokeAsync(() =>
        {
            if (hidden.Value.Parent.VisibleDockables is not null)
            {
                hidden.Value.Parent.VisibleDockables.Add(hidden.Value.Dockable);
                hidden.Value.Parent.ActiveDockable = hidden.Value.Dockable;
            }
        });

        await RaiseRegionChangedAsync(region, id, RegionChangeType.Added);
        _logger.LogDebug("Dockable shown: {Id}", id);

        return true;
    }

    /// <inheritdoc />
    public IDockable? GetDockable(string id)
    {
        return _dockFactory.FindDockable(id);
    }

    /// <inheritdoc />
    public IEnumerable<IDockable> GetDockablesInRegion(ShellRegion region)
    {
        if (region == ShellRegion.Center)
        {
            var docDock = _dockFactory.DocumentDock;
            if (docDock is IDock dock && dock.VisibleDockables is not null)
            {
                return dock.VisibleDockables;
            }
        }
        else
        {
            var toolDock = _dockFactory.GetToolDock(region);
            if (toolDock is IDock dock && dock.VisibleDockables is not null)
            {
                return dock.VisibleDockables;
            }
        }

        return [];
    }

    /// <summary>
    /// Raises the RegionChanged event and publishes MediatR notification.
    /// </summary>
    private async Task RaiseRegionChangedAsync(
        ShellRegion region,
        string dockableId,
        RegionChangeType changeType)
    {
        var args = new RegionChangedEventArgs(region, dockableId, changeType);

        try
        {
            RegionChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in RegionChanged event handler for {Id}",
                dockableId);
        }

        try
        {
            await _mediator.Publish(new RegionChangedNotification(region, dockableId, changeType));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception publishing RegionChangedNotification for {Id}",
                dockableId);
        }
    }

    /// <summary>
    /// Determines the region for a dockable based on its parent.
    /// </summary>
    private ShellRegion GetRegionForDockable(IDockable dockable)
    {
        if (dockable.Owner is IDocumentDock)
        {
            return ShellRegion.Center;
        }

        if (dockable is ILexichordTool tool)
        {
            return tool.PreferredRegion;
        }

        // LOGIC: Check parent dock ID for region mapping
        if (dockable.Owner is IDock parentDock)
        {
            return parentDock.Id switch
            {
                "Lexichord.Left" => ShellRegion.Left,
                "Lexichord.Right" => ShellRegion.Right,
                "Lexichord.Bottom" => ShellRegion.Bottom,
                "Lexichord.Documents" => ShellRegion.Center,
                _ => ShellRegion.Center
            };
        }

        return ShellRegion.Center;
    }
}
