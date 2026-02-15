// -----------------------------------------------------------------------
// <copyright file="SurroundingTextContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor.Context;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor.Context;

/// <summary>
/// Unit tests for <see cref="SurroundingTextContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify constructor validation, property values, GatherAsync behavior
/// including document retrieval, paragraph splitting, cursor location, surrounding
/// context extraction, and graceful degradation on failures.
/// Introduced in v0.7.3c.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3c")]
public class SurroundingTextContextStrategyTests
{
    #region Helper Factories

    /// <summary>
    /// Creates a <see cref="SurroundingTextContextStrategy"/> with optional dependency overrides.
    /// </summary>
    private static SurroundingTextContextStrategy CreateStrategy(
        IEditorService? editorService = null,
        ITokenCounter? tokenCounter = null,
        ILogger<SurroundingTextContextStrategy>? logger = null)
    {
        editorService ??= Substitute.For<IEditorService>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<SurroundingTextContextStrategy>>();
        return new SurroundingTextContextStrategy(editorService, tokenCounter, logger);
    }

    /// <summary>
    /// Creates a default <see cref="ITokenCounter"/> that estimates 1 token per 4 characters.
    /// </summary>
    private static ITokenCounter CreateDefaultTokenCounter()
    {
        var tc = Substitute.For<ITokenCounter>();
        tc.CountTokens(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Length / 4);
        return tc;
    }

    /// <summary>
    /// Creates a <see cref="ContextGatheringRequest"/> with the given parameters.
    /// </summary>
    private static ContextGatheringRequest CreateRequest(
        string? documentPath = "/test/document.md",
        int? cursorPosition = 50,
        string? selectedText = "selected text",
        string agentId = "editor")
    {
        return new ContextGatheringRequest(
            DocumentPath: documentPath,
            CursorPosition: cursorPosition,
            SelectedText: selectedText,
            AgentId: agentId,
            Hints: null);
    }

