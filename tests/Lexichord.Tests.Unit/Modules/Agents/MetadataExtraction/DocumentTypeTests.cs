// -----------------------------------------------------------------------
// <copyright file="DocumentTypeTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.MetadataExtraction;

/// <summary>
/// Unit tests for <see cref="DocumentType"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6b")]
public class DocumentTypeTests
{
    // ── Enum Values ─────────────────────────────────────────────────────

    [Fact]
    public void DocumentType_HasExpectedValues()
    {
        // Assert - Verify all expected enum values exist
        Enum.GetValues<DocumentType>().Should().HaveCount(15);
    }

    [Theory]
    [InlineData(DocumentType.Unknown, 0)]
    [InlineData(DocumentType.Article, 1)]
    [InlineData(DocumentType.Tutorial, 2)]
    [InlineData(DocumentType.HowTo, 3)]
    [InlineData(DocumentType.Reference, 4)]
    [InlineData(DocumentType.APIDocumentation, 5)]
    [InlineData(DocumentType.Specification, 6)]
    [InlineData(DocumentType.Report, 7)]
    [InlineData(DocumentType.Whitepaper, 8)]
    [InlineData(DocumentType.Proposal, 9)]
    [InlineData(DocumentType.Meeting, 10)]
    [InlineData(DocumentType.Notes, 11)]
    [InlineData(DocumentType.Readme, 12)]
    [InlineData(DocumentType.Changelog, 13)]
    [InlineData(DocumentType.Other, 14)]
    public void DocumentType_HasExpectedNumericValue(DocumentType type, int expectedValue)
    {
        // Assert
        ((int)type).Should().Be(expectedValue);
    }

    // ── Default Value ───────────────────────────────────────────────────

    [Fact]
    public void DocumentType_DefaultIsUnknown()
    {
        // Arrange & Act
        var defaultType = default(DocumentType);

        // Assert
        defaultType.Should().Be(DocumentType.Unknown);
    }

    // ── String Parsing ──────────────────────────────────────────────────

    [Theory]
    [InlineData("Unknown", DocumentType.Unknown)]
    [InlineData("Article", DocumentType.Article)]
    [InlineData("Tutorial", DocumentType.Tutorial)]
    [InlineData("HowTo", DocumentType.HowTo)]
    [InlineData("Reference", DocumentType.Reference)]
    [InlineData("APIDocumentation", DocumentType.APIDocumentation)]
    [InlineData("Specification", DocumentType.Specification)]
    [InlineData("Report", DocumentType.Report)]
    [InlineData("Whitepaper", DocumentType.Whitepaper)]
    [InlineData("Proposal", DocumentType.Proposal)]
    [InlineData("Meeting", DocumentType.Meeting)]
    [InlineData("Notes", DocumentType.Notes)]
    [InlineData("Readme", DocumentType.Readme)]
    [InlineData("Changelog", DocumentType.Changelog)]
    [InlineData("Other", DocumentType.Other)]
    public void DocumentType_CanBeParsedFromString(string input, DocumentType expected)
    {
        // Act
        var parsed = Enum.Parse<DocumentType>(input);

        // Assert
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown", DocumentType.Unknown)]
    [InlineData("ARTICLE", DocumentType.Article)]
    [InlineData("tutorial", DocumentType.Tutorial)]
    [InlineData("HOWTO", DocumentType.HowTo)]
    public void DocumentType_ParseIgnoresCase(string input, DocumentType expected)
    {
        // Act
        var success = Enum.TryParse<DocumentType>(input, ignoreCase: true, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Blog")]
    [InlineData("Documentation")]
    [InlineData("")]
    public void DocumentType_InvalidString_FailsToParse(string input)
    {
        // Act
        var success = Enum.TryParse<DocumentType>(input, out _);

        // Assert
        success.Should().BeFalse();
    }

    // ── String Conversion ───────────────────────────────────────────────

    [Theory]
    [InlineData(DocumentType.Unknown, "Unknown")]
    [InlineData(DocumentType.Article, "Article")]
    [InlineData(DocumentType.Tutorial, "Tutorial")]
    [InlineData(DocumentType.APIDocumentation, "APIDocumentation")]
    public void DocumentType_ToStringReturnsName(DocumentType type, string expected)
    {
        // Act
        var result = type.ToString();

        // Assert
        result.Should().Be(expected);
    }
}
