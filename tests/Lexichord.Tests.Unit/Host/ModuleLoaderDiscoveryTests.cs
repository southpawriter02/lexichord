using FluentAssertions;

using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Tests for ModuleLoader discovery functionality.
/// </summary>
public class ModuleLoaderDiscoveryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<ILogger<ModuleLoader>> _loggerMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;

    public ModuleLoaderDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ModuleLoaderTest_{Guid.NewGuid():N}");
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
    public async Task DiscoverAndLoadAsync_WithEmptyDirectory_ReturnsNoModules()
    {
        // Arrange
        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, _tempDir);
        var services = new ServiceCollection();

        // Act
        await loader.DiscoverAndLoadAsync(services);

        // Assert
        loader.LoadedModules.Should().BeEmpty();
        loader.FailedModules.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_CreatesModulesDirectoryIfMissing()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "Modules");
        Directory.Exists(nonExistentPath).Should().BeFalse();

        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, nonExistentPath);
        var services = new ServiceCollection();

        // Act
        await loader.DiscoverAndLoadAsync(services);

        // Assert
        Directory.Exists(nonExistentPath).Should().BeTrue();
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_RespectsModulesPath()
    {
        // Arrange
        var customPath = Path.Combine(_tempDir, "CustomModules");
        Directory.CreateDirectory(customPath);

        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, customPath);
        var services = new ServiceCollection();

        // Act
        await loader.DiscoverAndLoadAsync(services);

        // Assert
        // Should not throw and complete successfully
        loader.LoadedModules.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAndLoadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // We need to create at least one file to get past the early return
        File.WriteAllText(Path.Combine(_tempDir, "test.dll"), "not a real dll");

        var loader = new ModuleLoader(_loggerMock.Object, _licenseContextMock.Object, _tempDir);
        var services = new ServiceCollection();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => loader.DiscoverAndLoadAsync(services, cts.Token));
    }
}
