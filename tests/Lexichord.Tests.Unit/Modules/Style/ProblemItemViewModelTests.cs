using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.ViewModels;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for ProblemItemViewModel (v0.2.6a).
/// </summary>
[Trait("Category", "Unit")]
public class ProblemItemViewModelTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a test violation with specified properties.
    /// </summary>
    private static AggregatedStyleViolation CreateViolation(
        string id = "test-violation-1",
        string documentId = "doc-1",
        string ruleId = "rule-1",
        int line = 10,
        int column = 5,
        string message = "Test violation message",
        ViolationSeverity severity = ViolationSeverity.Warning,
        string violatingText = "bad text")
    {
        return new AggregatedStyleViolation
        {
            Id = id,
            DocumentId = documentId,
            RuleId = ruleId,
            StartOffset = 100,
            Length = violatingText.Length,
            Line = line,
            Column = column,
            EndLine = line,
            EndColumn = column + violatingText.Length,
            ViolatingText = violatingText,
            Message = message,
            Severity = severity,
            Category = RuleCategory.Terminology
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllPropertiesFromViolation()
    {
        // Arrange
        var violation = CreateViolation(
            id: "test-id",
            documentId: "my-doc",
            ruleId: "my-rule",
            line: 42,
            column: 15,
            message: "Test message",
            severity: ViolationSeverity.Error,
            violatingText: "offending");

        // Act
        var viewModel = new ProblemItemViewModel(violation);

        // Assert
        viewModel.Id.Should().Be("test-id");
        viewModel.DocumentId.Should().Be("my-doc");
        viewModel.RuleId.Should().Be("my-rule");
        viewModel.Line.Should().Be(42);
        viewModel.Column.Should().Be(15);
        viewModel.Message.Should().Be("Test message");
        viewModel.Severity.Should().Be(ViolationSeverity.Error);
        viewModel.ViolatingText.Should().Be("offending");
        viewModel.StartOffset.Should().Be(100);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenViolationIsNull()
    {
        // Act
        var act = () => new ProblemItemViewModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("violation");
    }

    #endregion

    #region Location Property Tests

    [Theory]
    [InlineData(1, 1, "1:1")]
    [InlineData(42, 15, "42:15")]
    [InlineData(100, 200, "100:200")]
    public void Location_FormatsLineAndColumn(int line, int column, string expected)
    {
        // Arrange
        var violation = CreateViolation(line: line, column: column);

        // Act
        var viewModel = new ProblemItemViewModel(violation);

        // Assert
        viewModel.Location.Should().Be(expected);
    }

    #endregion

    #region SeverityIcon Property Tests

    [Theory]
    [InlineData(ViolationSeverity.Error, "⛔")]
    [InlineData(ViolationSeverity.Warning, "⚠")]
    [InlineData(ViolationSeverity.Info, "ℹ")]
    [InlineData(ViolationSeverity.Hint, "💡")]
    public void SeverityIcon_ReturnsCorrectIcon_ForSeverity(ViolationSeverity severity, string expectedIcon)
    {
        // Arrange
        var violation = CreateViolation(severity: severity);

        // Act
        var viewModel = new ProblemItemViewModel(violation);

        // Assert
        viewModel.SeverityIcon.Should().Be(expectedIcon);
    }

    #endregion

    #region IProblemItem Interface Tests

    [Fact]
    public void ImplementsIProblemItem()
    {
        // Arrange
        var violation = CreateViolation();

        // Act
        var viewModel = new ProblemItemViewModel(violation);

        // Assert
        viewModel.Should().BeAssignableTo<IProblemItem>();
    }

    #endregion
}
