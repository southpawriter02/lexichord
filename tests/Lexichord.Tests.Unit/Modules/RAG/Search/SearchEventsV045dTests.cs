// =============================================================================
// File: SearchEventsV045dTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for v0.4.5d enhancements to search events:
//              UsedCachedEmbedding property on SemanticSearchExecutedEvent.
// =============================================================================
// LOGIC: Verifies the v0.4.5d additions to SemanticSearchExecutedEvent:
//   - UsedCachedEmbedding defaults to false when not specified.
//   - UsedCachedEmbedding can be explicitly set to true.
//   - Record equality includes UsedCachedEmbedding.
//   - Record inequality when only UsedCachedEmbedding differs.
//   - All properties (including new) can be set together.
//   - SearchDeniedEvent remains unchanged and still functions.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Search;
using MediatR;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for v0.4.5d enhancements to <see cref="SemanticSearchExecutedEvent"/>.
/// Verifies the <see cref="SemanticSearchExecutedEvent.UsedCachedEmbedding"/> property.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5d")]
public class SearchEventsV045dTests
{
    #region SemanticSearchExecutedEvent — UsedCachedEmbedding Tests

    [Fact]
    public void SemanticSearchExecutedEvent_UsedCachedEmbedding_DefaultsFalse()
    {
        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "test query",
            ResultCount = 5,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        evt.UsedCachedEmbedding.Should().BeFalse(
            because: "UsedCachedEmbedding should default to false when not specified");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_UsedCachedEmbedding_CanBeSetTrue()
    {
        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "test query",
            ResultCount = 3,
            Duration = TimeSpan.FromMilliseconds(50),
            UsedCachedEmbedding = true
        };

        // Assert
        evt.UsedCachedEmbedding.Should().BeTrue(
            because: "UsedCachedEmbedding should be settable to true via init property");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_WithCacheHit_RecordEquality()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(42);

        var evt1 = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 2,
            Duration = duration,
            Timestamp = timestamp,
            UsedCachedEmbedding = true
        };

        var evt2 = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 2,
            Duration = duration,
            Timestamp = timestamp,
            UsedCachedEmbedding = true
        };

        // Assert
        evt1.Should().Be(evt2,
            because: "records with identical properties (including UsedCachedEmbedding) should be equal");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_WithDifferentCacheFlag_NotEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(42);

        var evt1 = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 2,
            Duration = duration,
            Timestamp = timestamp,
            UsedCachedEmbedding = false
        };

        var evt2 = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 2,
            Duration = duration,
            Timestamp = timestamp,
            UsedCachedEmbedding = true
        };

        // Assert
        evt1.Should().NotBe(evt2,
            because: "records differing only in UsedCachedEmbedding should not be equal");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_AllProperties_SetCorrectly()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "semantic search test",
            ResultCount = 10,
            Duration = duration,
            Timestamp = timestamp,
            UsedCachedEmbedding = true
        };

        // Assert
        evt.Query.Should().Be("semantic search test",
            because: "the query property should be set from the initializer");
        evt.ResultCount.Should().Be(10,
            because: "the result count should be set from the initializer");
        evt.Duration.Should().Be(duration,
            because: "the duration should be set from the initializer");
        evt.Timestamp.Should().Be(timestamp,
            because: "the timestamp should be set from the initializer");
        evt.UsedCachedEmbedding.Should().BeTrue(
            because: "the cache flag should be set from the initializer");
    }

    #endregion

    #region SearchDeniedEvent — Unchanged from v0.4.5b

    [Fact]
    public void SearchDeniedEvent_UnchangedFromV045b_StillWorks()
    {
        // Act
        var evt = new SearchDeniedEvent
        {
            CurrentTier = LicenseTier.Core,
            RequiredTier = LicenseTier.WriterPro,
            FeatureName = "Semantic Search"
        };

        // Assert
        evt.Should().BeAssignableTo<INotification>(
            because: "SearchDeniedEvent should still implement INotification after v0.4.5d changes");
        evt.CurrentTier.Should().Be(LicenseTier.Core,
            because: "the denied event should still correctly record the current tier");
        evt.RequiredTier.Should().Be(LicenseTier.WriterPro,
            because: "the denied event should still correctly record the required tier");
    }

    #endregion
}
