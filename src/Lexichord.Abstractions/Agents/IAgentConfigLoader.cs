// Copyright (c) 2025 Ryan Witting. Licensed under the MIT License. See LICENSE in root.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Loads and parses agent configurations from YAML files.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts the loading of agent configurations from multiple sources:
/// <list type="bullet">
/// <item><description>Built-in agents from embedded YAML resources.</description></item>
/// <item><description>Workspace agents from <c>.lexichord/agents/*.yaml</c> (requires WriterPro license).</description></item>
/// </list>
/// </para>
/// <para>
/// The loader supports hot-reload via file watching, publishing <see cref="AgentFileChanged"/>
/// events when workspace agent files are created, modified, or deleted.
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents<br/>
/// <strong>License Gating:</strong> Workspace agents require WriterPro tier or higher.
/// </para>
/// </remarks>
public interface IAgentConfigLoader
{
    /// <summary>
    /// Loads all built-in agent configurations from embedded resources.
    /// </summary>
    /// <param name="ct">Cancellation token to stop the operation.</param>
    /// <returns>
    /// A read-only list of validated <see cref="AgentConfiguration"/> instances.
    /// Invalid configurations are logged and excluded from the result.
    /// </returns>
    /// <remarks>
    /// Built-in agents are loaded from embedded YAML resources in the
    /// <c>Lexichord.Modules.Agents.Resources.BuiltIn</c> namespace. These agents
    /// are available to all license tiers as defined in their configurations.
    /// </remarks>
    Task<IReadOnlyList<AgentConfiguration>> LoadBuiltInAgentsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Loads all workspace agent configurations from the <c>.lexichord/agents/</c> directory.
    /// </summary>
    /// <param name="workspacePath">The root path of the workspace.</param>
    /// <param name="ct">Cancellation token to stop the operation.</param>
    /// <returns>
    /// A read-only list of validated <see cref="AgentConfiguration"/> instances.
    /// Returns an empty list if the user's license tier does not include custom agents.
    /// Invalid configurations are logged and excluded from the result.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Workspace agents are loaded from <c>{workspacePath}/.lexichord/agents/*.yaml</c>
    /// or <c>*.yml</c> files. The directory is created if it doesn't exist.
    /// </para>
    /// <para>
    /// <strong>License Enforcement:</strong> Requires <c>Feature.CustomAgents</c> (WriterPro tier).
    /// If the feature is not licensed, this method returns an empty list.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<AgentConfiguration>> LoadWorkspaceAgentsAsync(
        string workspacePath,
        CancellationToken ct = default);

    /// <summary>
    /// Loads a single agent configuration from a YAML file.
    /// </summary>
    /// <param name="filePath">The absolute path to the YAML file.</param>
    /// <param name="ct">Cancellation token to stop the operation.</param>
    /// <returns>
    /// A <see cref="AgentConfigValidationResult"/> indicating whether the file was
    /// successfully parsed and validated.
    /// </returns>
    /// <remarks>
    /// This method reads the file, parses the YAML content, validates the structure,
    /// and returns the result. File not found or I/O errors are returned as validation
    /// failures with appropriate error messages.
    /// </remarks>
    Task<AgentConfigValidationResult> LoadFromFileAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Parses and validates YAML content directly.
    /// </summary>
    /// <param name="yamlContent">The YAML string to parse.</param>
    /// <param name="sourceName">A name for this source (e.g., file path) for error reporting.</param>
    /// <returns>
    /// A <see cref="AgentConfigValidationResult"/> indicating whether the YAML was
    /// successfully parsed and validated.
    /// </returns>
    /// <remarks>
    /// This method is useful for testing or parsing YAML from non-file sources.
    /// The <paramref name="sourceName"/> is included in error messages for debugging.
    /// </remarks>
    AgentConfigValidationResult ParseYaml(string yamlContent, string sourceName);

    /// <summary>
    /// Starts watching the workspace agents directory for file changes.
    /// </summary>
    /// <param name="workspacePath">The root path of the workspace.</param>
    /// <remarks>
    /// <para>
    /// This method begins monitoring <c>{workspacePath}/.lexichord/agents/*.yaml</c>
    /// and <c>*.yml</c> files for changes. When a file is created, modified, or deleted,
    /// the <see cref="AgentFileChanged"/> event is raised after a debounce period (300ms).
    /// </para>
    /// <para>
    /// If the directory does not exist, it is created automatically.
    /// </para>
    /// <para>
    /// Calling this method multiple times replaces the previous watch with a new one.
    /// </para>
    /// </remarks>
    void StartWatching(string workspacePath);

    /// <summary>
    /// Stops watching the workspace agents directory.
    /// </summary>
    /// <remarks>
    /// This method stops file monitoring and releases resources. Pending debounced
    /// events are not raised after this method is called.
    /// </remarks>
    void StopWatching();

    /// <summary>
    /// Event raised when a workspace agent file is created, modified, or deleted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised after a debounce period (300ms) to avoid rapid successive
    /// events for the same file. The <see cref="AgentFileChangedEventArgs.ValidationResult"/>
    /// contains the parsed configuration if the file was valid, or errors if it was invalid.
    /// </para>
    /// <para>
    /// For deleted files, <see cref="AgentFileChangedEventArgs.ValidationResult"/> is <c>null</c>.
    /// </para>
    /// <para>
    /// Subscribers should handle this event asynchronously to avoid blocking the file watcher.
    /// </para>
    /// </remarks>
    event EventHandler<AgentFileChangedEventArgs>? AgentFileChanged;
}
