using FluentValidation;
using Lexichord.Host.Commands;

namespace Lexichord.Host.Validators;

/// <summary>
/// Validator for <see cref="CreateDocumentCommand"/> demonstrating FluentValidation patterns.
/// </summary>
/// <remarks>
/// DESIGN: This validator demonstrates common validation patterns:
///
/// 1. **Required Fields**: Using <c>NotEmpty()</c> for required strings.
/// 2. **Length Constraints**: Using <c>MaximumLength()</c> for string limits.
/// 3. **Collection Validation**: Using <c>ForEach()</c> for item-level rules.
/// 4. **Custom Logic**: Using <c>Must()</c> for complex business rules.
/// 5. **Conditional Rules**: Using <c>When()</c> for optional field validation.
/// 6. **Error Codes**: Using <c>WithErrorCode()</c> for machine-readable errors.
/// 7. **Custom Messages**: Using <c>WithMessage()</c> with placeholders.
///
/// This is a sample validator for v0.0.7d demonstration purposes.
/// </remarks>
public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    /// <summary>
    /// Maximum length for document title.
    /// </summary>
    public const int MaxTitleLength = 200;

    /// <summary>
    /// Maximum number of tags allowed.
    /// </summary>
    public const int MaxTagCount = 10;

    /// <summary>
    /// Maximum length for each tag.
    /// </summary>
    public const int MaxTagLength = 50;

    /// <summary>
    /// Minimum allowed word count goal.
    /// </summary>
    public const int MinWordCountGoal = 1;

    /// <summary>
    /// Maximum allowed word count goal.
    /// </summary>
    public const int MaxWordCountGoal = 1_000_000;

    /// <summary>
    /// Initializes a new instance of the validator with all rules.
    /// </summary>
    public CreateDocumentCommandValidator()
    {
        // RULE: Title is required
        RuleFor(x => x.Title)
            .NotEmpty()
                .WithMessage("Document title is required.")
                .WithErrorCode("TITLE_REQUIRED")
            .MaximumLength(MaxTitleLength)
                .WithMessage("Document title must not exceed {MaxLength} characters.")
                .WithErrorCode("TITLE_TOO_LONG");

        // RULE: Tags collection validation (when provided)
        When(x => x.Tags is not null && x.Tags.Count > 0, () =>
        {
            RuleFor(x => x.Tags)
                .Must(tags => tags!.Count <= MaxTagCount)
                    .WithMessage($"Cannot have more than {{{nameof(MaxTagCount)}}} tags.")
                    .WithErrorCode("TOO_MANY_TAGS");

            RuleForEach(x => x.Tags)
                .NotEmpty()
                    .WithMessage("Tags cannot be empty.")
                    .WithErrorCode("TAG_EMPTY")
                .MaximumLength(MaxTagLength)
                    .WithMessage($"Each tag must not exceed {{{nameof(MaxTagLength)}}} characters.")
                    .WithErrorCode("TAG_TOO_LONG");
        });

        // RULE: Word count goal validation (when provided)
        When(x => x.WordCountGoal.HasValue, () =>
        {
            RuleFor(x => x.WordCountGoal)
                .GreaterThanOrEqualTo(MinWordCountGoal)
                    .WithMessage($"Word count goal must be at least {{{nameof(MinWordCountGoal)}}}.")
                    .WithErrorCode("WORD_COUNT_TOO_LOW")
                .LessThanOrEqualTo(MaxWordCountGoal)
                    .WithMessage($"Word count goal must not exceed {{{nameof(MaxWordCountGoal)}}}.")
                    .WithErrorCode("WORD_COUNT_TOO_HIGH");
        });
    }
}
