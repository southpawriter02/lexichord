using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Abstractions.Events;
using Lexichord.Host;
using Lexichord.Modules.StatusBar;
using Lexichord.Modules.StatusBar.Services;
using Lexichord.Modules.StatusBar.ViewModels;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Integration;

/// <summary>
/// Golden Skeleton integration tests — prove all foundational systems work together.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the complete integration of:
/// - Module Loading (v0.0.4)
/// - Shell Regions (v0.0.5)
/// - Database (v0.0.6)
/// - Secure Vault (v0.0.7)
/// - Event Bus (v0.0.7)
/// - Logging (v0.0.3)
/// - Configuration (v0.0.3)
/// - DI Container (v0.0.3)
///
/// If all these tests pass, the architecture is proven.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Category", "GoldenSkeleton")]
public class GoldenSkeletonTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private StatusBarModule _statusBarModule = null!;
    private string _tempDbPath = null!;
    private string _tempConfigPath = null!;
    private string _tempDir = null!;

    public async Task InitializeAsync()
    {
        // Create temporary paths
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"lexichord-golden-skeleton-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _tempDbPath = Path.Combine(_tempDir, "lexichord.db");
        _tempConfigPath = Path.Combine(_tempDir, "appsettings.json");

        // Create test configuration
        File.WriteAllText(_tempConfigPath, $$"""
            {
              "Lexichord": {
                "ApplicationName": "Lexichord Test",
                "Environment": "Testing",
                "DebugMode": true
              },
              "Database": {
                "ConnectionString": "Data Source={{_tempDbPath.Replace("\\", "\\\\")}}"
              }
            }
            """);

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.json")
            .Build();

        // Build services
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        // Add Host services
        services.ConfigureServices(configuration);

        // LOGIC: Add stub ISecureVault for testing vault-related services
        // In production, this is registered by the Host during startup
        services.AddSingleton<ISecureVault, StubSecureVault>();

        // Add StatusBar module services
        _statusBarModule = new StatusBarModule();
        _statusBarModule.RegisterServices(services);

        // Build provider
        _provider = services.BuildServiceProvider();

        // Initialize module
        await _statusBarModule.InitializeAsync(_provider);
    }

    public Task DisposeAsync()
    {
        _provider?.Dispose();

        // Clean up temp files
        if (_tempDir is not null && Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { /* Ignore cleanup errors */ }
        }

        return Task.CompletedTask;
    }

    #region Module Discovery Tests (v0.0.4)

    [Fact]
    public void Module_HasCorrectMetadata()
    {
        _statusBarModule.Info.Id.Should().Be("statusbar");
        _statusBarModule.Info.Name.Should().Be("Status Bar");
        _statusBarModule.Info.Version.Should().Be(new Version(0, 0, 8));
        _statusBarModule.Info.Author.Should().Be("Lexichord Team");
    }

    #endregion

    #region Service Registration Tests (v0.0.3)

    [Fact]
    public void Services_AllStatusBarServicesRegistered()
    {
        _provider.GetService<IHealthRepository>().Should().NotBeNull(
            "IHealthRepository should be registered");
        _provider.GetService<IHeartbeatService>().Should().NotBeNull(
            "IHeartbeatService should be registered");
        _provider.GetService<IVaultStatusService>().Should().NotBeNull(
            "IVaultStatusService should be registered");
        _provider.GetService<StatusBarViewModel>().Should().NotBeNull(
            "StatusBarViewModel should be registered");
    }

    [Fact]
    public void Services_SingletonsReturnSameInstance()
    {
        // Act
        var health1 = _provider.GetRequiredService<IHealthRepository>();
        var health2 = _provider.GetRequiredService<IHealthRepository>();

        // Assert
        health1.Should().BeSameAs(health2,
            "Singleton services should return the same instance");
    }

    #endregion

    #region Shell Region Tests (v0.0.5)

    [Fact]
    public void ShellRegion_StatusBarRegisteredInBottomRegion()
    {
        // Act
        var regionView = _provider.GetService<IShellRegionView>();

        // Assert
        regionView.Should().NotBeNull("IShellRegionView should be registered");
        regionView!.TargetRegion.Should().Be(ShellRegion.Bottom,
            "StatusBar should target Bottom region");
        regionView.Order.Should().Be(100,
            "StatusBar should have order 100");
    }

    [Fact(Skip = "Requires Avalonia runtime context with ICursorFactory")]
    public void ShellRegion_ViewContentIsAccessible()
    {
        // Act
        var regionView = _provider.GetRequiredService<IShellRegionView>();
        var content = regionView.ViewContent;

        // Assert
        content.Should().NotBeNull("ViewContent should return a view instance");
    }

    #endregion

    #region Database Tests (v0.0.6)

    [Fact]
    public async Task Database_HealthTableExists()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var version = await repo.GetDatabaseVersionAsync();

        // Assert
        version.Should().BeGreaterThan(0,
            "Database version should be set after migration");
    }

    [Fact]
    public async Task Database_StartupRecorded()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var lastHeartbeat = await repo.GetLastHeartbeatAsync();

        // Assert
        lastHeartbeat.Should().NotBeNull("Heartbeat should be recorded after startup");
        lastHeartbeat!.Value.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5),
            "Heartbeat should be recent");
    }

    [Fact]
    public async Task Database_UptimeIsPositive()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        await Task.Delay(100); // Ensure some time has passed
        var uptime = await repo.GetSystemUptimeAsync();

        // Assert
        uptime.Should().BeGreaterThan(TimeSpan.Zero,
            "Uptime should be greater than zero");
    }

    [Fact]
    public async Task Database_HeartbeatUpdates()
    {
        // Arrange
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var beforeHeartbeat = await repo.GetLastHeartbeatAsync();

        // Act
        await Task.Delay(100);
        await repo.RecordHeartbeatAsync();
        var afterHeartbeat = await repo.GetLastHeartbeatAsync();

        // Assert
        afterHeartbeat.Should().BeAfter(beforeHeartbeat!.Value,
            "Heartbeat should update after recording");
    }

    [Fact]
    public async Task Database_HealthCheckPasses()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var isHealthy = await repo.CheckHealthAsync();

        // Assert
        isHealthy.Should().BeTrue("Database health check should pass");
    }

    #endregion

    #region Vault Tests (v0.0.7)

    [Fact]
    public async Task Vault_StatusServiceAccessible()
    {
        // Act
        var vaultService = _provider.GetRequiredService<IVaultStatusService>();
        var status = await vaultService.GetVaultStatusAsync();

        // Assert — verify it's not in error state (could be Ready, Empty, or Unavailable)
        status.Should().NotBe(VaultStatus.Error,
            "Vault should not be in error state");
    }

    [Fact]
    public async Task Vault_KeyPresenceCheckWorks()
    {
        // Arrange
        var vaultService = _provider.GetRequiredService<IVaultStatusService>();

        // Act — CheckApiKeyPresenceAsync() checks for test API key presence
        var hasKey = await vaultService.CheckApiKeyPresenceAsync();

        // Assert — test key likely doesn't exist in fresh test environment
        hasKey.Should().BeFalse("Non-existent key should return false");
    }

    #endregion

    #region Event Bus Tests (v0.0.7)

    [Fact]
    public void EventBus_MediatRConfigured()
    {
        // Act
        var mediator = _provider.GetService<IMediator>();

        // Assert
        mediator.Should().NotBeNull("IMediator should be registered");
    }

    [Fact]
    public async Task EventBus_CanPublishEvents()
    {
        // Arrange
        var mediator = _provider.GetRequiredService<IMediator>();
        var testEvent = new SystemHealthChangedEvent(
            HealthStatus.Healthy,
            "Integration test event",
            DateTime.UtcNow);

        // Act & Assert — no exception means success
        var act = async () => await mediator.Publish(testEvent);
        await act.Should().NotThrowAsync("Event should publish without exception");
    }

    #endregion

    #region Configuration Tests (v0.0.3)

    [Fact]
    public void Configuration_LoadedCorrectly()
    {
        // Act
        var config = _provider.GetRequiredService<IConfiguration>();

        // Assert
        config["Lexichord:ApplicationName"].Should().Be("Lexichord Test");
        config["Lexichord:Environment"].Should().Be("Testing");
        config.GetValue<bool>("Lexichord:DebugMode").Should().BeTrue();
    }

    #endregion

    #region Logging Tests (v0.0.3)

    [Fact]
    public void Logging_LoggerFactoryRegistered()
    {
        // Act
        var loggerFactory = _provider.GetService<ILoggerFactory>();
        var logger = _provider.GetService<ILogger<GoldenSkeletonTests>>();

        // Assert
        loggerFactory.Should().NotBeNull("ILoggerFactory should be registered");
        logger.Should().NotBeNull("ILogger<T> should be resolvable");
    }

    [Fact]
    public void Logging_CanWriteLogs()
    {
        // Arrange
        var logger = _provider.GetRequiredService<ILogger<GoldenSkeletonTests>>();

        // Act & Assert — no exception means success
        var act = () => logger.LogInformation("Golden Skeleton integration test log entry");
        act.Should().NotThrow("Log entry should be written without exception");
    }

    #endregion

    #region HeartbeatService Tests

    [Fact]
    public void HeartbeatService_IsRunning()
    {
        // Act
        var heartbeat = _provider.GetRequiredService<IHeartbeatService>();

        // Assert
        heartbeat.IsRunning.Should().BeTrue(
            "Heartbeat service should be running after module init");
    }

    [Fact]
    public void HeartbeatService_IntervalIsOneMinute()
    {
        // Act
        var heartbeat = _provider.GetRequiredService<IHeartbeatService>();

        // Assert
        heartbeat.Interval.Should().Be(TimeSpan.FromSeconds(60),
            "Heartbeat interval should be 60 seconds");
    }

    #endregion

    #region End-to-End Flow Tests

    [Fact]
    public async Task E2E_CompleteStatusBarFlow()
    {
        // This test verifies the complete flow from module to ViewModel

        // 1. Get the ViewModel (which has all dependencies injected)
        var viewModel = _provider.GetRequiredService<StatusBarViewModel>();

        // 2. Verify database status properties are set
        await Task.Delay(500); // Allow time for initial refresh
        viewModel.DatabaseStatusText.Should().NotBeNullOrEmpty(
            "Database status should be populated");
        viewModel.UptimeText.Should().StartWith("Uptime:",
            "Uptime should be populated");

        // 3. Verify vault status properties are set
        viewModel.VaultStatusText.Should().NotBeNullOrEmpty(
            "Vault status should be populated");

        // 4. Verify one of the status indicators is true
        var hasDatabaseIndicator =
            viewModel.IsDatabaseHealthy ||
            viewModel.IsDatabaseWarning ||
            viewModel.IsDatabaseError;
        hasDatabaseIndicator.Should().BeTrue("One database indicator should be active");

        var hasVaultIndicator =
            viewModel.IsVaultReady ||
            viewModel.IsVaultEmpty ||
            viewModel.IsVaultError ||
            viewModel.IsVaultUnknown;
        hasVaultIndicator.Should().BeTrue("One vault indicator should be active");
    }

    #endregion
}

