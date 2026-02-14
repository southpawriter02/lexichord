// -----------------------------------------------------------------------
// <copyright file="DocumentContextStrategyTests.cs" company="Lexichord">
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
/// Unit tests for <see cref="DocumentContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify document content gathering, fallback behavior, smart truncation,
/// and fragment metadata. Introduced in v0.7.2b.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2b")]
public class DocumentContextStrategyTests
{
    #region Helper Factories

    private static DocumentContextStrategy CreateStrategy(
        IEditorService? editorService = null,
        ITokenCounter? tokenCounter = null,
        ILogger<DocumentContextStrategy>? logger = null)
    {
        editorService ??= Substitute.For<IEditorService>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<DocumentContextStrategy>>();
        return new DocumentContextStrategy(editorService, tokenCounter, logger);
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
    /// Verifies that passing a null editor service to the constructor throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = Substitute.For<ITokenCounter>();
        var logger = Substitute.For<ILogger<DocumentContextStrategy>>();

        // Act
        var act = () => new DocumentContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("editorService");
    }

    /// <summary>
    /// Verifies that passing a null token counter to the constructor throws ArgumentNullException
    /// from the base class.
    /// </summary>
    [Fact]
    public void Constructor_NullTokenCounter_ThrowsArgumentNullException()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var logger = Substitute.For<ILogger<DocumentContextStrategy>>();

        // Act
        var act = () => new DocumentContextStrategy(editorService, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("tokenCounter");
    }

    /// <summary>
    /// Verifies that passing a null logger to the constructor throws ArgumentNullException
    /// from the base class.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var tokenCounter = Substitute.For<ITokenCounter>();

        // Act
        var act = () => new DocumentContextStrategy(editorService, tokenCounter, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    /// <summary>
    /// Verifies that the constructor succeeds with all valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var tokenCounter = Substitute.For<ITokenCounter>();
        var logger = Substitute.For<ILogger<DocumentContextStrategy>>();

        // Act
        var sut = new DocumentContextStrategy(editorService, tokenCounter, logger);

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that StrategyId returns "document".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsDocument()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("document");
    }

    /// <summary>
    /// Verifies that DisplayName returns "Document Content".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsDocumentContent()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Document Content");
    }

    /// <summary>
    /// Verifies that Priority returns StrategyPriority.Critical (100).
    /// </summary>
    [Fact]
    public void Priority_ReturnsCritical()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(StrategyPriority.Critical);
        result.Should().Be(100);
    }

    /// <summary>
    /// Verifies that MaxTokens returns 4000.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns4000()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.MaxTokens;

        // Assert
        result.Should().Be(4000);
    }

    #endregion

    #region GatherAsync Tests

    /// <summary>
    /// Verifies that GatherAsync returns null when the request has no document path.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoDocumentPath_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(
            DocumentPath: null,
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GatherAsync returns a fragment when a valid document is found by path.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ValidDocument_ReturnsFragment()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns("Document content here");
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService: editorService);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Document content here");
    }

    /// <summary>
    /// Verifies that GatherAsync returns null when the manuscript has empty content.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EmptyContent_ReturnsNull()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns(string.Empty);
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService: editorService);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GatherAsync falls back to GetDocumentText when GetDocumentByPath
    /// returns null but CurrentDocumentPath matches the requested path.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FallbackToActiveDocument_ReturnsFragment()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        editorService.GetDocumentByPath("/path/to/doc.md").Returns((IManuscriptViewModel?)null);
        editorService.CurrentDocumentPath.Returns("/path/to/doc.md");
        editorService.GetDocumentText().Returns("Active document content");

        var sut = CreateStrategy(editorService: editorService);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Active document content");
        editorService.Received(1).GetDocumentText();
    }

    /// <summary>
    /// Verifies that GatherAsync returns null when GetDocumentByPath returns null
    /// and CurrentDocumentPath does not match the requested path.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FallbackDoesNotMatchPath_ReturnsNull()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        editorService.GetDocumentByPath("/path/to/doc.md").Returns((IManuscriptViewModel?)null);
        editorService.CurrentDocumentPath.Returns("/path/to/other.md");

        var sut = CreateStrategy(editorService: editorService);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        editorService.DidNotReceive().GetDocumentText();
    }

    /// <summary>
    /// Verifies that GatherAsync truncates content when the document exceeds the
    /// 4000 token budget, using line-by-line smart truncation.
    /// </summary>
    [Fact]
    public async Task GatherAsync_LargeDocument_TruncatesContent()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var lines = Enumerable.Range(1, 100)
            .Select(i => $"Line {i}: Some content for testing truncation behavior.")
            .ToArray();
        var fullContent = string.Join("\n", lines);

        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns(fullContent);
        editorService.GetDocumentByPath("/path/to/large.md").Returns(manuscript);

        var tokenCounter = Substitute.For<ITokenCounter>();
        // Full content exceeds budget
        tokenCounter.CountTokens(fullContent).Returns(5000);
        // Individual lines return 50 tokens each so accumulation exceeds 4000 after ~80 lines
        tokenCounter.CountTokens(Arg.Is<string>(s => s != fullContent))
            .Returns(ci =>
            {
                var text = ci.Arg<string>();
                return 50;
            });

        var sut = CreateStrategy(editorService: editorService, tokenCounter: tokenCounter);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/large.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Length.Should().BeLessThan(fullContent.Length);
        result.Content.Should().Contain("[Content truncated to fit token budget]");
    }

    /// <summary>
    /// Verifies that the returned fragment has correct metadata including
    /// SourceId and Relevance values.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FragmentHasCorrectMetadata()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns("Some document content");
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService: editorService);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("document");
        result.Relevance.Should().Be(1.0f);
        result.Label.Should().Be("Document Content");
    }

    /// <summary>
    /// Verifies that GatherAsync logs an Information-level message on successful
    /// content gathering.
    /// </summary>
    [Fact]
    public async Task GatherAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns("Logged document content");
        editorService.GetDocumentByPath("/path/to/doc.md").Returns(manuscript);

        var logger = Substitute.For<ILogger<DocumentContextStrategy>>();
        var sut = CreateStrategy(editorService: editorService, logger: logger);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("document")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
