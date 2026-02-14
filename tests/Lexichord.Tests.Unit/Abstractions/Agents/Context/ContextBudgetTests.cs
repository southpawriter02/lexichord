// -----------------------------------------------------------------------
// <copyright file="ContextBudgetTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextBudget"/> record.
/// </summary>
/// <remarks>
/// Tests cover factory methods, helper methods for strategy filtering,
/// and record equality behavior. Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class ContextBudgetTests
{
    #region Factory Methods

    [Fact]
    public void Default_Has8000TokensAndNoFilters()
    {
        // Act
        var budget = ContextBudget.Default;

        // Assert
        budget.MaxTokens.Should().Be(8000);
        budget.RequiredStrategies.Should().BeNull();
        budget.ExcludedStrategies.Should().BeNull();
    }

    [Fact]
    public void WithLimit_CreatesWithSpecifiedTokens()
    {
        // Act
        var budget = ContextBudget.WithLimit(5000);

        // Assert
        budget.MaxTokens.Should().Be(5000);
        budget.RequiredStrategies.Should().BeNull();
        budget.ExcludedStrategies.Should().BeNull();
    }

    #endregion

    #region IsRequired Method

    [Fact]
    public void IsRequired_ReturnsTrueWhenInRequiredList()
    {
        // Arrange
        var budget = new ContextBudget(8000, new[] { "document", "selection" }, null);

        // Act & Assert
        budget.IsRequired("document").Should().BeTrue();
        budget.IsRequired("selection").Should().BeTrue();
    }

    [Fact]
    public void IsRequired_ReturnsFalseWhenNotInRequiredList()
    {
        // Arrange
        var budget = new ContextBudget(8000, new[] { "document" }, null);

        // Act & Assert
        budget.IsRequired("rag").Should().BeFalse();
    }

    [Fact]
    public void IsRequired_ReturnsFalseWhenNoRequiredList()
    {
        // Arrange
        var budget = ContextBudget.Default;

        // Act & Assert
        budget.IsRequired("document").Should().BeFalse();
    }

    #endregion

    #region IsExcluded Method

    [Fact]
    public void IsExcluded_ReturnsTrueWhenInExcludedList()
    {
        // Arrange
        var budget = new ContextBudget(8000, null, new[] { "rag", "style" });

        // Act & Assert
        budget.IsExcluded("rag").Should().BeTrue();
        budget.IsExcluded("style").Should().BeTrue();
    }

    [Fact]
    public void IsExcluded_ReturnsFalseWhenNotInExcludedList()
    {
        // Arrange
        var budget = new ContextBudget(8000, null, new[] { "rag" });

        // Act & Assert
        budget.IsExcluded("document").Should().BeFalse();
    }

    [Fact]
    public void IsExcluded_ReturnsFalseWhenNoExcludedList()
    {
        // Arrange
        var budget = ContextBudget.Default;

        // Act & Assert
        budget.IsExcluded("rag").Should().BeFalse();
    }

    #endregion

    #region ShouldExecute Method

    [Fact]
    public void ShouldExecute_ReturnsTrueWhenNotExcluded()
    {
        // Arrange
        var budget = new ContextBudget(8000, null, new[] { "rag" });

        // Act & Assert
        budget.ShouldExecute("document").Should().BeTrue();
    }

    [Fact]
    public void ShouldExecute_ReturnsFalseWhenExcluded()
    {
        // Arrange
        var budget = new ContextBudget(8000, null, new[] { "rag" });

        // Act & Assert
        budget.ShouldExecute("rag").Should().BeFalse();
    }

    [Fact]
    public void ShouldExecute_ExclusionTakesPrecedenceOverRequirement()
    {
        // Arrange - strategy is both required and excluded
        var budget = new ContextBudget(
            8000,
            RequiredStrategies: new[] { "rag" },
            ExcludedStrategies: new[] { "rag" });

        // Act & Assert - exclusion wins
        budget.ShouldExecute("rag").Should().BeFalse();
    }

    #endregion

    #region Record Equality

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var required = new[] { "document" };
        var excluded = new[] { "rag" };
        var budget1 = new ContextBudget(8000, required, excluded);
        var budget2 = new ContextBudget(8000, required, excluded);

        // Assert
        budget1.Should().Be(budget2);
    }

    [Fact]
    public void WithExpression_CreatesNewInstanceWithChangedProperty()
    {
        // Arrange
        var original = ContextBudget.Default;

        // Act
        var modified = original with { MaxTokens = 5000 };

        // Assert
        modified.MaxTokens.Should().Be(5000);
        original.MaxTokens.Should().Be(8000); // Unchanged
    }

    #endregion
}
