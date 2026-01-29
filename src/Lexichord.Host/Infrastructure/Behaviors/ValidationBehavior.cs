using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

using LexichordValidation = Lexichord.Abstractions.Validation;

namespace Lexichord.Host.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that automatically validates all MediatR requests.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// LOGIC: This behavior integrates FluentValidation into the MediatR pipeline:
///
/// 1. **Auto-Discovery**: Resolves all <see cref="IValidator{TRequest}"/> instances
///    registered for the request type.
///
/// 2. **Parallel Execution**: Runs all validators concurrently for performance.
///
/// 3. **Error Aggregation**: Collects all validation failures from all validators
///    into a single exception.
///
/// 4. **Fail-Fast**: Throws <see cref="ValidationException"/> before the handler
///    executes if any validation fails.
///
/// 5. **Pass-Through**: If no validators exist or all pass, the request proceeds
///    to the next behavior/handler.
///
/// Pipeline Position: AFTER LoggingBehavior, BEFORE handler
/// This ensures validation errors are logged properly and we don't waste time
/// executing handlers with invalid data.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    private static readonly string RequestTypeName = typeof(TRequest).Name;

    /// <summary>
    /// Initializes a new instance of the ValidationBehavior.
    /// </summary>
    /// <param name="validators">All validators registered for this request type.</param>
    /// <param name="logger">The logger instance.</param>
    /// <remarks>
    /// LOGIC: The validators are injected as an enumerable. If no validators are
    /// registered for TRequest, this will be an empty collection, not null.
    /// </remarks>
    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request by validating it before passing to the next handler.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler if validation passes.</returns>
    /// <exception cref="ValidationException">Thrown when one or more validators fail.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // LOGIC: Early exit if no validators are registered
        // This is a common case for queries and simple commands
        if (!_validators.Any())
        {
            _logger.LogDebug(
                "No validators registered for {RequestType}, skipping validation",
                RequestTypeName);
            return await next();
        }

        _logger.LogDebug(
            "Validating {RequestType} with {ValidatorCount} validator(s)",
            RequestTypeName,
            _validators.Count());

        // LOGIC: Create validation context for the request
        var context = new ValidationContext<TRequest>(request);

        // LOGIC: Execute all validators in parallel for performance
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // LOGIC: Collect all failures from all validators
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning(
                "Validation failed for {RequestType} with {ErrorCount} error(s)",
                RequestTypeName,
                failures.Count);

            // LOGIC: Throw our custom ValidationException which provides
            // structured error data for API responses
            throw new LexichordValidation.ValidationException(failures);
        }

        _logger.LogDebug(
            "Validation passed for {RequestType}",
            RequestTypeName);

        return await next();
    }
}
