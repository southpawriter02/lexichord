// -----------------------------------------------------------------------
// <copyright file="TemplateChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Provides data for the <see cref="IPromptTemplateRepository.TemplateChanged"/> event.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when templates are added, updated, or removed from the repository.
/// Changes can occur due to:
/// </para>
/// <list type="bullet">
///   <item><description>Hot-reload detecting file system changes in watched directories</description></item>
///   <item><description>Manual repository reload via <see cref="IPromptTemplateRepository.ReloadTemplatesAsync"/></description></item>
///   <item><description>Priority resolution when templates from different sources share an ID</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong> Event handlers may be invoked from background threads.
/// Ensure proper synchronization when updating UI elements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// repository.TemplateChanged += (sender, args) =>
/// {
///     logger.LogInformation(
///         "Template {ChangeType}: {TemplateId} from {Source} at {Timestamp}",
///         args.ChangeType,
///         args.TemplateId,
///         args.Source,
///         args.Timestamp);
///
///     if (args.ChangeType != TemplateChangeType.Removed &amp;&amp; args.Template != null)
///     {
///         // Access the template content
///         Console.WriteLine($"New content available: {args.Template.Name}");
///     }
/// };
/// </code>
/// </example>
/// <seealso cref="IPromptTemplateRepository.TemplateChanged"/>
/// <seealso cref="TemplateChangeType"/>
/// <seealso cref="TemplateSource"/>
public class TemplateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the unique identifier of the template that changed.
    /// </summary>
    /// <value>
    /// The template ID in kebab-case format (e.g., "co-pilot-editor").
    /// </value>
    /// <remarks>
    /// This value is always set, even for <see cref="TemplateChangeType.Removed"/> events,
    /// allowing handlers to identify which template was affected.
    /// </remarks>
    public required string TemplateId { get; init; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    /// <value>
    /// One of the <see cref="TemplateChangeType"/> values indicating whether
    /// the template was added, updated, or removed.
    /// </value>
    public required TemplateChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the source from which the template change originated.
    /// </summary>
    /// <value>
    /// The <see cref="TemplateSource"/> indicating whether the change came from
    /// embedded resources, the global templates directory, or the user templates directory.
    /// </value>
    public required TemplateSource Source { get; init; }

    /// <summary>
    /// Gets the template instance after the change, if applicable.
    /// </summary>
    /// <value>
    /// The <see cref="IPromptTemplate"/> instance for <see cref="TemplateChangeType.Added"/>
    /// and <see cref="TemplateChangeType.Updated"/> events; <c>null</c> for
    /// <see cref="TemplateChangeType.Removed"/> events.
    /// </value>
    /// <remarks>
    /// For removal events, use the <see cref="TemplateId"/> property to identify
    /// which template was removed.
    /// </remarks>
    public IPromptTemplate? Template { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the change was detected.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the repository processed
    /// the change. Defaults to <see cref="DateTimeOffset.UtcNow"/> at construction time.
    /// </value>
    /// <remarks>
    /// This timestamp reflects when the repository processed the change, which may
    /// differ slightly from when the underlying file system change occurred due to
    /// debouncing and batching of file system events.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns a string representation of this event for debugging purposes.
    /// </summary>
    /// <returns>A string containing the change type, template ID, source, and timestamp.</returns>
    public override string ToString()
    {
        return $"TemplateChanged: {ChangeType} '{TemplateId}' from {Source} at {Timestamp:O}";
    }
}
