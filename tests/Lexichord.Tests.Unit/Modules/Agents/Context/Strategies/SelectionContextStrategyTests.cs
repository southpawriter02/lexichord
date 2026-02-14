// -----------------------------------------------------------------------
// <copyright file="SelectionContextStrategyTests.cs" company="Lexichord">
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
/// Unit tests for <see cref="SelectionContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify constructor validation, property values, and GatherAsync behavior
/// including selection marker formatting and surrounding paragraph context.
/// Introduced in v0.7.2b.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2b")]
public class SelectionContextStrategyTests
{
    #region Helper Factories

    private static SelectionContextStrategy CreateStrategy(
        IEditorService? editorService = null,
        ITokenCounter? tokenCounter = null,
        ILogger<SelectionContextStrategy>? logger = null)
    {
        editorService ??= Substitute.For<IEditorService>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<SelectionContextStrategy>>();
        return new SelectionContextStrategy(editorService, tokenCounter, logger);
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
    /// Verifies that passing a null editor service to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<SelectionContextStrategy>>();

        // Act
        var act = () => new SelectionContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    /// <summary>
    /// Verifies that constructing with valid parameters succeeds without error.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<SelectionContextStrategy>>();

        // Act
        var sut = new SelectionContextStrategy(editorService, tokenCounter, logger);

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.StrategyId"/> returns "selection".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsSelection()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("selection");
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.DisplayName"/> returns "Selected Text".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsSelectedText()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Selected Text");
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.Priority"/> returns
    /// <see cref="StrategyPriority.High"/> (80).
    /// </summary>
    [Fact]
    public void Priority_ReturnsHigh()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(StrategyPriority.High);
        result.Should().Be(80);
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.MaxTokens"/> returns 1000.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns1000()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.MaxTokens;

        // Assert
        result.Should().Be(1000);
    }

    #endregion

    #region GatherAsync Tests

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.GatherAsync"/> returns null
    /// when the request has no selected text.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoSelection_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest("/doc.md", 10, null, "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.GatherAsync"/> returns a fragment
    /// containing the selected text wrapped in <c>&lt;&lt;SELECTED_TEXT&gt;&gt;</c> and
    /// <c>&lt;&lt;/SELECTED_TEXT&gt;&gt;</c> markers.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithSelection_ReturnsFragmentWithMarkers()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, "the quick brown fox", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("<<SELECTED_TEXT>>");
        result.Content.Should().Contain("<</SELECTED_TEXT>>");
        result.Content.Should().Contain("the quick brown fox");
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.GatherAsync"/> includes surrounding
    /// paragraph context when a document path and cursor position are provided and the
    /// document contains multiple paragraphs.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithSurroundingContext_IncludesBeforeAndAfter()
    {
        // Arrange
        var editorService = Substitute.For<IEditorService>();
        var content = "First paragraph.\n\nSecond paragraph with selection.\n\nThird paragraph.";
        var manuscript = Substitute.For<IManuscriptViewModel>();
        manuscript.Content.Returns(content);
        editorService.GetDocumentByPath("/doc.md").Returns(manuscript);

        var sut = CreateStrategy(editorService: editorService);

        // cursor position ~20 puts cursor in "Second paragraph" area
        var request = new ContextGatheringRequest("/doc.md", 20, "selected text", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("<<SELECTED_TEXT>>");
        result.Content.Should().Contain("<</SELECTED_TEXT>>");
        result.Content.Should().Contain("selected text");
        result.Content.Should().Contain("First paragraph.");
        result.Content.Should().Contain("Third paragraph.");
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.GatherAsync"/> returns a fragment
    /// with selection markers but without surrounding context when no document path is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithoutDocumentPath_NoSurroundingContext()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, "selected text only", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("<<SELECTED_TEXT>>");
        result.Content.Should().Contain("<</SELECTED_TEXT>>");
        result.Content.Should().Contain("selected text only");
        result.Content.Should().NotContain("[Context before selection]");
        result.Content.Should().NotContain("[Context after selection]");
    }

    /// <summary>
    /// Verifies that the <see cref="ContextFragment"/> returned by
    /// <see cref="SelectionContextStrategy.GatherAsync"/> has the correct metadata:
    /// SourceId of "selection" and relevance of 1.0.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FragmentHasCorrectMetadata()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = new ContextGatheringRequest(null, null, "some selection", "editor", null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("selection");
        result.Relevance.Should().Be(1.0f);
    }

    /// <summary>
    /// Verifies that <see cref="SelectionContextStrategy.GatherAsync"/> logs an
    /// Information-level message upon successfully gathering selection context.
    /// </summary>
    [Fact]
    public async Task GatherAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SelectionContextStrategy>>();
        var sut = CreateStrategy(logger: logger);
        var request = new ContextGatheringRequest(null, null, "selected text", "editor", null);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("selection")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
