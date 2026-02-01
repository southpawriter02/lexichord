using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="IngestionProgressEventArgs"/> class.
/// </summary>
public class IngestionProgressEventArgsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // Arrange
        var currentFile = "docs/readme.md";
        var totalFiles = 100;
        var processedFiles = 50;
        var phase = IngestionPhase.Embedding;
        var estimatedRemaining = TimeSpan.FromMinutes(5);

        // Act
        var args = new IngestionProgressEventArgs(
            currentFile, totalFiles, processedFiles, phase, estimatedRemaining);

        // Assert
        args.CurrentFile.Should().Be(currentFile);
        args.TotalFiles.Should().Be(totalFiles);
        args.ProcessedFiles.Should().Be(processedFiles);
        args.CurrentPhase.Should().Be(phase);
        args.EstimatedRemaining.Should().Be(estimatedRemaining);
    }

    [Fact]
    public void Constructor_NullCurrentFile_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngestionProgressEventArgs(
            null!, 10, 5, IngestionPhase.Reading);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("currentFile");
    }

    [Fact]
    public void Constructor_ZeroTotalFiles_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new IngestionProgressEventArgs(
            "file.md", 0, 0, IngestionPhase.Scanning);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("totalFiles");
    }

    [Fact]
    public void Constructor_NegativeTotalFiles_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new IngestionProgressEventArgs(
            "file.md", -1, 0, IngestionPhase.Scanning);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("totalFiles");
    }

    [Fact]
    public void Constructor_NegativeProcessedFiles_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new IngestionProgressEventArgs(
            "file.md", 10, -1, IngestionPhase.Scanning);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("processedFiles");
    }

    [Fact]
    public void Constructor_WithoutEstimatedRemaining_DefaultsToNull()
    {
        // Act
        var args = new IngestionProgressEventArgs(
            "file.md", 10, 5, IngestionPhase.Chunking);

        // Assert
        args.EstimatedRemaining.Should().BeNull();
    }

    #endregion

    #region Percentage Calculation Tests

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(25, 100, 25)]
    [InlineData(50, 100, 50)]
    [InlineData(100, 100, 100)]
    public void Percentage_CalculatesCorrectly(int processed, int total, int expectedPercentage)
    {
        // Arrange
        var args = new IngestionProgressEventArgs(
            "file.md", total, processed, IngestionPhase.Reading);

        // Assert
        args.Percentage.Should().Be(expectedPercentage);
    }

    [Fact]
    public void Percentage_WithOddDivision_TruncatesToInteger()
    {
        // Arrange: 1/3 = 33.33...%
        var args = new IngestionProgressEventArgs(
            "file.md", 3, 1, IngestionPhase.Reading);

        // Assert
        args.Percentage.Should().Be(33);
    }

    [Fact]
    public void Percentage_AtStart_IsZero()
    {
        // Arrange
        var args = new IngestionProgressEventArgs(
            "file.md", 10, 0, IngestionPhase.Scanning);

        // Assert
        args.Percentage.Should().Be(0);
    }

    [Fact]
    public void Percentage_AtEnd_IsOneHundred()
    {
        // Arrange
        var args = new IngestionProgressEventArgs(
            "file.md", 10, 10, IngestionPhase.Complete);

        // Assert
        args.Percentage.Should().Be(100);
    }

    #endregion

    #region EventArgs Inheritance Tests

    [Fact]
    public void IngestionProgressEventArgs_InheritsFromEventArgs()
    {
        // Arrange
        var args = new IngestionProgressEventArgs(
            "file.md", 10, 5, IngestionPhase.Reading);

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }

    #endregion
}
