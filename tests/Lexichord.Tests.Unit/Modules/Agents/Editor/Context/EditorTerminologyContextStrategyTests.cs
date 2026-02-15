// -----------------------------------------------------------------------
// <copyright file="EditorTerminologyContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Agents.Editor.Context;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor.Context;

/// <summary>
/// Unit tests for <see cref="EditorTerminologyContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify constructor validation, property values, GatherAsync behavior
/// including term matching, case sensitivity, formatting, and graceful degradation
/// on failures. Introduced in v0.7.3c.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3c")]
public class EditorTerminologyContextStrategyTests
{
    #region Helper Factories

    /// <summary>
    /// Creates an <see cref="EditorTerminologyContextStrategy"/> with optional dependency overrides.
    /// </summary>
    private static EditorTerminologyContextStrategy CreateStrategy(
        ITerminologyRepository? terminologyRepository = null,
        ITokenCounter? tokenCounter = null,
        ILogger<EditorTerminologyContextStrategy>? logger = null)
    {
        terminologyRepository ??= Substitute.For<ITerminologyRepository>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<EditorTerminologyContextStrategy>>();
        return new EditorTerminologyContextStrategy(terminologyRepository, tokenCounter, logger);
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
        string? selectedText = "The API uses REST endpoints",
        string? documentPath = "/test/document.md",
        int? cursorPosition = 10,
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
    /// Creates a <see cref="StyleTerm"/> with the given parameters.
    /// </summary>
    private static StyleTerm CreateTerm(
        string term,
        string? replacement = null,
        string category = "General",
        string severity = "Suggestion",
        bool matchCase = false,
        string? notes = null)
    {
        return new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = Guid.NewGuid(),
            Term = term,
            Replacement = replacement,
            Category = category,
            Severity = severity,
            MatchCase = matchCase,
            Notes = notes,
            IsActive = true
        };
    }

    /// <summary>
    /// Creates an <see cref="ITerminologyRepository"/> that returns the given terms.
    /// </summary>
    private static ITerminologyRepository CreateRepositoryWithTerms(params StyleTerm[] terms)
    {
        var repo = Substitute.For<ITerminologyRepository>();
        repo.GetAllActiveTermsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<StyleTerm>(terms));
        return repo;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that passing a null terminology repository to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullTerminologyRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<EditorTerminologyContextStrategy>>();

