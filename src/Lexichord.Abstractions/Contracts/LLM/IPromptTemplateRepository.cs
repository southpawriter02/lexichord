// -----------------------------------------------------------------------
// <copyright file="IPromptTemplateRepository.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Repository for managing prompt templates from multiple sources with priority-based loading.
/// </summary>
/// <remarks>
/// <para>
/// The template repository provides centralized management of prompt templates used by AI agents.
/// Templates are loaded from multiple sources with the following priority (lowest to highest):
/// </para>
/// <list type="number">
///   <item><description><see cref="TemplateSource.Embedded"/> - Built-in templates shipped with the module</description></item>
///   <item><description><see cref="TemplateSource.Global"/> - Templates from the global templates directory</description></item>
///   <item><description><see cref="TemplateSource.User"/> - Templates from the user's personal directory</description></item>
/// </list>
/// <para>
/// When templates share the same <see cref="IPromptTemplate.TemplateId"/>, templates from
/// higher-priority sources override those from lower-priority sources.
/// </para>
/// <para>
/// <strong>License Gating:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Embedded templates are always available (all tiers)</description></item>
///   <item><description>Custom templates from Global/User directories require <see cref="LicenseTier.WriterPro"/> or higher</description></item>
///   <item><description>Hot-reload functionality requires <see cref="LicenseTier.Teams"/> or higher</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// Implementations must be thread-safe. All public methods may be called concurrently
/// from multiple threads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic template retrieval
/// var template = repository.GetTemplate("co-pilot-editor");
/// if (template != null)
/// {
///     var messages = renderer.RenderMessages(template, variables);
/// }
///
/// // Search for templates by category
/// var editingTemplates = repository.GetTemplatesByCategory("editing");
/// foreach (var t in editingTemplates)
/// {
///     Console.WriteLine($"- {t.Name}: {t.Description}");
/// }
///
/// // Listen for template changes (hot-reload)
/// repository.TemplateChanged += (sender, args) =>
/// {
///     Console.WriteLine($"Template {args.ChangeType}: {args.TemplateId}");
/// };
///
/// // Force reload all templates
/// await repository.ReloadTemplatesAsync();
/// </code>
/// </example>
/// <seealso cref="IPromptTemplate"/>
/// <seealso cref="IPromptRenderer"/>
/// <seealso cref="TemplateSource"/>
/// <seealso cref="TemplateInfo"/>
public interface IPromptTemplateRepository
{
    /// <summary>
    /// Gets all available templates from all loaded sources.
    /// </summary>
    /// <returns>
    /// A read-only list of all templates currently in the repository.
    /// When templates share the same ID, only the highest-priority version is included.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned list represents a snapshot of templates at the time of the call.
    /// The list is immutable and safe to enumerate even if the repository is modified
    /// concurrently.
    /// </para>
    /// <para>
    /// Templates are returned in an undefined order. Use LINQ to sort or filter
    /// the results as needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var templates = repository.GetAllTemplates();
    /// foreach (var template in templates.OrderBy(t => t.Name))
    /// {
    ///     Console.WriteLine($"{template.TemplateId}: {template.Name}");
    /// }
    /// </code>
    /// </example>
    IReadOnlyList<IPromptTemplate> GetAllTemplates();

    /// <summary>
    /// Gets a template by its unique identifier.
    /// </summary>
    /// <param name="templateId">
    /// The unique identifier of the template to retrieve (case-insensitive).
    /// </param>
    /// <returns>
    /// The template with the specified ID, or <c>null</c> if no matching template is found.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="templateId"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// Template ID matching is case-insensitive. For example, "co-pilot-editor",
    /// "Co-Pilot-Editor", and "CO-PILOT-EDITOR" all match the same template.
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = repository.GetTemplate("document-reviewer");
    /// if (template != null)
    /// {
    ///     var messages = renderer.RenderMessages(template, variables);
    ///     // Use messages with LLM...
    /// }
    /// </code>
    /// </example>
    IPromptTemplate? GetTemplate(string templateId);

    /// <summary>
    /// Gets all templates in a specific category.
    /// </summary>
    /// <param name="category">
    /// The category to filter by (case-insensitive). Common categories include
    /// "editing", "review", "analysis", "linting", and "translation".
    /// </param>
    /// <returns>
    /// A read-only list of templates matching the specified category.
    /// Returns an empty list if no templates match.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="category"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// Category matching is case-insensitive. Templates without a category assigned
    /// will not be returned by this method.
    /// </remarks>
    /// <example>
    /// <code>
    /// var reviewTemplates = repository.GetTemplatesByCategory("review");
    /// Console.WriteLine($"Found {reviewTemplates.Count} review templates");
    /// </code>
    /// </example>
    IReadOnlyList<IPromptTemplate> GetTemplatesByCategory(string category);

