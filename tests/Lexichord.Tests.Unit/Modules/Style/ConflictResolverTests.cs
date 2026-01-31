using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using static Lexichord.Abstractions.Contracts.ViolationSeverity;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ConflictResolver"/>.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the conflict resolution contract:
/// - Generic value resolution with null handling
/// - Conflict detection between configuration layers
/// - Term override logic (exclusions and additions)
/// - Rule ignore patterns with wildcard support
/// - Performance requirements (<10ms for 100 operations)
///
/// Version: v0.3.6b
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.6b")]
public class ConflictResolverTests
{
    private readonly Mock<ITerminologyRepository> _repositoryMock;
    private readonly Mock<ILogger<ConflictResolver>> _loggerMock;
    private readonly ConflictResolver _sut;

    public ConflictResolverTests()
    {
        _repositoryMock = new Mock<ITerminologyRepository>();
        _loggerMock = new Mock<ILogger<ConflictResolver>>();

        _sut = new ConflictResolver(
            _repositoryMock.Object,
            _loggerMock.Object);
    }

    #region Resolve Tests

    [Fact]
    public void Resolve_HigherNonNull_ReturnsHigher()
    {
        // Arrange
        var higher = 42;
        var lower = 10;

        // Act
        var result = _sut.Resolve(higher, lower);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Resolve_HigherNull_ReturnsLower()
    {
        // Arrange
        int? higher = null;
        int? lower = 10;

        // Act
        var result = _sut.Resolve(higher, lower);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void Resolve_BothNull_ReturnsNull()
    {
        // Arrange
        string? higher = null;
        string? lower = null;

        // Act
        var result = _sut.Resolve(higher, lower);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ResolveWithDefault Tests

    [Fact]
    public void ResolveWithDefault_HigherNonNull_ReturnsHigher()
    {
        // Arrange
        int? higher = 42;
        int? lower = 10;
        var defaultValue = 0;

        // Act
        var result = _sut.ResolveWithDefault(higher, lower, defaultValue);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ResolveWithDefault_HigherNullLowerNonNull_ReturnsLower()
    {
        // Arrange
        int? higher = null;
        int? lower = 10;
        var defaultValue = 0;

        // Act
        var result = _sut.ResolveWithDefault(higher, lower, defaultValue);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void ResolveWithDefault_BothNull_ReturnsDefault()
    {
        // Arrange
        string? higher = null;
        string? lower = null;
        var defaultValue = "default";

        // Act
        var result = _sut.ResolveWithDefault(higher, lower, defaultValue);

        // Assert
        result.Should().Be("default");
    }

    #endregion

    #region DetectConflicts Tests

    [Fact]
    public void DetectConflicts_ProjectVsUser_DetectsDifferences()
    {
        // Arrange
        var project = new StyleConfiguration
        {
            DefaultProfile = "Marketing",
            PassiveVoiceThreshold = 10
        };

        var user = new StyleConfiguration
        {
            DefaultProfile = "Technical",
            PassiveVoiceThreshold = 20
        };

        var system = StyleConfiguration.Defaults;

        // Act
        var conflicts = _sut.DetectConflicts(project, user, system);

        // Assert
        conflicts.Should().Contain(c => c.Key == "DefaultProfile" && c.IsSignificant);
        conflicts.Should().Contain(c => c.Key == "PassiveVoiceThreshold" && c.IsSignificant);
    }

    [Fact]
    public void DetectConflicts_HigherVsSystem_DetectsDifferences()
    {
        // Arrange
        var user = new StyleConfiguration
        {
            MaxSentenceLength = 30,
            FlagAdverbs = false
        };

        var system = StyleConfiguration.Defaults;

        // Act
        var conflicts = _sut.DetectConflicts(null, user, system);

        // Assert
        conflicts.Should().Contain(c => c.Key == "MaxSentenceLength" && c.IsSignificant);
        conflicts.Should().Contain(c => c.Key == "FlagAdverbs" && c.IsSignificant);
    }

    [Fact]
    public void DetectConflicts_NoHigherLayers_ReturnsEmpty()
    {
        // Arrange
        var system = StyleConfiguration.Defaults;

        // Act
        var conflicts = _sut.DetectConflicts(null, null, system);

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectConflicts_SameValues_ReturnsNonSignificant()
    {
        // Arrange
        var project = StyleConfiguration.Defaults;
        var user = StyleConfiguration.Defaults;
        var system = StyleConfiguration.Defaults;

        // Act
        var conflicts = _sut.DetectConflicts(project, user, system);

        // Assert
        conflicts.Where(c => c.IsSignificant).Should().BeEmpty();
    }

    #endregion

    #region ShouldFlagTerm Tests

    [Fact]
    public void ShouldFlagTerm_InExclusions_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            TerminologyExclusions = new[] { "whitelist", "blacklist" }
        };

        // Act
        var result = _sut.ShouldFlagTerm("whitelist", config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlagTerm_InAdditions_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            TerminologyAdditions = new[]
            {
                new TermAddition("blocklist", "Use 'deny list' instead", Error)
            }
        };

        // Act
        var result = _sut.ShouldFlagTerm("blocklist", config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFlagTerm_ExclusionOverridesAddition_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            TerminologyExclusions = new[] { "legacy-term" },
            TerminologyAdditions = new[]
            {
                new TermAddition("legacy-term", "Should be excluded", Warning)
            }
        };

        // Act
        var result = _sut.ShouldFlagTerm("legacy-term", config);

        // Assert
        // Exclusions take precedence over additions
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlagTerm_FallsBackToRepository_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration();

        _repositoryMock
            .Setup(r => r.GetAllActiveTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<StyleTerm>
            {
                new() { Id = Guid.NewGuid(), Term = "click", IsActive = true }
            });

        // Act
        var result = _sut.ShouldFlagTerm("click", config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFlagTerm_NotInRepositoryOrConfig_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration();

        _repositoryMock
            .Setup(r => r.GetAllActiveTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<StyleTerm>());

        // Act
        var result = _sut.ShouldFlagTerm("unknown-term", config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlagTerm_CaseInsensitive_ReturnsExpected()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            TerminologyExclusions = new[] { "WhiteList" }
        };

        // Act
        var result = _sut.ShouldFlagTerm("whitelist", config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlagTerm_EmptyTerm_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration();

        // Act
        var result = _sut.ShouldFlagTerm("", config);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsRuleIgnored Tests

    [Fact]
    public void IsRuleIgnored_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "PASSIVE-001" }
        };

        // Act
        var result = _sut.IsRuleIgnored("PASSIVE-001", config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRuleIgnored_ExactMatchCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "passive-001" }
        };

        // Act
        var result = _sut.IsRuleIgnored("PASSIVE-001", config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRuleIgnored_PrefixWildcard_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "PASSIVE-*" }
        };

        // Act
        var result001 = _sut.IsRuleIgnored("PASSIVE-001", config);
        var result002 = _sut.IsRuleIgnored("PASSIVE-002", config);
        var resultOther = _sut.IsRuleIgnored("VOICE-001", config);

        // Assert
        result001.Should().BeTrue();
        result002.Should().BeTrue();
        resultOther.Should().BeFalse();
    }

    [Fact]
    public void IsRuleIgnored_SuffixWildcard_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "*-WARNINGS" }
        };

