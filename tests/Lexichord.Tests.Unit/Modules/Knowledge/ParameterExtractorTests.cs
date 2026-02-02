// =============================================================================
// File: ParameterExtractorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the ParameterExtractor entity extractor.
// =============================================================================
// LOGIC: Validates that ParameterExtractor correctly identifies API parameter
//   mentions in text using five detection strategies:
//   - Explicit definitions (confidence 1.0)
//   - Inline code parameters (confidence 0.9)
//   - Path parameters (confidence 0.95)
//   - Query parameters (confidence 0.85)
//   - JSON properties (confidence 0.6)
//   Also validates deduplication and meta-property filtering.
//
// Test Categories:
//   - Explicit definition extraction
//   - Inline code parameter extraction
//   - Path parameter extraction
//   - Query parameter extraction
//   - JSON property extraction
//   - Meta-property filtering
//   - Cross-pattern deduplication
//   - Empty/whitespace text handling
//   - Extractor metadata (SupportedTypes, Priority)
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction.Extractors;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="ParameterExtractor"/> entity extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5g")]
public sealed class ParameterExtractorTests
{
    private readonly ParameterExtractor _extractor = new();
    private readonly ExtractionContext _context = new();

    #region Extractor Metadata

    [Fact]
    public void SupportedTypes_ContainsParameter()
    {
        // Assert
        _extractor.SupportedTypes.Should().ContainSingle("Parameter");
    }

    [Fact]
    public void Priority_Is90()
    {
        // Assert
        _extractor.Priority.Should().Be(90);
    }

    #endregion

    #region Explicit Definition Extraction

    [Fact]
    public async Task ExtractAsync_ExplicitDefinition_ExtractsWithConfidence1()
    {
        // Arrange
        var text = "The parameter limit controls pagination.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(1.0f);
        mentions[0].Value.Should().Be("limit");
        mentions[0].Properties["location"].Should().Be("body");
    }

    [Fact]
    public async Task ExtractAsync_ExplicitDefinitionWithType_ExtractsType()
    {
        // Arrange
        var text = "The param offset (integer) specifies the starting position.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Value.Should().Be("offset");
        mentions[0].Properties.Should().ContainKey("type");
        mentions[0].Properties["type"].Should().Be("integer");
    }

    [Fact]
    public async Task ExtractAsync_ArgumentKeyword_Extracts()
    {
        // Arrange
        var text = "The argument userId is required.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Value.Should().Be("userId");
    }

    #endregion

    #region Inline Code Parameter Extraction

    [Fact]
    public async Task ExtractAsync_InlineCodeParam_ExtractsWithConfidence09()
    {
        // Arrange — use text where the inline code pattern matches
        // but the explicit definition pattern does not.
        var text = "Set the `limit` parameter to restrict results.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().Contain(m => m.Value == "limit" && m.Confidence == 0.9f);
    }

    [Fact]
    public async Task ExtractAsync_InlineCodeField_Extracts()
    {
        // Arrange
        var text = "The `email` field must be unique.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Value.Should().Be("email");
    }

    #endregion

    #region Path Parameter Extraction

    [Fact]
    public async Task ExtractAsync_PathParameters_ExtractsWithConfidence095()
    {
        // Arrange
        var text = "/users/{userId}/orders/{orderId}";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().HaveCount(2);
        mentions.Should().Contain(m => m.Value == "userId" && m.Confidence == 0.95f);
        mentions.Should().Contain(m => m.Value == "orderId" && m.Confidence == 0.95f);
        mentions.Should().OnlyContain(m => (string)m.Properties["location"] == "path");
    }

    [Fact]
    public async Task ExtractAsync_PathParameter_HasStringType()
    {
        // Arrange
        var text = "/items/{id}";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Properties["type"].Should().Be("string");
    }

    #endregion

    #region Query Parameter Extraction

    [Fact]
    public async Task ExtractAsync_QueryParameters_ExtractsWithConfidence085()
    {
        // Arrange
        var text = "/users?page=1&limit=20&sort=name";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().Contain(m => m.Value == "page" && m.Confidence == 0.85f);
        mentions.Should().Contain(m => m.Value == "limit" && m.Confidence == 0.85f);
        mentions.Should().Contain(m => m.Value == "sort" && m.Confidence == 0.85f);
    }

    [Fact]
    public async Task ExtractAsync_QueryParameter_HasQueryLocation()
    {
        // Arrange
        var text = "?filter=active";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Properties["location"].Should().Be("query");
    }

    #endregion

    #region JSON Property Extraction

    [Fact]
    public async Task ExtractAsync_JsonProperty_ExtractsWithConfidence06()
    {
        // Arrange
        var text = """{"email": "test@example.com", "age": 25}""";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().Contain(m => m.Value == "email" && m.Confidence == 0.6f);
        mentions.Should().Contain(m => m.Value == "age" && m.Confidence == 0.6f);
    }

    [Fact]
    public async Task ExtractAsync_JsonMetaProperties_Filtered()
    {
        // Arrange — "type", "name", "id" are common meta-properties
        var text = """{"type": "user", "name": "John", "id": "123"}""";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_JsonMixedProperties_FiltersMetaOnly()
    {
        // Arrange
        var text = """{"type": "user", "email": "test@example.com"}""";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Value.Should().Be("email");
    }

    #endregion

    #region Cross-Pattern Deduplication

    [Fact]
    public async Task ExtractAsync_SameParamDifferentPatterns_DeduplicatedToFirstSeen()
    {
        // Arrange — "limit" appears in explicit definition AND query string
        var text = "The parameter limit is used like ?limit=10";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert — should only appear once (from explicit definition, confidence 1.0)
        mentions.Where(m => m.Value.Equals("limit", StringComparison.OrdinalIgnoreCase))
            .Should().HaveCount(1);
        mentions.First(m => m.Value.Equals("limit", StringComparison.OrdinalIgnoreCase))
            .Confidence.Should().Be(1.0f);
    }

    #endregion

    #region Empty/Whitespace Text

    [Fact]
    public async Task ExtractAsync_EmptyText_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_WhitespaceText_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("   \n\t  ", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_TextWithNoParameters_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("No parameters in this sentence.", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    #endregion

    #region Normalized Value

    [Fact]
    public async Task ExtractAsync_NormalizedValue_IsLowercase()
    {
        // Arrange
        var text = "/items/{UserId}";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].NormalizedValue.Should().Be("userid");
    }

    #endregion
}