    /// <summary>
    /// Creates an <see cref="IEditorService"/> mock that returns the specified document content.
    /// </summary>
    private static IEditorService CreateEditorWithContent(string content, string documentPath = "/test/document.md")
    {
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns(content);
        editorService.GetDocumentByPath(documentPath).Returns(manuscript);
        return editorService;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that passing a null editor service to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<SurroundingTextContextStrategy>>();

        // Act
        var act = () => new SurroundingTextContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    /// <summary>
    /// Verifies that passing a null token counter to the constructor throws
    /// an <see cref="ArgumentNullException"/> (from base class).
    /// </summary>
    [Fact]
    public void Constructor_NullTokenCounter_ThrowsArgumentNullException()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var logger = Substitute.For<ILogger<SurroundingTextContextStrategy>>();

        // Act
        var act = () => new SurroundingTextContextStrategy(editorService, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenCounter");
    }

    /// <summary>
    /// Verifies that passing a null logger to the constructor throws
    /// an <see cref="ArgumentNullException"/> (from base class).
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var tokenCounter = CreateDefaultTokenCounter();

        // Act
        var act = () => new SurroundingTextContextStrategy(editorService, tokenCounter, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.StrategyId"/> returns "surrounding-text".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsSurroundingText()
    {
        var sut = CreateStrategy();
        sut.StrategyId.Should().Be("surrounding-text");
    }

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.DisplayName"/> returns "Surrounding Text".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsSurroundingText()
    {
        var sut = CreateStrategy();
        sut.DisplayName.Should().Be("Surrounding Text");
    }

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.Priority"/> returns Critical (100).
    /// </summary>
    [Fact]
    public void Priority_ReturnsCritical()
    {
        var sut = CreateStrategy();
        sut.Priority.Should().Be(StrategyPriority.Critical);
        sut.Priority.Should().Be(100);
    }

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.MaxTokens"/> returns 1500.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns1500()
    {
        var sut = CreateStrategy();
        sut.MaxTokens.Should().Be(1500);
    }

    #endregion

    #region GatherAsync — Validation

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.GatherAsync"/> returns null
    /// when no document path is provided in the request.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoDocumentPath_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = CreateRequest(documentPath: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.GatherAsync"/> returns null
    /// when no cursor position is provided in the request.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoCursorPosition_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = CreateRequest(cursorPosition: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.GatherAsync"/> returns null
    /// when the document content is empty.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EmptyDocument_ReturnsNull()
    {
        // Arrange
        var editorService = CreateEditorWithContent(string.Empty);
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest(cursorPosition: 0);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.GatherAsync"/> returns null
    /// when the editor service cannot find the document.
    /// </summary>
    [Fact]
    public async Task GatherAsync_DocumentNotFound_ReturnsNull()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        editorService.GetDocumentByPath(Arg.Any<string>()).Returns((IManuscriptViewModel?)null);
        editorService.GetDocumentText().Returns((string?)null);
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GatherAsync — Context Gathering

    /// <summary>
    /// Verifies that surrounding paragraphs are included in the context with
    /// the [SELECTION IS HERE] marker.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithSurroundingParagraphs_IncludesContextWithMarker()
    {
        // Arrange
        var content = "First paragraph here.\n\nSecond paragraph with cursor.\n\nThird paragraph after.";
        var editorService = CreateEditorWithContent(content);
        var sut = CreateStrategy(editorService: editorService);
        // Cursor position in the second paragraph (offset ~24, after first paragraph + \n\n)
        var request = CreateRequest(cursorPosition: 24);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("First paragraph here.");
        result.Content.Should().Contain("[SELECTION IS HERE]");
        result.Content.Should().Contain("Third paragraph after.");
    }

    /// <summary>
    /// Verifies that the fragment SourceId is "surrounding-text".
    /// </summary>
    [Fact]
    public async Task GatherAsync_ReturnsFragment_WithCorrectSourceId()
    {
        // Arrange
        var content = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
        var editorService = CreateEditorWithContent(content);
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest(cursorPosition: 20);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("surrounding-text");
    }

    /// <summary>
    /// Verifies that the fragment relevance is 0.9.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ReturnsFragment_WithCorrectRelevance()
    {
        // Arrange
        var content = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
        var editorService = CreateEditorWithContent(content);
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest(cursorPosition: 20);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Relevance.Should().Be(0.9f);
    }

    /// <summary>
    /// Verifies that when the cursor is in the first paragraph, no "before" context
    /// is included but "after" context is present.
    /// </summary>
    [Fact]
    public async Task GatherAsync_CursorInFirstParagraph_NoBeforeContext()
    {
        // Arrange
        var content = "First paragraph here.\n\nSecond paragraph.\n\nThird paragraph.";
        var editorService = CreateEditorWithContent(content);
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest(cursorPosition: 5); // In first paragraph

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("[SELECTION IS HERE]");
        result.Content.Should().Contain("Second paragraph.");
        // The first paragraph is the selection — before it should be empty
        result.Content.Should().StartWith("[SELECTION IS HERE]");
    }

    /// <summary>
    /// Verifies that when the cursor is in the last paragraph, no "after" context
    /// is included but "before" context is present.
    /// </summary>
    [Fact]
    public async Task GatherAsync_CursorInLastParagraph_NoAfterContext()
    {
        // Arrange
        var content = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph here.";
        var editorService = CreateEditorWithContent(content);
        var sut = CreateStrategy(editorService: editorService);
        // Cursor in the third paragraph
        var request = CreateRequest(cursorPosition: 42);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("[SELECTION IS HERE]");
        result.Content.Should().Contain("First paragraph.");
        result.Content.Should().Contain("Second paragraph.");
        // After the marker, no more paragraphs
        result.Content.Should().EndWith("[SELECTION IS HERE]");
    }

    /// <summary>
    /// Verifies that a single-paragraph document returns context with only
    /// the [SELECTION IS HERE] marker.
    /// </summary>
    [Fact]
    public async Task GatherAsync_SingleParagraph_ReturnsMarkerOnly()
    {
        // Arrange
        var content = "Only one paragraph in this document.";
        var editorService = CreateEditorWithContent(content);
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest(cursorPosition: 5);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("[SELECTION IS HERE]");
    }

    /// <summary>
    /// Verifies that the strategy falls back to <see cref="IEditorService.GetDocumentText"/>
    /// when <see cref="IEditorService.GetDocumentByPath"/> returns null and paths match.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FallsBackToGetDocumentText_WhenPathMatches()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        editorService.GetDocumentByPath(Arg.Any<string>()).Returns((IManuscriptViewModel?)null);
        editorService.CurrentDocumentPath.Returns("/test/document.md");
        editorService.GetDocumentText().Returns("First paragraph.\n\nSecond paragraph.");

        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest(documentPath: "/test/document.md", cursorPosition: 5);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("[SELECTION IS HERE]");
        result.Content.Should().Contain("Second paragraph.");
    }

    #endregion

    #region GatherAsync — Error Handling

    /// <summary>
    /// Verifies that an exception in the editor service returns null gracefully.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EditorServiceThrows_ReturnsNull()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        editorService.GetDocumentByPath(Arg.Any<string>()).Throws(new InvalidOperationException("Editor error"));
        var sut = CreateStrategy(editorService: editorService);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region FindParagraphIndex — Static Helper

    /// <summary>
    /// Verifies that <see cref="SurroundingTextContextStrategy.FindParagraphIndex"/>
    /// correctly identifies the paragraph containing the cursor.
    /// </summary>
    [Fact]
    public void FindParagraphIndex_CursorInSecondParagraph_ReturnsOne()
    {
        // Arrange
        var paragraphs = new[] { "First paragraph.", "Second paragraph.", "Third paragraph." };
        // First paragraph is 16 chars + 2 separator = 18 offset for start of second
        var cursorPosition = 20; // In second paragraph

        // Act
        var index = SurroundingTextContextStrategy.FindParagraphIndex(paragraphs, cursorPosition);

        // Assert
        index.Should().Be(1);
    }

    /// <summary>
    /// Verifies that cursor at position 0 returns the first paragraph index (0).
    /// </summary>
    [Fact]
    public void FindParagraphIndex_CursorAtStart_ReturnsZero()
    {
        // Arrange
        var paragraphs = new[] { "First paragraph.", "Second paragraph." };

        // Act
        var index = SurroundingTextContextStrategy.FindParagraphIndex(paragraphs, 0);

        // Assert
        index.Should().Be(0);
    }

    /// <summary>
    /// Verifies that cursor beyond all paragraphs returns the last paragraph index.
    /// </summary>
    [Fact]
    public void FindParagraphIndex_CursorBeyondEnd_ReturnsLastIndex()
    {
        // Arrange
        var paragraphs = new[] { "First.", "Second." };

        // Act
        var index = SurroundingTextContextStrategy.FindParagraphIndex(paragraphs, 999);

        // Assert
        index.Should().Be(1);
    }

    #endregion
}
