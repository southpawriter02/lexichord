// =============================================================================
// File: CitationValidationStatusTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the CitationValidationStatus enum.
// =============================================================================
// LOGIC: Verifies that the CitationValidationStatus enum:
//   - Has exactly 4 values (Valid, Stale, Missing, Error).
//   - Values have correct ordinal positions (0-3).
//   - Can be parsed from string representation.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for the <see cref="CitationValidationStatus"/> enum.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2c")]
public class CitationValidationStatusTests
{
    [Fact]
    public void Enum_HasExactlyFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<CitationValidationStatus>();

        // Assert
        Assert.Equal(4, values.Length);
    }

    [Theory]
    [InlineData(CitationValidationStatus.Valid, 0)]
    [InlineData(CitationValidationStatus.Stale, 1)]
    [InlineData(CitationValidationStatus.Missing, 2)]
    [InlineData(CitationValidationStatus.Error, 3)]
    public void Enum_HasCorrectOrdinalValues(CitationValidationStatus status, int expectedOrdinal)
    {
        // Act & Assert
        Assert.Equal(expectedOrdinal, (int)status);
    }

    [Theory]
    [InlineData("Valid", CitationValidationStatus.Valid)]
    [InlineData("Stale", CitationValidationStatus.Stale)]
    [InlineData("Missing", CitationValidationStatus.Missing)]
    [InlineData("Error", CitationValidationStatus.Error)]
    public void Enum_ParsesFromString(string input, CitationValidationStatus expected)
    {
        // Act
        var parsed = Enum.Parse<CitationValidationStatus>(input);

        // Assert
        Assert.Equal(expected, parsed);
    }
}
