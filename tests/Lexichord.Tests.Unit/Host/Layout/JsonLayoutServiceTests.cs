using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;
using Lexichord.Abstractions.Layout;
using Lexichord.Host.Layout;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Host.Layout;

/// <summary>
/// Unit tests for JsonLayoutService.
/// </summary>
[Trait("Category", "Unit")]
public class JsonLayoutServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<ILogger<JsonLayoutService>> _mockLogger;
    private readonly Mock<IDockFactory> _mockDockFactory;
    private JsonLayoutService _service = null!;

    public JsonLayoutServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "Lexichord_Test_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _mockLogger = new Mock<ILogger<JsonLayoutService>>();
        _mockDockFactory = new Mock<IDockFactory>();
    }

    public void Dispose()
    {
        _service?.Dispose();

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    private JsonLayoutService CreateService()
    {
        return new JsonLayoutService(_mockLogger.Object, _mockDockFactory.Object, _testDirectory);
    }

    [Fact]
    public async Task SaveLayoutAsync_NullRootDock_ReturnsFalse()
    {
        // Arrange
        _mockDockFactory.Setup(f => f.RootDock).Returns((IRootDock?)null);
        _service = CreateService();

        // Act
        var result = await _service.SaveLayoutAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SaveLayoutAsync_ValidRoot_CreatesFile()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        // Act
        var result = await _service.SaveLayoutAsync("TestProfile");

        // Assert
        result.Should().BeTrue();
        var expectedPath = Path.Combine(_service.LayoutDirectory, "TestProfile.json");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveLayoutAsync_ContainsSchemaVersion()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        // Act
        var saveResult = await _service.SaveLayoutAsync("TestProfile");

        // Assert
        saveResult.Should().BeTrue("save should succeed with valid root dock");
        var filePath = Path.Combine(_service.LayoutDirectory, "TestProfile.json");
        var json = await File.ReadAllTextAsync(filePath);
        json.Should().Contain($"\"schemaVersion\": {LayoutMetadata.CurrentSchemaVersion}");
    }

    [Fact]
    public async Task LoadLayoutAsync_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        _service = CreateService();

        // Act
        var result = await _service.LoadLayoutAsync("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoadLayoutAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        // Save first
        await _service.SaveLayoutAsync("TestProfile");

        // Act
        var result = await _service.LoadLayoutAsync("TestProfile");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LoadLayoutAsync_RaisesLayoutLoadedEvent()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        LayoutLoadedEventArgs? raisedArgs = null;
        _service.LayoutLoaded += (sender, args) => raisedArgs = args;

        await _service.SaveLayoutAsync("TestProfile");

        // Act
        await _service.LoadLayoutAsync("TestProfile");

        // Assert
        raisedArgs.Should().NotBeNull();
        raisedArgs!.ProfileName.Should().Be("TestProfile");
    }

    [Fact]
    public async Task GetProfileNamesAsync_ReturnsFileNames()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        await _service.SaveLayoutAsync("Profile1");
        await _service.SaveLayoutAsync("Profile2");

        // Act
        var profiles = (await _service.GetProfileNamesAsync()).ToList();

        // Assert
        profiles.Should().Contain("Profile1");
        profiles.Should().Contain("Profile2");
    }

    [Fact]
    public async Task ProfileExistsAsync_ExistingProfile_ReturnsTrue()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        await _service.SaveLayoutAsync("TestProfile");

        // Act
        var exists = await _service.ProfileExistsAsync("TestProfile");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ProfileExistsAsync_NonExistingProfile_ReturnsFalse()
    {
        // Arrange
        _service = CreateService();

        // Act
        var exists = await _service.ProfileExistsAsync("NonExistent");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteLayoutAsync_ExistingProfile_DeletesFile()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        await _service.SaveLayoutAsync("DeleteMe");

        // Act
        var result = await _service.DeleteLayoutAsync("DeleteMe");

        // Assert
        result.Should().BeTrue();
        (await _service.ProfileExistsAsync("DeleteMe")).Should().BeFalse();
    }

    [Fact]
    public async Task ExportLayoutAsync_CopiesFile()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        await _service.SaveLayoutAsync("ExportTest");
        var exportPath = Path.Combine(_testDirectory, "exported.json");

        // Act
        var result = await _service.ExportLayoutAsync("ExportTest", exportPath);

        // Assert
        result.Should().BeTrue();
        File.Exists(exportPath).Should().BeTrue();
    }

    [Fact]
    public async Task ImportLayoutAsync_ValidFile_ImportsSuccessfully()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _service = CreateService();

        await _service.SaveLayoutAsync("Original");
        var exportPath = Path.Combine(_testDirectory, "to_import.json");
        await _service.ExportLayoutAsync("Original", exportPath);

        // Act
        var result = await _service.ImportLayoutAsync(exportPath, "Imported");

        // Assert
        result.Should().BeTrue();
        (await _service.ProfileExistsAsync("Imported")).Should().BeTrue();
    }

    [Fact]
    public void CurrentProfileName_DefaultIsDefault()
    {
        // Arrange
        _service = CreateService();

        // Assert
        _service.CurrentProfileName.Should().Be("Default");
    }

    [Fact]
    public async Task ResetToDefaultAsync_CreatesNewDefaultLayout()
    {
        // Arrange
        var mockRootDock = CreateMockRootDock();
        _mockDockFactory.Setup(f => f.RootDock).Returns(mockRootDock);
        _mockDockFactory.Setup(f => f.CreateDefaultLayout()).Returns(mockRootDock);
        _service = CreateService();

        // Act
        var result = await _service.ResetToDefaultAsync();

        // Assert
        result.Should().BeTrue();
        _mockDockFactory.Verify(f => f.CreateDefaultLayout(), Times.Once);
    }

    private static IRootDock CreateMockRootDock()
    {
        var mockDock = new Mock<IRootDock>();
        mockDock.Setup(d => d.Id).Returns("root-dock");
        mockDock.Setup(d => d.Title).Returns("Root");
        mockDock.Setup(d => d.CanClose).Returns(true);
        mockDock.Setup(d => d.CanFloat).Returns(false);
        mockDock.Setup(d => d.Proportion).Returns(1.0);
        mockDock.Setup(d => d.ActiveDockable).Returns((Dock.Model.Core.IDockable?)null);
        mockDock.Setup(d => d.VisibleDockables).Returns(new Avalonia.Collections.AvaloniaList<Dock.Model.Core.IDockable>());

        return mockDock.Object;
    }
}