        // Act
        var resultStyle = _sut.IsRuleIgnored("STYLE-WARNINGS", config);
        var resultVoice = _sut.IsRuleIgnored("VOICE-WARNINGS", config);
        var resultOther = _sut.IsRuleIgnored("STYLE-ERRORS", config);

        // Assert
        resultStyle.Should().BeTrue();
        resultVoice.Should().BeTrue();
        resultOther.Should().BeFalse();
    }

    [Fact]
    public void IsRuleIgnored_GlobalWildcard_ReturnsTrue()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "*" }
        };

        // Act
        var result = _sut.IsRuleIgnored("ANY-RULE-001", config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRuleIgnored_NoMatch_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "PASSIVE-001", "VOICE-*" }
        };

        // Act
        var result = _sut.IsRuleIgnored("STYLE-001", config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRuleIgnored_EmptyRuleId_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = new[] { "*" }
        };

        // Act
        var result = _sut.IsRuleIgnored("", config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRuleIgnored_EmptyIgnoredRules_ReturnsFalse()
    {
        // Arrange
        var config = new StyleConfiguration();

        // Act
        var result = _sut.IsRuleIgnored("PASSIVE-001", config);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void ShouldFlagTerm_100Terms_CompletesUnder10ms()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            TerminologyExclusions = Enumerable.Range(0, 50)
                .Select(i => $"excluded-term-{i}")
                .ToArray(),
            TerminologyAdditions = Enumerable.Range(0, 50)
                .Select(i => new TermAddition($"added-term-{i}", $"Message {i}", Warning))
                .ToArray()
        };

        _repositoryMock
            .Setup(r => r.GetAllActiveTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<StyleTerm>());

        var termsToCheck = Enumerable.Range(0, 100)
            .Select(i => $"check-term-{i}")
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var term in termsToCheck)
        {
            _sut.ShouldFlagTerm(term, config);
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
            "checking 100 terms should complete in under 10ms");
    }

    [Fact]
    public void IsRuleIgnored_100Rules_CompletesUnder10ms()
    {
        // Arrange
        var config = new StyleConfiguration
        {
            IgnoredRules = Enumerable.Range(0, 50)
                .Select(i => $"RULE-{i:D3}")
                .Concat(new[] { "PASSIVE-*", "*-WARNINGS" })
                .ToArray()
        };

        var rulesToCheck = Enumerable.Range(0, 100)
            .Select(i => $"CHECK-RULE-{i:D3}")
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var rule in rulesToCheck)
        {
            _sut.IsRuleIgnored(rule, config);
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
            "checking 100 rules should complete in under 10ms");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDependencies_DoesNotThrow()
    {
        // Act
        var sut = new ConflictResolver(null, null);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public void ShouldFlagTerm_NoRepository_ReturnsFalseForUnknownTerm()
    {
        // Arrange
        var sut = new ConflictResolver(null, null);
        var config = new StyleConfiguration();

        // Act
        var result = sut.ShouldFlagTerm("unknown", config);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
