// -----------------------------------------------------------------------
// <copyright file="AssembledContextTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="AssembledContext"/>.
/// </summary>
/// <remarks>
/// Tests verify the record's convenience properties and methods including
/// Empty, HasContext, GetCombinedContent, GetFragment, and HasFragmentFrom.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2c")]
public class AssembledContextTests
{
    #region Empty Tests

    /// <summary>
    /// Verifies that Empty returns a context with no fragments.
    /// </summary>
    [Fact]
    public void Empty_HasNoFragments()
    {
        // Act
        var result = AssembledContext.Empty;

        // Assert
        result.Fragments.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Empty has zero total tokens.
    /// </summary>
    [Fact]
    public void Empty_HasZeroTokens()
    {
        // Act
        var result = AssembledContext.Empty;

        // Assert
        result.TotalTokens.Should().Be(0);
    }

    /// <summary>
    /// Verifies that Empty has zero assembly duration.
    /// </summary>
    [Fact]
    public void Empty_HasZeroDuration()
    {
        // Act
        var result = AssembledContext.Empty;

        // Assert
        result.AssemblyDuration.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that Empty has empty variables dictionary.
    /// </summary>
    [Fact]
    public void Empty_HasEmptyVariables()
    {
        // Act
        var result = AssembledContext.Empty;

        // Assert
        result.Variables.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Empty indicates no context available.
    /// </summary>
    [Fact]
    public void Empty_HasContextIsFalse()
    {
        // Act
        var result = AssembledContext.Empty;

        // Assert
        result.HasContext.Should().BeFalse();
    }

    #endregion

    #region HasContext Tests

    /// <summary>
    /// Verifies that HasContext returns true when fragments are present.
    /// </summary>
    [Fact]
    public void HasContext_WithFragments_ReturnsTrue()
    {
        // Arrange
        var fragment = new ContextFragment("document", "Document Content", "Hello world", 5, 1.0f);
        var context = new AssembledContext(
            new[] { fragment },
            5,
            new Dictionary<string, object>(),
            TimeSpan.FromMilliseconds(100));

        // Act & Assert
        context.HasContext.Should().BeTrue();
    }

    #endregion

    #region GetCombinedContent Tests

    /// <summary>
    /// Verifies that GetCombinedContent formats fragments as markdown sections.
    /// </summary>
    [Fact]
    public void GetCombinedContent_FormatsFragmentsAsMarkdownSections()
    {
        // Arrange
        var fragments = new[]
        {
            new ContextFragment("document", "Document Content", "Chapter 1 text", 10, 1.0f),
            new ContextFragment("selection", "Selected Text", "Selected portion", 5, 0.9f)
        };
        var context = new AssembledContext(
            fragments,
            15,
            new Dictionary<string, object>(),
            TimeSpan.FromMilliseconds(50));

        // Act
        var result = context.GetCombinedContent();

        // Assert
        result.Should().Contain("## Document Content");
        result.Should().Contain("Chapter 1 text");
        result.Should().Contain("## Selected Text");
        result.Should().Contain("Selected portion");
    }

    /// <summary>
    /// Verifies that GetCombinedContent uses custom separator.
    /// </summary>
    [Fact]
    public void GetCombinedContent_WithCustomSeparator_UsesSeparator()
    {
        // Arrange
        var fragments = new[]
        {
            new ContextFragment("a", "A", "Content A", 5, 1.0f),
            new ContextFragment("b", "B", "Content B", 5, 0.9f)
        };
        var context = new AssembledContext(
            fragments,
            10,
            new Dictionary<string, object>(),
            TimeSpan.Zero);

        // Act
        var result = context.GetCombinedContent("---");

        // Assert
        result.Should().Be("## A\nContent A---## B\nContent B");
    }

    /// <summary>
    /// Verifies that GetCombinedContent returns empty for no fragments.
    /// </summary>
    [Fact]
    public void GetCombinedContent_NoFragments_ReturnsEmpty()
    {
        // Act
        var result = AssembledContext.Empty.GetCombinedContent();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFragment Tests

    /// <summary>
    /// Verifies that GetFragment returns matching fragment by source ID.
    /// </summary>
    [Fact]
    public void GetFragment_ExistingSourceId_ReturnsFragment()
    {
        // Arrange
        var expected = new ContextFragment("style", "Style Rules", "Use active voice", 8, 0.7f);
        var context = new AssembledContext(
            new[]
            {
                new ContextFragment("document", "Document", "Doc text", 10, 1.0f),
                expected
            },
            18,
            new Dictionary<string, object>(),
            TimeSpan.Zero);

        // Act
        var result = context.GetFragment("style");

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("style");
        result.Content.Should().Be("Use active voice");
    }

    /// <summary>
    /// Verifies that GetFragment returns null for non-existent source ID.
    /// </summary>
    [Fact]
    public void GetFragment_NonExistentSourceId_ReturnsNull()
    {
        // Arrange
        var context = new AssembledContext(
            new[] { new ContextFragment("document", "Document", "Text", 5, 1.0f) },
            5,
            new Dictionary<string, object>(),
            TimeSpan.Zero);

        // Act
        var result = context.GetFragment("unknown");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region HasFragmentFrom Tests

    /// <summary>
    /// Verifies that HasFragmentFrom returns true for present source.
    /// </summary>
    [Fact]
    public void HasFragmentFrom_PresentSource_ReturnsTrue()
    {
        // Arrange
        var context = new AssembledContext(
            new[] { new ContextFragment("rag", "RAG Results", "Relevant docs", 20, 0.8f) },
            20,
            new Dictionary<string, object>(),
            TimeSpan.Zero);

        // Act & Assert
        context.HasFragmentFrom("rag").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that HasFragmentFrom returns false for absent source.
    /// </summary>
    [Fact]
    public void HasFragmentFrom_AbsentSource_ReturnsFalse()
    {
        // Arrange
        var context = new AssembledContext(
            new[] { new ContextFragment("document", "Document", "Text", 5, 1.0f) },
            5,
            new Dictionary<string, object>(),
            TimeSpan.Zero);

        // Act & Assert
        context.HasFragmentFrom("style").Should().BeFalse();
    }

    #endregion
}
