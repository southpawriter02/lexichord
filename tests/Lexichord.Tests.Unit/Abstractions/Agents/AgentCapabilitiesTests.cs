// -----------------------------------------------------------------------
// <copyright file="AgentCapabilitiesTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents;

/// <summary>
/// Unit tests for <see cref="AgentCapabilities"/> enum and
/// <see cref="AgentCapabilitiesExtensions"/> extension methods.
/// </summary>
/// <remarks>
/// Tests cover enum values, flags behavior, HasCapability, SupportsContext,
/// and GetCapabilityNames extension methods.
/// Introduced in v0.6.6a.
/// </remarks>
public class AgentCapabilitiesTests
{
    // -----------------------------------------------------------------------
    // Enum Value Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that all expected AgentCapabilities values are defined with correct bit positions.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void AgentCapabilities_ShouldHaveExpectedValues()
    {
        // Assert — verify each flag value matches the spec
        ((int)AgentCapabilities.None).Should().Be(0);
        ((int)AgentCapabilities.Chat).Should().Be(1);
        ((int)AgentCapabilities.DocumentContext).Should().Be(2);
        ((int)AgentCapabilities.RAGContext).Should().Be(4);
        ((int)AgentCapabilities.StyleEnforcement).Should().Be(8);
        ((int)AgentCapabilities.Streaming).Should().Be(16);
    }

    /// <summary>
    /// Verifies that the All value is the combination of all individual capabilities.
    /// </summary>
    /// <remarks>
    /// Updated in v0.7.1a to include 6 new specialist agent capabilities.
    /// </remarks>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void All_ShouldBeCompositeOfAllCapabilities()
    {
        // Arrange
        var expected = AgentCapabilities.Chat |
                       AgentCapabilities.DocumentContext |
                       AgentCapabilities.RAGContext |
                       AgentCapabilities.StyleEnforcement |
                       AgentCapabilities.Streaming |
                       AgentCapabilities.CodeGeneration |
                       AgentCapabilities.ResearchAssistance |
                       AgentCapabilities.Summarization |
                       AgentCapabilities.StructureAnalysis |
                       AgentCapabilities.Brainstorming |
                       AgentCapabilities.Translation;

        // Assert
        AgentCapabilities.All.Should().Be(expected);
        ((int)AgentCapabilities.All).Should().Be(2047); // Updated from 31
    }

