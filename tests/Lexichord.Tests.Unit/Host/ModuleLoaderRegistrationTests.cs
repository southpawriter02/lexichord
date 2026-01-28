using FluentAssertions;

using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Tests for ModuleLoader registration and initialization behavior.
/// </summary>
public class ModuleLoaderRegistrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<ILogger<ModuleLoader>> _loggerMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;

    public ModuleLoaderRegistrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ModuleLoaderRegTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _loggerMock = new Mock<ILogger<ModuleLoader>>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(It.IsAny<string>())).Returns(true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task InitializeModulesAsync_WithNoModules_CompletesSuccessfully()
    {
        // Arrange
        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, _tempDir);
        var services = new ServiceCollection();
        await loader.DiscoverAndLoadAsync(services);
        var provider = services.BuildServiceProvider();

        // Act
        await loader.InitializeModulesAsync(provider);

        // Assert - should complete without exception
        loader.LoadedModules.Should().BeEmpty();
    }

    [Fact]
    public void LoadedModules_BeforeDiscovery_ReturnsEmptyList()
    {
        // Arrange
        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, _tempDir);

        // Assert
        loader.LoadedModules.Should().BeEmpty();
        loader.FailedModules.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_LogsCurrentLicenseTier()
    {
        // Arrange - need at least one DLL to get past early return
        File.WriteAllText(Path.Combine(_tempDir, "dummy.dll"), "not a real dll");
        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, _tempDir);
        var services = new ServiceCollection();

        // Act
        await loader.DiscoverAndLoadAsync(services);

        // Assert - verify license context was queried
        _licenseContextMock.Verify(l => l.GetCurrentTier(), Times.Once);
    }
}
