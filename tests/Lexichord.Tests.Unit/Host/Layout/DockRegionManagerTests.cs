using Dock.Model.Controls;
using Dock.Model.Core;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Layout;
using Lexichord.Host.Layout;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

// Alias to distinguish Lexichord's interfaces from Dock's
using ILexichordDocument = Lexichord.Abstractions.Layout.IDocument;
using ILexichordTool = Lexichord.Abstractions.Layout.ITool;

namespace Lexichord.Tests.Unit.Host.Layout;

/// <summary>
/// Unit tests for <see cref="DockRegionManager"/>.
/// </summary>
public class DockRegionManagerTests
{
    private readonly Mock<IDockFactory> _mockFactory;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly IUiDispatcher _dispatcher;
    private readonly Mock<ILogger<DockRegionManager>> _mockLogger;
    private readonly DockRegionManager _sut;

    public DockRegionManagerTests()
    {
        _mockFactory = new Mock<IDockFactory>();
        _mockMediator = new Mock<IMediator>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _dispatcher = new SynchronousDispatcher();
        _mockLogger = new Mock<ILogger<DockRegionManager>>();

        _sut = new DockRegionManager(
            _mockFactory.Object,
            _mockMediator.Object,
            _mockServiceProvider.Object,
            _dispatcher,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterToolAsync_AddsTool_ToCorrectRegion()
    {
        // Arrange
        var mockTool = new Mock<ILexichordTool>();
        mockTool.Setup(t => t.Id).Returns("test-tool");

        var mockToolDock = new Mock<IToolDock>();
        var dockables = new ObservableCollection<IDockable>();
        mockToolDock.As<IDock>().Setup(d => d.VisibleDockables).Returns(dockables);

        _mockFactory.Setup(f => f.CreateTool(
            ShellRegion.Left, "test-tool", "Test Tool", It.IsAny<object>()))
            .Returns(mockTool.Object);
        _mockFactory.Setup(f => f.GetToolDock(ShellRegion.Left)).Returns(mockToolDock.Object);

        // Act
        var result = await _sut.RegisterToolAsync(
            ShellRegion.Left,
            "test-tool",
            "Test Tool",
            sp => new object());

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-tool");
        dockables.Should().ContainSingle();
    }

    [Fact]
    public async Task RegisterToolAsync_SetsActiveDockable_WhenOptionEnabled()
    {
        // Arrange
        var mockTool = new Mock<ILexichordTool>();
        var mockToolDock = new Mock<IToolDock>();
        var dockables = new ObservableCollection<IDockable>();
        IDockable? activeDockable = null;

        mockToolDock.As<IDock>().Setup(d => d.VisibleDockables).Returns(dockables);
        mockToolDock.As<IDock>().SetupSet(d => d.ActiveDockable = It.IsAny<IDockable>())
            .Callback<IDockable>(d => activeDockable = d);

        _mockFactory.Setup(f => f.CreateTool(
            It.IsAny<ShellRegion>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(mockTool.Object);
        _mockFactory.Setup(f => f.GetToolDock(It.IsAny<ShellRegion>())).Returns(mockToolDock.Object);

        // Act
        await _sut.RegisterToolAsync(
            ShellRegion.Left,
            "active-tool",
            "Active Tool",
            sp => new object(),
            new ToolRegistrationOptions(ActivateOnRegister: true));

        // Assert
        activeDockable.Should().Be(mockTool.Object);
    }

    [Fact]
    public async Task RegisterToolAsync_ReturnsNull_ForInvalidRegion()
    {
        // Act
        var result = await _sut.RegisterToolAsync(
            ShellRegion.Center, // Invalid for tools
            "invalid-tool",
            "Invalid Tool",
            sp => new object());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterDocumentAsync_AddsDocument_ToDocumentDock()
    {
        // Arrange
        var mockDocument = new Mock<ILexichordDocument>();
        mockDocument.Setup(d => d.Id).Returns("test-doc");

        var mockDocDock = new Mock<IDocumentDock>();
        var dockables = new ObservableCollection<IDockable>();
        mockDocDock.As<IDock>().Setup(d => d.VisibleDockables).Returns(dockables);

        _mockFactory.Setup(f => f.CreateDocument("test-doc", "Test Document", It.IsAny<object>()))
            .Returns(mockDocument.Object);
        _mockFactory.Setup(f => f.DocumentDock).Returns(mockDocDock.Object);

        // Act
        var result = await _sut.RegisterDocumentAsync(
            "test-doc",
            "Test Document",
            sp => new object());

        // Assert
        result.Should().NotBeNull();
        dockables.Should().ContainSingle();
    }

    [Fact]
    public async Task NavigateToAsync_ActivatesExistingDockable()
    {
        // Arrange
        var mockDockable = new Mock<IDockable>();
        mockDockable.Setup(d => d.Id).Returns("existing");

        var mockParent = new Mock<IDock>();
        IDockable? activeDockable = null;
        mockParent.SetupSet(d => d.ActiveDockable = It.IsAny<IDockable>())
            .Callback<IDockable>(d => activeDockable = d);
        mockDockable.Setup(d => d.Owner).Returns(mockParent.Object);

        _mockFactory.Setup(f => f.FindDockable("existing")).Returns(mockDockable.Object);

        // Act
        var result = await _sut.NavigateToAsync("existing");

        // Assert
        result.Should().BeTrue();
        activeDockable.Should().Be(mockDockable.Object);
    }

    [Fact]
    public async Task NavigateToAsync_RaisesEvent_WhenNotFound()
    {
        // Arrange
        _mockFactory.Setup(f => f.FindDockable("unknown")).Returns((IDockable?)null);

        RegionNavigationRequestedEventArgs? receivedArgs = null;
        _sut.NavigationRequested += (_, args) => receivedArgs = args;

        // Act
        var result = await _sut.NavigateToAsync("unknown");

        // Assert
        result.Should().BeFalse();
        receivedArgs.Should().NotBeNull();
        receivedArgs!.RequestedId.Should().Be("unknown");
    }

    [Fact]
    public async Task CloseAsync_RemovesDockable()
    {
        // Arrange
        var mockDockable = new Mock<IDockable>();
        mockDockable.Setup(d => d.Id).Returns("closeme");

        var dockables = new ObservableCollection<IDockable> { mockDockable.Object };
        var mockParent = new Mock<IDock>();
        mockParent.Setup(d => d.VisibleDockables).Returns(dockables);
        mockDockable.Setup(d => d.Owner).Returns(mockParent.Object);

        _mockFactory.Setup(f => f.FindDockable("closeme")).Returns(mockDockable.Object);

        // Act
        var result = await _sut.CloseAsync("closeme");

        // Assert
        result.Should().BeTrue();
        dockables.Should().BeEmpty();
    }

    [Fact]
    public async Task CloseAsync_RespectsCanClose_WhenNotForced()
    {
        // Arrange
        var mockDocument = new Mock<ILexichordDocument>();
        mockDocument.Setup(d => d.Id).Returns("dirty-doc");
        mockDocument.Setup(d => d.CanCloseAsync()).ReturnsAsync(false);

        _mockFactory.Setup(f => f.FindDockable("dirty-doc")).Returns(mockDocument.Object);

        // Act
        var result = await _sut.CloseAsync("dirty-doc", force: false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HideAsync_RemovesFromVisible_AndTracksForShow()
    {
        // Arrange
        var mockDockable = new Mock<IDockable>();
        mockDockable.Setup(d => d.Id).Returns("hideme");

        var dockables = new ObservableCollection<IDockable> { mockDockable.Object };
        var mockParent = new Mock<IDock>();
        mockParent.Setup(d => d.VisibleDockables).Returns(dockables);
        mockDockable.Setup(d => d.Owner).Returns(mockParent.Object);

        _mockFactory.Setup(f => f.FindDockable("hideme")).Returns(mockDockable.Object);

        // Act
        var result = await _sut.HideAsync("hideme");

        // Assert
        result.Should().BeTrue();
        dockables.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsync_RestoresHiddenDockable_ToParent()
    {
        // Arrange - First hide the dockable
        var mockDockable = new Mock<IDockable>();
        mockDockable.Setup(d => d.Id).Returns("showme");

        var dockables = new ObservableCollection<IDockable> { mockDockable.Object };
        var mockParent = new Mock<IDock>();
        mockParent.Setup(d => d.VisibleDockables).Returns(dockables);
        mockDockable.Setup(d => d.Owner).Returns(mockParent.Object);

        _mockFactory.Setup(f => f.FindDockable("showme")).Returns(mockDockable.Object);
        await _sut.HideAsync("showme");

        // Act
        var result = await _sut.ShowAsync("showme");

        // Assert
        result.Should().BeTrue();
        dockables.Should().ContainSingle();
    }

    [Fact]
    public async Task RegionChanged_Event_IsFiredOnRegister()
    {
        // Arrange
        var mockTool = new Mock<ILexichordTool>();
        mockTool.Setup(t => t.Id).Returns("event-test");

        var mockToolDock = new Mock<IToolDock>();
        var dockables = new ObservableCollection<IDockable>();
        mockToolDock.As<IDock>().Setup(d => d.VisibleDockables).Returns(dockables);

        _mockFactory.Setup(f => f.CreateTool(
            ShellRegion.Left, "event-test", "Test", It.IsAny<object>()))
            .Returns(mockTool.Object);
        _mockFactory.Setup(f => f.GetToolDock(ShellRegion.Left)).Returns(mockToolDock.Object);

        RegionChangedEventArgs? receivedArgs = null;
        _sut.RegionChanged += (_, args) => receivedArgs = args;

        // Act
        await _sut.RegisterToolAsync(ShellRegion.Left, "event-test", "Test", sp => new object());

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Region.Should().Be(ShellRegion.Left);
        receivedArgs.ChangeType.Should().Be(RegionChangeType.Added);
        receivedArgs.DockableId.Should().Be("event-test");
    }
}
