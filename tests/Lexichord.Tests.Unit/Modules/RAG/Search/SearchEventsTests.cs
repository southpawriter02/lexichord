// =============================================================================
// File: SearchEventsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SemanticSearchExecutedEvent and SearchDeniedEvent.
// =============================================================================
// LOGIC: Verifies record construction, property assignment, and default values
//   for the MediatR notification events used in the semantic search pipeline.
//   Both events are INotification records with required and optional properties.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Search;
using MediatR;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="SemanticSearchExecutedEvent"/> and <see cref="SearchDeniedEvent"/>.
/// Verifies record construction, property assignment, and INotification implementation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5b")]
public class SearchEventsTests
{
    #region SemanticSearchExecutedEvent — Construction Tests

    [Fact]
    public void SemanticSearchExecutedEvent_WithRequiredProperties_SetsAllValues()
    {
        // Arrange
        var query = "test query";
        var resultCount = 5;
        var duration = TimeSpan.FromMilliseconds(42);

        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = query,
            ResultCount = resultCount,
            Duration = duration
        };

        // Assert
        evt.Query.Should().Be(query, because: "the query should be set from the initializer");
        evt.ResultCount.Should().Be(resultCount, because: "the result count should be set from the initializer");
        evt.Duration.Should().Be(duration, because: "the duration should be set from the initializer");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_Timestamp_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 0,
            Duration = TimeSpan.Zero
        };

        var after = DateTimeOffset.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before, because: "the timestamp should default to approximately now");
        evt.Timestamp.Should().BeOnOrBefore(after, because: "the timestamp should default to approximately now");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_Timestamp_CanBeOverridden()
    {
        // Arrange
        var custom = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 1,
            Duration = TimeSpan.FromSeconds(1),
            Timestamp = custom
        };

        // Assert
        evt.Timestamp.Should().Be(custom, because: "an explicit timestamp should override the default");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_ImplementsINotification()
    {
        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 0,
            Duration = TimeSpan.Zero
        };

        // Assert
        evt.Should().BeAssignableTo<INotification>(
            because: "search events are MediatR fire-and-forget notifications");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_ZeroResultCount_IsValid()
    {
        // Act
        var evt = new SemanticSearchExecutedEvent
        {
            Query = "no results query",
            ResultCount = 0,
            Duration = TimeSpan.FromMilliseconds(10)
        };

        // Assert
        evt.ResultCount.Should().Be(0, because: "zero results is a valid search outcome");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(50);

        var evt1 = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 3,
            Duration = duration,
            Timestamp = timestamp
        };

        var evt2 = new SemanticSearchExecutedEvent
        {
            Query = "test",
            ResultCount = 3,
            Duration = duration,
            Timestamp = timestamp
        };

        // Assert
        evt1.Should().Be(evt2, because: "records with identical properties should be equal");
    }

    [Fact]
    public void SemanticSearchExecutedEvent_RecordInequality_DifferentQuery()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var evt1 = new SemanticSearchExecutedEvent
        {
            Query = "query A",
            ResultCount = 3,
            Duration = TimeSpan.Zero,
            Timestamp = timestamp
        };

        var evt2 = new SemanticSearchExecutedEvent
        {
            Query = "query B",
            ResultCount = 3,
            Duration = TimeSpan.Zero,
            Timestamp = timestamp
        };

        // Assert
        evt1.Should().NotBe(evt2, because: "records with different queries should not be equal");
    }

    #endregion

    #region SearchDeniedEvent — Construction Tests

    [Fact]
    public void SearchDeniedEvent_WithRequiredProperties_SetsAllValues()
    {
        // Arrange
        var currentTier = LicenseTier.Core;
        var requiredTier = LicenseTier.WriterPro;
        var featureName = "Semantic Search";

        // Act
        var evt = new SearchDeniedEvent
        {
            CurrentTier = currentTier,
            RequiredTier = requiredTier,
            FeatureName = featureName
        };

        // Assert
        evt.CurrentTier.Should().Be(currentTier, because: "the current tier should be set from the initializer");
        evt.RequiredTier.Should().Be(requiredTier, because: "the required tier should be set from the initializer");
        evt.FeatureName.Should().Be(featureName, because: "the feature name should be set from the initializer");
    }

    [Fact]
    public void SearchDeniedEvent_Timestamp_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = new SearchDeniedEvent
        {
            CurrentTier = LicenseTier.Core,
            RequiredTier = LicenseTier.WriterPro,
            FeatureName = "Semantic Search"
        };

        var after = DateTimeOffset.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before, because: "the timestamp should default to approximately now");
        evt.Timestamp.Should().BeOnOrBefore(after, because: "the timestamp should default to approximately now");
    }

    [Fact]
    public void SearchDeniedEvent_Timestamp_CanBeOverridden()
    {
        // Arrange
        var custom = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var evt = new SearchDeniedEvent
        {
            CurrentTier = LicenseTier.Core,
            RequiredTier = LicenseTier.WriterPro,
            FeatureName = "Semantic Search",
            Timestamp = custom
        };

        // Assert
        evt.Timestamp.Should().Be(custom, because: "an explicit timestamp should override the default");
    }

    [Fact]
    public void SearchDeniedEvent_ImplementsINotification()
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
            because: "denial events are MediatR fire-and-forget notifications");
    }

    [Fact]
    public void SearchDeniedEvent_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var evt1 = new SearchDeniedEvent
        {
            CurrentTier = LicenseTier.Core,
            RequiredTier = LicenseTier.WriterPro,
            FeatureName = "Semantic Search",
            Timestamp = timestamp
        };

        var evt2 = new SearchDeniedEvent
        {
            CurrentTier = LicenseTier.Core,
            RequiredTier = LicenseTier.WriterPro,
            FeatureName = "Semantic Search",
            Timestamp = timestamp
        };

        // Assert
        evt1.Should().Be(evt2, because: "records with identical properties should be equal");
    }

    [Theory]
    [InlineData(LicenseTier.Core)]
    public void SearchDeniedEvent_VariousTiers_CorrectlyRecorded(LicenseTier tier)
    {
        // Act
        var evt = new SearchDeniedEvent
        {
            CurrentTier = tier,
            RequiredTier = LicenseTier.WriterPro,
            FeatureName = "Semantic Search"
        };

        // Assert
        evt.CurrentTier.Should().Be(tier,
            because: "the denied event should record the exact tier that was insufficient");
    }

    #endregion
}
