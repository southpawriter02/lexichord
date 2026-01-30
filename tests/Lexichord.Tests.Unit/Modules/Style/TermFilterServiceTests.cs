using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TermFilterService (v0.2.5b).
/// </summary>
/// <remarks>
/// LOGIC: Verifies the TermFilterService correctly filters terms
/// based on various criteria combinations.
/// </remarks>
[Trait("Category", "Unit")]
public class TermFilterServiceTests
{
    private readonly Mock<ILogger<TermFilterService>> _loggerMock = new();
    private readonly TermFilterService _service;

    public TermFilterServiceTests()
    {
        _service = new TermFilterService(_loggerMock.Object);
    }

    private static StyleTerm CreateTerm(
        string term = "Test",
        string? replacement = "Replacement",
        string? notes = "Test notes",
        string category = "General",
        string severity = "Suggestion",
        bool isActive = true)
    {
        return new StyleTerm
        {
            Id = Guid.NewGuid(),
            Term = term,
            Replacement = replacement,
            Notes = notes,
            Category = category,
            Severity = severity,
            IsActive = isActive
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var action = () => new TermFilterService(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Empty Criteria Tests

    [Fact]
    public void Filter_ReturnsActiveTerms_WhenCriteriaIsDefault()
    {
        var terms = new[]
        {
            CreateTerm("term1", isActive: true),
            CreateTerm("term2", isActive: true),
            CreateTerm("term3", isActive: false)  // This should be filtered out
        };
        var criteria = new FilterCriteria();

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);  // Only active terms
    }

    [Fact]
    public void Filter_ThrowsOnNullTerms()
    {
        var criteria = new FilterCriteria();

        var action = () => _service.Filter(null!, criteria).ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("terms");
    }

    [Fact]
    public void Filter_ThrowsOnNullCriteria()
    {
        var terms = new[] { CreateTerm() };

        var action = () => _service.Filter(terms, null!).ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("criteria");
    }

    #endregion

    #region Text Search Tests

    [Fact]
    public void Filter_MatchesTerm_CaseInsensitive()
    {
        var terms = new[]
        {
            CreateTerm(term: "HelloWorld"),
            CreateTerm(term: "GoodbyeWorld"),
            CreateTerm(term: "FooBar")
        };
        var criteria = new FilterCriteria { SearchText = "hello", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
        result[0].Term.Should().Be("HelloWorld");
    }

    [Fact]
    public void Filter_MatchesReplacement_CaseInsensitive()
    {
        var terms = new[]
        {
            CreateTerm(replacement: "UseThisInstead"),
            CreateTerm(replacement: "AnotherOption"),
            CreateTerm(replacement: "ThirdChoice")
        };
        var criteria = new FilterCriteria { SearchText = "instead", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
        result[0].Replacement.Should().Be("UseThisInstead");
    }

    [Fact]
    public void Filter_MatchesNotes_CaseInsensitive()
    {
        var terms = new[]
        {
            CreateTerm(notes: "Avoid passive voice"),
            CreateTerm(notes: "Use active constructions"),
            CreateTerm(notes: "Be concise")
        };
        var criteria = new FilterCriteria { SearchText = "passive", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
        result[0].Notes.Should().Be("Avoid passive voice");
    }

    [Fact]
    public void Filter_MatchesAnyField_Term_Replacement_Or_Notes()
    {
        var terms = new[]
        {
            CreateTerm(term: "utilize", replacement: "use", notes: "Prefer simple words"),
            CreateTerm(term: "foo", replacement: "bar", notes: "Test only"),
            CreateTerm(term: "xyz", replacement: "abc", notes: "Contains utilize in notes")
        };
        var criteria = new FilterCriteria { SearchText = "utilize", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Filter_ReturnsActiveTerms_WhenSearchTextIsWhitespace()
    {
        var terms = new[]
        {
            CreateTerm("term1", isActive: true),
            CreateTerm("term2", isActive: true)
        };
        var criteria = new FilterCriteria { SearchText = "   " };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);
    }

    #endregion

    #region IsActive Toggle Tests

    [Fact]
    public void Filter_ExcludesInactive_WhenShowInactiveIsFalse()
    {
        var terms = new[]
        {
            CreateTerm("active", isActive: true),
            CreateTerm("inactive", isActive: false),
            CreateTerm("another", isActive: true)
        };
        var criteria = new FilterCriteria { ShowInactive = false };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.IsActive);
    }

    [Fact]
    public void Filter_IncludesInactive_WhenShowInactiveIsTrue()
    {
        var terms = new[]
        {
            CreateTerm("active", isActive: true),
            CreateTerm("inactive", isActive: false)
        };
        var criteria = new FilterCriteria { ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);
    }

    #endregion

    #region Category Filter Tests

    [Fact]
    public void Filter_FiltersByCategory_CaseInsensitive()
    {
        var terms = new[]
        {
            CreateTerm(category: "Grammar"),
            CreateTerm(category: "Style"),
            CreateTerm(category: "Punctuation")
        };
        var criteria = new FilterCriteria { CategoryFilter = "grammar", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Grammar");
    }

    [Fact]
    public void Filter_ReturnsAll_WhenCategoryFilterIsNull()
    {
        var terms = new[]
        {
            CreateTerm(category: "Grammar"),
            CreateTerm(category: "Style")
        };
        var criteria = new FilterCriteria { CategoryFilter = null, ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);
    }

    #endregion

    #region Severity Filter Tests

    [Fact]
    public void Filter_FiltersBySeverity_CaseInsensitive()
    {
        var terms = new[]
        {
            CreateTerm(severity: "Error"),
            CreateTerm(severity: "Warning"),
            CreateTerm(severity: "Suggestion")
        };
        var criteria = new FilterCriteria { SeverityFilter = "error", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
        result[0].Severity.Should().Be("Error");
    }

    [Fact]
    public void Filter_ReturnsAll_WhenSeverityFilterIsNull()
    {
        var terms = new[]
        {
            CreateTerm(severity: "Error"),
            CreateTerm(severity: "Warning")
        };
        var criteria = new FilterCriteria { SeverityFilter = null, ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(2);
    }

    #endregion

    #region Combined Filter Tests

    [Fact]
    public void Filter_AppliesAllCriteria_AsAndLogic()
    {
        var terms = new[]
        {
            CreateTerm(term: "utilize", category: "Grammar", severity: "Error", isActive: true),
            CreateTerm(term: "utilize", category: "Grammar", severity: "Warning", isActive: true),
            CreateTerm(term: "utilize", category: "Style", severity: "Error", isActive: true),
            CreateTerm(term: "other", category: "Grammar", severity: "Error", isActive: true),
            CreateTerm(term: "utilize", category: "Grammar", severity: "Error", isActive: false)
        };
        var criteria = new FilterCriteria
        {
            SearchText = "utilize",
            CategoryFilter = "Grammar",
            SeverityFilter = "Error",
            ShowInactive = false
        };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
        result[0].Term.Should().Be("utilize");
        result[0].Category.Should().Be("Grammar");
        result[0].Severity.Should().Be("Error");
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public void Filter_ReturnsEmpty_WhenNoMatchesFound()
    {
        var terms = new[]
        {
            CreateTerm(term: "foo", category: "Grammar"),
            CreateTerm(term: "bar", category: "Style")
        };
        var criteria = new FilterCriteria { SearchText = "nonexistent", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().BeEmpty();
    }

    #endregion

    #region Null Field Handling

    [Fact]
    public void Filter_HandlesNullReplacement()
    {
        var terms = new[]
        {
            CreateTerm(term: "test", replacement: null)
        };
        var criteria = new FilterCriteria { SearchText = "test", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
    }

    [Fact]
    public void Filter_HandlesNullNotes()
    {
        var terms = new[]
        {
            CreateTerm(term: "test", notes: null)
        };
        var criteria = new FilterCriteria { SearchText = "test", ShowInactive = true };

        var result = _service.Filter(terms, criteria).ToList();

        result.Should().HaveCount(1);
    }

    #endregion
}
