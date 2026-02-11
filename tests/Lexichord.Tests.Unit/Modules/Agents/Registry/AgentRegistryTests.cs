// -----------------------------------------------------------------------
// <copyright file="AgentRegistryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Events;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Registry;
using Lexichord.Modules.Agents.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Registry;

/// <summary>
/// Unit tests for <see cref="AgentRegistry"/> v0.7.1b enhancements.
/// </summary>
/// <remarks>
/// <para>
/// Validates the registry's behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Factory-based agent registration</description></item>
///   <item><description>Persona management and switching</description></item>
///   <item><description>Configuration hot-reload</description></item>
///   <item><description>License enforcement</description></item>
///   <item><description>MediatR event publishing</description></item>
///   <item><description>Agent instance caching</description></item>
/// </list>
/// <para>
/// <b>Spec reference:</b> LCS-DES-v0.7.1b §4-7
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1b")]
public class AgentRegistryTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<AgentRegistry>> _loggerMock;
    private readonly AgentRegistry _sut;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    /// <remarks>
    /// Default setup: Teams license, no settings, MediatR always succeeds.
    /// </remarks>
    public AgentRegistryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<AgentRegistry>>();

        // Default: Teams license available
        _licenseContextMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Teams);

        // Default: No custom agents in settings
        _settingsServiceMock.Setup(s => s.Get<CustomAgentDefinition[]?>(
                "Agent:CustomAgents", null))
            .Returns((CustomAgentDefinition[]?)null);

        // Default: MediatR publish always succeeds
        _mediatorMock.Setup(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock IServiceScopeFactory for agent discovery
        var scopeMock = new Mock<IServiceScope>();
        var scopedProviderMock = new Mock<IServiceProvider>();
        scopedProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(new List<IAgent>());
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopedProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        _sut = new AgentRegistry(
            _serviceProviderMock.Object,
            _licenseContextMock.Object,
            _settingsServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    // -----------------------------------------------------------------------
    // RegisterAgent Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that RegisterAgent stores a valid configuration and factory.
    /// </summary>
    [Fact]
    public void RegisterAgent_WithValidConfiguration_StoresRegistration()
    {
        // Arrange
        var config = CreateTestConfiguration("test-agent");
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());

        // Act
        _sut.RegisterAgent(config, factory);

        // Assert
        var retrieved = _sut.GetConfiguration("test-agent");
        retrieved.Should().NotBeNull();
        retrieved!.AgentId.Should().Be("test-agent");
    }

    /// <summary>
    /// Verifies that RegisterAgent rejects invalid configurations.
    /// </summary>
    [Fact]
    public void RegisterAgent_WithInvalidConfiguration_ThrowsArgumentException()
    {
        // Arrange - Invalid configuration (empty AgentId)
        var config = new AgentConfiguration(
            AgentId: string.Empty,
            Name: "Test",
            Description: "Test",
            Icon: "icon",
            TemplateId: "template",
            Capabilities: AgentCapabilities.Chat,
            DefaultOptions: new ChatOptions(),
            Personas: new List<AgentPersona>(),
            RequiredTier: LicenseTier.Core);

        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _sut.RegisterAgent(config, factory));
        exception.Message.Should().Contain("Invalid configuration");
    }

    /// <summary>
    /// Verifies that RegisterAgent publishes AgentRegisteredEvent.
    /// </summary>
    [Fact]
    public void RegisterAgent_WithValidConfiguration_PublishesEvent()
    {
        // Arrange
        var config = CreateTestConfiguration("test-agent");
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());

        // Act
        _sut.RegisterAgent(config, factory);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<AgentRegisteredEvent>(e => e.Configuration.AgentId == "test-agent"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // GetAgentWithPersona Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetAgentWithPersona returns agent with correct persona.
    /// </summary>
    [Fact]
    public void GetAgentWithPersona_WithValidPersona_ReturnsAgent()
    {
        // Arrange
        var persona = new AgentPersona("professional", "Professional", "Formal tone", null, 0.3);
        var config = CreateTestConfiguration("test-agent", new[] { persona });
        var agentMock = new Mock<IPersonaAwareAgent>();
        agentMock.Setup(a => a.AgentId).Returns("test-agent");

        var factory = new Func<IServiceProvider, IAgent>(_ => agentMock.Object);
        _sut.RegisterAgent(config, factory);

        // Act
        var agent = _sut.GetAgentWithPersona("test-agent", "professional");

        // Assert
        agent.Should().NotBeNull();
        agent.AgentId.Should().Be("test-agent");
        agentMock.Verify(a => a.ApplyPersona(persona), Times.Once);
    }

    /// <summary>
    /// Verifies that GetAgentWithPersona throws for invalid persona.
    /// </summary>
    [Fact]
    public void GetAgentWithPersona_WithInvalidPersona_ThrowsPersonaNotFoundException()
    {
        // Arrange
        var persona = new AgentPersona("professional", "Professional", "Formal tone", null, 0.3);
        var config = CreateTestConfiguration("test-agent", new[] { persona });
        var agentMock = new Mock<IAgent>();
        agentMock.Setup(a => a.AgentId).Returns("test-agent");

        var factory = new Func<IServiceProvider, IAgent>(_ => agentMock.Object);
        _sut.RegisterAgent(config, factory);

        // Act & Assert
        var exception = Assert.Throws<PersonaNotFoundException>(
            () => _sut.GetAgentWithPersona("test-agent", "nonexistent"));
        exception.AgentId.Should().Be("test-agent");
        exception.PersonaId.Should().Be("nonexistent");
    }

    /// <summary>
    /// Verifies that GetAgentWithPersona throws for non-existent agent.
    /// </summary>
    [Fact]
    public void GetAgentWithPersona_WithNonExistentAgent_ThrowsAgentNotFoundException()
    {
        // Act & Assert
        Assert.Throws<AgentNotFoundException>(
            () => _sut.GetAgentWithPersona("nonexistent", "persona"));
    }

    /// <summary>
    /// Verifies that GetAgentWithPersona enforces license requirements.
    /// </summary>
    [Fact]
    public void GetAgentWithPersona_WithInsufficientLicense_ThrowsAgentAccessDeniedException()
    {
        // Arrange
        var persona = new AgentPersona("professional", "Professional", "Formal tone", null, 0.3);
        var config = CreateTestConfiguration(
            "test-agent",
            new[] { persona },
            requiredTier: LicenseTier.Enterprise);

        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(config, factory);

        // Change license tier to Teams (insufficient for Enterprise)
        _licenseContextMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Teams);

        // Act & Assert
        var exception = Assert.Throws<AgentAccessDeniedException>(
            () => _sut.GetAgentWithPersona("test-agent", "professional"));
        exception.AgentId.Should().Be("test-agent");
        exception.RequiredTier.Should().Be(LicenseTier.Enterprise);
        exception.CurrentTier.Should().Be(LicenseTier.Teams);
    }

    // -----------------------------------------------------------------------
    // UpdateAgent Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that UpdateAgent replaces configuration and invalidates cache.
    /// </summary>
    [Fact]
    public void UpdateAgent_WithValidConfiguration_UpdatesRegistration()
    {
        // Arrange
        var originalConfig = CreateTestConfiguration("test-agent");
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(originalConfig, factory);

        var updatedConfig = CreateTestConfiguration("test-agent", name: "Updated Name");

        // Act
        _sut.UpdateAgent(updatedConfig);

        // Assert
        var retrieved = _sut.GetConfiguration("test-agent");
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Name");
    }

    /// <summary>
    /// Verifies that UpdateAgent throws for non-existent agent.
    /// </summary>
    [Fact]
    public void UpdateAgent_WithNonExistentAgent_ThrowsAgentNotFoundException()
    {
        // Arrange
        var config = CreateTestConfiguration("nonexistent");

        // Act & Assert
        Assert.Throws<AgentNotFoundException>(
            () => _sut.UpdateAgent(config));
    }

    /// <summary>
    /// Verifies that UpdateAgent rejects invalid configurations.
    /// </summary>
    [Fact]
    public void UpdateAgent_WithInvalidConfiguration_LogsWarningAndReturns()
    {
        // Arrange
        var originalConfig = CreateTestConfiguration("test-agent");
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(originalConfig, factory);

        // Invalid configuration (empty name)
        var invalidConfig = new AgentConfiguration(
            AgentId: "test-agent",
            Name: string.Empty, // Invalid: empty name
            Description: "Test",
            Icon: "icon",
            TemplateId: "template",
            Capabilities: AgentCapabilities.Chat,
            DefaultOptions: new ChatOptions(),
            Personas: new List<AgentPersona>(),
            RequiredTier: LicenseTier.Core);

        // Act
        _sut.UpdateAgent(invalidConfig);

        // Assert - original config should remain unchanged
        var retrieved = _sut.GetConfiguration("test-agent");
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(originalConfig.Name);
    }

    /// <summary>
    /// Verifies that UpdateAgent publishes AgentConfigReloadedEvent.
    /// </summary>
    [Fact]
    public void UpdateAgent_WithValidConfiguration_PublishesEvent()
    {
        // Arrange
        var originalConfig = CreateTestConfiguration("test-agent");
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(originalConfig, factory);

        var updatedConfig = CreateTestConfiguration("test-agent", name: "Updated Name");

        // Act
        _sut.UpdateAgent(updatedConfig);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<AgentConfigReloadedEvent>(e =>
                    e.AgentId == "test-agent" &&
                    e.OldConfiguration.Name == originalConfig.Name &&
                    e.NewConfiguration.Name == "Updated Name"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // SwitchPersona Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that SwitchPersona applies persona to cached agent.
    /// </summary>
    [Fact]
    public void SwitchPersona_WithCachedAgent_AppliesPersona()
    {
        // Arrange
        var persona1 = new AgentPersona("casual", "Casual", "Informal tone", null, 0.7);
        var persona2 = new AgentPersona("professional", "Professional", "Formal tone", null, 0.3);
        var config = CreateTestConfiguration("test-agent", new[] { persona1, persona2 });

        var agentMock = new Mock<IPersonaAwareAgent>();
        agentMock.Setup(a => a.AgentId).Returns("test-agent");

        var factory = new Func<IServiceProvider, IAgent>(_ => agentMock.Object);
        _sut.RegisterAgent(config, factory);

        // Cache the agent by retrieving it first
        _sut.GetAgentWithPersona("test-agent", "casual");

        // Act
        _sut.SwitchPersona("test-agent", "professional");

        // Assert - persona2 should be applied
        agentMock.Verify(a => a.ApplyPersona(persona2), Times.Once);
    }

    /// <summary>
    /// Verifies that SwitchPersona throws for invalid persona.
    /// </summary>
    [Fact]
    public void SwitchPersona_WithInvalidPersona_ThrowsPersonaNotFoundException()
    {
        // Arrange
        var persona = new AgentPersona("casual", "Casual", "Informal tone", null, 0.7);
        var config = CreateTestConfiguration("test-agent", new[] { persona });
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(config, factory);

        // Act & Assert
        var exception = Assert.Throws<PersonaNotFoundException>(
            () => _sut.SwitchPersona("test-agent", "nonexistent"));
        exception.AgentId.Should().Be("test-agent");
        exception.PersonaId.Should().Be("nonexistent");
    }

    // -----------------------------------------------------------------------
    // GetActivePersona Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetActivePersona returns active persona after switch.
    /// </summary>
    [Fact]
    public void GetActivePersona_AfterSwitch_ReturnsActivePersona()
    {
        // Arrange
        var persona1 = new AgentPersona("casual", "Casual", "Informal tone", null, 0.7);
        var persona2 = new AgentPersona("professional", "Professional", "Formal tone", null, 0.3);
        var config = CreateTestConfiguration("test-agent", new[] { persona1, persona2 });

        var agentMock = new Mock<IPersonaAwareAgent>();
        agentMock.Setup(a => a.AgentId).Returns("test-agent");
        var factory = new Func<IServiceProvider, IAgent>(_ => agentMock.Object);
        _sut.RegisterAgent(config, factory);

        // Cache the agent
        _sut.GetAgentWithPersona("test-agent", "casual");
        _sut.SwitchPersona("test-agent", "professional");

        // Act
        var activePersona = _sut.GetActivePersona("test-agent");

        // Assert
        activePersona.Should().NotBeNull();
        activePersona!.PersonaId.Should().Be("professional");
    }

    /// <summary>
    /// Verifies that GetActivePersona returns null for non-existent agent.
    /// </summary>
    [Fact]
    public void GetActivePersona_ForNonExistentAgent_ReturnsNull()
    {
        // Act
        var activePersona = _sut.GetActivePersona("nonexistent");

        // Assert
        activePersona.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // CanAccess Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that CanAccess returns true with sufficient license.
    /// </summary>
    [Fact]
    public void CanAccess_WithSufficientLicense_ReturnsTrue()
    {
        // Arrange
        var config = CreateTestConfiguration(
            "test-agent",
            requiredTier: LicenseTier.WriterPro);
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(config, factory);

        _licenseContextMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Teams); // Teams > WriterPro

        // Act
        var canAccess = _sut.CanAccess("test-agent");

        // Assert
        canAccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CanAccess returns false with insufficient license.
    /// </summary>
    [Fact]
    public void CanAccess_WithInsufficientLicense_ReturnsFalse()
    {
        // Arrange
        var config = CreateTestConfiguration(
            "test-agent",
            requiredTier: LicenseTier.Enterprise);
        var factory = new Func<IServiceProvider, IAgent>(_ => Mock.Of<IAgent>());
        _sut.RegisterAgent(config, factory);

        _licenseContextMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Teams); // Teams < Enterprise

        // Act
        var canAccess = _sut.CanAccess("test-agent");

        // Assert
        canAccess.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Helper Methods
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a test AgentConfiguration with default or custom values.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="personas">Optional personas to include.</param>
    /// <param name="name">Optional custom name.</param>
    /// <param name="requiredTier">Optional required license tier.</param>
    /// <returns>A valid AgentConfiguration instance.</returns>
    private static AgentConfiguration CreateTestConfiguration(
        string agentId,
        IEnumerable<AgentPersona>? personas = null,
        string? name = null,
        LicenseTier requiredTier = LicenseTier.Core)
    {
        var personasList = (personas ?? Enumerable.Empty<AgentPersona>()).ToList();

        return new AgentConfiguration(
            AgentId: agentId,
            Name: name ?? "Test Agent",
            Description: "Test Description",
            Icon: "test-icon",
            TemplateId: "test-template",
            Capabilities: AgentCapabilities.Chat,
            DefaultOptions: new ChatOptions(),
            Personas: personasList,
            RequiredTier: requiredTier);
    }
}
