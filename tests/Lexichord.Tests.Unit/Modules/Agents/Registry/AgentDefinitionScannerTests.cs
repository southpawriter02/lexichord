// -----------------------------------------------------------------------
// <copyright file="AgentDefinitionScannerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Reflection.Emit;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Registry;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Registry;

/// <summary>
/// Unit tests for <see cref="AgentDefinitionScanner"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates the scanner's behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Discovery of [AgentDefinition] decorated types</description></item>
///   <item><description>Validation of IAgent implementation</description></item>
///   <item><description>Priority-based ordering</description></item>
///   <item><description>Invalid type handling</description></item>
///   <item><description>Empty assembly handling</description></item>
/// </list>
/// <para>
/// <b>Spec reference:</b> LCS-DES-v0.7.1b §5
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1b")]
public class AgentDefinitionScannerTests
{
    private readonly Mock<ILogger<AgentDefinitionScanner>> _loggerMock;
    private readonly AgentDefinitionScanner _sut;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    public AgentDefinitionScannerTests()
    {
        _loggerMock = new Mock<ILogger<AgentDefinitionScanner>>();
        _sut = new AgentDefinitionScanner(_loggerMock.Object);
    }

    // -----------------------------------------------------------------------
    // Test Agent Implementations
    // -----------------------------------------------------------------------

    /// <summary>
    /// Valid test agent decorated with [AgentDefinition].
    /// </summary>
    [AgentDefinition("test-agent-1", Priority = 50)]
    public class TestAgent1 : IAgent
    {
        public string AgentId => "test-agent-1";
        public string Name => "Test Agent 1";
        public string Description => "Test Description 1";
        public AgentCapabilities Capabilities => AgentCapabilities.Chat;
        public IPromptTemplate Template => null!;
        public Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(AgentResponse.Empty);
    }

    /// <summary>
    /// Valid test agent with different priority.
    /// </summary>
    [AgentDefinition("test-agent-2", Priority = 100)]
    public class TestAgent2 : IAgent
    {
        public string AgentId => "test-agent-2";
        public string Name => "Test Agent 2";
        public string Description => "Test Description 2";
        public AgentCapabilities Capabilities => AgentCapabilities.Chat;
        public IPromptTemplate Template => null!;
        public Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(AgentResponse.Empty);
    }

    /// <summary>
    /// Valid test agent with lower priority (should be first).
    /// </summary>
    [AgentDefinition("test-agent-3", Priority = 10)]
    public class TestAgent3 : IAgent
    {
        public string AgentId => "test-agent-3";
        public string Name => "Test Agent 3";
        public string Description => "Test Description 3";
        public AgentCapabilities Capabilities => AgentCapabilities.Chat;
        public IPromptTemplate Template => null!;
        public Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(AgentResponse.Empty);
    }

    /// <summary>
    /// Invalid test type - decorated but doesn't implement IAgent.
    /// </summary>
    [AgentDefinition("invalid-agent")]
    public class InvalidAgent
    {
        // Does NOT implement IAgent
    }

    /// <summary>
    /// Valid test type but not decorated with [AgentDefinition].
    /// </summary>
    public class UndecoratedAgent : IAgent
    {
        public string AgentId => "undecorated";
        public string Name => "Undecorated";
        public string Description => "Not decorated";
        public AgentCapabilities Capabilities => AgentCapabilities.Chat;
        public IPromptTemplate Template => null!;
        public Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(AgentResponse.Empty);
    }

    // -----------------------------------------------------------------------
    // ScanAssemblies Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that ScanAssemblies finds types decorated with [AgentDefinition].
    /// </summary>
    [Fact]
    public void ScanAssemblies_WithDecoratedTypes_ReturnsDefinitions()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var results = _sut.ScanAssemblies(assembly).ToList();

        // Assert - Should find at least our 3 test agents
        results.Should().NotBeEmpty();
        results.Should().Contain(d => d.AgentId == "test-agent-1");
        results.Should().Contain(d => d.AgentId == "test-agent-2");
        results.Should().Contain(d => d.AgentId == "test-agent-3");
    }

    /// <summary>
    /// Verifies that ScanAssemblies validates IAgent implementation.
    /// </summary>
    [Fact]
    public void ScanAssemblies_WithInvalidType_SkipsAndLogsWarning()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var results = _sut.ScanAssemblies(assembly).ToList();

        // Assert - InvalidAgent should be skipped
        results.Should().NotContain(d => d.AgentId == "invalid-agent");

        // Verify warning was logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("does not implement IAgent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that ScanAssemblies orders results by priority (ascending).
    /// </summary>
    [Fact]
    public void ScanAssemblies_WithMultipleTypes_OrdersByPriority()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var results = _sut.ScanAssemblies(assembly).ToList();

        // Assert - Find our test agents
        var testAgents = results
            .Where(d => d.AgentId.StartsWith("test-agent-"))
            .ToList();

        testAgents.Should().HaveCountGreaterOrEqualTo(3);

        // Verify priority ordering (lower priority first)
        var agent3 = testAgents.First(d => d.AgentId == "test-agent-3");
        var agent1 = testAgents.First(d => d.AgentId == "test-agent-1");
        var agent2 = testAgents.First(d => d.AgentId == "test-agent-2");

        agent3.Priority.Should().Be(10);
        agent1.Priority.Should().Be(50);
        agent2.Priority.Should().Be(100);

        // Verify that agent3 comes before agent1, and agent1 before agent2
        var index3 = testAgents.IndexOf(agent3);
        var index1 = testAgents.IndexOf(agent1);
        var index2 = testAgents.IndexOf(agent2);

        index3.Should().BeLessThan(index1);
        index1.Should().BeLessThan(index2);
    }

    /// <summary>
    /// Verifies that ScanAssemblies skips undecorated types.
    /// </summary>
    [Fact]
    public void ScanAssemblies_WithUndecoratedType_SkipsType()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var results = _sut.ScanAssemblies(assembly).ToList();

        // Assert - UndecoratedAgent should not be included
        results.Should().NotContain(d => d.AgentId == "undecorated");
    }

    /// <summary>
    /// Verifies that ScanAssemblies handles empty assemblies gracefully.
    /// </summary>
    [Fact]
    public void ScanAssemblies_WithEmptyAssembly_ReturnsEmpty()
    {
        // Arrange - Create an in-memory assembly with no types
        var assemblyName = new AssemblyName("EmptyTestAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);

        // Act
        var results = _sut.ScanAssemblies(assemblyBuilder).ToList();

        // Assert - Should return empty collection, not throw
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ScanAssemblies logs info for discovered agents.
    /// </summary>
    [Fact]
    public void ScanAssemblies_WithDecoratedTypes_LogsDiscovery()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var results = _sut.ScanAssemblies(assembly).ToList();

        // Assert - Verify debug logging occurred for each discovered agent
        foreach (var result in results.Where(d => d.AgentId.StartsWith("test-agent-")))
        {
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Found agent definition: {result.AgentId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
