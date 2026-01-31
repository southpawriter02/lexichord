// <copyright file="VoiceAnalyzerTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Unit tests for <see cref="VoiceAnalyzer"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.7b")]
public class VoiceAnalyzerTests
{
    private readonly Mock<IPassiveVoiceDetector> _passiveVoiceDetectorMock = new();
    private readonly Mock<IWeakWordScanner> _weakWordScannerMock = new();
    private readonly Mock<IVoiceProfileService> _voiceProfileServiceMock = new();
    private readonly ILogger<VoiceAnalyzer> _logger =
        NullLogger<VoiceAnalyzer>.Instance;

    private VoiceAnalyzer CreateAnalyzer() =>
        new(
            _passiveVoiceDetectorMock.Object,
            _weakWordScannerMock.Object,
            _voiceProfileServiceMock.Object,
            _logger);

    private static VoiceProfile CreateTestProfile() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            FlagAdverbs = true,
            FlagWeaselWords = true
        };

    [Fact]
    public async Task AnalyzeAsync_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var analyzer = CreateAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync("");

        // Assert
        result.Should().Be(VoiceAnalysisResult.Empty);
    }

    [Fact]
    public async Task AnalyzeAsync_WhitespaceContent_ReturnsEmpty()
    {
        // Arrange
        var analyzer = CreateAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync("   \t\n  ");

        // Assert
        result.Should().Be(VoiceAnalysisResult.Empty);
    }

    [Fact]
    public async Task AnalyzeAsync_CombinesPassiveAndWeakWordResults()
    {
        // Arrange
        var passiveMatches = new[]
        {
            new PassiveVoiceMatch(
                "The document was written.",
                "was written",
                PassiveType.ToBe,
                0.85,
                0,
                26)
        };
        var weakWordStats = new WeakWordStats(
            TotalWords: 50,
            TotalWeakWords: 3,
            CountByCategory: new Dictionary<WeakWordCategory, int>
            {
                { WeakWordCategory.Adverb, 2 },
                { WeakWordCategory.Filler, 1 }
            },
            Matches: []);

        var profile = CreateTestProfile();

        _voiceProfileServiceMock
            .Setup(s => s.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _passiveVoiceDetectorMock
            .Setup(d => d.Detect(It.IsAny<string>()))
            .Returns(passiveMatches);

        _weakWordScannerMock
            .Setup(s => s.GetStatistics(It.IsAny<string>(), profile))
            .Returns(weakWordStats);

        var analyzer = CreateAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync("The document was written very quickly.");

        // Assert
        result.PassiveVoiceMatches.Should().HaveCount(1);
        result.WeakWordStats.Should().NotBeNull();
        result.WeakWordStats!.TotalWeakWords.Should().Be(3);
        result.TotalIssueCount.Should().Be(4); // 1 passive + 3 weak words
    }

    [Fact]
    public async Task AnalyzeAsync_CancelledBeforeStart_ThrowsOperationCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var analyzer = CreateAnalyzer();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => analyzer.AnalyzeAsync("Test content", cts.Token));
    }

    [Fact]
    public async Task AnalyzeAsync_UsesActiveVoiceProfile()
    {
        // Arrange
        var profile = CreateTestProfile();
        VoiceProfile? capturedProfile = null;

        _voiceProfileServiceMock
            .Setup(s => s.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _passiveVoiceDetectorMock
            .Setup(d => d.Detect(It.IsAny<string>()))
            .Returns([]);

        _weakWordScannerMock
            .Setup(s => s.GetStatistics(It.IsAny<string>(), It.IsAny<VoiceProfile>()))
            .Callback<string, VoiceProfile>((_, p) => capturedProfile = p)
            .Returns(new WeakWordStats(100, 0, new Dictionary<WeakWordCategory, int>(), []));

        var analyzer = CreateAnalyzer();

        // Act
        await analyzer.AnalyzeAsync("Test content");

        // Assert
        capturedProfile.Should().NotBeNull();
        capturedProfile!.Name.Should().Be(profile.Name);
    }
}
