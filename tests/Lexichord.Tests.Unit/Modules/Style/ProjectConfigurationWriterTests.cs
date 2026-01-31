using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ProjectConfigurationWriter"/>.
/// </summary>
/// <remarks>
/// Tests cover file creation, YAML manipulation, atomic writes,
/// and error handling scenarios.
///
/// Version: v0.3.6c
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.6c")]
public class ProjectConfigurationWriterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IWorkspaceService> _mockWorkspace;

    public ProjectConfigurationWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _mockWorkspace = new Mock<IWorkspaceService>();
        _mockWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(true);
        _mockWorkspace.Setup(w => w.CurrentWorkspace).Returns(
            new WorkspaceInfo(_tempDir, "TestWorkspace", DateTimeOffset.Now));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region IgnoreRuleAsync Tests

    [Fact]
    public async Task IgnoreRuleAsync_CreatesConfigFile_WhenNotExists()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = await writer.IgnoreRuleAsync("TERM-001");

        // Assert
        result.Should().BeTrue();
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        File.Exists(configPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(configPath);
        content.Should().Contain("TERM-001");
        content.Should().Contain("ignored_rules");
    }

    [Fact]
    public async Task IgnoreRuleAsync_CreatesDirectory_WhenNotExists()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        await writer.IgnoreRuleAsync("TERM-001");

        // Assert
        var configDir = Path.Combine(_tempDir, ".lexichord");
        Directory.Exists(configDir).Should().BeTrue();
    }

    [Fact]
    public async Task IgnoreRuleAsync_AppendsToExisting_WhenFileExists()
    {
        // Arrange
        var configDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(configDir);

        // Use format that matches StyleConfiguration
        var existingYaml = @"version: 1
ignored_rules:
- EXISTING-001
";
        await File.WriteAllTextAsync(Path.Combine(configDir, "style.yaml"), existingYaml);

        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = await writer.IgnoreRuleAsync("TERM-002");

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(configDir, "style.yaml"));
        content.Should().Contain("EXISTING-001");
        content.Should().Contain("TERM-002");
    }

    [Fact]
    public async Task IgnoreRuleAsync_RuleAlreadyExists_ReturnsSuccessNoDuplicate()
    {
        // Arrange
        var configDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(configDir);

        var existingYaml = @"version: 1
ignored_rules:
- TERM-001
";
        await File.WriteAllTextAsync(Path.Combine(configDir, "style.yaml"), existingYaml);

        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = await writer.IgnoreRuleAsync("TERM-001");

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(configDir, "style.yaml"));
        // Count occurrences - should only appear once
        var count = content.Split("TERM-001").Length - 1;
        count.Should().Be(1);
    }

    [Fact]
    public async Task IgnoreRuleAsync_MaintainsAlphabeticalOrder()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        await writer.IgnoreRuleAsync("ZEBRA-001");
        await writer.IgnoreRuleAsync("ALPHA-001");
        await writer.IgnoreRuleAsync("MIDDLE-001");

        // Assert
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        var content = await File.ReadAllTextAsync(configPath);
        var alphaIndex = content.IndexOf("ALPHA-001");
        var middleIndex = content.IndexOf("MIDDLE-001");
        var zebraIndex = content.IndexOf("ZEBRA-001");

        alphaIndex.Should().BeLessThan(middleIndex);
        middleIndex.Should().BeLessThan(zebraIndex);
    }

    [Fact]
    public async Task IgnoreRuleAsync_EmptyRuleId_ReturnsFalse()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = await writer.IgnoreRuleAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IgnoreRuleAsync_NoWorkspaceOpen_ThrowsInvalidOperationException()
    {
        // Arrange
        var closedWorkspace = new Mock<IWorkspaceService>();
        closedWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(false);
        closedWorkspace.Setup(w => w.CurrentWorkspace).Returns((WorkspaceInfo?)null);

        var writer = new ProjectConfigurationWriter(closedWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.IgnoreRuleAsync("TERM-001"));
    }

    #endregion

    #region RestoreRuleAsync Tests

    [Fact]
    public async Task RestoreRuleAsync_RemovesRule_WhenExists()
    {
        // Arrange - First add rules, then remove one
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.IgnoreRuleAsync("TERM-001");
        await writer.IgnoreRuleAsync("TERM-002");

        // Act
        var result = await writer.RestoreRuleAsync("TERM-001");

        // Assert
        result.Should().BeTrue();
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        var content = await File.ReadAllTextAsync(configPath);
        content.Should().NotContain("TERM-001");
        content.Should().Contain("TERM-002");
    }

    [Fact]
    public async Task RestoreRuleAsync_RuleNotPresent_ReturnsSuccess()
    {
        // Arrange - Create a config with a different rule
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.IgnoreRuleAsync("OTHER-001");

        // Act
        var result = await writer.RestoreRuleAsync("NONEXISTENT-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreRuleAsync_CaseInsensitiveMatching()
    {
        // Arrange - Create config using the writer
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.IgnoreRuleAsync("TERM-001");

        // Act - Use lowercase to remove
        var result = await writer.RestoreRuleAsync("term-001");

        // Assert
        result.Should().BeTrue();
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        var content = await File.ReadAllTextAsync(configPath);
        content.Should().NotContain("TERM-001");
    }

    #endregion

    #region ExcludeTermAsync Tests

    [Fact]
    public async Task ExcludeTermAsync_AddsTerm_WhenNotExists()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = await writer.ExcludeTermAsync("whitelist");

        // Assert
        result.Should().BeTrue();
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        var content = await File.ReadAllTextAsync(configPath);
        content.Should().Contain("whitelist");
        content.Should().Contain("terminology_exclusions");
    }

    [Fact]
    public async Task ExcludeTermAsync_TermAlreadyExists_ReturnsSuccessNoDuplicate()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.ExcludeTermAsync("whitelist");

        // Act
        var result = await writer.ExcludeTermAsync("whitelist");

        // Assert
        result.Should().BeTrue();
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        var content = await File.ReadAllTextAsync(configPath);
        var count = content.Split("whitelist").Length - 1;
        count.Should().Be(1);
    }

    #endregion

    #region RestoreTermAsync Tests

    [Fact]
    public async Task RestoreTermAsync_RemovesTerm_WhenExists()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.ExcludeTermAsync("whitelist");

        // Act
        var result = await writer.RestoreTermAsync("whitelist");

        // Assert
        result.Should().BeTrue();
        var configPath = Path.Combine(_tempDir, ".lexichord", "style.yaml");
        var content = await File.ReadAllTextAsync(configPath);
        content.Should().NotContain("whitelist");
    }

    #endregion

    #region IsRuleIgnored Tests

    [Fact]
    public async Task IsRuleIgnored_ReturnsTrue_WhenRuleInList()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.IgnoreRuleAsync("TERM-001");

        // Act
        var result = writer.IsRuleIgnored("TERM-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRuleIgnored_ReturnsFalse_WhenNoConfigFile()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = writer.IsRuleIgnored("TERM-001");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRuleIgnored_CaseInsensitive()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.IgnoreRuleAsync("TERM-001");

        // Act
        var result = writer.IsRuleIgnored("term-001");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsTermExcluded Tests

    [Fact]
    public async Task IsTermExcluded_ReturnsTrue_WhenTermInList()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);
        await writer.ExcludeTermAsync("whitelist");

        // Act
        var result = writer.IsTermExcluded("whitelist");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTermExcluded_ReturnsFalse_WhenNoConfigFile()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var result = writer.IsTermExcluded("whitelist");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetConfigurationFilePath Tests

    [Fact]
    public void GetConfigurationFilePath_ReturnsPath_WhenWorkspaceOpen()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var path = writer.GetConfigurationFilePath();

        // Assert
        path.Should().NotBeNull();
        path.Should().Contain(".lexichord");
        path.Should().EndWith("style.yaml");
    }

    [Fact]
    public void GetConfigurationFilePath_ReturnsNull_WhenNoWorkspace()
    {
        // Arrange
        var closedWorkspace = new Mock<IWorkspaceService>();
        closedWorkspace.Setup(w => w.IsWorkspaceOpen).Returns(false);
        closedWorkspace.Setup(w => w.CurrentWorkspace).Returns((WorkspaceInfo?)null);

        var writer = new ProjectConfigurationWriter(closedWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var path = writer.GetConfigurationFilePath();

        // Assert
        path.Should().BeNull();
    }

    #endregion

    #region EnsureConfigurationFileAsync Tests

    [Fact]
    public async Task EnsureConfigurationFileAsync_CreatesFile_WhenNotExists()
    {
        // Arrange
        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var path = await writer.EnsureConfigurationFileAsync();

        // Assert
        path.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureConfigurationFileAsync_ReturnsExistingPath_WhenFileExists()
    {
        // Arrange
        var configDir = Path.Combine(_tempDir, ".lexichord");
        Directory.CreateDirectory(configDir);
        var configPath = Path.Combine(configDir, "style.yaml");
        await File.WriteAllTextAsync(configPath, "version: 1");

        var writer = new ProjectConfigurationWriter(_mockWorkspace.Object, NullLogger<ProjectConfigurationWriter>.Instance);

        // Act
        var path = await writer.EnsureConfigurationFileAsync();

        // Assert
        path.Should().Be(configPath);
        // Content should not be overwritten
        var content = await File.ReadAllTextAsync(configPath);
        content.Should().Be("version: 1");
    }

    #endregion
}
