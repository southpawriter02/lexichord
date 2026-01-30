using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for StyleModule.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the module contract compliance:
/// - ModuleInfo properties are correctly populated
/// - RegisterServices adds all required services
/// - All services are registered as singletons
/// - Module does not reference Lexichord.Host (architecture test)
/// </remarks>
public class StyleModuleTests
{
    [Fact]
    public void ModuleInfo_HasCorrectId()
    {
        // Arrange & Act
        var module = new StyleModule();

        // Assert
        module.Info.Id.Should().Be("style");
    }

    [Fact]
    public void ModuleInfo_HasCorrectName()
    {
        // Arrange & Act
        var module = new StyleModule();

        // Assert
        module.Info.Name.Should().Be("The Rulebook");
    }

    [Fact]
    public void ModuleInfo_HasCorrectVersion()
    {
        // Arrange & Act
        var module = new StyleModule();

        // Assert
        module.Info.Version.Should().Be(new Version(0, 2, 5));
    }

    [Fact]
    public void ModuleInfo_HasAuthor()
    {
        // Arrange & Act
        var module = new StyleModule();

        // Assert
        module.Info.Author.Should().Be("Lexichord Team");
    }

    [Fact]
    public void ModuleInfo_HasDescription()
    {
        // Arrange & Act
        var module = new StyleModule();

        // Assert
        module.Info.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RegisterServices_RegistersStyleEngine()
    {
        // Arrange
        var module = new StyleModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IStyleEngine) &&
            sd.ImplementationType == typeof(StyleEngine));
    }

    [Fact]
    public void RegisterServices_RegistersStyleSheetLoader()
    {
        // Arrange
        var module = new StyleModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IStyleSheetLoader) &&
            sd.ImplementationType == typeof(YamlStyleSheetLoader));
    }

    [Fact]
    public void RegisterServices_RegistersStyleConfigurationWatcher()
    {
        // Arrange
        var module = new StyleModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IStyleConfigurationWatcher) &&
            sd.ImplementationType == typeof(FileSystemStyleWatcher));
    }

    [Fact]
    public void RegisterServices_CoreServicesAreSingletons()
    {
        // Arrange
        var module = new StyleModule();
        var services = new ServiceCollection();

        // Act
        module.RegisterServices(services);

        // Assert
        // LOGIC: Filter to only Lexichord services (exclude framework services from AddMemoryCache)
        // ViewModels and Views are transient by design; core services should be singletons
        var lexichordServices = services.Where(sd =>
            (sd.ServiceType.FullName?.StartsWith("Lexichord") == true ||
             sd.ImplementationType?.FullName?.StartsWith("Lexichord") == true) &&
            !sd.ServiceType.Name.EndsWith("ViewModel") &&
            !sd.ServiceType.Name.EndsWith("View"));

        lexichordServices.Should().OnlyContain(sd => sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void StyleModule_DoesNotReference_LexichordHost()
    {
        // Arrange
        var styleAssembly = typeof(StyleModule).Assembly;

        // Act
        var referencedAssemblies = styleAssembly.GetReferencedAssemblies();

        // Assert
        referencedAssemblies
            .Select(a => a.Name)
            .Should().NotContain("Lexichord.Host",
                because: "modules must only reference Abstractions, not the Host");
    }
}
