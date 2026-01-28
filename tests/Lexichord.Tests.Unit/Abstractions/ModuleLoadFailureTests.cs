using FluentAssertions;

using Lexichord.Abstractions.Contracts;

using Xunit;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Tests for the ModuleLoadFailure record.
/// </summary>
public class ModuleLoadFailureTests
{
    [Fact]
    public void ToString_WithModuleName_IncludesModuleNameAndPath()
    {
        // Arrange
        var failure = new ModuleLoadFailure(
            AssemblyPath: "/path/to/test.dll",
            ModuleName: "TestModule",
            FailureReason: "License check failed",
            Exception: null);

        // Act
        var result = failure.ToString();

        // Assert
        result.Should().Be("TestModule (/path/to/test.dll): License check failed");
    }

    [Fact]
    public void ToString_WithoutModuleName_ShowsOnlyPath()
    {
        // Arrange
        var failure = new ModuleLoadFailure(
            AssemblyPath: "/path/to/broken.dll",
            ModuleName: null,
            FailureReason: "Assembly load failed",
            Exception: null);

        // Act
        var result = failure.ToString();

        // Assert
        result.Should().Be("/path/to/broken.dll: Assembly load failed");
    }

    [Fact]
    public void Constructor_WithException_StoresException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var failure = new ModuleLoadFailure(
            AssemblyPath: "/path/to/test.dll",
            ModuleName: "TestModule",
            FailureReason: "Module instantiation failed",
            Exception: exception);

        // Assert
        failure.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var failure1 = new ModuleLoadFailure(
            "/path/test.dll", "Module", "Reason", null);
        var failure2 = new ModuleLoadFailure(
            "/path/test.dll", "Module", "Reason", null);

        // Assert
        failure1.Should().Be(failure2);
    }
}