        // Act
        var act = () => new EditorTerminologyContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("terminologyRepository");
    }

    /// <summary>
    /// Verifies that passing a null token counter to the constructor throws
    /// an <see cref="ArgumentNullException"/> (from base class).
    /// </summary>
    [Fact]
    public void Constructor_NullTokenCounter_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = Substitute.For<ITerminologyRepository>();
        var logger = Substitute.For<ILogger<EditorTerminologyContextStrategy>>();

        // Act
        var act = () => new EditorTerminologyContextStrategy(repo, null!, logger);

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
        var repo = Substitute.For<ITerminologyRepository>();
        var tokenCounter = CreateDefaultTokenCounter();

        // Act
        var act = () => new EditorTerminologyContextStrategy(repo, tokenCounter, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.StrategyId"/> returns "terminology".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsTerminology()
    {
        var sut = CreateStrategy();
        sut.StrategyId.Should().Be("terminology");
    }

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.DisplayName"/> returns "Editor Terminology".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsEditorTerminology()
    {
        var sut = CreateStrategy();
        sut.DisplayName.Should().Be("Editor Terminology");
    }

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.Priority"/> returns Medium (60).
    /// </summary>
    [Fact]
    public void Priority_ReturnsMedium()
    {
        var sut = CreateStrategy();
        sut.Priority.Should().Be(StrategyPriority.Medium);
        sut.Priority.Should().Be(60);
    }

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.MaxTokens"/> returns 800.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns800()
    {
        var sut = CreateStrategy();
        sut.MaxTokens.Should().Be(800);
    }

    #endregion

    #region GatherAsync — Validation

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.GatherAsync"/> returns null
    /// when no selected text is provided in the request.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoSelectedText_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = CreateRequest(selectedText: null);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.GatherAsync"/> returns null
    /// when the terminology repository is empty.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EmptyRepository_ReturnsNull()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(); // Empty
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.GatherAsync"/> returns null
    /// when no terms match the selected text.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoMatchingTerms_ReturnsNull()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("kubernetes"),
            CreateTerm("docker"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "The application uses microservices.");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GatherAsync — Term Matching

    /// <summary>
    /// Verifies that matching terms are included in the context fragment.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithMatchingTerms_ReturnsFormattedContext()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("API", replacement: "Application Programming Interface", category: "Terminology"),
            CreateTerm("REST", replacement: "RESTful", category: "Terminology"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "The REST API provides endpoints");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("**API**");
        result.Content.Should().Contain("Application Programming Interface");
        result.Content.Should().Contain("**REST**");
    }

    /// <summary>
    /// Verifies that the fragment SourceId is "terminology".
    /// </summary>
    [Fact]
    public async Task GatherAsync_ReturnsFragment_WithCorrectSourceId()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("API", replacement: "Application Programming Interface"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "The API handles requests");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("terminology");
    }

    /// <summary>
    /// Verifies that the fragment relevance is 0.8.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ReturnsFragment_WithCorrectRelevance()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("API", replacement: "Application Programming Interface"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "The API handles requests");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Relevance.Should().Be(0.8f);
    }

    /// <summary>
    /// Verifies that case-insensitive matching works (default behavior).
    /// </summary>
    [Fact]
    public async Task GatherAsync_CaseInsensitiveMatch_FindsTerm()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("api", replacement: "Application Programming Interface", matchCase: false));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "The API handles requests");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("**api**");
    }

    /// <summary>
    /// Verifies that case-sensitive matching respects the MatchCase flag.
    /// </summary>
    [Fact]
    public async Task GatherAsync_CaseSensitiveMatch_RespectsMatchCase()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("API", replacement: "Application Programming Interface", matchCase: true));
        var sut = CreateStrategy(terminologyRepository: repo);
        // "api" lowercase won't match "API" with MatchCase=true
        var request = CreateRequest(selectedText: "The api handles requests");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that terms with Replacement are formatted as replacement rules.
    /// </summary>
    [Fact]
    public async Task GatherAsync_TermWithReplacement_FormatsAsReplacementRule()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("utilize", replacement: "use", category: "Terminology"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "We utilize this API");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Use \"use\" instead");
    }

    /// <summary>
    /// Verifies that terms without Replacement are formatted as flagged entries.
    /// </summary>
    [Fact]
    public async Task GatherAsync_TermWithoutReplacement_FormatsAsFlagged()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("TBD", replacement: null, severity: "Warning"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "This feature is TBD");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Flagged (Warning)");
    }

    /// <summary>
    /// Verifies that terms with Notes include the notes in the output.
    /// </summary>
    [Fact]
    public async Task GatherAsync_TermWithNotes_IncludesNotes()
    {
        // Arrange
        var repo = CreateRepositoryWithTerms(
            CreateTerm("backend", replacement: "back-end", notes: "Use hyphenated form per style guide"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest(selectedText: "The backend service handles this");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Note: Use hyphenated form per style guide");
    }

    #endregion

    #region GatherAsync — Limits

    /// <summary>
    /// Verifies that the number of matching terms is limited to MaxTerms (15).
    /// </summary>
    [Fact]
    public async Task GatherAsync_ManyMatchingTerms_LimitsToMaxTerms()
    {
        // Arrange — Create 20 terms that all match
        var terms = Enumerable.Range(0, 20)
            .Select(i => CreateTerm($"word{i}", replacement: $"replacement{i}"))
            .ToArray();
        var repo = CreateRepositoryWithTerms(terms);
        var sut = CreateStrategy(terminologyRepository: repo);
        // Selected text containing all 20 terms
        var selectedText = string.Join(" ", Enumerable.Range(0, 20).Select(i => $"word{i}"));
        var request = CreateRequest(selectedText: selectedText);

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // Count bold term entries in the content
        var termCount = result!.Content.Split("**").Length / 2; // Each term is wrapped in **
        termCount.Should().BeLessOrEqualTo(15);
    }

    #endregion

    #region GatherAsync — Error Handling

    /// <summary>
    /// Verifies that an exception in the terminology repository returns null gracefully.
    /// </summary>
    [Fact]
    public async Task GatherAsync_RepositoryThrows_ReturnsNull()
    {
        // Arrange
        var repo = Substitute.For<ITerminologyRepository>();
        repo.GetAllActiveTermsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database error"));
        var sut = CreateStrategy(terminologyRepository: repo);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region FindMatchingTerms — Static Helper

    /// <summary>
    /// Verifies that <see cref="EditorTerminologyContextStrategy.FindMatchingTerms"/>
    /// orders results by severity (Error first, then Warning, then Suggestion).
    /// </summary>
    [Fact]
    public void FindMatchingTerms_OrdersBySeverity()
    {
        // Arrange
        var terms = new[]
        {
            CreateTerm("word1", severity: "Suggestion"),
            CreateTerm("word2", severity: "Error"),
            CreateTerm("word3", severity: "Warning")
        };
        var selectedText = "word1 word2 word3";

        // Act
        var result = EditorTerminologyContextStrategy.FindMatchingTerms(selectedText, terms);

        // Assert
        result.Should().HaveCount(3);
        result[0].Term.Should().Be("word2"); // Error
        result[1].Term.Should().Be("word3"); // Warning
        result[2].Term.Should().Be("word1"); // Suggestion
    }

    /// <summary>
    /// Verifies that empty terms are skipped during matching.
    /// </summary>
    [Fact]
    public void FindMatchingTerms_SkipsEmptyTerms()
    {
        // Arrange
        var terms = new[]
        {
            CreateTerm(""),
            CreateTerm("API")
        };
        var selectedText = "The API handles requests";

        // Act
        var result = EditorTerminologyContextStrategy.FindMatchingTerms(selectedText, terms);

        // Assert
        result.Should().HaveCount(1);
        result[0].Term.Should().Be("API");
    }

    #endregion

    #region FormatTermsAsContext — Static Helper

    /// <summary>
    /// Verifies that the formatted output includes section headers and term entries.
    /// </summary>
    [Fact]
    public void FormatTermsAsContext_FormatsWithHeaders()
    {
        // Arrange
        var terms = new List<StyleTerm>
        {
            CreateTerm("API", replacement: "Application Programming Interface", category: "Terminology"),
            CreateTerm("utilize", replacement: "use", category: "Style")
        };

        // Act
        var result = EditorTerminologyContextStrategy.FormatTermsAsContext(terms);

        // Assert
        result.Should().Contain("## Terminology Guidelines");
        result.Should().Contain("### Terminology");
        result.Should().Contain("### Style");
    }

    #endregion
}
