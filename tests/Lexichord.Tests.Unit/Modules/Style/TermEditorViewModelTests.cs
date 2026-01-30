using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TermEditorViewModel (v0.2.5c).
/// </summary>
[Trait("Category", "Unit")]
public class TermEditorViewModelTests
{
    private readonly Mock<ITerminologyService> _terminologyServiceMock = new();
    private readonly Mock<ILogger<TermEditorViewModel>> _loggerMock = new();

    #region Initialization Tests

    [Fact]
    public void Constructor_AddMode_InitializesWithEmptyFields()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Pattern.Should().BeEmpty();
        viewModel.Recommendation.Should().BeEmpty();
        viewModel.Category.Should().Be("General");
        viewModel.Severity.Should().Be("Suggestion");
        viewModel.Notes.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
        viewModel.MatchCase.Should().BeFalse();
        viewModel.IsEditMode.Should().BeFalse();
        viewModel.DialogTitle.Should().Be("Add New Term");
    }

    [Fact]
    public void Constructor_EditMode_InitializesFromExistingTerm()
    {
        // Arrange
        var term = CreateStyleTerm();

        // Act
        var viewModel = CreateViewModel(term);

        // Assert
        viewModel.Pattern.Should().Be(term.Term);
        viewModel.Recommendation.Should().Be(term.Replacement);
        viewModel.Category.Should().Be(term.Category);
        viewModel.Severity.Should().Be(term.Severity);
        viewModel.Notes.Should().Be(term.Notes);
        viewModel.IsActive.Should().Be(term.IsActive);
        viewModel.MatchCase.Should().Be(term.MatchCase);
        viewModel.IsEditMode.Should().BeTrue();
        viewModel.DialogTitle.Should().Contain("Edit Term");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Pattern_ValidLiteral_PassesValidation()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Pattern = "simple text";

        // Assert
        viewModel.ValidationResult.Should().NotBeNull();
        viewModel.ValidationResult!.IsSuccess.Should().BeTrue();
        viewModel.HasValidationError.Should().BeFalse();
    }

    [Fact]
    public void Pattern_InvalidRegex_FailsValidation()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Pattern = "(unclosed";

        // Assert
        viewModel.ValidationResult.Should().NotBeNull();
        viewModel.ValidationResult!.IsFailure.Should().BeTrue();
        viewModel.HasValidationError.Should().BeTrue();
        viewModel.ValidationError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Pattern_EmptyString_FailsValidation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Pattern = "test"; // Set first to trigger validation

        // Act
        viewModel.Pattern = "";

        // Assert
        viewModel.ValidationResult.Should().NotBeNull();
        viewModel.ValidationResult!.IsFailure.Should().BeTrue();
    }

    #endregion

    #region CanSave Tests

    [Fact]
    public void CanSave_WithValidPattern_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Pattern = "test pattern";
        viewModel.Category = "General";

        // Assert
        viewModel.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_WithInvalidPattern_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Pattern = "(invalid";
        viewModel.Category = "General";

        // Assert
        viewModel.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_WithEmptyCategory_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Pattern = "test";
        viewModel.Category = "";

        // Assert
        viewModel.CanSave.Should().BeFalse();
    }

    #endregion

    #region IsDirty Tests

    [Fact]
    public void IsDirty_AddMode_TrueWhenPatternHasContent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsDirty.Should().BeFalse();

        // Act
        viewModel.Pattern = "new pattern";

        // Assert
        viewModel.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void IsDirty_EditMode_TrueWhenPatternChanged()
    {
        // Arrange
        var term = CreateStyleTerm();
        var viewModel = CreateViewModel(term);
        viewModel.IsDirty.Should().BeFalse();

        // Act
        viewModel.Pattern = "changed pattern";

        // Assert
        viewModel.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void IsDirty_EditMode_FalseWhenNoChanges()
    {
        // Arrange
        var term = CreateStyleTerm();
        var viewModel = CreateViewModel(term);

        // Assert
        viewModel.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void IsDirty_EditMode_TrueWhenMatchCaseChanged()
    {
        // Arrange
        var term = CreateStyleTerm();
        var viewModel = CreateViewModel(term);

        // Act
        viewModel.MatchCase = !term.MatchCase;

        // Assert
        viewModel.IsDirty.Should().BeTrue();
    }

    #endregion

    #region Pattern Testing Tests

    [Fact]
    public void SampleText_WithValidPattern_PopulatesTestResult()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Pattern = "test";

        // Act
        viewModel.SampleText = "This is a test string with test words.";

        // Assert
        viewModel.TestResult.Should().NotBeNull();
        viewModel.TestResult!.IsValid.Should().BeTrue();
        viewModel.TestResult.Matches.Should().HaveCount(2); // "test" appears twice
    }

    [Fact]
    public void SampleText_WithNoMatches_ReturnsEmptyMatches()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Pattern = "xyz";

        // Act
        viewModel.SampleText = "No matches here.";

        // Assert
        viewModel.TestResult.Should().NotBeNull();
        viewModel.TestResult!.IsValid.Should().BeTrue();
        viewModel.TestResult.Matches.Should().BeEmpty();
    }

    [Fact]
    public void MatchCase_ToggleAffectsPatternTest()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Pattern = "Test";
        viewModel.MatchCase = false;
        viewModel.SampleText = "test TEST Test";

        // Get initial count (case-insensitive)
        var initialCount = viewModel.TestResult?.Matches.Count ?? 0;

        // Act
        viewModel.MatchCase = true;

        // Assert - case-sensitive should find fewer matches
        viewModel.TestResult.Should().NotBeNull();
        viewModel.TestResult!.Matches.Count.Should().BeLessThan(initialCount);
    }

    #endregion

    #region Save Command Tests

    [Fact]
    public async Task SaveCommand_AddMode_CallsCreateAsync()
    {
        // Arrange
        _terminologyServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateTermCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var viewModel = CreateViewModel();
        viewModel.Pattern = "new pattern";
        viewModel.Recommendation = "recommendation";
        viewModel.Category = "General";

        var closeRequested = false;
        viewModel.CloseRequested += (s, success) => closeRequested = success;

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _terminologyServiceMock.Verify(x => x.CreateAsync(
            It.Is<CreateTermCommand>(c => c.Term == "new pattern"),
            It.IsAny<CancellationToken>()), Times.Once);
        closeRequested.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_EditMode_CallsUpdateAsync()
    {
        // Arrange
        var term = CreateStyleTerm();
        _terminologyServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateTermCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var viewModel = CreateViewModel(term);
        viewModel.Pattern = "updated pattern";

        var closeRequested = false;
        viewModel.CloseRequested += (s, success) => closeRequested = success;

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _terminologyServiceMock.Verify(x => x.UpdateAsync(
            It.Is<UpdateTermCommand>(c => c.Id == term.Id && c.Term == "updated pattern"),
            It.IsAny<CancellationToken>()), Times.Once);
        closeRequested.Should().BeTrue();
    }

    #endregion

    #region Cancel Command Tests

    [Fact]
    public void CancelCommand_RaisesCloseRequestedWithFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var closeResult = true; // Set to opposite of expected

        viewModel.CloseRequested += (s, success) => closeResult = success;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        closeResult.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private TermEditorViewModel CreateViewModel(StyleTerm? existingTerm = null)
    {
        return new TermEditorViewModel(
            _terminologyServiceMock.Object,
            _loggerMock.Object,
            existingTerm);
    }

    private static StyleTerm CreateStyleTerm()
    {
        return new StyleTerm
        {
            Id = Guid.NewGuid(),
            Term = "original pattern",
            Replacement = "recommended replacement",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Some notes",
            IsActive = true,
            MatchCase = false,
            StyleSheetId = Guid.NewGuid()
        };
    }

    #endregion
}
