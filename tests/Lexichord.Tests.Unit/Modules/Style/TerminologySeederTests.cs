using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TerminologySeeder.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify seeding logic, idempotency, and data coverage.
/// Integration tests verify actual database operations.
/// </remarks>
[Trait("Category", "Unit")]
public class TerminologySeederTests
{
    private readonly Mock<ITerminologyRepository> _repositoryMock = new();
    private readonly ILogger<TerminologySeeder> _logger = NullLogger<TerminologySeeder>.Instance;

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologySeeder(null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminologySeeder(_repositoryMock.Object, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var seeder = CreateSeeder();

        // Assert
        seeder.Should().NotBeNull();
    }

    #endregion

    #region SeedIfEmptyAsync Tests

    [Fact]
    public async Task SeedIfEmptyAsync_DatabaseEmpty_SeedsTerms()
    {
        // Arrange
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<StyleTerm> terms, CancellationToken _) => terms.Count());

        var seeder = CreateSeeder();

        // Act
        var result = await seeder.SeedIfEmptyAsync();

        // Assert
        result.WasEmpty.Should().BeTrue();
        result.TermsSeeded.Should().BeGreaterThan(0);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        _repositoryMock.Verify(
            r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_DatabaseHasData_SkipsSeeding()
    {
        // Arrange
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var seeder = CreateSeeder();

        // Act
        var result = await seeder.SeedIfEmptyAsync();

        // Assert
        result.WasEmpty.Should().BeFalse();
        result.TermsSeeded.Should().Be(0);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        _repositoryMock.Verify(
            r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_Idempotent_MultipleCallsSameResult()
    {
        // Arrange
        var callCount = 0;
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++);
        _repositoryMock.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<StyleTerm> terms, CancellationToken _) => terms.Count());

        var seeder = CreateSeeder();

        // Act
        var first = await seeder.SeedIfEmptyAsync();
        var second = await seeder.SeedIfEmptyAsync();

        // Assert
        first.WasEmpty.Should().BeTrue("first call should seed");
        second.WasEmpty.Should().BeFalse("second call should skip");

        _repositoryMock.Verify(
            r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "InsertManyAsync should only be called once");
    }

    #endregion

    #region ReseedAsync Tests

    [Fact]
    public async Task ReseedAsync_ClearExistingFalse_AddsWithoutClearing()
    {
        // Arrange
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _repositoryMock.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<StyleTerm> terms, CancellationToken _) => terms.Count());

        var seeder = CreateSeeder();

        // Act
        var result = await seeder.ReseedAsync(clearExisting: false);

        // Assert
        result.WasEmpty.Should().BeFalse();
        result.TermsSeeded.Should().BeGreaterThan(0);

        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not fetch all terms when not clearing");
        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not delete terms when not clearing");
    }

    [Fact]
    public async Task ReseedAsync_ClearExistingTrue_DeletesAndReseeds()
    {
        // Arrange
        var existingTerms = new List<StyleTerm>
        {
            new() { Id = Guid.NewGuid(), Term = "old1" },
            new() { Id = Guid.NewGuid(), Term = "old2" },
            new() { Id = Guid.NewGuid(), Term = "old3" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTerms);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<StyleTerm> terms, CancellationToken _) => terms.Count());

        var seeder = CreateSeeder();

        // Act
        var result = await seeder.ReseedAsync(clearExisting: true);

        // Assert
        result.WasEmpty.Should().BeFalse("wasEmpty reflects state before clearing");
        result.TermsSeeded.Should().BeGreaterThan(0);

        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3),
            "Should delete all existing terms");
        _repositoryMock.Verify(
            r => r.InsertManyAsync(It.IsAny<IEnumerable<StyleTerm>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetDefaultTerms Tests

    [Fact]
    public void GetDefaultTerms_ReturnsNonEmptyList()
    {
        // Arrange
        var seeder = CreateSeeder();

        // Act
        var terms = seeder.GetDefaultTerms();

        // Assert
        terms.Should().NotBeNull();
        terms.Should().NotBeEmpty();
        terms.Should().HaveCountGreaterThan(40, "should have ~50 terms");
    }

    [Fact]
    public void GetDefaultTerms_AllTermsHaveRequiredFields()
    {
        // Arrange
        var seeder = CreateSeeder();

        // Act
        var terms = seeder.GetDefaultTerms();

        // Assert
        foreach (var term in terms)
        {
            term.Id.Should().NotBe(Guid.Empty, because: "each term needs a unique ID");
            term.StyleSheetId.Should().NotBe(Guid.Empty, because: "each term needs a style sheet ID");
            term.Term.Should().NotBeNullOrWhiteSpace(because: "each term needs a pattern");
            term.Category.Should().NotBeNullOrWhiteSpace(because: "each term needs a category");
            term.Severity.Should().NotBeNullOrWhiteSpace(because: "each term needs a severity");
        }
    }

    [Fact]
    public void GetDefaultTerms_CoversExpectedCategories()
    {
        // Arrange
        var seeder = CreateSeeder();
        var expectedCategories = new[] { "Terminology", "Capitalization", "Punctuation", "Voice", "Clarity", "Grammar" };

        // Act
        var terms = seeder.GetDefaultTerms();
        var actualCategories = terms.Select(t => t.Category).Distinct().ToList();

        // Assert
        foreach (var expected in expectedCategories)
        {
            actualCategories.Should().Contain(expected,
                $"expected category '{expected}' to be present in seed data");
        }
    }

    [Fact]
    public void GetDefaultTerms_SeveritiesAreValid()
    {
        // Arrange
        var seeder = CreateSeeder();
        var validSeverities = new[] { "Error", "Warning", "Suggestion" };

        // Act
        var terms = seeder.GetDefaultTerms();

        // Assert
        foreach (var term in terms)
        {
            validSeverities.Should().Contain(term.Severity,
                $"term '{term.Term}' has invalid severity '{term.Severity}'");
        }
    }

    [Fact]
    public void GetDefaultTerms_AllTermsHaveNotes()
    {
        // Arrange
        var seeder = CreateSeeder();

        // Act
        var terms = seeder.GetDefaultTerms();

        // Assert
        foreach (var term in terms)
        {
            term.Notes.Should().NotBeNullOrWhiteSpace(
                $"term '{term.Term}' should have notes explaining rationale");
        }
    }

    [Fact]
    public void GetDefaultTerms_ReturnsSameInstanceOnMultipleCalls()
    {
        // Arrange
        var seeder = CreateSeeder();

        // Act
        var first = seeder.GetDefaultTerms();
        var second = seeder.GetDefaultTerms();

        // Assert
        first.Should().BeSameAs(second, because: "default terms are immutable and reused");
    }

    #endregion

    #region Helper Methods

    private TerminologySeeder CreateSeeder()
    {
        return new TerminologySeeder(_repositoryMock.Object, _logger);
    }

    #endregion
}
