// -----------------------------------------------------------------------
// <copyright file="HeadingContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Agents.Context.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context.Strategies;

/// <summary>
/// Unit tests for <see cref="HeadingContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify heading hierarchy context gathering, breadcrumb inclusion,
/// FormatHeadingTree output, and fragment metadata. Introduced in v0.7.2b.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2b")]
public class HeadingContextStrategyTests
{
    #region Helper Factories

    private static HeadingContextStrategy CreateStrategy(
        IHeadingHierarchyService? headingService = null,
        IDocumentRepository? documentRepository = null,
        ITokenCounter? tokenCounter = null,
        ILogger<HeadingContextStrategy>? logger = null)
    {
        headingService ??= Substitute.For<IHeadingHierarchyService>();
        documentRepository ??= Substitute.For<IDocumentRepository>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<HeadingContextStrategy>>();
        return new HeadingContextStrategy(headingService, documentRepository, tokenCounter, logger);
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
    /// Verifies that passing a null heading service to the constructor throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullHeadingService_ThrowsArgumentNullException()
    {
        // Arrange
        var documentRepository = Substitute.For<IDocumentRepository>();
        var tokenCounter = Substitute.For<ITokenCounter>();
        var logger = Substitute.For<ILogger<HeadingContextStrategy>>();

        // Act
        var act = () => new HeadingContextStrategy(null!, documentRepository, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("headingService");
    }

    /// <summary>
    /// Verifies that passing a null document repository to the constructor throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_NullDocumentRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var headingService = Substitute.For<IHeadingHierarchyService>();
        var tokenCounter = Substitute.For<ITokenCounter>();
        var logger = Substitute.For<ILogger<HeadingContextStrategy>>();

        // Act
        var act = () => new HeadingContextStrategy(headingService, null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("documentRepository");
    }

    /// <summary>
    /// Verifies that the constructor succeeds with all valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var headingService = Substitute.For<IHeadingHierarchyService>();
        var documentRepository = Substitute.For<IDocumentRepository>();
        var tokenCounter = Substitute.For<ITokenCounter>();
        var logger = Substitute.For<ILogger<HeadingContextStrategy>>();

        // Act
        var sut = new HeadingContextStrategy(headingService, documentRepository, tokenCounter, logger);

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that StrategyId returns "heading".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsHeading()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("heading");
    }

    /// <summary>
    /// Verifies that DisplayName returns "Heading Hierarchy".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsHeadingHierarchy()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Heading Hierarchy");
    }

