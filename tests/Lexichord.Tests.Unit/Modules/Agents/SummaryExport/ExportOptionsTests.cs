// -----------------------------------------------------------------------
// <copyright file="SummaryExportOptionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.SummaryExport;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="SummaryExportOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class SummaryExportOptionsTests
{
    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SummaryExportOptions();

        // Assert
        options.Destination.Should().Be(ExportDestination.Panel);
        options.OutputPath.Should().BeNull();
        options.Fields.Should().Be(FrontmatterFields.All);
        options.Overwrite.Should().BeTrue();
        options.IncludeMetadata.Should().BeTrue();
        options.IncludeSourceReference.Should().BeTrue();
        options.ExportTemplate.Should().BeNull();
        options.ClipboardAsMarkdown.Should().BeTrue();
        options.UseCalloutBlock.Should().BeTrue();
        options.CalloutType.Should().Be("info");
    }

    // ── Destination Property ────────────────────────────────────────────

    [Theory]
    [InlineData(ExportDestination.Panel)]
    [InlineData(ExportDestination.Frontmatter)]
    [InlineData(ExportDestination.File)]
    [InlineData(ExportDestination.Clipboard)]
    [InlineData(ExportDestination.InlineInsert)]
    public void Destination_CanBeSetToAnyValue(ExportDestination destination)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { Destination = destination };

        // Assert
        options.Destination.Should().Be(destination);
    }

    // ── OutputPath Property ─────────────────────────────────────────────

    [Fact]
    public void OutputPath_CanBeNull()
    {
        // Arrange & Act
        var options = new SummaryExportOptions { OutputPath = null };

        // Assert
        options.OutputPath.Should().BeNull();
    }

    [Fact]
    public void OutputPath_CanBeSet()
    {
        // Arrange
        const string path = "/path/to/output.md";

        // Act
        var options = new SummaryExportOptions { OutputPath = path };

        // Assert
        options.OutputPath.Should().Be(path);
    }

    // ── Fields Property ─────────────────────────────────────────────────

    [Theory]
    [InlineData(FrontmatterFields.None)]
    [InlineData(FrontmatterFields.Abstract)]
    [InlineData(FrontmatterFields.Tags)]
    [InlineData(FrontmatterFields.KeyTerms)]
    [InlineData(FrontmatterFields.ReadingTime)]
    [InlineData(FrontmatterFields.Category)]
    [InlineData(FrontmatterFields.Audience)]
    [InlineData(FrontmatterFields.GeneratedAt)]
    [InlineData(FrontmatterFields.All)]
    public void Fields_CanBeSetToAnySingleFlag(FrontmatterFields fields)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { Fields = fields };

        // Assert
        options.Fields.Should().Be(fields);
    }

    [Fact]
    public void Fields_CanBeCombined()
    {
        // Arrange
        var fields = FrontmatterFields.Abstract | FrontmatterFields.Tags | FrontmatterFields.ReadingTime;

        // Act
        var options = new SummaryExportOptions { Fields = fields };

        // Assert
        options.Fields.Should().HaveFlag(FrontmatterFields.Abstract);
        options.Fields.Should().HaveFlag(FrontmatterFields.Tags);
        options.Fields.Should().HaveFlag(FrontmatterFields.ReadingTime);
        options.Fields.Should().NotHaveFlag(FrontmatterFields.KeyTerms);
        options.Fields.Should().NotHaveFlag(FrontmatterFields.Category);
    }

    // ── Overwrite Property ──────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Overwrite_CanBeSetToAnyValue(bool overwrite)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { Overwrite = overwrite };

        // Assert
        options.Overwrite.Should().Be(overwrite);
    }

    // ── IncludeMetadata Property ────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncludeMetadata_CanBeSetToAnyValue(bool include)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { IncludeMetadata = include };

        // Assert
        options.IncludeMetadata.Should().Be(include);
    }

    // ── IncludeSourceReference Property ─────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncludeSourceReference_CanBeSetToAnyValue(bool include)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { IncludeSourceReference = include };

        // Assert
        options.IncludeSourceReference.Should().Be(include);
    }

    // ── ExportTemplate Property ─────────────────────────────────────────

    [Fact]
    public void ExportTemplate_CanBeNull()
    {
        // Arrange & Act
        var options = new SummaryExportOptions { ExportTemplate = null };

        // Assert
        options.ExportTemplate.Should().BeNull();
    }

    [Fact]
    public void ExportTemplate_CanBeSet()
    {
        // Arrange
        const string template = "# {{document_title}}\n\n{{summary}}";

        // Act
        var options = new SummaryExportOptions { ExportTemplate = template };

        // Assert
        options.ExportTemplate.Should().Be(template);
    }

    // ── ClipboardAsMarkdown Property ────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClipboardAsMarkdown_CanBeSetToAnyValue(bool asMarkdown)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { ClipboardAsMarkdown = asMarkdown };

        // Assert
        options.ClipboardAsMarkdown.Should().Be(asMarkdown);
    }

    // ── UseCalloutBlock Property ────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseCalloutBlock_CanBeSetToAnyValue(bool useCallout)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { UseCalloutBlock = useCallout };

        // Assert
        options.UseCalloutBlock.Should().Be(useCallout);
    }

    // ── CalloutType Property ────────────────────────────────────────────

    [Theory]
    [InlineData("info")]
    [InlineData("note")]
    [InlineData("tip")]
    [InlineData("warning")]
    public void CalloutType_CanBeSetToAnyValue(string calloutType)
    {
        // Arrange & Act
        var options = new SummaryExportOptions { CalloutType = calloutType };

        // Assert
        options.CalloutType.Should().Be(calloutType);
    }

    // ── Validate: File Destination ──────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_FileDestinationWithEmptyOutputPath_ThrowsArgumentException(string outputPath)
    {
        // Arrange
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.File,
            OutputPath = outputPath
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("OutputPath")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Validate_FileDestinationWithNullOutputPath_DoesNotThrow()
    {
        // Arrange — OutputPath null means auto-generate
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.File,
            OutputPath = null
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_FileDestinationWithValidOutputPath_DoesNotThrow()
    {
        // Arrange
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.File,
            OutputPath = "/path/to/output.md"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: InlineInsert Destination ──────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InlineInsertWithEmptyCalloutType_ThrowsArgumentException(string calloutType)
    {
        // Arrange
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.InlineInsert,
            UseCalloutBlock = true,
            CalloutType = calloutType
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("CalloutType")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Validate_InlineInsertWithCalloutDisabled_DoesNotThrowOnEmptyCalloutType()
    {
        // Arrange — CalloutType is ignored when UseCalloutBlock is false
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.InlineInsert,
            UseCalloutBlock = false,
            CalloutType = ""
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_InlineInsertWithValidCalloutType_DoesNotThrow()
    {
        // Arrange
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.InlineInsert,
            UseCalloutBlock = true,
            CalloutType = "note"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── Validate: Other Destinations ────────────────────────────────────

    [Theory]
    [InlineData(ExportDestination.Panel)]
    [InlineData(ExportDestination.Frontmatter)]
    [InlineData(ExportDestination.Clipboard)]
    public void Validate_OtherDestinations_DoesNotThrow(ExportDestination destination)
    {
        // Arrange
        var options = new SummaryExportOptions { Destination = destination };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // ── ForDestination Factory ──────────────────────────────────────────

    [Theory]
    [InlineData(ExportDestination.Panel)]
    [InlineData(ExportDestination.Frontmatter)]
    [InlineData(ExportDestination.File)]
    [InlineData(ExportDestination.Clipboard)]
    [InlineData(ExportDestination.InlineInsert)]
    public void ForDestination_CreatesOptionsWithSpecifiedDestination(ExportDestination destination)
    {
        // Act
        var options = SummaryExportOptions.ForDestination(destination);

        // Assert
        options.Destination.Should().Be(destination);
        // Other properties should have default values
        options.OutputPath.Should().BeNull();
        options.Fields.Should().Be(FrontmatterFields.All);
        options.Overwrite.Should().BeTrue();
    }

    // ── Full Options Construction ───────────────────────────────────────

    [Fact]
    public void FullOptions_SetsAllProperties()
    {
        // Arrange & Act
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.File,
            OutputPath = "/path/to/summary.md",
            Fields = FrontmatterFields.Abstract | FrontmatterFields.Tags,
            Overwrite = false,
            IncludeMetadata = true,
            IncludeSourceReference = false,
            ExportTemplate = "# Summary\n{{summary}}",
            ClipboardAsMarkdown = false,
            UseCalloutBlock = true,
            CalloutType = "warning"
        };

        // Assert
        options.Destination.Should().Be(ExportDestination.File);
        options.OutputPath.Should().Be("/path/to/summary.md");
        options.Fields.Should().Be(FrontmatterFields.Abstract | FrontmatterFields.Tags);
        options.Overwrite.Should().BeFalse();
        options.IncludeMetadata.Should().BeTrue();
        options.IncludeSourceReference.Should().BeFalse();
        options.ExportTemplate.Should().Be("# Summary\n{{summary}}");
        options.ClipboardAsMarkdown.Should().BeFalse();
        options.UseCalloutBlock.Should().BeTrue();
        options.CalloutType.Should().Be("warning");
    }
}
