// -----------------------------------------------------------------------
// <copyright file="ContextStrategyBaseTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextStrategyBase"/> abstract class.
/// </summary>
/// <remarks>
/// Tests verify the base class helper methods via a test implementation.
/// Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class ContextStrategyBaseTests
{
    #region Test Implementation

    /// <summary>
    /// Concrete test implementation of ContextStrategyBase for testing.
    /// </summary>
    public sealed class TestContextStrategy : ContextStrategyBase
    {
        public TestContextStrategy(ITokenCounter tokenCounter, ILogger<TestContextStrategy> logger)
            : base(tokenCounter, logger)
        {
        }

        public override string StrategyId => "test";

        public override string DisplayName => "Test Strategy";

        public override int Priority => StrategyPriority.Medium;

        public override int MaxTokens => 1000;

        public override Task<ContextFragment?> GatherAsync(
            ContextGatheringRequest request,
            CancellationToken cancellationToken = default)
        {
            // Simple test implementation
            if (!ValidateRequest(request, requireDocument: true))
                return Task.FromResult<ContextFragment?>(null);

            var content = $"Test content from {request.DocumentPath}";
            var fragment = CreateFragment(content, relevance: 0.8f);
            return Task.FromResult<ContextFragment?>(fragment);
        }

        // Expose protected methods for testing
        public ContextFragment TestCreateFragment(string content, float relevance = 1.0f, string? customLabel = null)
            => CreateFragment(content, relevance, customLabel);

        public string TestTruncateToMaxTokens(string content)
            => TruncateToMaxTokens(content);

        public bool TestValidateRequest(
            ContextGatheringRequest request,
            bool requireDocument = false,
            bool requireSelection = false,
            bool requireCursor = false)
            => ValidateRequest(request, requireDocument, requireSelection, requireCursor);
    }

    #endregion

    #region Helper Factories

    private static TestContextStrategy CreateStrategy(
        ITokenCounter? tokenCounter = null,
        ILogger<TestContextStrategy>? logger = null)
    {
        tokenCounter ??= Substitute.For<ITokenCounter>();
        logger ??= Substitute.For<ILogger<TestContextStrategy>>();
        return new TestContextStrategy(tokenCounter, logger);
    }

    #endregion

    #region CreateFragment Tests

    /// <summary>
    /// Verifies that CreateFragment creates a fragment with correct properties.
    /// </summary>
    [Fact]
    public void CreateFragment_CreatesFragmentWithCorrectProperties()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(42);
        var sut = CreateStrategy(tokenCounter);

        // Act
        var result = sut.TestCreateFragment("Test content", relevance: 0.8f);

        // Assert
        result.SourceId.Should().Be("test");
        result.Label.Should().Be("Test Strategy");
        result.Content.Should().Be("Test content");
        result.TokenEstimate.Should().Be(42);
        result.Relevance.Should().Be(0.8f);
    }

    /// <summary>
    /// Verifies that CreateFragment uses custom label when provided.
    /// </summary>
    [Fact]
    public void CreateFragment_WithCustomLabel_UsesCustomLabel()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(42);
        var sut = CreateStrategy(tokenCounter);

        // Act
        var result = sut.TestCreateFragment("Test content", customLabel: "Custom Label");

        // Assert
        result.Label.Should().Be("Custom Label");
    }

    /// <summary>
    /// Verifies that CreateFragment calls token counter.
    /// </summary>
    [Fact]
    public void CreateFragment_CallsTokenCounter()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(42);
        var sut = CreateStrategy(tokenCounter);

        // Act
        sut.TestCreateFragment("Test content");

        // Assert
        tokenCounter.Received(1).CountTokens("Test content");
    }

    /// <summary>
    /// Verifies that CreateFragment logs debug message.
    /// </summary>
    [Fact]
    public void CreateFragment_LogsDebugMessage()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(42);
        var logger = Substitute.For<ILogger<TestContextStrategy>>();
        var sut = CreateStrategy(tokenCounter, logger);

        // Act
        sut.TestCreateFragment("Test content");

        // Assert
        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("test") && o.ToString()!.Contains("42")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region TruncateToMaxTokens Tests

    /// <summary>
    /// Verifies that TruncateToMaxTokens returns content unchanged when within limit.
    /// </summary>
    [Fact]
    public void TruncateToMaxTokens_WithinLimit_ReturnsUnchanged()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(500); // Within 1000 limit
        var sut = CreateStrategy(tokenCounter);

        // Act
        var result = sut.TestTruncateToMaxTokens("Test content");

        // Assert
        result.Should().Be("Test content");
    }

    /// <summary>
    /// Verifies that TruncateToMaxTokens truncates when over limit.
    /// </summary>
    [Fact]
    public void TruncateToMaxTokens_OverLimit_Truncates()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(callInfo =>
        {
            var text = callInfo.Arg<string>();
            // Simulate realistic token counts per paragraph
            if (text.Contains("Paragraph 3")) return 1500; // Full text
            if (text == "Paragraph 1") return 400;
            if (text == "Paragraph 2") return 450;
            if (text == "Paragraph 3") return 600;
            return text.Length / 4; // Fallback: rough estimate
        });

        var sut = CreateStrategy(tokenCounter);
        var content = "Paragraph 1\n\nParagraph 2\n\nParagraph 3";

        // Act
        var result = sut.TestTruncateToMaxTokens(content);

        // Assert
        result.Should().NotBe(content);
        result.Should().Contain("Paragraph 1");
        result.Should().Contain("Paragraph 2");
        result.Should().NotContain("Paragraph 3");
    }

    /// <summary>
    /// Verifies that TruncateToMaxTokens logs warning when truncating.
    /// </summary>
    [Fact]
    public void TruncateToMaxTokens_WhenTruncating_LogsWarning()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(1500); // Over 1000 limit
        tokenCounter.TruncateToTokenLimit(Arg.Any<string>(), Arg.Any<int>()).Returns(("Truncated", true));

        var logger = Substitute.For<ILogger<TestContextStrategy>>();
        var sut = CreateStrategy(tokenCounter, logger);

        // Act
        sut.TestTruncateToMaxTokens("Test content");

        // Assert
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("1500") && o.ToString()!.Contains("1000")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region ValidateRequest Tests

    /// <summary>
    /// Verifies that ValidateRequest returns true when no requirements.
    /// </summary>
    [Fact]
    public void ValidateRequest_NoRequirements_ReturnsTrue()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, null, "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns true when document required and present.
    /// </summary>
    [Fact]
    public void ValidateRequest_RequireDocument_WithDocument_ReturnsTrue()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest("/path/to/doc.md", null, null, "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireDocument: true);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns false when document required but missing.
    /// </summary>
    [Fact]
    public void ValidateRequest_RequireDocument_WithoutDocument_ReturnsFalse()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, null, "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireDocument: true);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns true when selection required and present.
    /// </summary>
    [Fact]
    public void ValidateRequest_RequireSelection_WithSelection_ReturnsTrue()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, "Selected text", "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireSelection: true);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns false when selection required but missing.
    /// </summary>
    [Fact]
    public void ValidateRequest_RequireSelection_WithoutSelection_ReturnsFalse()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, null, "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireSelection: true);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns true when cursor required and present.
    /// </summary>
    [Fact]
    public void ValidateRequest_RequireCursor_WithCursor_ReturnsTrue()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, 42, null, "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireCursor: true);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns false when cursor required but missing.
    /// </summary>
    [Fact]
    public void ValidateRequest_RequireCursor_WithoutCursor_ReturnsFalse()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, null, "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireCursor: true);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns true when all requirements met.
    /// </summary>
    [Fact]
    public void ValidateRequest_AllRequirements_AllPresent_ReturnsTrue()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest("/path/to/doc.md", 42, "Selected text", "test-agent", null);

        // Act
        var result = sut.TestValidateRequest(request, requireDocument: true, requireSelection: true, requireCursor: true);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateRequest returns false when any requirement missing.
    /// </summary>
    [Fact]
    public void ValidateRequest_AllRequirements_OneMissing_ReturnsFalse()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest("/path/to/doc.md", 42, null, "test-agent", null); // Missing selection

        // Act
        var result = sut.TestValidateRequest(request, requireDocument: true, requireSelection: true, requireCursor: true);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateRequest logs debug message when validation fails.
    /// </summary>
    [Fact]
    public void ValidateRequest_WhenFails_LogsDebugMessage()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestContextStrategy>>();
        var sut = CreateStrategy(logger: logger);
        var request = new ContextGatheringRequest(null, null, null, "test-agent", null);

        // Act
        sut.TestValidateRequest(request, requireDocument: true);

        // Assert
        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("test") && o.ToString()!.Contains("document")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region GatherAsync Tests

    /// <summary>
    /// Verifies that GatherAsync returns null when validation fails.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ValidationFails_ReturnsNull()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        var sut = CreateStrategy(tokenCounter);
        var request = new ContextGatheringRequest(null, null, null, "test-agent", null); // No document

        // Act
        var result = await sut.GatherAsync(request);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GatherAsync returns fragment when validation passes.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ValidationPasses_ReturnsFragment()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        tokenCounter.CountTokens(Arg.Any<string>()).Returns(42);
        var sut = CreateStrategy(tokenCounter);
        var request = new ContextGatheringRequest("/path/to/doc.md", null, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("test");
        result.Content.Should().Contain("/path/to/doc.md");
        result.Relevance.Should().Be(0.8f);
    }

    #endregion
}
