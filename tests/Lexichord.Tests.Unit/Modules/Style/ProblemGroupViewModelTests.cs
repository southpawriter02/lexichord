using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.ViewModels;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for ProblemGroupViewModel (v0.2.6a).
/// </summary>
[Trait("Category", "Unit")]
public class ProblemGroupViewModelTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a mock problem item for testing.
    /// </summary>
    private static ProblemItemViewModel CreateProblemItem(
        ViolationSeverity severity = ViolationSeverity.Warning)
    {
        var violation = new AggregatedStyleViolation
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = "doc-1",
            RuleId = "rule-1",
            StartOffset = 0,
            Length = 5,
            Line = 1,
            Column = 1,
            EndLine = 1,
            EndColumn = 6,
            ViolatingText = "test",
            Message = "Test message",
            Severity = severity,
            Category = RuleCategory.Terminology
        };
        return new ProblemItemViewModel(violation);
    }

    #endregion

    #region Constructor Tests

    [Theory]
    [InlineData(ViolationSeverity.Error, "Errors")]
    [InlineData(ViolationSeverity.Warning, "Warnings")]
    [InlineData(ViolationSeverity.Info, "Info")]
    [InlineData(ViolationSeverity.Hint, "Hints")]
    public void Constructor_SetsDisplayName_BasedOnSeverity(ViolationSeverity severity, string expectedName)
    {
        // Act
        var viewModel = new ProblemGroupViewModel(severity);

        // Assert
        viewModel.DisplayName.Should().Be(expectedName);
        viewModel.Severity.Should().Be(severity);
    }

    [Fact]
    public void Constructor_InitializesEmptyItems()
    {
        // Act
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Error);

        // Assert
        viewModel.Items.Should().BeEmpty();
        viewModel.Count.Should().Be(0);
    }

    [Fact]
    public void Constructor_StartsExpanded()
    {
        // Act
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Warning);

        // Assert
        viewModel.IsExpanded.Should().BeTrue();
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_IncreasesCount()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Error);
        var item = CreateProblemItem();

        // Act
        viewModel.AddItem(item);

        // Assert
        viewModel.Count.Should().Be(1);
        viewModel.Items.Should().HaveCount(1);
        viewModel.Items.Should().Contain(item);
    }

    [Fact]
    public void AddItem_UpdatesHeaderText()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Warning);
        viewModel.HeaderText.Should().Be("Warnings (0)");

        // Act
        viewModel.AddItem(CreateProblemItem());

        // Assert
        viewModel.HeaderText.Should().Be("Warnings (1)");
    }

    [Fact]
    public void AddItem_RaisesPropertyChangedForCount()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Info);
        var countChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProblemGroupViewModel.Count))
                countChanged = true;
        };

        // Act
        viewModel.AddItem(CreateProblemItem());

        // Assert
        countChanged.Should().BeTrue();
    }

    [Fact]
    public void AddItem_ThrowsArgumentNullException_WhenItemIsNull()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Error);

        // Act
        var act = () => viewModel.AddItem(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("item");
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Error);
        viewModel.AddItem(CreateProblemItem());
        viewModel.AddItem(CreateProblemItem());
        viewModel.Count.Should().Be(2);

        // Act
        viewModel.Clear();

        // Assert
        viewModel.Items.Should().BeEmpty();
        viewModel.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_UpdatesHeaderText()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Error);
        viewModel.AddItem(CreateProblemItem());
        viewModel.HeaderText.Should().Be("Errors (1)");

        // Act
        viewModel.Clear();

        // Assert
        viewModel.HeaderText.Should().Be("Errors (0)");
    }

    #endregion

    #region IsExpanded Tests

    [Fact]
    public void IsExpanded_CanBeToggled()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Warning);
        viewModel.IsExpanded.Should().BeTrue();

        // Act
        viewModel.IsExpanded = false;

        // Assert
        viewModel.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void IsExpanded_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Info);
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProblemGroupViewModel.IsExpanded))
                propertyChanged = true;
        };

        // Act
        viewModel.IsExpanded = false;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion

    #region SeverityIcon Tests

    [Theory]
    [InlineData(ViolationSeverity.Error, "⛔")]
    [InlineData(ViolationSeverity.Warning, "⚠")]
    [InlineData(ViolationSeverity.Info, "ℹ")]
    [InlineData(ViolationSeverity.Hint, "💡")]
    public void SeverityIcon_ReturnsCorrectIcon(ViolationSeverity severity, string expectedIcon)
    {
        // Act
        var viewModel = new ProblemGroupViewModel(severity);

        // Assert
        viewModel.SeverityIcon.Should().Be(expectedIcon);
    }

    #endregion

    #region IProblemGroup Interface Tests

    [Fact]
    public void ImplementsIProblemGroup()
    {
        // Act
        var viewModel = new ProblemGroupViewModel(ViolationSeverity.Error);

        // Assert
        viewModel.Should().BeAssignableTo<IProblemGroup>();
    }

    #endregion
}
