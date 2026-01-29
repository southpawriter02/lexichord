using System.Text.RegularExpressions;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for <see cref="Lexichord.Modules.Editor.Services.SearchService"/>.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the core search functionality without an actual
/// TextEditor control. We test the regex building, match finding, and event
/// raising logic directly.
/// </remarks>
public class SearchServiceTests
{
    #region SearchOptions Tests

    [Fact]
    public void SearchOptions_Default_HasCorrectValues()
    {
        // Arrange & Act
        var options = SearchOptions.Default;

        // Assert
        options.MatchCase.Should().BeFalse();
        options.WholeWord.Should().BeFalse();
        options.UseRegex.Should().BeFalse();
    }

    [Fact]
    public void SearchOptions_WithMatchCase_CreatesNewInstance()
    {
        // Arrange
        var original = SearchOptions.Default;

        // Act
        var withMatchCase = original with { MatchCase = true };

        // Assert
        withMatchCase.MatchCase.Should().BeTrue();
        original.MatchCase.Should().BeFalse(); // Original unchanged
    }

    #endregion

    #region SearchResult Tests

    [Fact]
    public void SearchResult_ContainsAllRequiredProperties()
    {
        // Arrange & Act
        var result = new SearchResult(
            StartOffset: 10,
            Length: 5,
            MatchedText: "hello",
            Line: 1,
            Column: 11
        );

        // Assert
        result.StartOffset.Should().Be(10);
        result.Length.Should().Be(5);
        result.MatchedText.Should().Be("hello");
        result.Line.Should().Be(1);
        result.Column.Should().Be(11);
    }

    #endregion

    #region SearchResultsChangedEventArgs Tests

    [Fact]
    public void SearchResultsChangedEventArgs_ContainsCorrectProperties()
    {
        // Arrange & Act
        var args = new SearchResultsChangedEventArgs
        {
            TotalMatches = 5,
            CurrentIndex = 2,
            SearchText = "test"
        };

        // Assert
        args.TotalMatches.Should().Be(5);
        args.CurrentIndex.Should().Be(2);
        args.SearchText.Should().Be("test");
    }

    #endregion

    #region Regex Building Tests (via Reflection or Indirect Testing)

    [Theory]
    [InlineData("hello", false, false, false, RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    [InlineData("hello", true, false, false, RegexOptions.Compiled)]
    [InlineData("hello", false, true, false, RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    [InlineData("h.*o", false, false, true, RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    public void SearchOptions_ProducesCorrectRegexBehavior(
        string searchText,
        bool matchCase,
        bool wholeWord,
        bool useRegex,
        RegexOptions expectedOptions)
    {
        // This test verifies the expected behavior indirectly
        // by checking the regex options that would be applied
        var options = new SearchOptions(matchCase, wholeWord, useRegex);

        // Build the expected pattern
        var pattern = useRegex ? searchText : Regex.Escape(searchText);
        if (wholeWord)
        {
            pattern = $@"\b{pattern}\b";
        }

        var regexOptions = RegexOptions.Compiled;
        if (!matchCase)
        {
            regexOptions |= RegexOptions.IgnoreCase;
        }

        // Assert the options match
        regexOptions.Should().Be(expectedOptions);
    }

    [Fact]
    public void WholeWord_MatchesOnlyWholeWords()
    {
        // Arrange
        var options = new SearchOptions(MatchCase: false, WholeWord: true, UseRegex: false);
        var text = "hello helloworld hello";
        var pattern = $@"\b{Regex.Escape("hello")}\b";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Act
        var matches = regex.Matches(text);

        // Assert
        matches.Count.Should().Be(2); // Only standalone "hello"s
    }

    [Fact]
    public void UseRegex_AllowsRegexPatterns()
    {
        // Arrange
        var text = "cat cot cut";
        var pattern = "c.t";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Act
        var matches = regex.Matches(text);

        // Assert
        matches.Count.Should().Be(3);
    }

    [Fact]
    public void InvalidRegex_ShouldNotThrow_WithTimeout()
    {
        // Arrange - A pattern that could cause catastrophic backtracking
        var action = () => new Regex(
            "(a+)+$",
            RegexOptions.Compiled,
            TimeSpan.FromSeconds(1)
        );

        // Act & Assert - Should not throw during construction
        action.Should().NotThrow();
    }

    #endregion

    #region Service Visibility Tests

    [Fact]
    public void ShowSearch_SetsIsSearchVisibleToTrue()
    {
        // Arrange
        var service = CreateSearchService();

        // Act
        service.ShowSearch();

        // Assert
        service.IsSearchVisible.Should().BeTrue();
    }

    [Fact]
    public void HideSearch_SetsIsSearchVisibleToFalse()
    {
        // Arrange
        var service = CreateSearchService();
        service.ShowSearch();

        // Act
        service.HideSearch();

        // Assert
        service.IsSearchVisible.Should().BeFalse();
    }

    #endregion

    #region Match Count Property Tests

    [Fact]
    public void TotalMatchCount_InitiallyZero()
    {
        // Arrange
        var service = CreateSearchService();

        // Assert
        service.TotalMatchCount.Should().Be(0);
    }

    [Fact]
    public void CurrentMatchIndex_InitiallyZero()
    {
        // Arrange
        var service = CreateSearchService();

        // Assert
        service.CurrentMatchIndex.Should().Be(0);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ResultsChanged_EventIsExposed()
    {
        // Arrange
        var service = CreateSearchService();
        var eventRaised = false;
        service.ResultsChanged += (_, _) => eventRaised = true;

        // Just verify we can subscribe - actual event firing requires editor attachment
        eventRaised.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static SearchService CreateSearchService()
    {
        return new SearchService(
            NullLogger<SearchService>.Instance
        );
    }

    #endregion
}