    /// <summary>
    /// Verifies that Priority returns 70 (StrategyPriority.Medium + 10).
    /// </summary>
    [Fact]
    public void Priority_Returns70()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(StrategyPriority.Medium + 10);
        result.Should().Be(70);
    }

    /// <summary>
    /// Verifies that MaxTokens returns 300.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns300()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.MaxTokens;

        // Assert
        result.Should().Be(300);
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
    /// Verifies that GatherAsync returns null when the document is not indexed
    /// (GetByFilePathAsync returns null).
    /// </summary>
    [Fact]
    public async Task GatherAsync_DocumentNotIndexed_ReturnsNull()
    {
        // Arrange
        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Document?)null);

        var sut = CreateStrategy(documentRepository: documentRepository);
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
    /// Verifies that GatherAsync returns null when the document has no headings
    /// (BuildHeadingTreeAsync returns null).
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoHeadingsFound_ReturnsNull()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document(docId, Guid.Empty, "/path/to/doc.md", "Test Doc", "hash", DocumentStatus.Indexed, DateTime.UtcNow, null);

        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), "/path/to/doc.md", Arg.Any<CancellationToken>())
            .Returns(document);

        var headingService = Substitute.For<IHeadingHierarchyService>();
        headingService.BuildHeadingTreeAsync(docId, Arg.Any<CancellationToken>())
            .Returns((HeadingNode?)null);

        var sut = CreateStrategy(headingService: headingService, documentRepository: documentRepository);
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
    /// Verifies that GatherAsync returns a formatted fragment when valid headings are found.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ValidHeadings_ReturnsFormattedFragment()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document(docId, Guid.Empty, "/path/to/doc.md", "Test Doc", "hash", DocumentStatus.Indexed, DateTime.UtcNow, null);

        var leaf = new HeadingNode(Guid.NewGuid(), "Section 1.1", 2, 1, Array.Empty<HeadingNode>());
        var root = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, new[] { leaf });

        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), "/path/to/doc.md", Arg.Any<CancellationToken>())
            .Returns(document);

        var headingService = Substitute.For<IHeadingHierarchyService>();
        headingService.BuildHeadingTreeAsync(docId, Arg.Any<CancellationToken>())
            .Returns(root);

        var sut = CreateStrategy(headingService: headingService, documentRepository: documentRepository);
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
        result!.Content.Should().Contain("- Chapter 1 (H1)");
        result.Content.Should().Contain("  - Section 1.1 (H2)");
    }

    /// <summary>
    /// Verifies that GatherAsync includes a breadcrumb when the request has a cursor position.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithCursorPosition_IncludesBreadcrumb()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document(docId, Guid.Empty, "/path/to/doc.md", "Test Doc", "hash", DocumentStatus.Indexed, DateTime.UtcNow, null);

        var leaf = new HeadingNode(Guid.NewGuid(), "Section 1.1", 2, 1, Array.Empty<HeadingNode>());
        var root = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, new[] { leaf });

        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), "/path/to/doc.md", Arg.Any<CancellationToken>())
            .Returns(document);

        var headingService = Substitute.For<IHeadingHierarchyService>();
        headingService.BuildHeadingTreeAsync(docId, Arg.Any<CancellationToken>())
            .Returns(root);
        headingService.GetBreadcrumbAsync(docId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Chapter 1", "Section 1.1" });

        var sut = CreateStrategy(headingService: headingService, documentRepository: documentRepository);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: 500,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Current Location:");
        result.Content.Should().Contain("Chapter 1 > Section 1.1");
    }

    /// <summary>
    /// Verifies that GatherAsync does not include a breadcrumb when the request
    /// has no cursor position.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithoutCursorPosition_NoBreadcrumb()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document(docId, Guid.Empty, "/path/to/doc.md", "Test Doc", "hash", DocumentStatus.Indexed, DateTime.UtcNow, null);

        var root = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, Array.Empty<HeadingNode>());

        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), "/path/to/doc.md", Arg.Any<CancellationToken>())
            .Returns(document);

        var headingService = Substitute.For<IHeadingHierarchyService>();
        headingService.BuildHeadingTreeAsync(docId, Arg.Any<CancellationToken>())
            .Returns(root);

        var sut = CreateStrategy(headingService: headingService, documentRepository: documentRepository);
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
        result!.Content.Should().NotContain("Current Location:");
    }

    /// <summary>
    /// Verifies that the returned fragment has SourceId set to "heading".
    /// </summary>
    [Fact]
    public async Task GatherAsync_FragmentHasCorrectSourceId()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document(docId, Guid.Empty, "/path/to/doc.md", "Test Doc", "hash", DocumentStatus.Indexed, DateTime.UtcNow, null);

        var root = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, Array.Empty<HeadingNode>());

        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), "/path/to/doc.md", Arg.Any<CancellationToken>())
            .Returns(document);

        var headingService = Substitute.For<IHeadingHierarchyService>();
        headingService.BuildHeadingTreeAsync(docId, Arg.Any<CancellationToken>())
            .Returns(root);

        var sut = CreateStrategy(headingService: headingService, documentRepository: documentRepository);
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
        result!.SourceId.Should().Be("heading");
    }

    /// <summary>
    /// Verifies that GatherAsync calls GetByFilePathAsync with Guid.Empty as the project ID.
    /// </summary>
    [Fact]
    public async Task GatherAsync_UsesGuidEmptyForProjectId()
    {
        // Arrange
        var documentRepository = Substitute.For<IDocumentRepository>();
        documentRepository.GetByFilePathAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Document?)null);

        var sut = CreateStrategy(documentRepository: documentRepository);
        var request = new ContextGatheringRequest(
            DocumentPath: "/path/to/doc.md",
            CursorPosition: null,
            SelectedText: null,
            AgentId: "test-agent",
            Hints: null);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await documentRepository.Received(1).GetByFilePathAsync(
            Guid.Empty,
            "/path/to/doc.md",
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region FormatHeadingTree Tests

    /// <summary>
    /// Verifies that FormatHeadingTree formats a single H1 node correctly.
    /// </summary>
    [Fact]
    public void FormatHeadingTree_SingleNode_FormatsCorrectly()
    {
        // Arrange
        var node = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, Array.Empty<HeadingNode>());

        // Act
        var result = HeadingContextStrategy.FormatHeadingTree(node);

        // Assert
        result.Should().Be("- Chapter 1 (H1)\n");
    }

    /// <summary>
    /// Verifies that FormatHeadingTree formats nested nodes with correct 2-space indentation.
    /// </summary>
    [Fact]
    public void FormatHeadingTree_NestedNodes_FormatsWithIndentation()
    {
        // Arrange
        var child1 = new HeadingNode(Guid.NewGuid(), "Section 1.1", 2, 1, Array.Empty<HeadingNode>());
        var child2 = new HeadingNode(Guid.NewGuid(), "Section 1.2", 2, 5, Array.Empty<HeadingNode>());
        var root = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, new[] { child1, child2 });

        // Act
        var result = HeadingContextStrategy.FormatHeadingTree(root);

        // Assert
        result.Should().Contain("- Chapter 1 (H1)");
        result.Should().Contain("  - Section 1.1 (H2)");
        result.Should().Contain("  - Section 1.2 (H2)");
    }

    /// <summary>
    /// Verifies that FormatHeadingTree indents deeply nested nodes correctly (H1 > H2 > H3).
    /// </summary>
    [Fact]
    public void FormatHeadingTree_DeeplyNested_IndentsCorrectly()
    {
        // Arrange
        var h3 = new HeadingNode(Guid.NewGuid(), "Subsection 1.1.1", 3, 2, Array.Empty<HeadingNode>());
        var h2 = new HeadingNode(Guid.NewGuid(), "Section 1.1", 2, 1, new[] { h3 });
        var h1 = new HeadingNode(Guid.NewGuid(), "Chapter 1", 1, 0, new[] { h2 });

        // Act
        var result = HeadingContextStrategy.FormatHeadingTree(h1);

        // Assert
        var expected =
            "- Chapter 1 (H1)\n" +
            "  - Section 1.1 (H2)\n" +
            "    - Subsection 1.1.1 (H3)\n";
        result.Should().Be(expected);
    }

    #endregion
}