/// <summary>
/// Stub ISecureVault implementation for integration tests.
/// </summary>
/// <remarks>
/// LOGIC: This stub provides a minimal ISecureVault implementation that:
/// - Always reports Unavailable status (no real keychain on test environments)
/// - Returns empty/null for all key operations
/// - Allows tests to verify vault service behavior without platform-specific keychain dependencies
/// </remarks>
internal sealed class StubSecureVault : ISecureVault
{
    private readonly Dictionary<string, (string Value, SecretMetadata Metadata)> _secrets = new();

    public Task StoreSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (_secrets.TryGetValue(key, out var existing))
        {
            // Preserve CreatedAt, update LastModifiedAt
            _secrets[key] = (value, new SecretMetadata(key, existing.Metadata.CreatedAt, existing.Metadata.LastAccessedAt, now));
        }
        else
        {
            _secrets[key] = (value, new SecretMetadata(key, now, null, now));
        }
        return Task.CompletedTask;
    }

    public Task<string> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(key, out var entry))
        {
            // Update last accessed time
            _secrets[key] = (entry.Value, entry.Metadata with { LastAccessedAt = DateTimeOffset.UtcNow });
            return Task.FromResult(entry.Value);
        }
        throw new SecretNotFoundException(key);
    }

    public Task<bool> DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_secrets.Remove(key));
    }

    public Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_secrets.ContainsKey(key));
    }

    public Task<SecretMetadata?> GetSecretMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_secrets.TryGetValue(key, out var entry) ? entry.Metadata : null);
    }

    public async IAsyncEnumerable<string> ListSecretsAsync(string? prefix = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Satisfy async requirement
        foreach (var key in _secrets.Keys)
        {
            if (prefix == null || key.StartsWith(prefix))
            {
                yield return key;
            }
        }
    }
}
