using FluentAssertions;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.Style.Filters;

/// <summary>
/// Unit tests for <see cref="YamlFrontmatterFilter"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// - Empty and null content handling
/// - YAML frontmatter detection (--- delimited)
/// - TOML frontmatter detection (+++ delimited)
/// - JSON frontmatter detection ({ on first line)
/// - Edge cases: BOM, CRLF, unclosed frontmatter, etc.
/// - CanFilter for file extensions
/// - Properties (Name, Priority)
/// - Performance with large content
///
/// Version: v0.2.7c
/// </remarks>
public sealed class YamlFrontmatterFilterTests
{
    private readonly YamlFrontmatterFilter _sut;
    private readonly ILogger<YamlFrontmatterFilter> _logger;

    public YamlFrontmatterFilterTests()
    {
        _logger = NullLogger<YamlFrontmatterFilter>.Instance;
        _sut = new YamlFrontmatterFilter(_logger);
    }

    #region Empty and No Frontmatter

    [Fact]
    public void Filter_EmptyContent_ReturnsNoExclusions()
    {
        // Act
        var result = _sut.Filter("", ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeFalse();
        result.ExcludedRegions.Should().BeEmpty();
    }

    [Fact]
    public void Filter_NullContent_ReturnsNoExclusions()
    {
        // Act
        var result = _sut.Filter(null!, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeFalse();
    }

    [Fact]
    public void Filter_NoFrontmatter_ReturnsNoExclusions()
    {
        // Arrange
        var content = "This is plain prose with no frontmatter at all.";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeFalse();
        result.ProcessedContent.Should().Be(content);
        result.OriginalContent.Should().Be(content);
    }

    [Fact]
    public void Filter_FrontmatterFilterDisabled_ReturnsNoExclusions()
    {
        // Arrange
        var content = "---\ntitle: test\n---";
        var options = ContentFilterOptions.None;

        // Act
        var result = _sut.Filter(content, options);

        // Assert
        result.HasExclusions.Should().BeFalse();
    }

    [Fact]
    public void Filter_ContentWithHorizontalRule_ReturnsNoExclusions()
    {
        // Arrange - Horizontal rule in middle of document should NOT be detected
        var content = """
            Some prose before.

            ---

            Some prose after.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert - No frontmatter because --- is not at document start
        result.HasExclusions.Should().BeFalse();
    }

    #endregion

    #region YAML Frontmatter Detection

    [Fact]
    public void Filter_SimpleYamlFrontmatter_ExcludesBlock()
    {
        // Arrange
        var content = """
            ---
            title: My Document
            ---
            
            Document content here.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);

        var region = result.ExcludedRegions[0];
        region.Reason.Should().Be(ExclusionReason.Frontmatter);
        region.Metadata.Should().Be("---");
        region.StartOffset.Should().Be(0);
    }

    [Fact]
    public void Filter_YamlWithMultipleFields_ExcludesBlock()
    {
        // Arrange
        var content = """
            ---
            title: My Document
            author: John Doe
            date: 2024-01-15
            tags:
              - writing
              - style
            ---
            
            Content starts here.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.Frontmatter);
    }

    [Fact]
    public void Filter_YamlWithComplexContent_ExcludesBlock()
    {
        // Arrange - YAML with nested structures and special characters
        var content = """
            ---
            title: "Document: A Guide"
            description: |
              This is a multi-line
              description field.
            metadata:
              version: 1.0
              authors:
                - name: Alice
                  role: editor
            ---
            
            Body content here.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.Frontmatter);

        // The exclusion should end after the closing ---
        var frontmatter = content[..result.ExcludedRegions[0].EndOffset];
        frontmatter.Should().EndWith("---\n");
    }

    [Fact]
    public void Filter_YamlWithDashesInContent_DetectsCorrectly()
    {
        // Arrange - YAML with dashes inside values (not delimiters)
        var content = """
            ---
            title: A -- B --- C
            range: 1-10
            ---
            
            Content.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);
    }

    #endregion

    #region TOML Frontmatter Detection

    [Fact]
    public void Filter_TomlFrontmatter_ExcludesBlock()
    {
        // Arrange
        var content = """
            +++
            title = "My Document"
            +++
            
            Document content.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);

