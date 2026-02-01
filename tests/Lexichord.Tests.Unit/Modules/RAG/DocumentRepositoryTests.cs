// =============================================================================
// File: DocumentRepositoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DocumentRepository constructor and argument validation.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for <see cref="DocumentRepository"/>.
/// </summary>
/// <remarks>
/// These tests focus on constructor validation and null argument handling.
/// Full repository functionality is tested in integration tests with a real database.
/// </remarks>
public class DocumentRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<ILogger<DocumentRepository>> _mockLogger = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act
        var act = () => new DocumentRepository(
            _mockConnectionFactory.Object,
            _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DocumentRepository(
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
        var act = () => new DocumentRepository(
            _mockConnectionFactory.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    #endregion
}
