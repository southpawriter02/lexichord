// -----------------------------------------------------------------------
// <copyright file="SyncWorkflowStepFactory.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Factory for creating SyncWorkflowStep instances with injected
//   dependencies. Holds references to ISyncService and ILoggerFactory,
//   creating fully-configured step instances on demand. Also contains
//   the SyncWorkflowStepOptions record for step configuration.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: SyncWorkflowStep (v0.7.7g), ISyncService (v0.7.6e),
//               ILoggerFactory (v0.0.3b)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Factory for creating <see cref="ISyncWorkflowStep"/> instances
/// with injected dependencies.
/// </summary>
/// <remarks>
/// <para>
/// The factory pattern decouples step creation from the caller, allowing
/// the workflow engine and designer to create sync steps without directly
/// managing the <see cref="ISyncService"/> and logger dependencies.
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
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var factory = serviceProvider.GetRequiredService&lt;SyncWorkflowStepFactory&gt;();
///
/// var step = factory.CreateStep(
///     "post-validation-sync",
///     "Post-Validation Sync",
///     new SyncWorkflowStepOptions
///     {
///         Direction = SyncDirection.DocumentToGraph,
///         ConflictStrategy = ConflictStrategy.PreferNewer,
///         SkipIfValidationFailed = true,
///         TimeoutMs = 30000
///     });
///
/// var configErrors = step.ValidateConfiguration();
/// if (configErrors.Count == 0)
/// {
///     var result = await step.ExecuteSyncAsync(context);
/// }
/// </code>
/// </example>
internal sealed class SyncWorkflowStepFactory
{
    private readonly ISyncService _syncService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SyncWorkflowStepFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncWorkflowStepFactory"/> class.
    /// </summary>
    /// <param name="syncService">
    /// The sync service used by all created steps for document-graph synchronization.
    /// </param>
    /// <param name="loggerFactory">
    /// The logger factory for creating step-specific loggers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="syncService"/> or <paramref name="loggerFactory"/> is null.
    /// </exception>
    public SyncWorkflowStepFactory(
        ISyncService syncService,
        ILoggerFactory loggerFactory)
    {
        _syncService = syncService ??
            throw new ArgumentNullException(nameof(syncService));
        _loggerFactory = loggerFactory ??
            throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<SyncWorkflowStepFactory>();

        _logger.LogDebug("SyncWorkflowStepFactory initialized");
    }

    /// <summary>
    /// Creates a new <see cref="ISyncWorkflowStep"/> with the specified configuration.
    /// </summary>
    /// <param name="id">
    /// Unique identifier for the step. Convention: lowercase kebab-case
    /// (e.g., "post-validation-sync", "bidirectional-sync").
    /// </param>
    /// <param name="name">
    /// Human-readable display name for the step.
    /// </param>
    /// <param name="options">
    /// Configuration options for the step. All properties have sensible defaults.
    /// </param>
    /// <returns>
    /// A fully-configured <see cref="ISyncWorkflowStep"/> instance
    /// ready for execution or configuration validation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="id"/> or <paramref name="name"/> is null.
    /// </exception>
    public ISyncWorkflowStep CreateStep(
        string id,
        string name,
        SyncWorkflowStepOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);

        var stepOptions = options ?? new SyncWorkflowStepOptions();

        _logger.LogInformation(
            "Creating sync workflow step '{StepId}' (name: '{StepName}', " +
            "direction: {Direction}, order: {Order}, " +
            "conflictStrategy: {ConflictStrategy}, " +
            "skipIfValidationFailed: {SkipIfValidationFailed}, " +
            "timeout: {TimeoutMs}ms)",
            id, name, stepOptions.Direction, stepOptions.Order,
            stepOptions.ConflictStrategy, stepOptions.SkipIfValidationFailed,
            stepOptions.TimeoutMs);

        var step = new SyncWorkflowStep(
            id: id,
            name: name,
            direction: stepOptions.Direction,
            syncService: _syncService,
            logger: _loggerFactory.CreateLogger<SyncWorkflowStep>(),
            description: stepOptions.Description,
            order: stepOptions.Order,
            timeoutMs: stepOptions.TimeoutMs,
            conflictStrategy: stepOptions.ConflictStrategy,
            skipIfValidationFailed: stepOptions.SkipIfValidationFailed);

        _logger.LogDebug(
            "Successfully created sync workflow step '{StepId}'", id);

        return step;
    }
}

/// <summary>
/// Configuration options for creating sync workflow steps via
/// <see cref="SyncWorkflowStepFactory.CreateStep"/>.
/// </summary>
/// <remarks>
/// <para>
/// All properties have sensible defaults. The most commonly customized
/// properties are <see cref="Direction"/> and <see cref="ConflictStrategy"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record SyncWorkflowStepOptions
{
    /// <summary>
    /// Gets the optional description of the step's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the execution order within the workflow.
    /// </summary>
    /// <remarks>Default: 0. Lower values execute first.</remarks>
    public int Order { get; init; } = 0;

    /// <summary>
    /// Gets the execution timeout in milliseconds.
    /// </summary>
    /// <remarks>Default: 60000ms (1 minute). Null for no timeout enforcement.</remarks>
    public int? TimeoutMs { get; init; } = 60000;

    /// <summary>
    /// Gets the synchronization direction.
    /// </summary>
    /// <remarks>Default: <see cref="SyncDirection.Bidirectional"/>.</remarks>
    public SyncDirection Direction { get; init; } = SyncDirection.Bidirectional;

    /// <summary>
    /// Gets the conflict resolution strategy.
    /// </summary>
    /// <remarks>Default: <see cref="ConflictStrategy.PreferNewer"/>.</remarks>
    public ConflictStrategy ConflictStrategy { get; init; } = ConflictStrategy.PreferNewer;

    /// <summary>
    /// Gets whether to skip sync when prior validation steps have failed.
    /// </summary>
    /// <remarks>Default: <c>true</c>. Prevents syncing invalid documents.</remarks>
    public bool SkipIfValidationFailed { get; init; } = true;
}
