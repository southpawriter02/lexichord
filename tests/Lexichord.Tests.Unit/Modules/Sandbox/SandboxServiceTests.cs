using FluentAssertions;

using Lexichord.Modules.Sandbox.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Lexichord.Tests.Unit.Modules.Sandbox;

/// <summary>
/// Tests for the SandboxService implementation.
/// </summary>
public class SandboxServiceTests
{
    private readonly SandboxService _sut;
    private readonly Mock<ILogger<SandboxService>> _mockLogger;

    public SandboxServiceTests()
    {
        _mockLogger = new Mock<ILogger<SandboxService>>();
        _sut = new SandboxService(_mockLogger.Object);
    }

    [Fact]
    public void GetModuleName_ReturnsCorrectName()
    {
        // Act
        var result = _sut.GetModuleName();

        // Assert
        result.Should().Be("Lexichord.Modules.Sandbox");
    }

    [Fact]
    public void Echo_AppendsModuleSignature()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = _sut.Echo(input);

        // Assert
        result.Should().Be("[Sandbox] Hello, World!");
    }

    [Fact]
    public void GetInitializationTime_BeforeSet_ReturnsMinValue()
    {
        // Act
        var result = _sut.GetInitializationTime();

        // Assert
        result.Should().Be(DateTime.MinValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    [InlineData("Hello with spaces")]
    public void Echo_HandlesVariousInputs(string input)
    {
        // Act
        var result = _sut.Echo(input);

        // Assert
        result.Should().Be($"[Sandbox] {input}");
    }

    // Note: SetInitializationTime is internal and tested via SandboxModule integration tests
}
