// -----------------------------------------------------------------------
// <copyright file="KnowledgeContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Modules.Agents.Context.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context.Strategies;

/// <summary>
/// Unit tests for <see cref="KnowledgeContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify constructor validation, property values, GatherAsync behavior
/// including search query construction, agent-specific configuration, license
/// restrictions, hint overrides, error handling, and result formatting.
/// Introduced in v0.7.2e.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2e")]
public class KnowledgeContextStrategyTests
{
    #region Helper Factories

    /// <summary>
    /// Creates a <see cref="KnowledgeContextStrategy"/> with optional mock overrides.
    /// </summary>
    private static KnowledgeContextStrategy CreateStrategy(
        IKnowledgeContextProvider? contextProvider = null,
        ILicenseContext? license = null,
        ITokenCounter? tokenCounter = null,
        ILogger<KnowledgeContextStrategy>? logger = null)
    {
        contextProvider ??= Substitute.For<IKnowledgeContextProvider>();
        license ??= CreateTeamsLicense();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<KnowledgeContextStrategy>>();
        return new KnowledgeContextStrategy(contextProvider, license, tokenCounter, logger);
    }

    /// <summary>
    /// Creates an <see cref="ITokenCounter"/> mock that estimates tokens at ~4 chars/token.
    /// </summary>
    private static ITokenCounter CreateDefaultTokenCounter()
    {
        var tc = Substitute.For<ITokenCounter>();
        tc.CountTokens(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Length / 4);
        return tc;
    }

    /// <summary>
    /// Creates an <see cref="ILicenseContext"/> mock set to Teams tier.
    /// </summary>
    private static ILicenseContext CreateTeamsLicense()
    {
        var license = Substitute.For<ILicenseContext>();
        license.Tier.Returns(LicenseTier.Teams);
        return license;
    }

    /// <summary>
    /// Creates an <see cref="ILicenseContext"/> mock set to WriterPro tier.
    /// </summary>
    private static ILicenseContext CreateWriterProLicense()
    {
        var license = Substitute.For<ILicenseContext>();
        license.Tier.Returns(LicenseTier.WriterPro);
        return license;
    }

    /// <summary>
    /// Creates a <see cref="KnowledgeContext"/> with specified entities and formatted content.
    /// </summary>
    private static KnowledgeContext CreateKnowledgeContext(
        int entityCount = 5,
        string formattedContent = "## Knowledge Entities\n\nentities:\n  - name: TestEntity\n    type: Endpoint",
        bool wasTruncated = false,
        int? relationshipCount = null,
        int? axiomCount = null)
    {
        var entities = Enumerable.Range(0, entityCount)
            .Select(i => new KnowledgeEntity
            {
                Type = "Endpoint",
                Name = $"Entity_{i}"
            })
            .ToList();

        var relationships = relationshipCount.HasValue
            ? Enumerable.Range(0, relationshipCount.Value)
                .Select(i => new KnowledgeRelationship
                {
                    Type = "CONTAINS",
                    FromEntityId = entities.First().Id,
                    ToEntityId = entities.Last().Id
                })
                .ToList()
                .AsReadOnly()
            : null;

        var axioms = axiomCount.HasValue
            ? Enumerable.Range(0, axiomCount.Value)
                .Select(i => new Axiom
                {
                    Id = $"axiom-{i}",
                    Name = $"Axiom {i}",
                    TargetType = "Endpoint",
                    Rules = []
                })
                .ToList()
                .AsReadOnly()
            : null;

        return new KnowledgeContext
        {
            Entities = entities.AsReadOnly(),
            FormattedContext = formattedContent,
            TokenCount = formattedContent.Length / 4,
            WasTruncated = wasTruncated,
            Relationships = relationships as IReadOnlyList<KnowledgeRelationship>,
            Axioms = axioms as IReadOnlyList<Axiom>
        };
    }

