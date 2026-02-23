// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowStepFactory.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Factory for creating ValidationWorkflowStep instances with
//   injected dependencies. The factory holds references to shared services
//   (IUnifiedValidationService, ILoggerFactory) and creates fully-configured
//   step instances on demand.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: ValidationWorkflowStep (v0.7.7e), IUnifiedValidationService (v0.7.5f),
//               ILoggerFactory (v0.0.3b)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Factory for creating <see cref="IValidationWorkflowStep"/> instances
/// with injected dependencies.
/// </summary>
/// <remarks>
/// <para>
/// The factory pattern decouples step creation from the caller, allowing
/// the workflow engine and designer to create steps without directly
/// managing the validation service and logger dependencies.
/// </para>
/// <para>
/// Registered as a singleton in the DI container. Each call to
/// <see cref="CreateStep"/> produces a new step instance with its own
/// logger obtained from the <see cref="ILoggerFactory"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. Multiple callers can
/// invoke <see cref="CreateStep"/> concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var factory = serviceProvider.GetRequiredService&lt;ValidationWorkflowStepFactory&gt;();
///
/// var step = factory.CreateStep(
///     "schema-check",
///     "Schema Compliance",
///     ValidationStepType.Schema,
///     new ValidationWorkflowStepOptions
///     {
///         Description = "Validates document schema compliance",
///         FailureAction = ValidationFailureAction.Halt,
///         TimeoutMs = 10000
///     });
///
/// var configErrors = step.ValidateConfiguration();
/// if (configErrors.Count == 0)
/// {
///     var result = await step.ExecuteValidationAsync(context);
/// }
/// </code>
/// </example>
internal sealed class ValidationWorkflowStepFactory
{
    private readonly IUnifiedValidationService _validationService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ValidationWorkflowStepFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationWorkflowStepFactory"/> class.
    /// </summary>
    /// <param name="validationService">
    /// The unified validation service used by all created steps.
    /// </param>
    /// <param name="loggerFactory">
    /// The logger factory for creating step-specific loggers.
    /// </param>
    public ValidationWorkflowStepFactory(
        IUnifiedValidationService validationService,
        ILoggerFactory loggerFactory)
    {
        _validationService = validationService ??
            throw new ArgumentNullException(nameof(validationService));
        _loggerFactory = loggerFactory ??
            throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<ValidationWorkflowStepFactory>();

        _logger.LogDebug("ValidationWorkflowStepFactory initialized");
    }

    /// <summary>
    /// Creates a new <see cref="IValidationWorkflowStep"/> with the specified configuration.
    /// </summary>
    /// <param name="id">
    /// Unique identifier for the step. Convention: lowercase kebab-case
    /// (e.g., "schema-check", "consistency-verify").
    /// </param>
    /// <param name="name">
    /// Human-readable display name for the step.
    /// </param>
    /// <param name="stepType">
    /// The type of validation this step performs. Used for routing
    /// and license gating.
    /// </param>
    /// <param name="options">
    /// Configuration options for the step. All properties have sensible defaults.
    /// </param>
    /// <returns>
    /// A fully-configured <see cref="IValidationWorkflowStep"/> instance
    /// ready for execution or configuration validation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="id"/> or <paramref name="name"/> is null.
    /// </exception>
    public IValidationWorkflowStep CreateStep(
        string id,
        string name,
        ValidationStepType stepType,
        ValidationWorkflowStepOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);

        var stepOptions = options ?? new ValidationWorkflowStepOptions();

        _logger.LogInformation(
            "Creating validation workflow step '{StepId}' (name: '{StepName}', " +
            "type: {StepType}, order: {Order}, failureAction: {FailureAction}, " +
            "severity: {Severity}, timeout: {TimeoutMs}ms, async: {IsAsync})",
            id, name, stepType, stepOptions.Order,
            stepOptions.FailureAction, stepOptions.FailureSeverity,
            stepOptions.TimeoutMs, stepOptions.ExecuteAsync);

        var step = new ValidationWorkflowStep(
            id: id,
            name: name,
            stepType: stepType,
            validationService: _validationService,
            logger: _loggerFactory.CreateLogger<ValidationWorkflowStep>(),
            description: stepOptions.Description,
            order: stepOptions.Order,
            timeoutMs: stepOptions.TimeoutMs,
            failureAction: stepOptions.FailureAction,
            failureSeverity: stepOptions.FailureSeverity,
            isAsync: stepOptions.ExecuteAsync,
            options: stepOptions.StepOptions);

        _logger.LogDebug(
            "Successfully created validation workflow step '{StepId}'", id);

        return step;
    }
}
