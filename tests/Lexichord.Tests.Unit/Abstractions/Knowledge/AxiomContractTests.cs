// =============================================================================
// File: AxiomContractTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomFilter and AxiomStatistics records.
// =============================================================================
// LOGIC: Tests record defaults, immutability, and equality.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge;

/// <summary>
/// Unit tests for <see cref="AxiomFilter"/> and <see cref="AxiomStatistics"/> records.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6f")]
public class AxiomContractTests
{
    #region AxiomFilter Tests

    [Fact]
    public void AxiomFilter_DefaultValues_AllNull()
    {
        // Arrange & Act
        var filter = new AxiomFilter();

        // Assert
        Assert.Null(filter.TargetType);
        Assert.Null(filter.Category);
        Assert.Null(filter.Tags);
        Assert.Null(filter.IsEnabled);
    }

    [Fact]
    public void AxiomFilter_WithAllFields_Populated()
    {
        // Arrange
        var tags = new List<string> { "api", "v2" };

        // Act
        var filter = new AxiomFilter
        {
            TargetType = "Endpoint",
            Category = "API Documentation",
            Tags = tags,
            IsEnabled = true
        };

        // Assert
        Assert.Equal("Endpoint", filter.TargetType);
        Assert.Equal("API Documentation", filter.Category);
        Assert.Equal(2, filter.Tags!.Count);
        Assert.True(filter.IsEnabled);
    }

    [Fact]
    public void AxiomFilter_Equality_SameValues()
    {
        // Arrange
        var filter1 = new AxiomFilter { TargetType = "Endpoint", IsEnabled = true };
        var filter2 = new AxiomFilter { TargetType = "Endpoint", IsEnabled = true };

        // Assert
        Assert.Equal(filter1, filter2);
    }

    [Fact]
    public void AxiomFilter_Inequality_DifferentValues()
    {
        // Arrange
        var filter1 = new AxiomFilter { TargetType = "Endpoint" };
        var filter2 = new AxiomFilter { TargetType = "Concept" };

        // Assert
        Assert.NotEqual(filter1, filter2);
    }

    #endregion

    #region AxiomStatistics Tests

    [Fact]
    public void AxiomStatistics_WithRequiredFields_CreatesSuccessfully()
    {
        // Arrange & Act
        var stats = new AxiomStatistics
        {
            TotalCount = 100,
            EnabledCount = 85,
            DisabledCount = 15,
            ByCategory = new Dictionary<string, int>
            {
                ["API Documentation"] = 50,
                ["Security"] = 30,
                [""] = 20  // Uncategorized
            },
            ByTargetType = new Dictionary<string, int>
            {
                ["Endpoint"] = 60,
                ["Concept"] = 40
            }
        };

        // Assert
        Assert.Equal(100, stats.TotalCount);
        Assert.Equal(85, stats.EnabledCount);
        Assert.Equal(15, stats.DisabledCount);
        Assert.Equal(3, stats.ByCategory.Count);
        Assert.Equal(2, stats.ByTargetType.Count);
    }

    [Fact]
    public void AxiomStatistics_Equality_SameValues()
    {
        // Arrange
        var byCategory = new Dictionary<string, int> { ["API"] = 10 };
        var byType = new Dictionary<string, int> { ["Endpoint"] = 10 };

        var stats1 = new AxiomStatistics
        {
            TotalCount = 10,
            EnabledCount = 10,
            DisabledCount = 0,
            ByCategory = byCategory,
            ByTargetType = byType
        };

        var stats2 = new AxiomStatistics
        {
            TotalCount = 10,
            EnabledCount = 10,
            DisabledCount = 0,
            ByCategory = byCategory,
            ByTargetType = byType
        };

        // Assert (Note: Dictionary reference equality, not value equality)
        Assert.Equal(stats1.TotalCount, stats2.TotalCount);
        Assert.Equal(stats1.EnabledCount, stats2.EnabledCount);
        Assert.Equal(stats1.DisabledCount, stats2.DisabledCount);
    }

    [Fact]
    public void AxiomStatistics_Counts_Match()
    {
        // Arrange
        var stats = new AxiomStatistics
        {
            TotalCount = 100,
            EnabledCount = 75,
            DisabledCount = 25,
            ByCategory = new Dictionary<string, int>(),
            ByTargetType = new Dictionary<string, int>()
        };

        // Assert: Total = Enabled + Disabled
        Assert.Equal(stats.TotalCount, stats.EnabledCount + stats.DisabledCount);
    }

    #endregion
}
