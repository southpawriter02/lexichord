using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for the <see cref="DocumentStatus"/> enum.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the enum has the expected values and count,
/// ensuring the contract is maintained for consumers who depend on these values.
/// </remarks>
public class DocumentStatusTests
{
    [Fact]
    public void DocumentStatus_HasExpectedValues()
    {
        // Arrange
        var expectedValues = new[]
        {
            DocumentStatus.Pending,
            DocumentStatus.Indexing,
            DocumentStatus.Indexed,
            DocumentStatus.Failed,
            DocumentStatus.Stale
        };

        // Act
        var actualValues = Enum.GetValues<DocumentStatus>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues,
            because: "DocumentStatus must have exactly these five values for the lifecycle state machine");
    }

    [Fact]
    public void DocumentStatus_HasCorrectCount()
    {
        // Act
        var count = Enum.GetValues<DocumentStatus>().Length;

        // Assert
        count.Should().Be(5,
            because: "DocumentStatus should have exactly 5 states");
    }

    [Theory]
    [InlineData(DocumentStatus.Pending, 0)]
    [InlineData(DocumentStatus.Indexing, 1)]
    [InlineData(DocumentStatus.Indexed, 2)]
    [InlineData(DocumentStatus.Failed, 3)]
    [InlineData(DocumentStatus.Stale, 4)]
    public void DocumentStatus_HasExpectedNumericValues(DocumentStatus status, int expectedValue)
    {
        // Assert
        ((int)status).Should().Be(expectedValue,
            because: "enum values must maintain stable numeric representations for database storage");
    }

    [Fact]
    public void DocumentStatus_CanParseFromString()
    {
        // Arrange & Act & Assert
        Enum.TryParse<DocumentStatus>("Indexed", out var result).Should().BeTrue();
        result.Should().Be(DocumentStatus.Indexed);
    }

    [Fact]
    public void DocumentStatus_ContainsPending()
    {
        // Assert
        Enum.IsDefined(typeof(DocumentStatus), "Pending").Should().BeTrue(
            because: "Pending is the initial state for new documents");
    }

    [Fact]
    public void DocumentStatus_ContainsIndexing()
    {
        // Assert
        Enum.IsDefined(typeof(DocumentStatus), "Indexing").Should().BeTrue(
            because: "Indexing indicates active processing");
    }

    [Fact]
    public void DocumentStatus_ContainsIndexed()
    {
        // Assert
        Enum.IsDefined(typeof(DocumentStatus), "Indexed").Should().BeTrue(
            because: "Indexed is the success terminal state");
    }

    [Fact]
    public void DocumentStatus_ContainsFailed()
    {
        // Assert
        Enum.IsDefined(typeof(DocumentStatus), "Failed").Should().BeTrue(
            because: "Failed indicates an error during indexing");
    }

    [Fact]
    public void DocumentStatus_ContainsStale()
    {
        // Assert
        Enum.IsDefined(typeof(DocumentStatus), "Stale").Should().BeTrue(
            because: "Stale indicates the source file has changed");
    }
}
