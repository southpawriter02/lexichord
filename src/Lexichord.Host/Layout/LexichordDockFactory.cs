using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Layout;
using Microsoft.Extensions.Logging;

// Alias to distinguish Lexichord's interfaces from Dock's
using ILexichordDocument = Lexichord.Abstractions.Layout.IDocument;
using ILexichordTool = Lexichord.Abstractions.Layout.ITool;

namespace Lexichord.Host.Layout;

/// <summary>
/// Factory for creating and managing Lexichord dock layouts.
/// </summary>
/// <remarks>
/// LOGIC: Implements IDockFactory to abstract Dock.Avalonia from modules.
/// Encapsulates all docking library specifics and provides a stable API
/// for layout management.
///
/// Default layout structure:
/// ```
/// ┌─────────────────────────────────────────────────────────┐
/// │                      RootDock                           │
/// │  ┌─────────────────────────────────────────────────────┐│
/// │  │                  ProportionalDock                   ││
/// │  │ ┌────────┬────────────────────────┬───────────────┐ ││
/// │  │ │  Left  │        Center          │     Right     │ ││
/// │  │ │ToolDock│     DocumentDock       │   ToolDock    │ ││
/// │  │ │ (200px)│                        │   (250px)     │ ││
/// │  │ ├────────┴────────────────────────┴───────────────┤ ││
/// │  │ │                    Bottom                       │ ││
/// │  │ │                   ToolDock                      │ ││
/// │  │ │                   (200px)                       │ ││
/// │  │ └─────────────────────────────────────────────────┘ ││
/// │  └─────────────────────────────────────────────────────┘│
/// └─────────────────────────────────────────────────────────┘
/// ```
/// </remarks>
public sealed class LexichordDockFactory : Factory, IDockFactory
{
    private const string RootId = "Lexichord.Root";
    private const string MainId = "Lexichord.Main";
    private const string DocumentsId = "Lexichord.Documents";
    private const string LeftId = "Lexichord.Left";
    private const string RightId = "Lexichord.Right";
    private const string BottomId = "Lexichord.Bottom";

    private readonly ILogger<LexichordDockFactory> _logger;
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private readonly Dictionary<ShellRegion, IToolDock> _toolDocks = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="LexichordDockFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for recording layout operations.</param>
    public LexichordDockFactory(ILogger<LexichordDockFactory> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IRootDock? RootDock => _rootDock;

    /// <inheritdoc />
    public IDocumentDock? DocumentDock => _documentDock;

    /// <inheritdoc />
    public IToolDock? GetToolDock(ShellRegion region)
    {
        return _toolDocks.TryGetValue(region, out var toolDock) ? toolDock : null;
    }

    /// <inheritdoc />
    public IRootDock CreateDefaultLayout()
    {
        _logger.LogInformation("Creating default dock layout");

        // LOGIC: Create tool docks for each region
        var leftToolDock = new ToolDock
        {
            Id = LeftId,
            Title = "Left",
            Proportion = DockRegionConfig.Left.DefaultWidth / 1000, // Approximate proportion
            VisibleDockables = CreateList<IDockable>()
        };

        var rightToolDock = new ToolDock
        {
            Id = RightId,
            Title = "Right",
            Proportion = DockRegionConfig.Right.DefaultWidth / 1000,
            VisibleDockables = CreateList<IDockable>()
        };

        var bottomToolDock = new ToolDock
        {
            Id = BottomId,
            Title = "Bottom",
            Proportion = DockRegionConfig.Bottom.DefaultHeight / 1000,
            VisibleDockables = CreateList<IDockable>()
        };

        // LOGIC: Create main document dock
        _documentDock = new DocumentDock
        {
            Id = DocumentsId,
            Title = "Documents",
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(),
            CanCreateDocument = false // Modules control document creation
        };

        // LOGIC: Cache tool docks for region lookup
        _toolDocks[ShellRegion.Left] = leftToolDock;
        _toolDocks[ShellRegion.Right] = rightToolDock;
        _toolDocks[ShellRegion.Bottom] = bottomToolDock;

        // LOGIC: Create horizontal layout (Left | Center | Right)
        var horizontalDock = new ProportionalDock
        {
            Id = "Lexichord.Horizontal",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                leftToolDock,
                new ProportionalDockSplitter(),
                _documentDock,
                new ProportionalDockSplitter(),
                rightToolDock
            )
        };

        // LOGIC: Create vertical layout (Horizontal | Bottom)
        var mainDock = new ProportionalDock
        {
            Id = MainId,
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(
                horizontalDock,
                new ProportionalDockSplitter(),
                bottomToolDock
            )
        };

        // LOGIC: Create root dock
        _rootDock = new RootDock
        {
            Id = RootId,
            Title = "Lexichord",
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(mainDock),
            ActiveDockable = mainDock,
            DefaultDockable = mainDock
        };

        _logger.LogDebug(
            "Default layout created with root: {RootId}, documents: {DocumentsId}",
            RootId,
            DocumentsId);

        // LOGIC: Initialize the factory context
        _logger.LogInformation("About to call InitLayout (Dock.Mvvm.Factory base method)");
        try
        {
            InitLayout(_rootDock);
            _logger.LogInformation("InitLayout completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitLayout threw exception");
            throw;
        }

        return _rootDock;
    }

    /// <inheritdoc />
    public ILexichordDocument CreateDocument(string id, string title, object content)
    {
        _logger.LogDebug("Creating document: {DocumentId} - {Title}", id, title);

        var document = new LexichordDocument
        {
            Id = id,
            Title = title,
            Context = content
        };

        return document;
    }

    /// <inheritdoc />
    public ILexichordTool CreateTool(ShellRegion region, string id, string title, object content)
    {
        _logger.LogDebug(
            "Creating tool: {ToolId} - {Title} for region {Region}",
            id,
            title,
            region);

        var tool = new LexichordTool
        {
            Id = id,
            Title = title,
            Context = content,
            PreferredRegion = region,
            MinWidth = region switch
            {
                ShellRegion.Left => DockRegionConfig.Left.MinWidth,
                ShellRegion.Right => DockRegionConfig.Right.MinWidth,
                ShellRegion.Bottom => DockRegionConfig.Bottom.MinWidth,
                _ => 150
            },
            MinHeight = region switch
            {
                ShellRegion.Bottom => DockRegionConfig.Bottom.MinHeight,
                _ => 100
            }
        };

        return tool;
    }

    /// <inheritdoc />
    public IDockable? FindDockable(string id)
    {
        if (_rootDock is null)
        {
            _logger.LogWarning("FindDockable called before layout was created");
            return null;
        }

        return FindDockableRecursive(_rootDock, id);
    }

    /// <summary>
    /// Recursively searches for a dockable by ID.
    /// </summary>
    private static IDockable? FindDockableRecursive(IDockable dockable, string id)
    {
        if (dockable.Id == id)
        {
            return dockable;
        }

        if (dockable is IDock dock && dock.VisibleDockables is not null)
        {
            foreach (var child in dock.VisibleDockables)
            {
                var found = FindDockableRecursive(child, id);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }
}
