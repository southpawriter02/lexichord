// =============================================================================
// File: ChunkRepositoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ChunkRepository constructor and argument validation.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for <see cref="ChunkRepository"/>.
/// </summary>
/// <remarks>
/// These tests focus on constructor validation and embedding dimension validation.
/// Full repository functionality is tested in integration tests with a real database.
/// </remarks>
public class ChunkRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<ILogger<ChunkRepository>> _mockLogger = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act
        var act = () => new ChunkRepository(
            _mockConnectionFactory.Object,
            _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ChunkRepository(
            null!,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("connectionFactory");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ChunkRepository(
            _mockConnectionFactory.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    #endregion

    #region SearchSimilarAsync Validation Tests

    [Fact]
    public async Task SearchSimilarAsync_WithNullEmbedding_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new ChunkRepository(
            _mockConnectionFactory.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await repository.SearchSimilarAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
                 .WithParameterName("queryEmbedding");
    }

    [Fact]
    public async Task SearchSimilarAsync_WithWrongDimensions_ThrowsArgumentException()
    {
        // Arrange
        var repository = new ChunkRepository(
            _mockConnectionFactory.Object,
            _mockLogger.Object);

        // 100 dimensions instead of 1536
        var wrongSizeEmbedding = new float[100];

        // Act
        var act = async () => await repository.SearchSimilarAsync(wrongSizeEmbedding);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithParameterName("queryEmbedding")
                 .WithMessage("*1536 dimensions*100*");
    }

    [Fact]
    public async Task SearchSimilarAsync_WithEmptyEmbedding_ThrowsArgumentException()
    {
        // Arrange
        var repository = new ChunkRepository(
            _mockConnectionFactory.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await repository.SearchSimilarAsync([]);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithParameterName("queryEmbedding");
    }

    #endregion
}
