using Dock.Model.Core;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Layout;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Host.Layout;

/// <summary>
/// Unit tests for <see cref="LexichordDockFactory"/>.
/// </summary>
public class LexichordDockFactoryTests
{
    private readonly Mock<ILogger<LexichordDockFactory>> _mockLogger;
    private readonly LexichordDockFactory _factory;

    public LexichordDockFactoryTests()
    {
        _mockLogger = new Mock<ILogger<LexichordDockFactory>>();
        _factory = new LexichordDockFactory(_mockLogger.Object);
    }

    [Fact]
    public void CreateDefaultLayout_ReturnsValidRootDock()
    {
        // Act
        var result = _factory.CreateDefaultLayout();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("Lexichord.Root");
    }

    [Fact]
    public void CreateDefaultLayout_ContainsDocumentDock()
    {
        // Act
        _ = _factory.CreateDefaultLayout();

        // Assert
        _factory.DocumentDock.Should().NotBeNull();
        _factory.DocumentDock!.Id.Should().Be("Lexichord.Documents");
    }

    [Fact]
    public void CreateDefaultLayout_ContainsAllToolDocks()
    {
        // Act
        _ = _factory.CreateDefaultLayout();

        // Assert
        _factory.GetToolDock(ShellRegion.Left).Should().NotBeNull();
        _factory.GetToolDock(ShellRegion.Left)!.Id.Should().Be("Lexichord.Left");

        _factory.GetToolDock(ShellRegion.Right).Should().NotBeNull();
        _factory.GetToolDock(ShellRegion.Right)!.Id.Should().Be("Lexichord.Right");

        _factory.GetToolDock(ShellRegion.Bottom).Should().NotBeNull();
        _factory.GetToolDock(ShellRegion.Bottom)!.Id.Should().Be("Lexichord.Bottom");
    }

    [Fact]
    public void CreateDocument_ReturnsConfiguredDocument()
    {
        // Arrange
        var content = new object();

        // Act
        var result = _factory.CreateDocument("doc.test", "Test Document", content);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<LexichordDocument>();
        result.Id.Should().Be("doc.test");
        result.Title.Should().Be("Test Document");
        ((IDockable)result).Context.Should().BeSameAs(content);
    }

    [Fact]
    public void CreateTool_ReturnsConfiguredTool()
    {
        // Arrange
        var content = new object();

        // Act
        var result = _factory.CreateTool(ShellRegion.Left, "tool.test", "Test Tool", content);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<LexichordTool>();
        result.Id.Should().Be("tool.test");
        result.Title.Should().Be("Test Tool");
        result.PreferredRegion.Should().Be(ShellRegion.Left);
        ((IDockable)result).Context.Should().BeSameAs(content);
    }

    [Fact]
    public void FindDockable_ReturnsCorrectDockable()
    {
        // Arrange
        _ = _factory.CreateDefaultLayout();

        // Act
        var result = _factory.FindDockable("Lexichord.Documents");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("Lexichord.Documents");
    }

    [Fact]
    public void FindDockable_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _ = _factory.CreateDefaultLayout();

        // Act
        var result = _factory.FindDockable("non.existent.id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RootDock_IsNull_BeforeLayoutCreated()
    {
        // Assert
        _factory.RootDock.Should().BeNull();
    }

    [Fact]
    public void RootDock_IsPopulated_AfterLayoutCreated()
    {
        // Act
        _ = _factory.CreateDefaultLayout();

        // Assert
        _factory.RootDock.Should().NotBeNull();
    }
}