    /// <summary>
    /// Creates a <see cref="ContextGatheringRequest"/> with optional parameters.
    /// </summary>
    private static ContextGatheringRequest CreateRequest(
        string? documentPath = null,
        int? cursorPosition = null,
        string? selectedText = null,
        string agentId = "editor",
        Dictionary<string, object>? hints = null)
    {
        return new ContextGatheringRequest(
            documentPath, cursorPosition, selectedText, agentId, hints);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that passing a null context provider to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullContextProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var license = CreateTeamsLicense();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<KnowledgeContextStrategy>>();

        // Act
        var act = () => new KnowledgeContextStrategy(null!, license, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contextProvider");
    }

    /// <summary>
    /// Verifies that passing a null license context to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullLicense_ThrowsArgumentNullException()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<KnowledgeContextStrategy>>();

        // Act
        var act = () => new KnowledgeContextStrategy(contextProvider, null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("license");
    }

    /// <summary>
    /// Verifies that passing a null token counter to the constructor throws
    /// an <see cref="ArgumentNullException"/> (base class guard).
    /// </summary>
    [Fact]
    public void Constructor_NullTokenCounter_ThrowsArgumentNullException()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var license = CreateTeamsLicense();
        var logger = Substitute.For<ILogger<KnowledgeContextStrategy>>();

        // Act
        var act = () => new KnowledgeContextStrategy(contextProvider, license, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenCounter");
    }

    /// <summary>
    /// Verifies that passing a null logger to the constructor throws
    /// an <see cref="ArgumentNullException"/> (base class guard).
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var license = CreateTeamsLicense();
        var tokenCounter = CreateDefaultTokenCounter();

        // Act
        var act = () => new KnowledgeContextStrategy(contextProvider, license, tokenCounter, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructing with valid parameters succeeds without error.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var license = CreateTeamsLicense();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<KnowledgeContextStrategy>>();

        // Act
        var sut = new KnowledgeContextStrategy(contextProvider, license, tokenCounter, logger);

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.StrategyId"/> returns "knowledge".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsKnowledge()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("knowledge");
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.DisplayName"/> returns "Knowledge Graph".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsKnowledgeGraph()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Knowledge Graph");
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.Priority"/> returns 30.
    /// </summary>
    [Fact]
    public void Priority_Returns30()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(30);
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.MaxTokens"/> returns 4000.
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

    #region GatherAsync Tests — Query Building

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> returns null
    /// when the request has no selected text and no document path.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoSelectionAndNoDocument_ReturnsNull()
    {
        // Arrange
        var sut = CreateStrategy();
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> uses the selected text
    /// as the search query when a selection is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithSelection_UsesSelectionAsQuery()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var knowledgeContext = CreateKnowledgeContext();

        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(knowledgeContext);

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(
            documentPath: "/docs/chapter1.md",
            selectedText: "GET /api/users endpoint");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            "GET /api/users endpoint",
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> uses the document
    /// file name (without extension) as the search query when no selection is provided.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithDocumentOnly_UsesFileNameAsQuery()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var knowledgeContext = CreateKnowledgeContext();

        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(knowledgeContext);

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(documentPath: "/docs/api-reference.md");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            "api-reference",
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the selected text query is truncated to 200 characters.
    /// </summary>
    [Fact]
    public async Task GatherAsync_LongSelection_TruncatesQueryTo200Chars()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var knowledgeContext = CreateKnowledgeContext();

        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(knowledgeContext);

        var sut = CreateStrategy(contextProvider: contextProvider);
        var longSelection = new string('x', 300);
        var request = CreateRequest(selectedText: longSelection);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Is<string>(q => q.Length == 200),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GatherAsync Tests — Results

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> returns null
    /// when the context provider returns no entities.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoEntities_ReturnsNull()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(KnowledgeContext.Empty);

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query text");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> returns null
    /// when the formatted context is empty.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EmptyFormattedContent_ReturnsNull()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var knowledgeContext = CreateKnowledgeContext(
            entityCount: 3,
            formattedContent: "");

        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(knowledgeContext);

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query text");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> returns a fragment
    /// with the formatted knowledge content when entities are found.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithEntities_ReturnsFormattedFragment()
    {
        // Arrange
        var formattedContent = "## Knowledge Entities\n\nentities:\n  - name: UserAPI\n    type: Endpoint";
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        var knowledgeContext = CreateKnowledgeContext(
            entityCount: 3,
            formattedContent: formattedContent);

        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(knowledgeContext);

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "GET /api/users");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Knowledge Entities");
        result.Content.Should().Contain("UserAPI");
    }

