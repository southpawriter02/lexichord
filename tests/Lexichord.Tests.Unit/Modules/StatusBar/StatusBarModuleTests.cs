using FluentAssertions;
using Lexichord.Modules.StatusBar;
using Lexichord.Modules.StatusBar.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for StatusBarModule.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the module contract compliance:
/// - ModuleInfo properties are correctly populated
/// - RegisterServices adds all required services
/// - InitializeAsync can be called without error
/// </remarks>
public class StatusBarModuleTests
{
    [Fact]
    public void ModuleInfo_HasCorrectId()
    {
        // Arrange & Act
        var module = new StatusBarModule();

        // Assert
        module.Info.Id.Should().Be("statusbar");
    }

    [Fact]
    public void ModuleInfo_HasCorrectName()
    {
        // Arrange & Act
        var module = new StatusBarModule();

        // Assert
        module.Info.Name.Should().Be("Status Bar");
    }

    [Fact]
    public void ModuleInfo_HasCorrectVersion()
    {
        // Arrange & Act
        var module = new StatusBarModule();

        // Assert
        module.Info.Version.Should().Be(new Version(0, 0, 8));
    }

    [Fact]
    public void ModuleInfo_HasAuthor()
    {
        // Arrange & Act
        var module = new StatusBarModule();

        // Assert
        module.Info.Author.Should().Be("Lexichord Team");
    }

    [Fact]
    public void ModuleInfo_HasDescription()
    {
        // Arrange & Act
        var module = new StatusBarModule();

        // Assert
        module.Info.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RegisterServices_RegistersHealthRepository()
    {
        // Arrange
        var module = new StatusBarModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHealthRepository) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterServices_RegistersHeartbeatService()
    {
        // Arrange
        var module = new StatusBarModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHeartbeatService) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterServices_RegistersVaultStatusService()
    {
        // Arrange
        var module = new StatusBarModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IVaultStatusService) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterServices_RegistersShellRegionView()
    {
        // Arrange
        var module = new StatusBarModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(Lexichord.Abstractions.Contracts.IShellRegionView));
    }
}
