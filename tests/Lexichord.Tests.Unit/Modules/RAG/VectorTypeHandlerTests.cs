// =============================================================================
// File: VectorTypeHandlerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the VectorTypeHandler Dapper type handler.
// =============================================================================

using System.Data;
using Pgvector;
using Lexichord.Modules.RAG.Data;
using FluentAssertions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for <see cref="VectorTypeHandler"/>.
/// </summary>
public class VectorTypeHandlerTests
{
    private readonly VectorTypeHandler _handler = new();

    #region SetValue Tests

    [Fact]
    public void SetValue_WithValidArray_SetsVectorValue()
    {
        // Arrange
        var mockParameter = new Mock<IDbDataParameter>();
        object? capturedValue = null;
        mockParameter.SetupSet(p => p.Value = It.IsAny<object?>())
                     .Callback<object?>(v => capturedValue = v);

        float[] embedding = [0.1f, 0.2f, 0.3f];

        // Act
        _handler.SetValue(mockParameter.Object, embedding);

        // Assert
        capturedValue.Should().BeOfType<Vector>();
        var vector = (Vector)capturedValue!;
        vector.ToArray().Should().BeEquivalentTo(embedding);
    }

    [Fact]
    public void SetValue_WithNullArray_SetsDBNullValue()
    {
        // Arrange
        var mockParameter = new Mock<IDbDataParameter>();
        object? capturedValue = null;
        mockParameter.SetupSet(p => p.Value = It.IsAny<object?>())
                     .Callback<object?>(v => capturedValue = v);

        // Act
        _handler.SetValue(mockParameter.Object, null);

        // Assert
        capturedValue.Should().Be(DBNull.Value);
    }

    [Fact]
    public void SetValue_WithEmptyArray_SetsEmptyVector()
    {
        // Arrange
        var mockParameter = new Mock<IDbDataParameter>();
        object? capturedValue = null;
        mockParameter.SetupSet(p => p.Value = It.IsAny<object?>())
                     .Callback<object?>(v => capturedValue = v);

        float[] embedding = [];

        // Act
        _handler.SetValue(mockParameter.Object, embedding);

        // Assert
        capturedValue.Should().BeOfType<Vector>();
        var vector = (Vector)capturedValue!;
        vector.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void SetValue_WithLargeArray_SetsVectorValue()
    {
        // Arrange
        var mockParameter = new Mock<IDbDataParameter>();
        object? capturedValue = null;
        mockParameter.SetupSet(p => p.Value = It.IsAny<object?>())
                     .Callback<object?>(v => capturedValue = v);

        // 1536 dimensions like OpenAI embeddings
        float[] embedding = new float[1536];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = i * 0.001f;
        }

        // Act
        _handler.SetValue(mockParameter.Object, embedding);

        // Assert
        capturedValue.Should().BeOfType<Vector>();
        var vector = (Vector)capturedValue!;
        vector.ToArray().Should().BeEquivalentTo(embedding);
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_WithVector_ReturnsFloatArray()
    {
        // Arrange
        float[] expected = [0.1f, 0.2f, 0.3f];
        var vector = new Vector(expected);

        // Act
        var result = _handler.Parse(vector);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_WithFloatArray_ReturnsArrayAsIs()
    {
        // Arrange
        float[] expected = [0.1f, 0.2f, 0.3f];

        // Act
        var result = _handler.Parse(expected);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void Parse_WithNull_ReturnsNull()
    {
        // Act
        var result = _handler.Parse(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithDBNull_ReturnsNull()
    {
        // Act
        var result = _handler.Parse(DBNull.Value);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithInvalidType_ThrowsInvalidCastException()
    {
        // Arrange
        var invalidValue = "not a vector";

        // Act
        var act = () => _handler.Parse(invalidValue);

        // Assert
        act.Should().Throw<InvalidCastException>()
           .WithMessage("*Cannot convert*to float[]*");
    }

    [Fact]
    public void Parse_WithEmptyVector_ReturnsEmptyArray()
    {
        // Arrange
        var vector = new Vector(Array.Empty<float>());

        // Act
        var result = _handler.Parse(vector);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_With1536DimensionVector_ReturnsFullArray()
    {
        // Arrange
        float[] expected = new float[1536];
        for (int i = 0; i < expected.Length; i++)
        {
            expected[i] = i * 0.001f;
        }
        var vector = new Vector(expected);

        // Act
        var result = _handler.Parse(vector);

        // Assert
        result.Should().HaveCount(1536);
        result.Should().BeEquivalentTo(expected);
    }

    #endregion
}
