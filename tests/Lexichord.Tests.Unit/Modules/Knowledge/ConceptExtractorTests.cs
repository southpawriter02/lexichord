// =============================================================================
// File: ConceptExtractorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the ConceptExtractor entity extractor.
// =============================================================================
// LOGIC: Validates that ConceptExtractor correctly identifies domain concept
//   mentions in text using four detection strategies:
//   - Defined terms (confidence 0.9)
//   - Acronyms with definitions (confidence 0.95)
//   - Glossary-style entries (confidence 0.85)
//   - Capitalized multi-word terms, discovery mode only (confidence 0.5)
//   Also validates filtering of common words and phrases.
//
// Test Categories:
//   - Defined term extraction
//   - Acronym extraction
//   - Glossary entry extraction
//   - Capitalized term extraction (discovery mode)
//   - Common word filtering
//   - Common phrase filtering
//   - Discovery mode toggle
//   - Empty/whitespace text handling
//   - Extractor metadata (SupportedTypes, Priority)
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction.Extractors;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="ConceptExtractor"/> entity extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5g")]
public sealed class ConceptExtractorTests
{
    private readonly ConceptExtractor _extractor = new();
    private readonly ExtractionContext _context = new();
    private readonly ExtractionContext _discoveryContext = new() { DiscoveryMode = true };

    #region Extractor Metadata

    [Fact]
    public void SupportedTypes_ContainsConcept()
    {
        // Assert
        _extractor.SupportedTypes.Should().ContainSingle("Concept");
    }

    [Fact]
    public void Priority_Is50()
    {
        // Assert
        _extractor.Priority.Should().Be(50);
    }

    #endregion

    #region Defined Term Extraction

    [Fact]
    public async Task ExtractAsync_DefinedTerm_CalledPattern_ExtractsWithConfidence09()
    {
        // Arrange
        var text = "This technique is called Rate Limiting.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(0.9f);
        mentions[0].EntityType.Should().Be("Concept");
        mentions[0].Value.Should().Contain("Rate Limiting");
    }

    [Fact]
    public async Task ExtractAsync_DefinedTerm_KnownAsPattern_Extracts()
    {
        // Arrange
        var text = "The protocol is known as OAuth.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Value.Should().Contain("OAuth");
    }

    [Fact]
    public async Task ExtractAsync_DefinedTerm_TermedPattern_Extracts()
    {
        // Arrange
        var text = "This behavior is termed Idempotent.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
    }

    #endregion

    #region Acronym Extraction

    [Fact]
    public async Task ExtractAsync_AcronymWithDefinition_ExtractsWithConfidence095()
    {
        // Arrange
        var text = "API (Application Programming Interface) is a common concept.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(0.95f);
        mentions[0].Value.Should().Be("API");
        mentions[0].Properties.Should().ContainKey("definition");
        mentions[0].Properties["definition"].Should().Be("Application Programming Interface");
    }

    [Fact]
    public async Task ExtractAsync_MultipleAcronyms_ExtractsAll()
    {
        // Arrange
        var text = "REST (Representational State Transfer) and SOAP (Simple Object Access Protocol) are both used.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().HaveCount(2);
        mentions.Should().Contain(m => m.Value == "REST");
        mentions.Should().Contain(m => m.Value == "SOAP");
    }

    #endregion

    #region Glossary Entry Extraction

    [Fact]
    public async Task ExtractAsync_GlossaryEntry_ExtractsWithConfidence085()
    {
        // Arrange
        var text = "**Rate Limiting**: Controls request frequency to prevent abuse.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(0.85f);
        mentions[0].Properties.Should().ContainKey("definition");
    }

    [Fact]
    public async Task ExtractAsync_GlossaryEntryWithDash_Extracts()
    {
        // Arrange
        var text = "OAuth - Authorization protocol for secure API access.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
    }

    #endregion

    #region Capitalized Term Extraction (Discovery Mode)

    [Fact]
    public async Task ExtractAsync_CapitalizedTerm_DiscoveryMode_ExtractsWithConfidence05()
    {
        // Arrange — start the sentence with a lowercase word so the
        // capitalized term regex only captures "Service Mesh".
        var text = "Using a Service Mesh pattern is widely adopted in microservices.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _discoveryContext);

        // Assert
        mentions.Should().Contain(m => m.Value == "Service Mesh" && m.Confidence == 0.5f);
    }

    [Fact]
    public async Task ExtractAsync_CapitalizedTerm_NonDiscoveryMode_Skipped()
    {
        // Arrange
        var text = "The Service Mesh pattern is widely used.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        // In non-discovery mode, capitalized terms are not extracted
        mentions.Where(m => m.Value == "Service Mesh").Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_CapitalizedCommonPhrase_Filtered()
    {
        // Arrange
        var text = "For Example the term is important. In Addition to the concept.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _discoveryContext);

        // Assert
        mentions.Where(m => m.Value == "For Example").Should().BeEmpty();
        mentions.Where(m => m.Value == "In Addition").Should().BeEmpty();
    }

    #endregion

    #region Validation and Filtering

    [Fact]
    public void IsValidConcept_CommonWord_False()
    {
        // Assert
        ConceptExtractor.IsValidConcept("Note").Should().BeFalse();
        ConceptExtractor.IsValidConcept("Example").Should().BeFalse();
        ConceptExtractor.IsValidConcept("This").Should().BeFalse();
    }

    [Fact]
    public void IsValidConcept_ShortTerm_False()
    {
        // Assert
        ConceptExtractor.IsValidConcept("AB").Should().BeFalse();
        ConceptExtractor.IsValidConcept("X").Should().BeFalse();
    }

    [Fact]
    public void IsValidConcept_AllUppercase_False()
    {
        // Assert
        ConceptExtractor.IsValidConcept("API").Should().BeFalse();
        ConceptExtractor.IsValidConcept("REST").Should().BeFalse();
    }

    [Fact]
    public void IsValidConcept_ValidTerm_True()
    {
        // Assert
        ConceptExtractor.IsValidConcept("Rate Limiting").Should().BeTrue();
        ConceptExtractor.IsValidConcept("OAuth").Should().BeTrue();
        ConceptExtractor.IsValidConcept("Idempotent").Should().BeTrue();
    }

    #endregion

    #region Deduplication

    [Fact]
    public async Task ExtractAsync_SameConceptDifferentPatterns_Deduplicated()
    {
        // Arrange — "OAuth" appears in defined term AND glossary
        var text = "This protocol is called OAuth.\nOAuth - An authorization protocol.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert — should only appear once
        mentions.Where(m => m.NormalizedValue != null &&
            m.NormalizedValue.Contains("oauth", StringComparison.OrdinalIgnoreCase))
            .Should().HaveCount(1);
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
    public async Task ExtractAsync_TextWithNoConcepts_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("just a simple lowercase sentence.", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    #endregion
}
