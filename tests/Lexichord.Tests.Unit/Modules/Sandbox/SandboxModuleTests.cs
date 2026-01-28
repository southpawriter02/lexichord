using FluentAssertions;

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Sandbox;
using Lexichord.Modules.Sandbox.Contracts;
using Lexichord.Modules.Sandbox.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Lexichord.Tests.Unit.Modules.Sandbox;

/// <summary>
/// Tests for the SandboxModule IModule implementation.
/// </summary>
public class SandboxModuleTests
{
    [Fact]
    public void Info_ReturnsCorrectMetadata()
    {
        // Arrange
        var sut = new SandboxModule();

        // Act
        var info = sut.Info;

        // Assert
        info.Id.Should().Be("sandbox");
        info.Name.Should().Be("Sandbox Module");
        info.Version.Should().Be(new Version(0, 0, 1));
        info.Author.Should().Be("Lexichord Team");
        info.Description.Should().Contain("proof-of-concept");
        info.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void RegisterServices_RegistersSandboxService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var sut = new SandboxModule();

        // Act
        sut.RegisterServices(services);
        var provider = services.BuildServiceProvider();
        var result = provider.GetService<ISandboxService>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SandboxService>();
    }

    [Fact]
    public async Task InitializeAsync_SetsInitializationTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var sut = new SandboxModule();
        sut.RegisterServices(services);
        var provider = services.BuildServiceProvider();

        var beforeInit = DateTime.UtcNow;

        // Act
        await sut.InitializeAsync(provider);

        var afterInit = DateTime.UtcNow;
        var service = provider.GetRequiredService<ISandboxService>();
        var initTime = service.GetInitializationTime();

        // Assert
        initTime.Should().BeOnOrAfter(beforeInit);
        initTime.Should().BeOnOrBefore(afterInit);
    }

    [Fact]
    public void SandboxModule_ImplementsIModule()
    {
        // Arrange
        var sut = new SandboxModule();

        // Assert
        sut.Should().BeAssignableTo<IModule>();
    }

    [Fact]
    public void SandboxModule_HasNoRequiresLicenseAttribute()
    {
        // Arrange
        var moduleType = typeof(SandboxModule);

        // Act
        var licenseAttr = moduleType
            .GetCustomAttributes(typeof(RequiresLicenseAttribute), false);

        // Assert - No attribute means Core tier
        licenseAttr.Should().BeEmpty();
    }

    [Fact]
    public void SandboxModule_HasParameterlessConstructor()
    {
        // Arrange
        var moduleType = typeof(SandboxModule);

        // Act
        var constructor = moduleType.GetConstructor(Type.EmptyTypes);

        // Assert
        constructor.Should().NotBeNull(
            "SandboxModule must have a parameterless constructor for Activator.CreateInstance");
    }
}