        var region = result.ExcludedRegions[0];
        region.Reason.Should().Be(ExclusionReason.TomlFrontmatter);
        region.Metadata.Should().Be("+++");
    }

    [Fact]
    public void Filter_TomlWithMultipleFields_ExcludesBlock()
    {
        // Arrange
        var content = """
            +++
            title = "My Document"
            date = 2024-01-15
            draft = false
            
            [author]
            name = "John Doe"
            email = "john@example.com"
            +++
            
            Content here.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.TomlFrontmatter);
    }

    [Fact]
    public void Filter_TomlDisabled_ReturnsNoExclusion()
    {
        // Arrange
        var content = "+++\ntitle = \"test\"\n+++";
        var options = YamlFrontmatterOptions.YamlOnly;
        var sut = new YamlFrontmatterFilter(_logger, options);

        // Act
        var result = sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeFalse();
    }

    #endregion

    #region JSON Frontmatter Detection

    [Fact]
    public void Filter_JsonFrontmatter_ExcludesBlock()
    {
        // Arrange
        var content = """
            {
              "title": "My Document"
            }
            
            Document content.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);

        var region = result.ExcludedRegions[0];
        region.Reason.Should().Be(ExclusionReason.JsonFrontmatter);
        region.Metadata.Should().Be("json");
    }

    [Fact]
    public void Filter_JsonWithNestedObjects_ExcludesBlock()
    {
        // Arrange
        var content = """
            {
              "title": "My Document",
              "metadata": {
                "author": {
                  "name": "John",
                  "role": "writer"
                },
                "tags": ["a", "b"]
              }
            }
            
            Content here.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.JsonFrontmatter);
    }

    [Fact]
    public void Filter_JsonOnSingleLine_ExcludesBlock()
    {
        // Arrange
        var content = "{\"title\": \"test\"}\n\nContent here.";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.JsonFrontmatter);
    }

    [Fact]
    public void Filter_JsonDisabled_ReturnsNoExclusion()
    {
        // Arrange
        var content = "{\"title\": \"test\"}";
        var options = new YamlFrontmatterOptions { DetectJsonFrontmatter = false };
        var sut = new YamlFrontmatterFilter(_logger, options);

        // Act
        var result = sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeFalse();
    }

    #endregion

    #region Edge Cases - BOM Handling

    [Fact]
    public void Filter_YamlWithBom_ExcludesBlock()
    {
        // Arrange - UTF-8 BOM followed by YAML frontmatter
        var content = "\uFEFF---\ntitle: test\n---\n\nContent";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.Frontmatter);
        result.ExcludedRegions[0].StartOffset.Should().Be(0); // Includes BOM
    }

    [Fact]
    public void Filter_TomlWithBom_ExcludesBlock()
    {
        // Arrange
        var content = "\uFEFF+++\ntitle = \"test\"\n+++";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.TomlFrontmatter);
    }

    [Fact]
    public void Filter_BomStrippingDisabled_NoDetectionWithBom()
    {
        // Arrange
        var content = "\uFEFF---\ntitle: test\n---";
        var options = new YamlFrontmatterOptions { StripBom = false };
        var sut = new YamlFrontmatterFilter(_logger, options);

        // Act
        var result = sut.Filter(content, ContentFilterOptions.Default);

        // Assert - Should NOT detect because BOM is before ---
        result.HasExclusions.Should().BeFalse();
    }

    #endregion

    #region Edge Cases - Line Endings

    [Fact]
    public void Filter_YamlWithWindowsLineEndings_ExcludesBlock()
    {
        // Arrange - CRLF line endings
        var content = "---\r\ntitle: test\r\n---\r\n\r\nContent";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.Frontmatter);
    }

    [Fact]
    public void Filter_YamlNoTrailingNewline_ExcludesBlock()
    {
        // Arrange - No newline after closing delimiter
        var content = "---\ntitle: test\n---";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].EndOffset.Should().Be(content.Length);
    }

    [Fact]
    public void Filter_YamlWithTrailingSpaces_ExcludesBlock()
    {
        // Arrange - Trailing spaces after delimiter
        var content = "---   \ntitle: test\n---  \n\nContent";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.Frontmatter);
    }

    #endregion

    #region Edge Cases - Positioning

    [Fact]
    public void Filter_FrontmatterNotOnFirstLine_ReturnsNoExclusions()
    {
        // Arrange - Content before frontmatter
        var content = """
            Some preamble text.
            ---
            title: test
            ---
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert - Should NOT detect because not at start
        result.HasExclusions.Should().BeFalse();
    }

    [Fact]
    public void Filter_WhitespaceBeforeFrontmatter_ReturnsNoExclusions()
    {
        // Arrange - Leading whitespace before delimiter
        var content = "  ---\ntitle: test\n---";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert - Should NOT detect (delimiter must be at column 0)
        result.HasExclusions.Should().BeFalse();
    }

    #endregion

    #region Edge Cases - Unclosed Frontmatter

    [Fact]
    public void Filter_UnclosedYamlFrontmatter_ExcludesToEOF()
    {
        // Arrange
        var content = "---\ntitle: test\nNo closing delimiter";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].EndOffset.Should().Be(content.Length);
        result.ExcludedRegions[0].Metadata.Should().Contain("unclosed");
    }

    [Fact]
    public void Filter_UnclosedTomlFrontmatter_ExcludesToEOF()
    {
        // Arrange
        var content = "+++\ntitle = \"test\"\nNo closing";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].EndOffset.Should().Be(content.Length);
    }

    [Fact]
    public void Filter_UnclosedJsonFrontmatter_ExcludesToEOF()
    {
        // Arrange
        var content = "{\n  \"title\": \"test\"\n  No closing brace";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].EndOffset.Should().Be(content.Length);
        result.ExcludedRegions[0].Metadata.Should().Contain("unclosed");
    }

    #endregion

    #region Edge Cases - Empty and Minimal

    [Fact]
    public void Filter_EmptyFrontmatter_ExcludesBlock()
    {
        // Arrange - Empty YAML frontmatter
        var content = "---\n---\n\nContent";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
    }

    [Fact]
    public void Filter_OnlyFrontmatter_ExcludesEntireContent()
    {
        // Arrange - Only frontmatter, no body
        var content = "---\ntitle: test\n---";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].EndOffset.Should().Be(content.Length);
    }

    [Fact]
    public void Filter_EmptyJsonFrontmatter_ExcludesBlock()
    {
        // Arrange
        var content = "{}\n\nContent";

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.JsonFrontmatter);
    }

    #endregion

    #region CanFilter Tests

    [Fact]
    public void CanFilter_MarkdownFile_ReturnsTrue()
    {
        _sut.CanFilter(".md").Should().BeTrue();
    }

    [Fact]
    public void CanFilter_MarkdownExtension_ReturnsTrue()
    {
        _sut.CanFilter(".markdown").Should().BeTrue();
    }

    [Fact]
    public void CanFilter_MdxFile_ReturnsTrue()
    {
        _sut.CanFilter(".mdx").Should().BeTrue();
    }

    [Fact]
    public void CanFilter_TextFile_ReturnsFalse()
    {
        _sut.CanFilter(".txt").Should().BeFalse();
    }

    [Fact]
    public void CanFilter_YamlFile_ReturnsFalse()
    {
        // Note: .yaml files are NOT filtered - frontmatter is a Markdown concept
        _sut.CanFilter(".yaml").Should().BeFalse();
    }

    [Fact]
    public void CanFilter_CaseInsensitive()
    {
        _sut.CanFilter(".MD").Should().BeTrue();
        _sut.CanFilter(".Md").Should().BeTrue();
        _sut.CanFilter(".MARKDOWN").Should().BeTrue();
    }

    #endregion

    #region Properties

    [Fact]
    public void Name_ReturnsYamlFrontmatter()
    {
        _sut.Name.Should().Be("YamlFrontmatter");
    }

    [Fact]
    public void Priority_Returns100()
    {
        // Priority 100 means it runs BEFORE code block filter (200)
        _sut.Priority.Should().Be(100);
    }

    [Fact]
    public void Priority_IsLowerThanCodeBlockFilter()
    {
        // Verify the priority ordering is correct
        var codeBlockFilter = new MarkdownCodeBlockFilter(
            NullLogger<MarkdownCodeBlockFilter>.Instance);

        _sut.Priority.Should().BeLessThan(codeBlockFilter.Priority);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new YamlFrontmatterFilter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullOptions_UsesDefaults()
    {
        // Act
        var sut = new YamlFrontmatterFilter(_logger, null);

        // Assert - Should work and detect all formats
        var yamlResult = sut.Filter("---\ntest\n---", ContentFilterOptions.Default);
        var tomlResult = sut.Filter("+++\ntest\n+++", ContentFilterOptions.Default);
        var jsonResult = sut.Filter("{}", ContentFilterOptions.Default);

        yamlResult.HasExclusions.Should().BeTrue();
        tomlResult.HasExclusions.Should().BeTrue();
        jsonResult.HasExclusions.Should().BeTrue();
    }

    #endregion

    #region Performance

    [Fact]
    public void Filter_LongContent_CompletesWithinTimeout()
    {
        // Arrange - Large document with frontmatter and lots of content
        var body = string.Join("\n\n", Enumerable.Range(0, 1000).Select(i =>
            $"This is paragraph {i} with some content that might contain style issues."));
        var content = $"---\ntitle: Large Document\n---\n\n{body}";

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = _sut.Filter(content, ContentFilterOptions.Default);
        sw.Stop();

        // Assert
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);
        sw.ElapsedMilliseconds.Should().BeLessThan(100); // Should be very fast
    }

    [Fact]
    public void Filter_ContentWithManyDashes_CompletesQuickly()
    {
        // Arrange - Content with many --- that are NOT frontmatter
        var content = "---\ntitle: test\n---\n\n" +
            string.Join("\n", Enumerable.Range(0, 100).Select(_ => "---\nNot frontmatter\n---"));

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = _sut.Filter(content, ContentFilterOptions.Default);
        sw.Stop();

        // Assert - Should only detect the first frontmatter block
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions.Should().HaveCount(1);
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region Integration with MarkdownCodeBlockFilter

    [Fact]
    public void Filter_FrontmatterContainingCodeFence_ExcludesAll()
    {
        // Arrange - Frontmatter that happens to have ``` in a value
        var content = """
            ---
            title: Code Example
            example: "```python"
            ---
            
            Content here.
            """;

        // Act
        var result = _sut.Filter(content, ContentFilterOptions.Default);

        // Assert - Frontmatter filter should catch this first
        result.HasExclusions.Should().BeTrue();
        result.ExcludedRegions[0].Reason.Should().Be(ExclusionReason.Frontmatter);
    }

    #endregion
}
