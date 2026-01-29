using Lexichord.Host.Commands;
using Lexichord.Host.Validators;

namespace Lexichord.Tests.Unit.Host.Validators;

/// <summary>
/// Unit tests for CreateDocumentCommandValidator demonstrating FluentValidation testing patterns.
/// </summary>
[Trait("Category", "Unit")]
public class CreateDocumentCommandValidatorTests
{
    private readonly CreateDocumentCommandValidator _validator = new();

    #region Valid Command Tests

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "My Document",
            Content: "Some content here",
            Tags: new[] { "tag1", "tag2" },
            WordCountGoal: 5000);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMinimalValidCommand_ReturnsValid()
    {
        // Arrange - only required field (Title)
        var command = new CreateDocumentCommand(
            Title: "Just a title",
            Content: null,
            Tags: null,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Title Validation Tests

    [Fact]
    public void Validate_WithEmptyTitle_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "",
            Content: null,
            Tags: null,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Title" &&
            e.ErrorCode == "TITLE_REQUIRED");
    }

    [Fact]
    public void Validate_WithNullTitle_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: null!,
            Content: null,
            Tags: null,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Title" &&
            e.ErrorCode == "TITLE_REQUIRED");
    }

    [Fact]
    public void Validate_WithTitleTooLong_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: new string('x', CreateDocumentCommandValidator.MaxTitleLength + 1),
            Content: null,
            Tags: null,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Title" &&
            e.ErrorCode == "TITLE_TOO_LONG");
    }

    [Fact]
    public void Validate_WithTitleAtMaxLength_ReturnsValid()
    {
        // Arrange - exactly at limit
        var command = new CreateDocumentCommand(
            Title: new string('x', CreateDocumentCommandValidator.MaxTitleLength),
            Content: null,
            Tags: null,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Tags Validation Tests

    [Fact]
    public void Validate_WithTooManyTags_ReturnsInvalid()
    {
        // Arrange
        var tooManyTags = Enumerable.Range(1, CreateDocumentCommandValidator.MaxTagCount + 1)
            .Select(i => $"tag{i}")
            .ToList();

        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: tooManyTags,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Tags" &&
            e.ErrorCode == "TOO_MANY_TAGS");
    }

    [Fact]
    public void Validate_WithEmptyTag_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: new[] { "valid", "", "also-valid" },
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Tags") &&
            e.ErrorCode == "TAG_EMPTY");
    }

    [Fact]
    public void Validate_WithTagTooLong_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: new[] { new string('x', CreateDocumentCommandValidator.MaxTagLength + 1) },
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Tags") &&
            e.ErrorCode == "TAG_TOO_LONG");
    }

    [Fact]
    public void Validate_WithEmptyTagsList_ReturnsValid()
    {
        // Arrange - empty list should be treated same as null (no validation)
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: new List<string>(),
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMaxTags_ReturnsValid()
    {
        // Arrange - exactly at limit
        var maxTags = Enumerable.Range(1, CreateDocumentCommandValidator.MaxTagCount)
            .Select(i => $"tag{i}")
            .ToList();

        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: maxTags,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region WordCountGoal Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithInvalidWordCount_ReturnsInvalid(int invalidWordCount)
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: null,
            WordCountGoal: invalidWordCount);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "WordCountGoal" &&
            e.ErrorCode == "WORD_COUNT_TOO_LOW");
    }

    [Fact]
    public void Validate_WithWordCountTooHigh_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: null,
            WordCountGoal: CreateDocumentCommandValidator.MaxWordCountGoal + 1);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "WordCountGoal" &&
            e.ErrorCode == "WORD_COUNT_TOO_HIGH");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(50000)]
    [InlineData(1000000)]
    public void Validate_WithValidWordCount_ReturnsValid(int validWordCount)
    {
        // Arrange
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: null,
            WordCountGoal: validWordCount);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullWordCountGoal_ReturnsValid()
    {
        // Arrange - when not specified, no validation needed
        var command = new CreateDocumentCommand(
            Title: "Valid Title",
            Content: null,
            Tags: null,
            WordCountGoal: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange - multiple validation problems
        var command = new CreateDocumentCommand(
            Title: "",
            Content: null,
            Tags: new[] { "" },
            WordCountGoal: -10);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Errors.Select(e => e.ErrorCode).Should().Contain("TITLE_REQUIRED");
        result.Errors.Select(e => e.ErrorCode).Should().Contain("TAG_EMPTY");
        result.Errors.Select(e => e.ErrorCode).Should().Contain("WORD_COUNT_TOO_LOW");
    }

    #endregion
}
