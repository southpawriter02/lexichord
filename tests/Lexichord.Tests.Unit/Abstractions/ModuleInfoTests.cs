using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for <see cref="ModuleInfo"/> record.
/// </summary>
public class ModuleInfoTests
{
    [Fact]
    public void ModuleInfo_WithAllFields_CreatesValidRecord()
    {
        // Arrange & Act
        var info = new ModuleInfo(
            Id: "test-module",
            Name: "Test Module",
            Version: new Version(1, 2, 3),
            Author: "Test Author",
            Description: "A test module for unit testing"
        );

        // Assert
        info.Id.Should().Be("test-module");
        info.Name.Should().Be("Test Module");
        info.Version.Should().Be(new Version(1, 2, 3));
        info.Author.Should().Be("Test Author");
        info.Description.Should().Be("A test module for unit testing");
        info.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void ModuleInfo_WithDependencies_StoresDependencyList()
    {
        // Arrange & Act
        var info = new ModuleInfo(
            Id: "dependent-module",
            Name: "Dependent Module",
            Version: new Version(1, 0, 0),
            Author: "Test Author",
            Description: "Module with dependencies",
            Dependencies: ["core-module", "utility-module"]
        );

        // Assert
        info.Dependencies.Should().HaveCount(2);
        info.Dependencies.Should().Contain("core-module");
        info.Dependencies.Should().Contain("utility-module");
    }

    [Fact]
    public void ModuleInfo_NullDependencies_ReturnsEmptyList()
    {
        // Arrange & Act
        var info = new ModuleInfo(
            Id: "simple-module",
            Name: "Simple Module",
            Version: new Version(1, 0, 0),
            Author: "Test Author",
            Description: "Module without dependencies",
            Dependencies: null
        );

        // Assert
        info.Dependencies.Should().NotBeNull();
        info.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void ModuleInfo_ToString_ReturnsFormattedString()
    {
        // Arrange
        var info = new ModuleInfo(
            Id: "test",
            Name: "Test Module",
            Version: new Version(2, 1, 0),
            Author: "Lexichord Team",
            Description: "Test"
        );

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Be("Test Module v2.1.0 by Lexichord Team");
    }

    [Fact]
    public void ModuleInfo_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var info1 = new ModuleInfo("test", "Test", new Version(1, 0, 0), "Author", "Desc");
        var info2 = new ModuleInfo("test", "Test", new Version(1, 0, 0), "Author", "Desc");
        var info3 = new ModuleInfo("other", "Test", new Version(1, 0, 0), "Author", "Desc");

        // Assert
        info1.Should().Be(info2);
        info1.Should().NotBe(info3);
    }
}
