// =============================================================================
// File: CosineSimilarityTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CosineSimilarity.Compute().
// =============================================================================
// LOGIC: Tests the cosine similarity computation including edge cases:
//   null vectors, empty vectors, zero-magnitude vectors, mismatched lengths,
//   identical vectors (1.0), orthogonal vectors (0.0), and known values.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// =============================================================================

using FluentAssertions;
using Lexichord.Modules.Knowledge.Copilot.Context.Scoring;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Copilot.Context.Scoring;

/// <summary>
/// Unit tests for <see cref="CosineSimilarity"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2f")]
public class CosineSimilarityTests
{
    #region Null and Empty Inputs

    [Fact]
    public void Compute_WithNullFirstVector_ReturnsZero()
    {
        // Arrange & Act
        var result = CosineSimilarity.Compute(null!, new float[] { 1, 2, 3 });

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithNullSecondVector_ReturnsZero()
    {
        // Arrange & Act
        var result = CosineSimilarity.Compute(new float[] { 1, 2, 3 }, null!);

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithBothNull_ReturnsZero()
    {
        // Arrange & Act
        var result = CosineSimilarity.Compute(null!, null!);

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithEmptyFirstVector_ReturnsZero()
    {
        // Arrange & Act
        var result = CosineSimilarity.Compute([], new float[] { 1, 2, 3 });

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithEmptySecondVector_ReturnsZero()
    {
        // Arrange & Act
        var result = CosineSimilarity.Compute(new float[] { 1, 2, 3 }, []);

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithBothEmpty_ReturnsZero()
    {
        // Arrange & Act
        var result = CosineSimilarity.Compute([], []);

        // Assert
        result.Should().Be(0.0f);
    }

    #endregion

    #region Length Mismatch

    [Fact]
    public void Compute_WithDifferentLengths_ReturnsZero()
    {
        // Arrange
        float[] a = [1, 2, 3];
        float[] b = [1, 2];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().Be(0.0f);
    }

    #endregion

    #region Zero-Magnitude Vectors

    [Fact]
    public void Compute_WithZeroMagnitudeFirstVector_ReturnsZero()
    {
        // Arrange
        float[] a = [0, 0, 0];
        float[] b = [1, 2, 3];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithZeroMagnitudeSecondVector_ReturnsZero()
    {
        // Arrange
        float[] a = [1, 2, 3];
        float[] b = [0, 0, 0];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Compute_WithBothZeroMagnitude_ReturnsZero()
    {
        // Arrange
        float[] a = [0, 0, 0];
        float[] b = [0, 0, 0];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().Be(0.0f);
    }

    #endregion

    #region Identical Vectors

    [Fact]
    public void Compute_WithIdenticalVectors_ReturnsOne()
    {
        // Arrange
        float[] a = [1, 2, 3];
        float[] b = [1, 2, 3];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void Compute_WithScaledIdenticalVectors_ReturnsOne()
    {
        // Arrange — [1,2,3] and [2,4,6] are the same direction, different magnitude
        float[] a = [1, 2, 3];
        float[] b = [2, 4, 6];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().BeApproximately(1.0f, 0.001f);
    }

    #endregion

    #region Orthogonal Vectors

    [Fact]
    public void Compute_WithOrthogonalVectors_ReturnsZero()
    {
        // Arrange — orthogonal vectors have zero dot product
        float[] a = [1, 0, 0];
        float[] b = [0, 1, 0];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().BeApproximately(0.0f, 0.001f);
    }

    #endregion

    #region Opposite Vectors (Clamped)

    [Fact]
    public void Compute_WithOppositeVectors_ReturnsClamped()
    {
        // Arrange — opposite direction, cosine similarity = -1.0, clamped to 0.0
        float[] a = [1, 2, 3];
        float[] b = [-1, -2, -3];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert — clamped to [0, 1]
        result.Should().Be(0.0f);
    }

    #endregion

    #region Known Values

    [Fact]
    public void Compute_WithKnownValues_ReturnsExpectedSimilarity()
    {
        // Arrange — a = [1, 0], b = [1, 1]
        // dot = 1*1 + 0*1 = 1
        // ||a|| = 1, ||b|| = sqrt(2) ≈ 1.414
        // cosine = 1 / (1 * 1.414) ≈ 0.707
        float[] a = [1, 0];
        float[] b = [1, 1];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert
        result.Should().BeApproximately(0.707f, 0.01f);
    }

    [Fact]
    public void Compute_WithSingleElement_ReturnsCorrectSimilarity()
    {
        // Arrange — single-dimension vectors with same sign
        float[] a = [5];
        float[] b = [3];

        // Act
        var result = CosineSimilarity.Compute(a, b);

        // Assert — both positive, cosine = 1.0
        result.Should().BeApproximately(1.0f, 0.001f);
    }

    #endregion
}
