using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="IngestionPhase"/> enum.
/// </summary>
public class IngestionPhaseTests
{
    [Fact]
    public void IngestionPhase_HasCorrectNumberOfValues()
    {
        // Assert
        var values = Enum.GetValues<IngestionPhase>();
        values.Should().HaveCount(7, because: "there are 7 phases in the pipeline");
    }

    [Theory]
    [InlineData(IngestionPhase.Scanning, 0)]
    [InlineData(IngestionPhase.Hashing, 1)]
    [InlineData(IngestionPhase.Reading, 2)]
    [InlineData(IngestionPhase.Chunking, 3)]
    [InlineData(IngestionPhase.Embedding, 4)]
    [InlineData(IngestionPhase.Storing, 5)]
    [InlineData(IngestionPhase.Complete, 6)]
    public void IngestionPhase_HasCorrectIntegerValues(IngestionPhase phase, int expectedValue)
    {
        // Assert
        ((int)phase).Should().Be(expectedValue);
    }

    [Fact]
    public void IngestionPhase_Scanning_IsFirstPhase()
    {
        // Assert
        IngestionPhase.Scanning.Should().Be((IngestionPhase)0);
    }

    [Fact]
    public void IngestionPhase_Complete_IsLastPhase()
    {
        // Assert
        var maxValue = Enum.GetValues<IngestionPhase>().Max();
        maxValue.Should().Be(IngestionPhase.Complete);
    }

    [Theory]
    [InlineData(IngestionPhase.Scanning, "Scanning")]
    [InlineData(IngestionPhase.Complete, "Complete")]
    public void IngestionPhase_ToString_ReturnsExpectedName(IngestionPhase phase, string expectedName)
    {
        // Assert
        phase.ToString().Should().Be(expectedName);
    }
}