    /// <summary>
    /// Searches templates by name, description, template ID, or tags.
    /// </summary>
    /// <param name="query">
    /// The search query string (case-insensitive). Matches against template ID,
    /// name, description, and tags.
    /// </param>
    /// <returns>
    /// A read-only list of templates matching the search query.
    /// Returns an empty list if no templates match.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search is performed using simple substring matching against multiple fields:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Template ID (e.g., "co-pilot-editor")</description></item>
    ///   <item><description>Template name (e.g., "Co-pilot Editor")</description></item>
    ///   <item><description>Template description</description></item>
    ///   <item><description>Template tags</description></item>
    /// </list>
    /// <para>
    /// Results are returned with more relevant matches (e.g., name starts with query)
    /// appearing before less relevant matches.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Search for templates related to "writing"
    /// var results = repository.SearchTemplates("writing");
    /// foreach (var template in results)
    /// {
    ///     Console.WriteLine($"- {template.Name}");
    /// }
    /// </code>
    /// </example>
    IReadOnlyList<IPromptTemplate> SearchTemplates(string query);

    /// <summary>
    /// Reloads all templates from all sources.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>A task representing the asynchronous reload operation.</returns>
    /// <remarks>
    /// <para>
    /// This method clears the template cache and reloads all templates from:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Embedded resources (always loaded)</description></item>
    ///   <item><description>Global templates directory (if licensed)</description></item>
    ///   <item><description>User templates directory (if licensed)</description></item>
    /// </list>
    /// <para>
    /// The <see cref="TemplateChanged"/> event is raised for any templates that
    /// were added, updated, or removed during the reload.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> This method is thread-safe. Concurrent calls
    /// are serialized to prevent race conditions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Force reload all templates
    /// await repository.ReloadTemplatesAsync();
    /// Console.WriteLine($"Loaded {repository.GetAllTemplates().Count} templates");
    /// </code>
    /// </example>
    Task ReloadTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about a specific template including its source and load information.
    /// </summary>
    /// <param name="templateId">
    /// The unique identifier of the template (case-insensitive).
    /// </param>
    /// <returns>
    /// A <see cref="TemplateInfo"/> containing metadata about the template,
    /// or <c>null</c> if no template with the specified ID exists.
    /// </returns>
    /// <remarks>
    /// Use this method to determine:
    /// <list type="bullet">
    ///   <item><description>Where a template was loaded from (<see cref="TemplateInfo.Source"/>)</description></item>
    ///   <item><description>Whether it's a built-in or custom template (<see cref="TemplateInfo.IsBuiltIn"/>)</description></item>
    ///   <item><description>The file path for custom templates (<see cref="TemplateInfo.FilePath"/>)</description></item>
    ///   <item><description>When the template was loaded (<see cref="TemplateInfo.LoadedAt"/>)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = repository.GetTemplateInfo("co-pilot-editor");
    /// if (info != null)
    /// {
    ///     if (info.IsBuiltIn)
    ///     {
    ///         Console.WriteLine("Using built-in template");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"Using custom template from: {info.FilePath}");
    ///     }
    /// }
    /// </code>
    /// </example>
    TemplateInfo? GetTemplateInfo(string templateId);

    /// <summary>
    /// Occurs when templates are added, updated, or removed from the repository.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Hot-reload detects file system changes in watched directories</description></item>
    ///   <item><description><see cref="ReloadTemplatesAsync"/> completes with changes</description></item>
    ///   <item><description>A template file is added, modified, or deleted</description></item>
    /// </list>
    /// <para>
    /// <strong>Hot-Reload:</strong> Hot-reload functionality requires <see cref="LicenseTier.Teams"/>
    /// or higher. Without this license tier, templates are only loaded at startup and
    /// when <see cref="ReloadTemplatesAsync"/> is called explicitly.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> Event handlers may be invoked from background threads.
    /// Ensure proper synchronization when updating UI elements or shared state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// repository.TemplateChanged += (sender, args) =>
    /// {
    ///     switch (args.ChangeType)
    ///     {
    ///         case TemplateChangeType.Added:
    ///             logger.LogInformation("Template added: {TemplateId}", args.TemplateId);
    ///             break;
    ///         case TemplateChangeType.Updated:
    ///             logger.LogInformation("Template updated: {TemplateId}", args.TemplateId);
    ///             break;
    ///         case TemplateChangeType.Removed:
    ///             logger.LogWarning("Template removed: {TemplateId}", args.TemplateId);
    ///             break;
    ///     }
    /// };
    /// </code>
    /// </example>
    event EventHandler<TemplateChangedEventArgs>? TemplateChanged;
}
