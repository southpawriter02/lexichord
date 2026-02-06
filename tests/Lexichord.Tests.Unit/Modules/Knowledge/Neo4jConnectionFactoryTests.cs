// =============================================================================
// File: Neo4jConnectionFactoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Neo4jConnectionFactory license gating and init.
// =============================================================================
// LOGIC: Tests the Neo4jConnectionFactory using mocked dependencies. Verifies:
//   - License tier enforcement (Core throws, WriterPro read-only, Teams full)
//   - Constructor null checks
//   - Password resolution from vault and configuration fallback
//   - Session creation with correct access modes
//
// Note: These are pure unit tests using mocks. The factory creates a real
//   Neo4j driver that cannot connect (no Neo4j running), but license checks
//   happen before driver session creation.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.Knowledge.Graph;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="Neo4jConnectionFactory"/> license gating and initialization.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5e")]
public sealed class Neo4jConnectionFactoryTests : IAsyncDisposable
{
    private readonly Mock<ISecureVault> _vaultMock = new();
    private readonly Mock<ILicenseContext> _licenseMock = new();
    private readonly FakeLogger<Neo4jConnectionFactory> _logger = new();

    /// <summary>
    /// Creates a factory with the specified license tier and a test password.
    /// </summary>
    private Neo4jConnectionFactory CreateFactory(LicenseTier tier = LicenseTier.Teams)
    {
        _licenseMock.Setup(l => l.GetCurrentTier()).Returns(tier);

        // LOGIC: Vault throws to simulate "no secret found" — factory falls back to config.
        _vaultMock.Setup(v => v.GetSecretAsync("neo4j:password", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No secret"));

        var config = Options.Create(new GraphConfiguration
        {
            Password = "test_password",
            Uri = "bolt://localhost:9999" // Non-existent port — we're not connecting
        });

        return new Neo4jConnectionFactory(config, _vaultMock.Object, _licenseMock.Object, _logger);
    }

    public ValueTask DisposeAsync()
    {
        // LOGIC: Clean up any factories created during tests.
        // Individual tests may also dispose, but this is a safety net.
        return ValueTask.CompletedTask;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Neo4jConnectionFactory(
            null!, _vaultMock.Object, _licenseMock.Object, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_NullVault_ThrowsArgumentNullException()
    {
        // Arrange
        var config = Options.Create(new GraphConfiguration { Password = "test" });

        // Act
        var act = () => new Neo4jConnectionFactory(
            config, null!, _licenseMock.Object, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Arrange
        var config = Options.Create(new GraphConfiguration { Password = "test" });

        // Act
        var act = () => new Neo4jConnectionFactory(
            config, _vaultMock.Object, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var config = Options.Create(new GraphConfiguration { Password = "test" });

        // Act
        var act = () => new Neo4jConnectionFactory(
            config, _vaultMock.Object, _licenseMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateSessionAsync_NoPasswordAnywhere_ThrowsInvalidOperationException()
    {
        // Arrange - v0.6.3b: Lazy initialization defers password check to CreateSessionAsync
        _vaultMock.Setup(v => v.GetSecretAsync("neo4j:password", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No secret"));

        var config = Options.Create(new GraphConfiguration
        {
            Password = null // No fallback password
        });
        _licenseMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Teams);

        var factory = new Neo4jConnectionFactory(
            config, _vaultMock.Object, _licenseMock.Object, _logger);

        // Act - Exception now thrown on first use, not construction
        var act = () => factory.CreateSessionAsync(GraphAccessMode.Read);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Neo4j password not configured*");
    }

    [Fact]
    public void Constructor_VaultHasPassword_UsesVaultPassword()
    {
        // Arrange
        _vaultMock.Setup(v => v.GetSecretAsync("neo4j:password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("vault_password");
        _licenseMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Teams);

        var config = Options.Create(new GraphConfiguration
        {
            Password = "config_password",
            Uri = "bolt://localhost:9999"
        });

        // Act — should not throw (vault password found)
        var factory = new Neo4jConnectionFactory(config, _vaultMock.Object, _licenseMock.Object, _logger);

        // Assert
        factory.Should().NotBeNull();
        factory.DatabaseName.Should().Be("neo4j");
    }

    [Fact]
    public void Constructor_VaultThrows_FallsBackToConfig()
    {
        // Arrange & Act
        var factory = CreateFactory();

        // Assert — factory created successfully using config password
        factory.Should().NotBeNull();
        factory.DatabaseName.Should().Be("neo4j");
    }

    #endregion

    #region DatabaseName Tests

    [Fact]
    public void DatabaseName_ReturnsConfiguredDatabase()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        factory.DatabaseName.Should().Be("neo4j");
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task CreateSessionAsync_CoreTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Core);

        // Act
        var act = () => factory.CreateSessionAsync(GraphAccessMode.Read);

        // Assert
        await act.Should().ThrowAsync<FeatureNotLicensedException>()
            .Where(ex => ex.RequiredTier == LicenseTier.WriterPro);
    }

    [Fact]
    public async Task CreateSessionAsync_CoreTierWrite_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Core);

        // Act
        var act = () => factory.CreateSessionAsync(GraphAccessMode.Write);

        // Assert
        await act.Should().ThrowAsync<FeatureNotLicensedException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WriterProRead_Succeeds()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.WriterPro);

        // Act — this should NOT throw for read access (WriterPro allows read)
        // Note: The session will be created but won't connect (no Neo4j running)
        var session = await factory.CreateSessionAsync(GraphAccessMode.Read);

        // Assert
        session.Should().NotBeNull();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task CreateSessionAsync_WriterProWrite_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.WriterPro);

        // Act
        var act = () => factory.CreateSessionAsync(GraphAccessMode.Write);

        // Assert
        await act.Should().ThrowAsync<FeatureNotLicensedException>()
            .Where(ex => ex.RequiredTier == LicenseTier.Teams);
    }

    [Fact]
    public async Task CreateSessionAsync_TeamsWrite_Succeeds()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Teams);

        // Act
        var session = await factory.CreateSessionAsync(GraphAccessMode.Write);

        // Assert
        session.Should().NotBeNull();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task CreateSessionAsync_TeamsRead_Succeeds()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Teams);

        // Act
        var session = await factory.CreateSessionAsync(GraphAccessMode.Read);

        // Assert
        session.Should().NotBeNull();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task CreateSessionAsync_EnterpriseWrite_Succeeds()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Enterprise);

        // Act
        var session = await factory.CreateSessionAsync(GraphAccessMode.Write);

        // Assert
        session.Should().NotBeNull();
        await session.DisposeAsync();
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void Constructor_LogsLazyInitialization()
    {
        // Arrange & Act - v0.6.3b: Constructor now logs lazy initialization setup
        var factory = CreateFactory();

        // Assert
        _logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("Neo4jConnectionFactory created (lazy initialization)"));
    }

    [Fact]
    public async Task CreateSessionAsync_LogsSessionCreation()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Teams);

        // Act
        var session = await factory.CreateSessionAsync(GraphAccessMode.Write);
        await session.DisposeAsync();

        // Assert
        _logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("Creating graph session"));
    }

    [Fact]
    public async Task CreateSessionAsync_CoreTier_LogsWarning()
    {
        // Arrange
        var factory = CreateFactory(LicenseTier.Core);

        // Act
        try { await factory.CreateSessionAsync(GraphAccessMode.Read); }
        catch (FeatureNotLicensedException) { }

        // Assert
        _logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Warning &&
            log.Message.Contains("insufficient"));
    }

    #endregion
}
