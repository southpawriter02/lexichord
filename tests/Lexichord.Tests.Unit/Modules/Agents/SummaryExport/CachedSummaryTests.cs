// -----------------------------------------------------------------------
// <copyright file="CachedSummaryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Agents.SummaryExport;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="CachedSummary"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class CachedSummaryTests
{
    // ── Create Factory ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParameters_CreatesCachedSummary()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var cached = CachedSummary.Create(
            "/path/to/document.md",
            "sha256:abc123",
            summary);

        // Assert
        cached.DocumentPath.Should().Be("/path/to/document.md");
        cached.ContentHash.Should().Be("sha256:abc123");
        cached.Summary.Should().BeSameAs(summary);
        cached.Metadata.Should().BeNull();
        cached.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Create_WithMetadata_IncludesMetadata()
    {
        // Arrange
        var summary = CreateTestSummary();
        var metadata = CreateTestMetadata();

        // Act
        var cached = CachedSummary.Create(
            "/path/to/document.md",
            "sha256:abc123",
            summary,
            metadata);

        // Assert
        cached.Metadata.Should().NotBeNull();
        cached.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void Create_WithExpirationDays_SetsExpiresAt()
    {
        // Arrange
        var summary = CreateTestSummary();
        var before = DateTimeOffset.UtcNow;

        // Act
        var cached = CachedSummary.Create(
            "/path/to/document.md",
            "sha256:abc123",
            summary,
            expirationDays: 14);

        // Assert
        var after = DateTimeOffset.UtcNow;
        var expectedMin = before.AddDays(14);
        var expectedMax = after.AddDays(14);

        cached.ExpiresAt.Should().BeOnOrAfter(expectedMin);
        cached.ExpiresAt.Should().BeOnOrBefore(expectedMax);
    }

    [Fact]
    public void Create_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var act = () => CachedSummary.Create(null!, "sha256:abc123", summary);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public void Create_WithNullContentHash_ThrowsArgumentNullException()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var act = () => CachedSummary.Create("/path/to/document.md", null!, summary);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contentHash");
    }

    [Fact]
    public void Create_WithNullSummary_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CachedSummary.Create("/path/to/document.md", "sha256:abc123", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("summary");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidExpirationDays_ThrowsArgumentOutOfRangeException(int days)
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var act = () => CachedSummary.Create("/path/to/document.md", "sha256:abc123", summary, expirationDays: days);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("expirationDays");
    }

    [Fact]
    public void Create_SetsCachedAtToCurrentTime()
    {
        // Arrange
        var summary = CreateTestSummary();
        var before = DateTimeOffset.UtcNow;

        // Act
        var cached = CachedSummary.Create("/path/to/document.md", "sha256:abc123", summary);

        // Assert
        var after = DateTimeOffset.UtcNow;
        cached.CachedAt.Should().BeOnOrAfter(before);
        cached.CachedAt.Should().BeOnOrBefore(after);
    }

    // ── IsExpired Property ──────────────────────────────────────────────

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        // Arrange
        var cached = new CachedSummary
        {
            DocumentPath = "/path/to/document.md",
            ContentHash = "sha256:abc123",
            Summary = CreateTestSummary(),
            CachedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Assert
        cached.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        // Arrange
        var cached = new CachedSummary
        {
            DocumentPath = "/path/to/document.md",
            ContentHash = "sha256:abc123",
            Summary = CreateTestSummary(),
            CachedAt = DateTimeOffset.UtcNow.AddDays(-8),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        // Assert
        cached.IsExpired.Should().BeTrue();
    }

    // ── Age Property ────────────────────────────────────────────────────

    [Fact]
    public void Age_ReturnsTimeSinceCachedAt()
    {
        // Arrange
        var cachedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var cached = new CachedSummary
        {
            DocumentPath = "/path/to/document.md",
            ContentHash = "sha256:abc123",
            Summary = CreateTestSummary(),
            CachedAt = cachedAt,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Act
        var age = cached.Age;

        // Assert
        age.Should().BeGreaterThanOrEqualTo(TimeSpan.FromHours(2));
        age.Should().BeLessThan(TimeSpan.FromHours(3));
    }

    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_CachedAtAndExpiresAt_SetToCurrentTimeAndSevenDays()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var cached = new CachedSummary
        {
            DocumentPath = "/path/to/document.md",
            ContentHash = "sha256:abc123",
            Summary = CreateTestSummary()
        };

        // Assert
        var after = DateTimeOffset.UtcNow;
        cached.CachedAt.Should().BeOnOrAfter(before);
        cached.CachedAt.Should().BeOnOrBefore(after);

        var expectedExpiration = cached.CachedAt.AddDays(7);
        cached.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }

    // ── Full Construction ───────────────────────────────────────────────

    [Fact]
    public void FullConstruction_SetsAllProperties()
    {
        // Arrange
        var summary = CreateTestSummary();
        var metadata = CreateTestMetadata();
        var cachedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(5);

        // Act
        var cached = new CachedSummary
        {
            DocumentPath = "/path/to/document.md",
            ContentHash = "sha256:def456",
            Summary = summary,
            Metadata = metadata,
            CachedAt = cachedAt,
            ExpiresAt = expiresAt
        };

        // Assert
        cached.DocumentPath.Should().Be("/path/to/document.md");
        cached.ContentHash.Should().Be("sha256:def456");
        cached.Summary.Should().BeSameAs(summary);
        cached.Metadata.Should().BeSameAs(metadata);
        cached.CachedAt.Should().Be(cachedAt);
        cached.ExpiresAt.Should().Be(expiresAt);
        cached.IsExpired.Should().BeFalse();
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private static SummarizationResult CreateTestSummary()
    {
        return new SummarizationResult
        {
            Summary = "This is a test summary.",
            Mode = SummarizationMode.BulletPoints,
            OriginalWordCount = 1000,
            SummaryWordCount = 100,
            Usage = UsageMetrics.Zero
        };
    }

    private static DocumentMetadata CreateTestMetadata()
    {
        return new DocumentMetadata
        {
            OneLiner = "A test document",
            KeyTerms = Array.Empty<KeyTerm>(),
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            EstimatedReadingMinutes = 5,
            ComplexityScore = 6,
            Usage = UsageMetrics.Zero
        };
    }
}
