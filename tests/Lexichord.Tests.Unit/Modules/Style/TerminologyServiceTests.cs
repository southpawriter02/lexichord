using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TerminologyService.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify CRUD operations, validation, and event publishing.
/// Integration tests verify actual database and MediatR operations.
/// </remarks>
[Trait("Category", "Unit")]
public class TerminologyServiceTests
{
    private readonly Mock<ITerminologyRepository> _repositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ILogger<TerminologyService> _logger = NullLogger<TerminologyService>.Instance;

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyService(null!, _mediatorMock.Object, _logger));
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyService(_repositoryMock.Object, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologyService(_repositoryMock.Object, _mediatorMock.Object, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidCommand_ReturnsSuccessWithNewId()
    {
        // Arrange
        var command = new CreateTermCommand("test term", "replacement", "Grammar", "Warning");
        _repositoryMock.Setup(r => r.InsertAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StyleTerm t, CancellationToken _) => t);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _repositoryMock.Verify(
            r => r.InsertAsync(It.Is<StyleTerm>(t =>
                t.Term == "test term" &&
                t.Replacement == "replacement" &&
                t.Category == "Grammar" &&
                t.Severity == "Warning" &&
                t.IsActive == true),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidCommand_PublishesLexiconChangedEvent()
    {
        // Arrange
        var command = new CreateTermCommand("test term");
        _repositoryMock.Setup(r => r.InsertAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StyleTerm t, CancellationToken _) => t);

        var service = CreateService();

        // Act
        await service.CreateAsync(command);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.Is<LexiconChangedEvent>(e =>
                e.ChangeType == LexiconChangeType.Created &&
                e.TermPattern == "test term"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyPattern_ReturnsFailure()
    {
        // Arrange
        var command = new CreateTermCommand("");
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be empty");

        _repositoryMock.Verify(
            r => r.InsertAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PatternTooLong_ReturnsFailure()
    {
        // Arrange
        var command = new CreateTermCommand(new string('a', 501));
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500 characters");
    }

    [Fact]
    public async Task CreateAsync_InvalidRegex_ReturnsFailure()
    {
        // Arrange - unbalanced parentheses
        var command = new CreateTermCommand("(unbalanced");
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid regex");
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsFailure()
    {
        // Arrange
        var command = new CreateTermCommand("test term");
        _repositoryMock.Setup(r => r.InsertAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Database error");
    }

    [Fact]
    public async Task CreateAsync_EventPublishFails_StillReturnsSuccess()
    {
        // Arrange
        var command = new CreateTermCommand("test term");
        _repositoryMock.Setup(r => r.InsertAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StyleTerm t, CancellationToken _) => t);
        _mediatorMock.Setup(m => m.Publish(It.IsAny<LexiconChangedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Event handler failure"));

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(command);

        // Assert - event failure doesn't fail the operation
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingTerm_ReturnsSuccess()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "old term", IsActive = true };
        var command = new UpdateTermCommand(termId, "new term", "replacement", "Grammar", "Error");

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<StyleTerm>(t =>
                t.Id == termId &&
                t.Term == "new term" &&
                t.Replacement == "replacement"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingTerm_ReturnsFailure()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var command = new UpdateTermCommand(termId, "new term");

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StyleTerm?)null);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateAsync_InvalidPattern_ReturnsFailure()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "old term", IsActive = true };
        var command = new UpdateTermCommand(termId, "(invalid");

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid regex");
    }

    [Fact]
    public async Task UpdateAsync_PublishesLexiconChangedEvent()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "old term", IsActive = true };
        var command = new UpdateTermCommand(termId, "new term");

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        await service.UpdateAsync(command);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.Is<LexiconChangedEvent>(e =>
                e.ChangeType == LexiconChangeType.Updated &&
                e.TermId == termId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ActiveTerm_SoftDeletes()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "test", IsActive = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(termId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<StyleTerm>(t =>
                t.Id == termId &&
                t.IsActive == false),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingTerm_ReturnsFailure()
    {
        // Arrange
        var termId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StyleTerm?)null);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(termId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteAsync_AlreadyInactiveTerm_ReturnsFailure()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "test", IsActive = false };

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(termId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already inactive");
    }

    [Fact]
    public async Task DeleteAsync_PublishesLexiconChangedEvent()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "test", IsActive = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        await service.DeleteAsync(termId);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.Is<LexiconChangedEvent>(e =>
                e.ChangeType == LexiconChangeType.Deleted &&
                e.TermId == termId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ReactivateAsync Tests

    [Fact]
    public async Task ReactivateAsync_InactiveTerm_Reactivates()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "test", IsActive = false };

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.ReactivateAsync(termId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<StyleTerm>(t =>
                t.Id == termId &&
                t.IsActive == true),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_AlreadyActiveTerm_ReturnsFailure()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "test", IsActive = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);

        var service = CreateService();

        // Act
        var result = await service.ReactivateAsync(termId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already active");
    }

    [Fact]
    public async Task ReactivateAsync_PublishesLexiconChangedEvent()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var existingTerm = new StyleTerm { Id = termId, Term = "test", IsActive = false };

        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerm);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StyleTerm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        await service.ReactivateAsync(termId);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.Is<LexiconChangedEvent>(e =>
                e.ChangeType == LexiconChangeType.Reactivated &&
                e.TermId == termId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Query Operations Tests

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        // Arrange
        var termId = Guid.NewGuid();
        var expectedTerm = new StyleTerm { Id = termId, Term = "test" };
        _repositoryMock.Setup(r => r.GetByIdAsync(termId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTerm);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(termId);

        // Assert
        result.Should().BeEquivalentTo(expectedTerm);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var terms = new List<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "term1" },
            new() { Id = Guid.NewGuid(), Term = "term2" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var service = CreateService();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveAsync_DelegatesToCachedQuery()
    {
        // Arrange
        var terms = new HashSet<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "term1", IsActive = true }
        };
        _repositoryMock.Setup(r => r.GetAllActiveTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var service = CreateService();

        // Act
        var result = await service.GetActiveAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByCategoryAsync_DelegatesToRepository()
    {
        // Arrange
        var terms = new List<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "term1", Category = "Grammar" }
        };
        _repositoryMock.Setup(r => r.GetByCategoryAsync("Grammar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var service = CreateService();

        // Act
        var result = await service.GetByCategoryAsync("Grammar");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBySeverityAsync_DelegatesToRepository()
    {
        // Arrange
        var terms = new List<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "term1", Severity = "Error" }
        };
        _repositoryMock.Setup(r => r.GetBySeverityAsync("Error", It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var service = CreateService();

        // Act
        var result = await service.GetBySeverityAsync("Error");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_DelegatesToRepository()
    {
        // Arrange
        var terms = new List<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "utilize" }
        };
        _repositoryMock.Setup(r => r.SearchAsync("util", It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var service = CreateService();

        // Act
        var result = await service.SearchAsync("util");

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var terms = new List<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "t1", Category = "Grammar", Severity = "Error", IsActive = true },
            new() { Id = Guid.NewGuid(), Term = "t2", Category = "Grammar", Severity = "Warning", IsActive = true },
            new() { Id = Guid.NewGuid(), Term = "t3", Category = "Style", Severity = "Error", IsActive = false }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var service = CreateService();

        // Act
        var stats = await service.GetStatisticsAsync();

        // Assert
        stats.TotalCount.Should().Be(3);
        stats.ActiveCount.Should().Be(2);
        stats.InactiveCount.Should().Be(1);
        stats.CategoryCounts.Should().ContainKey("Grammar").WhoseValue.Should().Be(2);
        stats.CategoryCounts.Should().ContainKey("Style").WhoseValue.Should().Be(1);
        stats.SeverityCounts.Should().ContainKey("Error").WhoseValue.Should().Be(2);
        stats.SeverityCounts.Should().ContainKey("Warning").WhoseValue.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private TerminologyService CreateService() =>
        new(_repositoryMock.Object, _mediatorMock.Object, _logger);

    #endregion
}
