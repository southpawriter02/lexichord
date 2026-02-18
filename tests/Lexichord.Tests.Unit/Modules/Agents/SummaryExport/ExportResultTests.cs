// -----------------------------------------------------------------------
// <copyright file="SummaryExportResultTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.SummaryExport;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="SummaryExportResult"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class SummaryExportResultTests
{
    // ── Succeeded Factory ───────────────────────────────────────────────

    [Theory]
    [InlineData(ExportDestination.Panel)]
    [InlineData(ExportDestination.Frontmatter)]
    [InlineData(ExportDestination.File)]
    [InlineData(ExportDestination.Clipboard)]
    [InlineData(ExportDestination.InlineInsert)]
    public void Succeeded_CreatesSuccessfulResult(ExportDestination destination)
    {
        // Act
        var result = SummaryExportResult.Succeeded(destination);

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(destination);
        result.ErrorMessage.Should().BeNull();
        result.OutputPath.Should().BeNull();
        result.BytesWritten.Should().BeNull();
        result.CharactersWritten.Should().BeNull();
        result.DidOverwrite.Should().BeFalse();
    }

    [Fact]
    public void Succeeded_WithOutputPath_SetsOutputPath()
    {
        // Arrange
        const string outputPath = "/path/to/output.md";

        // Act
        var result = SummaryExportResult.Succeeded(ExportDestination.File, outputPath);

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(ExportDestination.File);
        result.OutputPath.Should().Be(outputPath);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Succeeded_SetsExportedAtToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = SummaryExportResult.Succeeded(ExportDestination.Clipboard);

        // Assert
        var after = DateTimeOffset.UtcNow;
        result.ExportedAt.Should().BeOnOrAfter(before);
        result.ExportedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Succeeded_WithExpression_CanAddBytesWritten()
    {
        // Act
        var result = SummaryExportResult.Succeeded(ExportDestination.File, "/output.md")
            with { BytesWritten = 1234L };

        // Assert
        result.Success.Should().BeTrue();
        result.BytesWritten.Should().Be(1234L);
    }

    [Fact]
    public void Succeeded_WithExpression_CanAddCharactersWritten()
    {
        // Act
        var result = SummaryExportResult.Succeeded(ExportDestination.Clipboard)
            with { CharactersWritten = 500 };

        // Assert
        result.Success.Should().BeTrue();
        result.CharactersWritten.Should().Be(500);
    }

    [Fact]
    public void Succeeded_WithExpression_CanSetDidOverwrite()
    {
        // Act
        var result = SummaryExportResult.Succeeded(ExportDestination.File, "/output.md")
            with { DidOverwrite = true };

        // Assert
        result.Success.Should().BeTrue();
        result.DidOverwrite.Should().BeTrue();
    }

    // ── Failed Factory ──────────────────────────────────────────────────

    [Theory]
    [InlineData(ExportDestination.Panel)]
    [InlineData(ExportDestination.Frontmatter)]
    [InlineData(ExportDestination.File)]
    [InlineData(ExportDestination.Clipboard)]
    [InlineData(ExportDestination.InlineInsert)]
    public void Failed_CreatesFailedResult(ExportDestination destination)
    {
        // Act
        var result = SummaryExportResult.Failed(destination, "Test error message");

        // Assert
        result.Success.Should().BeFalse();
        result.Destination.Should().Be(destination);
        result.ErrorMessage.Should().Be("Test error message");
        result.OutputPath.Should().BeNull();
        result.BytesWritten.Should().BeNull();
        result.CharactersWritten.Should().BeNull();
        result.DidOverwrite.Should().BeFalse();
    }

    [Fact]
    public void Failed_WithNullErrorMessage_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SummaryExportResult.Failed(ExportDestination.File, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_SetsExportedAtToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = SummaryExportResult.Failed(ExportDestination.File, "Error");

        // Assert
        var after = DateTimeOffset.UtcNow;
        result.ExportedAt.Should().BeOnOrAfter(before);
        result.ExportedAt.Should().BeOnOrBefore(after);
    }

    // ── Default Values ──────────────────────────────────────────────────

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var result = new SummaryExportResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Destination.Should().Be(ExportDestination.Panel);
        result.OutputPath.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.BytesWritten.Should().BeNull();
        result.CharactersWritten.Should().BeNull();
        result.DidOverwrite.Should().BeFalse();
    }

    // ── Full Result Construction ────────────────────────────────────────

    [Fact]
    public void FullResult_SetsAllProperties()
    {
        // Arrange
        var exportedAt = DateTimeOffset.UtcNow;

        // Act
        var result = new SummaryExportResult
        {
            Success = true,
            Destination = ExportDestination.File,
            OutputPath = "/path/to/file.md",
            ErrorMessage = null,
            BytesWritten = 2048,
            CharactersWritten = 512,
            DidOverwrite = true,
            ExportedAt = exportedAt
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(ExportDestination.File);
        result.OutputPath.Should().Be("/path/to/file.md");
        result.ErrorMessage.Should().BeNull();
        result.BytesWritten.Should().Be(2048);
        result.CharactersWritten.Should().Be(512);
        result.DidOverwrite.Should().BeTrue();
        result.ExportedAt.Should().Be(exportedAt);
    }
}