    /// <summary>
    /// Verifies that the returned fragment has SourceId set to "knowledge".
    /// </summary>
    [Fact]
    public async Task GatherAsync_Fragment_HasCorrectSourceId()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("knowledge");
    }

    /// <summary>
    /// Verifies that the returned fragment has Label set to "Knowledge Graph".
    /// </summary>
    [Fact]
    public async Task GatherAsync_Fragment_HasCorrectLabel()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Label.Should().Be("Knowledge Graph");
    }

    /// <summary>
    /// Verifies that the relevance is 0.7 when the context was budget-truncated.
    /// </summary>
    [Fact]
    public async Task GatherAsync_TruncatedContext_RelevanceIs07()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext(entityCount: 20, wasTruncated: true));

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Relevance.Should().BeApproximately(0.7f, 0.001f);
    }

    /// <summary>
    /// Verifies that relevance scales with entity count when not truncated.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FewEntities_RelevanceScalesWithCount()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext(entityCount: 3, wasTruncated: false));

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Relevance.Should().BeApproximately(0.3f, 0.001f); // 3/10 = 0.3
    }

    /// <summary>
    /// Verifies that relevance is capped at 1.0 when many entities are found.
    /// </summary>
    [Fact]
    public async Task GatherAsync_ManyEntities_RelevanceCappedAt1()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext(entityCount: 15, wasTruncated: false));

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Relevance.Should().Be(1.0f); // 15/10 > 1.0, capped
    }

    #endregion

    #region GatherAsync Tests — Error Handling

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> returns null
    /// when the context provider throws <see cref="FeatureNotLicensedException"/>.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FeatureNotLicensed_ReturnsNull()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new FeatureNotLicensedException(
                "Knowledge graph requires Teams license.",
                LicenseTier.Teams));

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> returns null
    /// when the context provider throws a general exception.
    /// </summary>
    [Fact]
    public async Task GatherAsync_GeneralException_ReturnsNull()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Graph database unavailable"));

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="KnowledgeContextStrategy.GatherAsync"/> re-throws
    /// <see cref="OperationCanceledException"/> instead of swallowing it.
    /// </summary>
    [Fact]
    public async Task GatherAsync_OperationCanceled_Rethrows()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        var act = () => sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GatherAsync Tests — Agent Configuration

    /// <summary>
    /// Verifies that the editor agent configuration uses MaxEntities=30.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EditorAgent_UsesEditorConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query", agentId: "editor");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.MaxEntities == 30),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the simplifier agent configuration uses MaxEntities=10
    /// and disables axioms and relationships.
    /// </summary>
    [Fact]
    public async Task GatherAsync_SimplifierAgent_UsesSimplifierConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query", agentId: "simplifier");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o =>
                o.MaxEntities == 10 &&
                o.IncludeAxioms == false &&
                o.IncludeRelationships == false),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the simplifier agent filters to Concept, Term, Definition entity types.
    /// </summary>
    [Fact]
    public async Task GatherAsync_SimplifierAgent_FiltersEntityTypes()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query", agentId: "simplifier");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o =>
                o.EntityTypes != null &&
                o.EntityTypes.Contains("Concept") &&
                o.EntityTypes.Contains("Term") &&
                o.EntityTypes.Contains("Definition")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an unknown agent ID falls back to default configuration.
    /// </summary>
    [Fact]
    public async Task GatherAsync_UnknownAgent_UsesDefaultConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query", agentId: "unknown-agent");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — default config has MaxEntities=20
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.MaxEntities == 20),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the co-pilot agent configuration uses MaxEntities=25.
    /// </summary>
    [Fact]
    public async Task GatherAsync_CoPilotAgent_UsesCoPilotConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query", agentId: "co-pilot");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.MaxEntities == 25),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GatherAsync Tests — License Restrictions

    /// <summary>
    /// Verifies that WriterPro tier restricts MaxEntities to 10 and disables
    /// axioms and relationships.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WriterProTier_AppliesRestrictions()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var license = CreateWriterProLicense();
        var sut = CreateStrategy(contextProvider: contextProvider, license: license);
        // Editor config requests 30 entities + axioms + rels
        var request = CreateRequest(selectedText: "query", agentId: "editor");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — WriterPro caps at 10 entities, no axioms, no rels
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o =>
                o.MaxEntities == 10 &&
                o.IncludeAxioms == false &&
                o.IncludeRelationships == false),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that Teams tier does not apply any restrictions.
    /// </summary>
    [Fact]
    public async Task GatherAsync_TeamsTier_NoRestrictions()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var license = CreateTeamsLicense();
        var sut = CreateStrategy(contextProvider: contextProvider, license: license);
        var request = CreateRequest(selectedText: "query", agentId: "editor");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — Teams gets full editor config (30 entities + axioms + rels)
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o =>
                o.MaxEntities == 30 &&
                o.IncludeAxioms == true &&
                o.IncludeRelationships == true),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that WriterPro restricts a small agent config's entity count
    /// when it's already below 10 (uses Math.Min).
    /// </summary>
    [Fact]
    public async Task GatherAsync_WriterProTier_DoesNotIncreaseSmallEntityCount()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var license = CreateWriterProLicense();
        var sut = CreateStrategy(contextProvider: contextProvider, license: license);
        // Simplifier already has MaxEntities=10, WriterPro cap is also 10
        var request = CreateRequest(selectedText: "query", agentId: "simplifier");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — Math.Min(10, 10) = 10
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.MaxEntities == 10),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GatherAsync Tests — Hint Overrides

    /// <summary>
    /// Verifies that the MaxEntities hint overrides the agent config value.
    /// </summary>
    [Fact]
    public async Task GatherAsync_MaxEntitiesHint_OverridesConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var hints = new Dictionary<string, object> { { "MaxEntities", 5 } };
        var request = CreateRequest(selectedText: "query", hints: hints);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — default config has 20, hint overrides to 5
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.MaxEntities == 5),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the MinRelevanceScore hint overrides the agent config value.
    /// </summary>
    [Fact]
    public async Task GatherAsync_MinRelevanceScoreHint_OverridesConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var hints = new Dictionary<string, object> { { "MinRelevanceScore", 0.8f } };
        var request = CreateRequest(selectedText: "query", hints: hints);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — default config has 0.5, hint overrides to 0.8
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o =>
                Math.Abs(o.MinRelevanceScore - 0.8f) < 0.001f),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the IncludeAxioms hint overrides the agent config value.
    /// </summary>
    [Fact]
    public async Task GatherAsync_IncludeAxiomsHint_OverridesConfig()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        // Default editor config has IncludeAxioms=true, override to false
        var hints = new Dictionary<string, object> { { "IncludeAxioms", false } };
        var request = CreateRequest(selectedText: "query", agentId: "editor", hints: hints);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.IncludeAxioms == false),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the KnowledgeFormat hint overrides the output format.
    /// </summary>
    [Fact]
    public async Task GatherAsync_KnowledgeFormatHint_OverridesFormat()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var hints = new Dictionary<string, object> { { "KnowledgeFormat", "Markdown" } };
        var request = CreateRequest(selectedText: "query", hints: hints);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — default format is Yaml, override to Markdown
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.Format == ContextFormat.Markdown),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an invalid KnowledgeFormat hint string falls back to default.
    /// </summary>
    [Fact]
    public async Task GatherAsync_InvalidFormatHint_FallsBackToDefault()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var hints = new Dictionary<string, object> { { "KnowledgeFormat", "InvalidFormat" } };
        var request = CreateRequest(selectedText: "query", hints: hints);

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — invalid format falls back to Yaml (default)
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.Format == ContextFormat.Yaml),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetConfigForAgent Tests

    /// <summary>
    /// Verifies that GetConfigForAgent returns the correct editor config.
    /// </summary>
    [Fact]
    public void GetConfigForAgent_Editor_ReturnsEditorConfig()
    {
        // Act
        var config = KnowledgeContextStrategy.GetConfigForAgent("editor");

        // Assert
        config.MaxEntities.Should().Be(30);
        config.MaxTokens.Should().Be(5000);
        config.IncludeAxioms.Should().BeTrue();
        config.IncludeRelationships.Should().BeTrue();
        config.MinRelevanceScore.Should().BeApproximately(0.4f, 0.001f);
    }

    /// <summary>
    /// Verifies that GetConfigForAgent returns the correct simplifier config.
    /// </summary>
    [Fact]
    public void GetConfigForAgent_Simplifier_ReturnsSimplifierConfig()
    {
        // Act
        var config = KnowledgeContextStrategy.GetConfigForAgent("simplifier");

        // Assert
        config.MaxEntities.Should().Be(10);
        config.MaxTokens.Should().Be(2000);
        config.IncludeAxioms.Should().BeFalse();
        config.IncludeRelationships.Should().BeFalse();
        config.MinRelevanceScore.Should().BeApproximately(0.6f, 0.001f);
        config.IncludeEntityTypes.Should().Contain("Concept");
        config.IncludeEntityTypes.Should().Contain("Term");
        config.IncludeEntityTypes.Should().Contain("Definition");
    }

    /// <summary>
    /// Verifies that GetConfigForAgent returns the correct tuning config.
    /// </summary>
    [Fact]
    public void GetConfigForAgent_Tuning_ReturnsTuningConfig()
    {
        // Act
        var config = KnowledgeContextStrategy.GetConfigForAgent("tuning");

        // Assert
        config.MaxEntities.Should().Be(20);
        config.IncludeEntityTypes.Should().Contain("Endpoint");
        config.IncludeEntityTypes.Should().Contain("Parameter");
    }

    /// <summary>
    /// Verifies that GetConfigForAgent returns default config for unknown agent IDs.
    /// </summary>
    [Fact]
    public void GetConfigForAgent_Unknown_ReturnsDefaultConfig()
    {
        // Act
        var config = KnowledgeContextStrategy.GetConfigForAgent("unknown-agent");

        // Assert
        config.MaxEntities.Should().Be(20);
        config.MaxTokens.Should().Be(4000);
        config.IncludeAxioms.Should().BeTrue();
        config.IncludeRelationships.Should().BeTrue();
        config.MinRelevanceScore.Should().BeApproximately(0.5f, 0.001f);
        config.IncludeEntityTypes.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetConfigForAgent returns the correct summarizer config.
    /// </summary>
    [Fact]
    public void GetConfigForAgent_Summarizer_ReturnsSummarizerConfig()
    {
        // Act
        var config = KnowledgeContextStrategy.GetConfigForAgent("summarizer");

        // Assert
        config.MaxEntities.Should().Be(15);
        config.MaxTokens.Should().Be(3000);
        config.IncludeAxioms.Should().BeFalse();
        config.IncludeRelationships.Should().BeTrue();
        config.IncludeEntityTypes.Should().Contain("Product");
        config.IncludeEntityTypes.Should().Contain("Component");
    }

    /// <summary>
    /// Verifies that GetConfigForAgent returns the correct co-pilot config.
    /// </summary>
    [Fact]
    public void GetConfigForAgent_CoPilot_ReturnsCoPilotConfig()
    {
        // Act
        var config = KnowledgeContextStrategy.GetConfigForAgent("co-pilot");

        // Assert
        config.MaxEntities.Should().Be(25);
        config.MaxTokens.Should().Be(4000);
        config.IncludeAxioms.Should().BeTrue();
        config.IncludeRelationships.Should().BeTrue();
    }

    #endregion

    #region GatherAsync Tests — Options Building

    /// <summary>
    /// Verifies that the default format passed to the provider is YAML.
    /// </summary>
    [Fact]
    public async Task GatherAsync_DefaultFormat_IsYaml()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        var request = CreateRequest(selectedText: "query");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.Format == ContextFormat.Yaml),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that entity type filtering uses IReadOnlySet when types are specified.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithEntityTypes_PassesAsReadOnlySet()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        // Tuning agent has entity types
        var request = CreateRequest(selectedText: "query", agentId: "tuning");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — EntityTypes should be non-null and contain the expected types
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o =>
                o.EntityTypes != null &&
                o.EntityTypes.Count == 4),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that entity type filtering passes null when no types are specified.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithoutEntityTypes_PassesNullSet()
    {
        // Arrange
        var contextProvider = Substitute.For<IKnowledgeContextProvider>();
        contextProvider.GetContextAsync(
            Arg.Any<string>(),
            Arg.Any<KnowledgeContextOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateKnowledgeContext());

        var sut = CreateStrategy(contextProvider: contextProvider);
        // Editor agent has no entity type filter (all types)
        var request = CreateRequest(selectedText: "query", agentId: "editor");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert — EntityTypes should be null (all types)
        await contextProvider.Received(1).GetContextAsync(
            Arg.Any<string>(),
            Arg.Is<KnowledgeContextOptions>(o => o.EntityTypes == null),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