    /// <summary>
    /// Verifies that the Flags attribute is applied and bitwise OR works correctly.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void AgentCapabilities_ShouldSupportBitwiseCombination()
    {
        // Arrange
        var combined = AgentCapabilities.Chat | AgentCapabilities.RAGContext;

        // Assert
        ((int)combined).Should().Be(5); // 1 | 4 = 5
        combined.HasFlag(AgentCapabilities.Chat).Should().BeTrue();
        combined.HasFlag(AgentCapabilities.RAGContext).Should().BeTrue();
        combined.HasFlag(AgentCapabilities.DocumentContext).Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // HasCapability Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that HasCapability correctly identifies present and absent capabilities.
    /// </summary>
    [Theory]
    [Trait("SubPart", "v0.6.6a")]
    [InlineData(AgentCapabilities.Chat, AgentCapabilities.Chat, true)]
    [InlineData(AgentCapabilities.Chat | AgentCapabilities.RAGContext, AgentCapabilities.Chat, true)]
    [InlineData(AgentCapabilities.Chat, AgentCapabilities.RAGContext, false)]
    [InlineData(AgentCapabilities.All, AgentCapabilities.Streaming, true)]
    [InlineData(AgentCapabilities.None, AgentCapabilities.Chat, false)]
    [InlineData(AgentCapabilities.All, AgentCapabilities.All, true)]
    public void HasCapability_ReturnsCorrectResult(
        AgentCapabilities capabilities,
        AgentCapabilities check,
        bool expected)
    {
        // Act & Assert
        capabilities.HasCapability(check).Should().Be(expected);
    }

    /// <summary>
    /// Verifies that HasCapability with None always returns true (0 & 0 == 0).
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasCapability_None_AlwaysReturnsTrue()
    {
        // Assert — None (0) is always present via bitwise AND
        AgentCapabilities.None.HasCapability(AgentCapabilities.None).Should().BeTrue();
        AgentCapabilities.Chat.HasCapability(AgentCapabilities.None).Should().BeTrue();
        AgentCapabilities.All.HasCapability(AgentCapabilities.None).Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // SupportsContext Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that SupportsContext correctly identifies context-capable agents.
    /// </summary>
    [Theory]
    [Trait("SubPart", "v0.6.6a")]
    [InlineData(AgentCapabilities.Chat, false)]
    [InlineData(AgentCapabilities.Streaming, false)]
    [InlineData(AgentCapabilities.None, false)]
    [InlineData(AgentCapabilities.DocumentContext, true)]
    [InlineData(AgentCapabilities.RAGContext, true)]
    [InlineData(AgentCapabilities.StyleEnforcement, true)]
    [InlineData(AgentCapabilities.Chat | AgentCapabilities.DocumentContext, true)]
    [InlineData(AgentCapabilities.All, true)]
    public void SupportsContext_ReturnsCorrectResult(AgentCapabilities capabilities, bool expected)
    {
        // Act & Assert
        capabilities.SupportsContext().Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // GetCapabilityNames Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetCapabilityNames returns all expected names for All capabilities.
    /// </summary>
    /// <remarks>
    /// Updated in v0.7.1a to include 6 new specialist agent capability names.
    /// </remarks>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void GetCapabilityNames_All_ReturnsAllNames()
    {
        // Arrange
        var capabilities = AgentCapabilities.All;

        // Act
        var names = capabilities.GetCapabilityNames();

        // Assert
        names.Should().HaveCount(11); // Updated from 5
        names.Should().Contain("Chat");
        names.Should().Contain("Document Context");
        names.Should().Contain("RAG");
        names.Should().Contain("Style");
        names.Should().Contain("Streaming");
        names.Should().Contain("CodeGen");
        names.Should().Contain("Research");
        names.Should().Contain("Summary");
        names.Should().Contain("Structure");
        names.Should().Contain("Brainstorm");
        names.Should().Contain("Translate");
    }

    /// <summary>
    /// Verifies that GetCapabilityNames returns an empty array for None.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void GetCapabilityNames_None_ReturnsEmptyArray()
    {
        // Act
        var names = AgentCapabilities.None.GetCapabilityNames();

        // Assert
        names.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetCapabilityNames returns only the names for enabled capabilities.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void GetCapabilityNames_Subset_ReturnsOnlyEnabledNames()
    {
        // Arrange
        var capabilities = AgentCapabilities.Chat | AgentCapabilities.RAGContext;

        // Act
        var names = capabilities.GetCapabilityNames();

        // Assert
        names.Should().HaveCount(2);
        names.Should().Contain("Chat");
        names.Should().Contain("RAG");
        names.Should().NotContain("Document Context");
        names.Should().NotContain("Style");
        names.Should().NotContain("Streaming");
    }

    // -----------------------------------------------------------------------
    // v0.7.1a New Capabilities Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that new v0.7.1a capabilities have correct bit values.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.7.1a")]
    public void AgentCapabilities_NewCapabilities_HaveExpectedBitValues()
    {
        // Assert — verify each new flag value matches the spec
        ((int)AgentCapabilities.CodeGeneration).Should().Be(32);
        ((int)AgentCapabilities.ResearchAssistance).Should().Be(64);
        ((int)AgentCapabilities.Summarization).Should().Be(128);
        ((int)AgentCapabilities.StructureAnalysis).Should().Be(256);
        ((int)AgentCapabilities.Brainstorming).Should().Be(512);
        ((int)AgentCapabilities.Translation).Should().Be(1024);
    }

    /// <summary>
    /// Verifies that All flag includes all 11 capabilities (updated in v0.7.1a).
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.7.1a")]
    public void All_IncludesAllNewCapabilities()
    {
        // Arrange
        var expected = AgentCapabilities.Chat |
                       AgentCapabilities.DocumentContext |
                       AgentCapabilities.RAGContext |
                       AgentCapabilities.StyleEnforcement |
                       AgentCapabilities.Streaming |
                       AgentCapabilities.CodeGeneration |
                       AgentCapabilities.ResearchAssistance |
                       AgentCapabilities.Summarization |
                       AgentCapabilities.StructureAnalysis |
                       AgentCapabilities.Brainstorming |
                       AgentCapabilities.Translation;

        // Assert
        AgentCapabilities.All.Should().Be(expected);
        ((int)AgentCapabilities.All).Should().Be(2047); // Updated from 31
    }

    /// <summary>
    /// Verifies that GetCapabilityNames includes display names for new capabilities.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.7.1a")]
    public void GetCapabilityNames_IncludesNewCapabilityNames()
    {
        // Act
        var names = AgentCapabilities.All.GetCapabilityNames();

        // Assert
        names.Should().HaveCount(11); // Updated from 5
        names.Should().Contain("CodeGen");
        names.Should().Contain("Research");
        names.Should().Contain("Summary");
        names.Should().Contain("Structure");
        names.Should().Contain("Brainstorm");
        names.Should().Contain("Translate");
    }
}
