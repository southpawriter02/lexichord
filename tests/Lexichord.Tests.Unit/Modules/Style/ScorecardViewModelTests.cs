using FluentAssertions;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for ScorecardViewModel (v0.2.6c).
/// </summary>
[Trait("Category", "Unit")]
public class ScorecardViewModelTests
{
    private readonly Mock<ILogger<ScorecardViewModel>> _loggerMock = new();

    #region Test Helpers

    private ScorecardViewModel CreateViewModel() => new(_loggerMock.Object);

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithPerfectScore()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ComplianceScore.Should().Be(100);
        viewModel.PreviousScore.Should().Be(100);
        viewModel.ScoreGrade.Should().Be("A");
        viewModel.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_InitializesWithZeroCounts()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.TotalErrors.Should().Be(0);
        viewModel.TotalWarnings.Should().Be(0);
        viewModel.TotalInfo.Should().Be(0);
    }

    [Fact]
    public void Constructor_InitializesStableTrend()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Trend.Should().Be(ScoreTrend.Stable);
        viewModel.TrendIcon.Should().Be("→");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act
        var act = () => new ScorecardViewModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Score Calculation Tests

    [Theory]
    [InlineData(0, 0, 0, 100)]     // Perfect score
    [InlineData(1, 0, 0, 95)]     // 1 error = 5 points
    [InlineData(0, 1, 0, 98)]     // 1 warning = 2 points
    [InlineData(0, 0, 1, 99.5)]   // 1 info = 0.5 points
    [InlineData(2, 3, 4, 82)]     // (10 + 6 + 2) = 18 penalty
    [InlineData(20, 0, 0, 0)]     // Max penalty (clamped to 0)
    [InlineData(10, 10, 10, 25)]  // (50 + 20 + 5) = 75 penalty
    public void Update_CalculatesCorrectScore(int errors, int warnings, int info, double expectedScore)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Update(errors, warnings, info);

        // Assert
        viewModel.ComplianceScore.Should().Be(expectedScore);
    }

    [Fact]
    public void Update_ClampsNegativeCountsToZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Update(-5, -3, -1);

        // Assert
        viewModel.TotalErrors.Should().Be(0);
        viewModel.TotalWarnings.Should().Be(0);
        viewModel.TotalInfo.Should().Be(0);
        viewModel.ComplianceScore.Should().Be(100);
    }

    [Fact]
    public void Update_ClampsScoreToMinimumZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act - 50 errors = 250 penalty points
        viewModel.Update(50, 50, 50);

        // Assert
        viewModel.ComplianceScore.Should().Be(0);
    }

    [Fact]
    public void Update_UpdatesTotalCount()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Update(3, 5, 7);

        // Assert
        viewModel.TotalCount.Should().Be(15);
    }

    #endregion

    #region Grade Assignment Tests

    [Fact]
    public void ScoreGrade_ReturnsA_ForScoreAtOrAbove90()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(0, 0, 0); // Score = 100
        viewModel.ScoreGrade.Should().Be("A");

        viewModel.Update(2, 0, 0); // Score = 90
        viewModel.ScoreGrade.Should().Be("A");
    }

    [Fact]
    public void ScoreGrade_ReturnsB_ForScoreBetween80And89()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(2, 1, 0); // Score = 88
        viewModel.ScoreGrade.Should().Be("B");

        viewModel.Update(4, 0, 0); // Score = 80
        viewModel.ScoreGrade.Should().Be("B");
    }

    [Fact]
    public void ScoreGrade_ReturnsC_ForScoreBetween70And79()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(4, 1, 0); // Score = 78
        viewModel.ScoreGrade.Should().Be("C");

        viewModel.Update(6, 0, 0); // Score = 70
        viewModel.ScoreGrade.Should().Be("C");
    }

    [Fact]
    public void ScoreGrade_ReturnsD_ForScoreBetween50And69()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(6, 1, 0); // Score = 68
        viewModel.ScoreGrade.Should().Be("D");

        viewModel.Update(10, 0, 0); // Score = 50
        viewModel.ScoreGrade.Should().Be("D");
    }

    [Fact]
    public void ScoreGrade_ReturnsF_ForScoreBelow50()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(10, 1, 0); // Score = 48
        viewModel.ScoreGrade.Should().Be("F");

        viewModel.Update(20, 0, 0); // Score = 0
        viewModel.ScoreGrade.Should().Be("F");
    }

    #endregion

    #region Color Mapping Tests

    [Theory]
    [InlineData(100, "#22C55E")] // A - Green
    [InlineData(90, "#22C55E")]  // A boundary
    [InlineData(85, "#84CC16")] // B - Light Green
    [InlineData(80, "#84CC16")] // B boundary
    [InlineData(75, "#EAB308")] // C - Yellow
    [InlineData(70, "#EAB308")] // C boundary
    [InlineData(60, "#F97316")] // D - Orange
    [InlineData(50, "#F97316")] // D boundary
    [InlineData(40, "#EF4444")] // F - Red
    [InlineData(0, "#EF4444")]  // F minimum
    public void ScoreColor_ReturnsCorrectColor(double targetScore, string expectedColor)
    {
        // Arrange
        var viewModel = CreateViewModel();
        // Calculate errors needed: errors = (100 - targetScore) / 5
        var errorsNeeded = (int)((100 - targetScore) / 5);
        viewModel.Update(errorsNeeded, 0, 0);

        // Assert
        viewModel.ScoreColor.Should().Be(expectedColor);
    }

    #endregion

    #region Trend Tracking Tests

    [Fact]
    public void Update_SetsTrendToImproving_WhenScoreIncreasesMoreThanTolerance()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(5, 0, 0); // Score = 75
        
        // Act - Reduce errors, score goes up
        viewModel.Update(2, 0, 0); // Score = 90, delta = +15

        // Assert
        viewModel.Trend.Should().Be(ScoreTrend.Improving);
        viewModel.TrendIcon.Should().Be("↑");
        viewModel.TrendColor.Should().Be("#22C55E");
    }

    [Fact]
    public void Update_SetsTrendToDeclining_WhenScoreDecreasesMoreThanTolerance()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(1, 0, 0); // Score = 95

        // Act - Add more errors
        viewModel.Update(5, 0, 0); // Score = 75, delta = -20

        // Assert
        viewModel.Trend.Should().Be(ScoreTrend.Declining);
        viewModel.TrendIcon.Should().Be("↓");
        viewModel.TrendColor.Should().Be("#EF4444");
    }

    [Fact]
    public void Update_SetsTrendToStable_WhenScoreChangeWithinTolerance()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(2, 0, 0); // Score = 90

        // Act - Minor change within 1% tolerance
        viewModel.Update(2, 0, 1); // Score = 89.5, delta = -0.5

        // Assert
        viewModel.Trend.Should().Be(ScoreTrend.Stable);
        viewModel.TrendIcon.Should().Be("→");
        viewModel.TrendColor.Should().Be("#9CA3AF");
    }

    [Fact]
    public void Update_PreservesPreviousScore()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(2, 0, 0); // Score = 90

        // Act
        viewModel.Update(4, 0, 0); // Score = 80

        // Assert
        viewModel.PreviousScore.Should().Be(90);
        viewModel.ComplianceScore.Should().Be(80);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(5, 10, 15);

        // Act
        viewModel.Reset();

        // Assert
        viewModel.TotalErrors.Should().Be(0);
        viewModel.TotalWarnings.Should().Be(0);
        viewModel.TotalInfo.Should().Be(0);
        viewModel.TotalCount.Should().Be(0);
        viewModel.ComplianceScore.Should().Be(100);
        viewModel.PreviousScore.Should().Be(100);
        viewModel.ScoreGrade.Should().Be("A");
        viewModel.Trend.Should().Be(ScoreTrend.Stable);
    }

    #endregion

    #region Display Formatting Tests

    [Theory]
    [InlineData(0, 0, 0, "100%")]
    [InlineData(1, 0, 0, "95%")]
    [InlineData(2, 3, 4, "82%")]
    public void ComplianceScoreDisplay_FormatsCorrectly(int errors, int warnings, int info, string expected)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Update(errors, warnings, info);

        // Assert
        viewModel.ComplianceScoreDisplay.Should().Be(expected);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void Update_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        viewModel.Update(1, 1, 1);

        // Assert
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TotalErrors));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TotalWarnings));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TotalInfo));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TotalCount));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.ComplianceScore));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.ScoreGrade));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.ScoreColor));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.ComplianceScoreDisplay));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.Trend));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TrendIcon));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TrendColor));
    }

    [Fact]
    public void Reset_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Update(5, 5, 5);

        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        viewModel.Reset();

        // Assert
        changedProperties.Should().Contain(nameof(ScorecardViewModel.TotalCount));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.ScoreGrade));
        changedProperties.Should().Contain(nameof(ScorecardViewModel.ScoreColor));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIScorecardViewModel()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Should().BeAssignableTo<IScorecardViewModel>();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Update_HandlesLargeViolationCounts()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Update(1000, 1000, 1000);

        // Assert - Score should clamp to 0
        viewModel.ComplianceScore.Should().Be(0);
        viewModel.ScoreGrade.Should().Be("F");
        viewModel.TotalCount.Should().Be(3000);
    }

    [Fact]
    public void Update_HandlesMixedViolationTypes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act - 3 errors (15) + 2 warnings (4) + 10 info (5) = 24 penalty
        viewModel.Update(3, 2, 10);

        // Assert
        viewModel.ComplianceScore.Should().Be(76);
        viewModel.ScoreGrade.Should().Be("C");
    }

    #endregion
}
