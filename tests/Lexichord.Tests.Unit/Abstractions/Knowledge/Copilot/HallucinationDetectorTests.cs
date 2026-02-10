// =============================================================================
// File: HallucinationDetectorTests.cs
// Tests: Lexichord.Modules.Knowledge.Copilot.Validation.HallucinationDetector
// Feature: v0.6.6g
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Copilot;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.6g")]
public class HallucinationDetectorTests
{
    private readonly IHallucinationDetector _detector;

    public HallucinationDetectorTests()
    {
        var detectorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.HallucinationDetector")!;
        var loggerType = typeof(Logger<>).MakeGenericType(detectorType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance);

        _detector = (IHallucinationDetector)Activator.CreateInstance(
            detectorType,
            logger)!;
    }

    private static KnowledgeContext ContextWithEntities(params KnowledgeEntity[] entities) =>
        new()
        {
            Entities = entities,
            FormattedContext = "test context",
            TokenCount = 100
        };

    // =========================================================================
    // DetectAsync Tests
    // =========================================================================

    [Fact]
    public async Task DetectAsync_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity { Type = "Endpoint", Name = "GET /api/users" });

        // Act
        var findings = await _detector.DetectAsync("", context);

        // Assert
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectAsync_EmptyContext_ReturnsEmpty()
    {
        // Arrange
        var context = KnowledgeContext.Empty;

        // Act
        var findings = await _detector.DetectAsync("Some content here.", context);

        // Assert
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectAsync_NoContradictions_ReturnsEmpty()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity
            {
                Type = "Endpoint",
                Name = "GET /api/users",
                Properties = new Dictionary<string, object>
                {
                    ["method"] = "GET",
                    ["path"] = "/api/users"
                }
            });

        // Act
        var findings = await _detector.DetectAsync(
            "The GET /api/users endpoint returns user data.",
            context);

        // Assert
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectAsync_ContradictoryValue_ReturnsFinding()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity
            {
                Type = "Parameter",
                Name = "limit",
                Properties = new Dictionary<string, object>
                {
                    ["default"] = "10"
                }
            });

        // Act — content says value is "20" but context says "10"
        var findings = await _detector.DetectAsync(
            "limit has default: 20",
            context);

        // Assert
        findings.Should().ContainSingle();
        findings[0].Type.Should().Be(HallucinationType.ContradictoryValue);
        findings[0].Confidence.Should().Be(0.8f);
        findings[0].SuggestedCorrection.Should().NotBeNull();
        findings[0].Location.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectAsync_MatchingValue_NoFinding()
    {
        // Arrange
        var context = ContextWithEntities(
            new KnowledgeEntity
            {
                Type = "Parameter",
                Name = "limit",
                Properties = new Dictionary<string, object>
                {
                    ["default"] = "10"
                }
            });

        // Act — content matches context value
        var findings = await _detector.DetectAsync(
            "limit has default: 10",
            context);

        // Assert
        findings.Should().BeEmpty();
    }

    // =========================================================================
    // LevenshteinDistance Tests
    // =========================================================================

    [Fact]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        // Use reflection to access internal static method
        var detectorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.HallucinationDetector")!;

        var method = detectorType.GetMethod("LevenshteinDistance",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public)!;

        var distance = (int)method.Invoke(null, ["hello", "hello"])!;
        distance.Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_SingleCharDiff_ReturnsOne()
    {
        var detectorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.HallucinationDetector")!;

        var method = detectorType.GetMethod("LevenshteinDistance",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public)!;

        var distance = (int)method.Invoke(null, ["hello", "hallo"])!;
        distance.Should().Be(1);
    }

    [Fact]
    public void FindClosestMatch_CloseMatch_ReturnsCandidate()
    {
        var detectorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.HallucinationDetector")!;

        var method = detectorType.GetMethod("FindClosestMatch",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public)!;

        var candidates = new HashSet<string> { "users", "orders", "products" };
        var match = (string?)method.Invoke(null, ["usres", candidates]);

        match.Should().Be("users");
    }

    [Fact]
    public void FindClosestMatch_NoCloseMatch_ReturnsNull()
    {
        var detectorType = typeof(Lexichord.Modules.Knowledge.KnowledgeModule).Assembly
            .GetType("Lexichord.Modules.Knowledge.Copilot.Validation.HallucinationDetector")!;

        var method = detectorType.GetMethod("FindClosestMatch",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public)!;

        var candidates = new HashSet<string> { "users", "orders", "products" };
        var match = (string?)method.Invoke(null, ["completely_different_word", candidates]);

        match.Should().BeNull();
    }
}
