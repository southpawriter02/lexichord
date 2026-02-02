// =============================================================================
// File: IAxiomLoader.cs
// Project: Lexichord.Modules.Knowledge
// Description: Interface for loading axioms from YAML files and built-in resources.
// =============================================================================
// LOGIC: Defines the contract for loading, validating, and watching axiom files.
//   - LoadAllAsync: Orchestrates loading from built-in and workspace sources
//   - LoadBuiltInAsync: Loads axioms embedded in application resources
//   - LoadWorkspaceAsync: Loads axioms from .lexichord/knowledge/axioms/ (Teams+)
//   - ValidateFileAsync: Validates YAML syntax and schema without persisting
//   - File watching: Automatic reload on workspace axiom file changes
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// Dependencies: Axiom (v0.4.6e), IAxiomRepository (v0.4.6f), ISchemaRegistry (v0.4.5f)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Loads axioms from YAML files and built-in resources.
/// </summary>
/// <remarks>
/// <para>
/// The Axiom Loader is responsible for parsing YAML axiom definitions, validating
/// them against the schema registry, and persisting valid axioms via the repository.
/// </para>
/// <para>
/// <b>Sources:</b>
/// <list type="bullet">
///   <item><description>Built-in: Embedded in application resources (WriterPro+ tier).</description></item>
///   <item><description>Workspace: <c>.lexichord/knowledge/axioms/</c> directory (Teams+ tier).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item><description>WriterPro+: Can load built-in axioms.</description></item>
///   <item><description>Teams+: Can load custom workspace axioms.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6g as part of the Axiom Store (CKVS Phase 1).
/// </para>
/// </remarks>
public interface IAxiomLoader
{
    /// <summary>
    /// Loads all axioms from workspace and built-in sources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result with statistics and errors.</returns>
    /// <remarks>
    /// LOGIC: Orchestrates loading:
    /// 1. Load built-in axioms (always, for WriterPro+)
    /// 2. Load workspace axioms (if Teams+ and workspace is open)
    /// 3. Persist all valid axioms to repository
    /// </remarks>
    Task<AxiomLoadResult> LoadAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads axioms from a specific YAML file.
    /// </summary>
    /// <param name="filePath">Path to YAML file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result for this file.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <remarks>
    /// LOGIC: Deletes existing axioms from this source file before re-saving.
    /// This ensures clean reload semantics when files are modified.
    /// </remarks>
    Task<AxiomLoadResult> LoadFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Loads built-in axioms from application resources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result for built-in axioms.</returns>
    /// <remarks>
    /// LOGIC: Reads embedded resources matching pattern 
    /// <c>Lexichord.Modules.Knowledge.Resources.BuiltInAxioms.*.yaml</c>.
    /// Sets <see cref="Axiom.SourceFile"/> to <c>builtin:{resourceName}</c>.
    /// </remarks>
    Task<AxiomLoadResult> LoadBuiltInAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads axioms from workspace directory.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Load result for workspace axioms.</returns>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below Teams.
    /// </exception>
    /// <remarks>
    /// LOGIC: Scans <c>{workspacePath}/.lexichord/knowledge/axioms/</c> recursively
    /// for *.yaml and *.yml files. Each file is parsed and validated independently.
    /// </remarks>
    Task<AxiomLoadResult> LoadWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Validates a YAML file without loading.
    /// </summary>
    /// <param name="filePath">Path to YAML file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    /// <remarks>
    /// LOGIC: Performs full validation (YAML syntax, axiom structure, schema types)
    /// without persisting to repository. Useful for editor validation and dry-runs.
    /// </remarks>
    Task<AxiomValidationReport> ValidateFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Starts watching workspace for axiom file changes.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <remarks>
    /// LOGIC: Creates a <see cref="FileSystemWatcher"/> for the axiom directory.
    /// Changes trigger automatic reload with a 500ms debounce to handle rapid saves.
    /// No-op if license tier is below Teams.
    /// </remarks>
    void StartWatching(string workspacePath);

    /// <summary>
    /// Stops watching for file changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Disposes the <see cref="FileSystemWatcher"/> if active.
    /// Idempotent - calling when not watching is a no-op.
    /// </remarks>
    void StopWatching();

    /// <summary>
    /// Event raised when axioms are reloaded due to file changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after successful file change processing.
    /// Consumers (e.g., validation services) can subscribe to refresh their caches.
    /// </remarks>
    event EventHandler<AxiomLoadResult>? AxiomsReloaded;
}
