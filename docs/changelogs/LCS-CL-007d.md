# LCS-CL-007d: Validation Pipeline Behavior

**Version:** v0.0.7d  
**Category:** Infrastructure  
**Feature Name:** Validation Pipeline Behavior  
**Date:** 2026-01-28

---

## Summary

Implements FluentValidation integration into the MediatR pipeline with automatic request validation, structured error aggregation, and validator auto-discovery.

---

## New Features

### Validation Infrastructure

- **ValidationError** — Structured record for individual validation errors
    - `PropertyName` — The property that failed validation
    - `ErrorMessage` — Human-readable error description
    - `ErrorCode` — Machine-readable error identifier (optional)
    - `AttemptedValue` — The value that caused the failure (optional)

- **ValidationException** — Custom exception aggregating all validation failures
    - `Errors` — Read-only collection of `ValidationError` instances
    - Constructor overloads for raw errors and FluentValidation failures
    - Formatted message listing all validation problems

### Pipeline Behavior

- **ValidationBehavior<TRequest, TResponse>** — Validation pipeline behavior
    - Automatically resolves all `IValidator<TRequest>` instances from DI
    - Executes validators in parallel using `Task.WhenAll`
    - Aggregates all failures from all validators
    - Throws `ValidationException` with structured errors before handler executes
    - Skips validation gracefully when no validators are registered
    - Debug logging for validation pass/fail outcomes

### Validator Auto-Discovery

- **AddValidatorsFromAssemblies** — Automatic validator registration
    - Discovers all `AbstractValidator<T>` implementations in scanned assemblies
    - No manual registration required for new validators
    - Follows assembly scanning pattern from MediatR registration

### Sample Implementation

- **CreateDocumentCommand** — Example command for demonstration
    - Title (required, max 200 chars)
    - Content (optional)
    - Tags (optional, max 10 tags, max 50 chars each)
    - WordCountGoal (optional, 1-1,000,000)

- **CreateDocumentCommandValidator** — Comprehensive validator example
    - Demonstrates `NotEmpty`, `MaximumLength` rules
    - Conditional validation with `When()` clause
    - Collection validation with `RuleForEach()`
    - Custom error messages and error codes

---

## Files Added

| File                                                                                | Description                |
| :---------------------------------------------------------------------------------- | :------------------------- |
| `src/Lexichord.Abstractions/Validation/ValidationError.cs`                          | Error record               |
| `src/Lexichord.Abstractions/Validation/ValidationException.cs`                      | Exception class            |
| `src/Lexichord.Host/Infrastructure/Behaviors/ValidationBehavior.cs`                 | Pipeline behavior          |
| `src/Lexichord.Host/Commands/CreateDocumentCommand.cs`                              | Sample command             |
| `src/Lexichord.Host/Validators/CreateDocumentCommandValidator.cs`                   | Sample validator           |
| `tests/Lexichord.Tests.Unit/Host/Behaviors/ValidationBehaviorTests.cs`              | Behavior unit tests        |
| `tests/Lexichord.Tests.Unit/Host/Validators/CreateDocumentCommandValidatorTests.cs` | Validator unit tests       |
| `tests/Lexichord.Tests.Integration/MediatR/ValidationPipelineIntegrationTests.cs`   | Pipeline integration tests |

## Files Modified

| File                                                                   | Description                             |
| :--------------------------------------------------------------------- | :-------------------------------------- |
| `Directory.Build.props`                                                | Add FluentValidationVersion property    |
| `src/Lexichord.Abstractions/Lexichord.Abstractions.csproj`             | Add FluentValidation package            |
| `src/Lexichord.Host/Lexichord.Host.csproj`                             | Add FluentValidation.DI package         |
| `src/Lexichord.Host/Infrastructure/MediatRServiceExtensions.cs`        | Register behavior + validator discovery |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj`               | Add FluentValidation for testing        |
| `tests/Lexichord.Tests.Integration/Lexichord.Tests.Integration.csproj` | Add MediatR + FV packages               |

---

## Usage

### Creating a Validator

```csharp
public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Project name is required.")
                .WithErrorCode("PROJECT_NAME_REQUIRED")
            .MaximumLength(100)
                .WithMessage("Project name must not exceed 100 characters.")
                .WithErrorCode("PROJECT_NAME_TOO_LONG");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                    .WithMessage("Description must not exceed 500 characters.")
                    .WithErrorCode("DESCRIPTION_TOO_LONG");
        });
    }
}
```

### Handling Validation Errors

```csharp
try
{
    await mediator.Send(new CreateProjectCommand { Name = "" });
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage} [{error.ErrorCode}]");
    }
}
```

### Validation Pipeline Behavior Order

```
Request → LoggingBehavior → ValidationBehavior → Handler → Response
          ↑ logs start       ↑ validates            ↑ executes
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run ValidationBehavior unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~ValidationBehaviorTests"

# 3. Run CreateDocumentCommandValidator tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~CreateDocumentCommandValidatorTests"

# 4. Run validation pipeline integration tests
dotnet test tests/Lexichord.Tests.Integration --filter "FullyQualifiedName~ValidationPipeline"

# 5. Verify files exist
ls src/Lexichord.Abstractions/Validation/
ls src/Lexichord.Host/Infrastructure/Behaviors/
ls src/Lexichord.Host/Validators/
```

---

## Test Summary

| Test Class                          | Tests  | Status |
| :---------------------------------- | :----- | :----- |
| ValidationBehaviorTests             | 6      | ✅     |
| CreateDocumentCommandValidatorTests | 21     | ✅     |
| ValidationPipelineIntegrationTests  | 4      | ✅     |
| **Total**                           | **31** | **✅** |

---

## Dependencies

- **From v0.0.7a:** `ICommand<T>`, `IQuery<T>` interfaces
- **From v0.0.7c:** `LoggingBehavior` (runs before validation)
- **NuGet:** FluentValidation 11.9.0, FluentValidation.DependencyInjectionExtensions 11.9.0

## Enables

- **v0.0.8+:** All future commands/queries automatically validated
- **API Layer:** Structured validation error responses for REST/GraphQL
- **UI Layer:** Rich validation feedback with property-level error binding
