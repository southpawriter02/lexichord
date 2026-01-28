using Lexichord.Abstractions.Contracts;

using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for <see cref="ModuleBase"/> abstract class.
/// </summary>
public class ModuleBaseTests
{
    /// <summary>
    /// Test implementation of ModuleBase for verification.
    /// </summary>
    private sealed class TestModule : ModuleBase
    {
        public override ModuleInfo Info => new(
            Id: "test",
            Name: "Test",
            Version: new Version(1, 0, 0),
            Author: "Test",
            Description: "Test module"
        );

        public bool RegisterServicesCalled { get; private set; }

        public override void RegisterServices(IServiceCollection services)
        {
            RegisterServicesCalled = true;
        }
    }

    [Fact]
    public void ModuleBase_InitializeAsync_ReturnsCompletedTask()
    {
        // Arrange
        var sut = new TestModule();
        var mockProvider = Mock.Of<IServiceProvider>();

        // Act
        var result = sut.InitializeAsync(mockProvider);

        // Assert
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ModuleBase_ImplementsIModule()
    {
        // Arrange
        var sut = new TestModule();

        // Assert
        sut.Should().BeAssignableTo<IModule>();
    }

    [Fact]
    public void ModuleBase_Info_ReturnsModuleInfo()
    {
        // Arrange
        var sut = new TestModule();

        // Act
        var info = sut.Info;

        // Assert
        info.Should().NotBeNull();
        info.Id.Should().Be("test");
    }
}
