// =============================================================================
// File: ConfidenceToColorConverterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConfidenceToColorConverter.
// Version: v0.4.7e
// =============================================================================

using System.Globalization;
using Lexichord.Modules.Knowledge.UI.Converters;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Converters;

/// <summary>
/// Unit tests for <see cref="ConfidenceToColorConverter"/>.
/// </summary>
/// <remarks>
/// Tests verify the correct color hex codes are returned for each confidence tier.
/// </remarks>
public class ConfidenceToColorConverterTests
{
    private readonly ConfidenceToColorConverter _sut = new();

    #region Threshold Tests

    [Theory]
    [InlineData(1.0f, "#22c55e")]   // High confidence >= 0.9
    [InlineData(0.95f, "#22c55e")]
    [InlineData(0.9f, "#22c55e")]
    public void Convert_HighConfidence_ReturnsGreen(float confidence, string expectedColor)
    {
        var result = _sut.Convert(confidence, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expectedColor, result);
    }

    [Theory]
    [InlineData(0.89f, "#eab308")]  // Medium-high confidence >= 0.7
    [InlineData(0.8f, "#eab308")]
    [InlineData(0.7f, "#eab308")]
    public void Convert_MediumHighConfidence_ReturnsYellow(float confidence, string expectedColor)
    {
        var result = _sut.Convert(confidence, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expectedColor, result);
    }

    [Theory]
    [InlineData(0.69f, "#f97316")]  // Medium confidence >= 0.5
    [InlineData(0.6f, "#f97316")]
    [InlineData(0.5f, "#f97316")]
    public void Convert_MediumConfidence_ReturnsOrange(float confidence, string expectedColor)
    {
        var result = _sut.Convert(confidence, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expectedColor, result);
    }

    [Theory]
    [InlineData(0.49f, "#ef4444")]  // Low confidence < 0.5
    [InlineData(0.25f, "#ef4444")]
    [InlineData(0.0f, "#ef4444")]
    public void Convert_LowConfidence_ReturnsRed(float confidence, string expectedColor)
    {
        var result = _sut.Convert(confidence, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expectedColor, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Convert_WithNullValue_ReturnsGray()
    {
        var result = _sut.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("#6b7280", result);
    }

    [Fact]
    public void Convert_WithNonFloatValue_ReturnsGray()
    {
        var result = _sut.Convert("not a float", typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("#6b7280", result);
    }

    [Fact]
    public void Convert_WithIntValue_ReturnsGray()
    {
        // LOGIC: Only float is accepted, int will fall through to gray
        var result = _sut.Convert(1, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("#6b7280", result);
    }

    [Fact]
    public void Convert_BoundaryAt0Point9_ReturnsGreen()
    {
        // Exact boundary test - 0.9 should be green (>= 0.9)
        var result = _sut.Convert(0.9f, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("#22c55e", result);
    }

    [Fact]
    public void Convert_JustBelow0Point9_ReturnsYellow()
    {
        // Just below boundary - 0.899... should be yellow
        var result = _sut.Convert(0.8999f, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("#eab308", result);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            _sut.ConvertBack("#22c55e", typeof(float), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
