// -----------------------------------------------------------------------
// <copyright file="CursorContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Context.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context.Strategies;

/// <summary>
/// Unit tests for <see cref="CursorContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify cursor context gathering, word boundary expansion,
/// cursor marker formatting, and relevance calculation.
/// Introduced in v0.7.2b.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2b")]
public class CursorContextStrategyTests
{
    #region Helper Factories

    private static CursorContextStrategy CreateStrategy(
        IEditorService? editorService = null,
        ITokenCounter? tokenCounter = null,
        ILogger<CursorContextStrategy>? logger = null)
    {
        editorService ??= Substitute.For<IEditorService>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<CursorContextStrategy>>();
        return new CursorContextStrategy(editorService, tokenCounter, logger);
    }

    private static ITokenCounter CreateDefaultTokenCounter()
    {
        var tc = Substitute.For<ITokenCounter>();
        tc.CountTokens(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Length / 4);
        return tc;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws when editorService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<CursorContextStrategy>>();

        // Act
        var act = () => new CursorContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("editorService");
    }

    /// <summary>
    /// Verifies that the constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<CursorContextStrategy>>();

        // Act
        var act = () => new CursorContextStrategy(editorService, tokenCounter, logger);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that StrategyId returns "cursor".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsCursor()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("cursor");
    }

    /// <summary>
    /// Verifies that DisplayName returns "Cursor Context".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsCursorContext()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Cursor Context");
    }

    /// <summary>
    /// Verifies that Priority returns 80 (StrategyPriority.High).
    /// </summary>
    [Fact]
    public void Priority_ReturnsHigh()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(80);
    }

    /// <summary>
    /// Verifies that MaxTokens returns 500.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns500()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.MaxTokens;

        // Assert
        result.Should().Be(500);
    }

    #endregion

    #region GatherAsync Tests

    /// <summary>
    /// Verifies that GatherAsync returns null when no document path is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoDocument_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, 10, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GatherAsync returns null when no cursor position is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoCursor_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest("/path/to/doc.md", null, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GatherAsync returns null when document content is empty.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EmptyContent_ReturnsNull()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns(string.Empty);
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService);
        var request = new ContextGatheringRequest("/path/to/doc.md", 0, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GatherAsync returns a fragment containing the cursor marker
    /// when given a valid document and cursor position.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ValidRequest_ReturnsFragmentWithCursorMarker()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns("Hello world, this is a test document.");
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService);
        var request = new ContextGatheringRequest("/path/to/doc.md", 5, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("\u258C");
    }

    /// <summary>
    /// Verifies that GatherAsync returns null when cursor position is beyond content length.
    /// </summary>
    [Fact]
    public async Task GatherAsync_CursorOutOfRange_ReturnsNull()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns("Short text.");
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService);
        var request = new ContextGatheringRequest("/path/to/doc.md", 9999, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the fragment returned by GatherAsync has SourceId set to "cursor".
    /// </summary>
    [Fact]
    public async Task GatherAsync_FragmentHasCorrectSourceId()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns("Hello world, this is a test document.");
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService);
        var request = new ContextGatheringRequest("/path/to/doc.md", 5, null, "test-agent", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("cursor");
    }

    #endregion

    #region ExpandToWordBoundary Tests

    /// <summary>
    /// Verifies that ExpandToWordBoundary expands to the left until whitespace is found.
    /// </summary>
    [Fact]
    public void ExpandToWordBoundary_LeftDirection_ExpandsToWhitespace()
    {
        // Arrange
        var content = "Hello World";
        var pos = 7; // In the middle of "World"

        // Act
        var result = CursorContextStrategy.ExpandToWordBoundary(
            content, pos, CursorContextStrategy.Direction.Left);

        // Assert
        result.Should().Be(5); // Position of the space
    }

    /// <summary>
    /// Verifies that ExpandToWordBoundary expands to the right until whitespace is found.
    /// </summary>
    [Fact]
    public void ExpandToWordBoundary_RightDirection_ExpandsToWhitespace()
    {
        // Arrange
        var content = "Hello World";
        var pos = 2; // In the middle of "Hello"

        // Act
        var result = CursorContextStrategy.ExpandToWordBoundary(
            content, pos, CursorContextStrategy.Direction.Right);

        // Assert
        result.Should().Be(5); // Position of the space
    }

    /// <summary>
    /// Verifies that ExpandToWordBoundary returns the start position when already at start.
    /// </summary>
    [Fact]
    public void ExpandToWordBoundary_AtStart_ReturnsStart()
    {
        // Arrange
        var content = "Hello World";
        var pos = 0;

        // Act
        var result = CursorContextStrategy.ExpandToWordBoundary(
            content, pos, CursorContextStrategy.Direction.Left);

        // Assert
        result.Should().Be(0);
    }

    /// <summary>
    /// Verifies that ExpandToWordBoundary returns the end position when already at end.
    /// </summary>
    [Fact]
    public void ExpandToWordBoundary_AtEnd_ReturnsEnd()
    {
        // Arrange
        var content = "Hello World";
        var pos = content.Length;

        // Act
        var result = CursorContextStrategy.ExpandToWordBoundary(
            content, pos, CursorContextStrategy.Direction.Right);

        // Assert
        result.Should().Be(content.Length);
    }

    #endregion

    #region FormatWithCursorMarker Tests

    /// <summary>
    /// Verifies that FormatWithCursorMarker inserts the cursor marker at a valid offset.
    /// </summary>
    [Fact]
    public void FormatWithCursorMarker_ValidOffset_InsertsCursorMarker()
    {
        // Arrange
        var window = "Hello World";
        var offset = 5;

        // Act
        var result = CursorContextStrategy.FormatWithCursorMarker(window, offset);

        // Assert
        result.Should().Be("Hello\u258C World");
    }

    /// <summary>
    /// Verifies that FormatWithCursorMarker inserts the cursor marker at the start.
    /// </summary>
    [Fact]
    public void FormatWithCursorMarker_AtStart_InsertsCursorAtStart()
    {
        // Arrange
        var window = "Hello World";
        var offset = 0;

        // Act
        var result = CursorContextStrategy.FormatWithCursorMarker(window, offset);

        // Assert
        result.Should().Be("\u258CHello World");
    }

    /// <summary>
    /// Verifies that FormatWithCursorMarker inserts the cursor marker at the end.
    /// </summary>
    [Fact]
    public void FormatWithCursorMarker_AtEnd_InsertsCursorAtEnd()
    {
        // Arrange
        var window = "Hello World";
        var offset = 11;

        // Act
        var result = CursorContextStrategy.FormatWithCursorMarker(window, offset);

        // Assert
        result.Should().Be("Hello World\u258C");
    }

    /// <summary>
    /// Verifies that FormatWithCursorMarker returns the window unchanged for a negative offset.
    /// </summary>
    [Fact]
    public void FormatWithCursorMarker_NegativeOffset_ReturnsUnchanged()
    {
        // Arrange
        var window = "Hello World";
        var offset = -1;

        // Act
        var result = CursorContextStrategy.FormatWithCursorMarker(window, offset);

        // Assert
        result.Should().Be("Hello World");
    }

    #endregion

    #region CalculateRelevance Tests

    /// <summary>
    /// Verifies that CalculateRelevance returns high relevance for a cursor in the middle of the document.
    /// </summary>
    [Fact]
    public void CalculateRelevance_MiddleOfDocument_ReturnsHighRelevance()
    {
        // Arrange
        var cursorPos = 500;
        var contentLength = 1000;

        // Act
        var result = CursorContextStrategy.CalculateRelevance(cursorPos, contentLength);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0.9f);
        result.Should().BeLessThanOrEqualTo(1.0f);
    }

    /// <summary>
    /// Verifies that CalculateRelevance returns lower relevance for a cursor at the start of the document.
    /// </summary>
    [Fact]
    public void CalculateRelevance_StartOfDocument_ReturnsLowerRelevance()
    {
        // Arrange
        var cursorPos = 0;
        var contentLength = 1000;

        // Act
        var result = CursorContextStrategy.CalculateRelevance(cursorPos, contentLength);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0.6f);
        result.Should().BeLessThan(1.0f);
    }

    /// <summary>
    /// Verifies that CalculateRelevance returns 0.5 for an empty document.
    /// </summary>
    [Fact]
    public void CalculateRelevance_EmptyDocument_ReturnsHalf()
    {
        // Arrange
        var cursorPos = 0;
        var contentLength = 0;

        // Act
        var result = CursorContextStrategy.CalculateRelevance(cursorPos, contentLength);

        // Assert
        result.Should().Be(0.5f);
    }

    #endregion
}
