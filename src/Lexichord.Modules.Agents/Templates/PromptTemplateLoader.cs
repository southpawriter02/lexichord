// -----------------------------------------------------------------------
// <copyright file="PromptTemplateLoader.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Loads and validates prompt templates from YAML files and embedded resources.
/// </summary>
/// <remarks>
/// <para>
/// This class handles the loading pipeline for prompt templates:
/// </para>
/// <list type="number">
///   <item><description>Read YAML content from file or embedded resource</description></item>
///   <item><description>Deserialize YAML to <see cref="PromptTemplateYaml"/> DTO</description></item>
///   <item><description>Validate required fields and structure</description></item>
///   <item><description>Convert to domain <see cref="PromptTemplate"/> record</description></item>
///   <item><description>Wrap in <see cref="TemplateEntry"/> with metadata</description></item>
/// </list>
/// <para>
/// <strong>Error Handling:</strong>
/// The loader is designed to be fault-tolerant. Invalid templates are logged and skipped
/// rather than causing the entire load operation to fail. This allows the repository
/// to continue functioning with the valid templates.
/// </para>
/// </remarks>
/// <seealso cref="PromptTemplateRepository"/>
/// <seealso cref="PromptTemplateYaml"/>
internal sealed class PromptTemplateLoader
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// The resource name prefix for embedded prompt templates.
    /// </summary>
    private const string EmbeddedResourcePrefix = "Lexichord.Modules.Agents.Resources.Prompts.";

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTemplateLoader"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public PromptTemplateLoader(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Configure YamlDotNet with underscore naming convention
        // to map snake_case YAML keys (template_id) to PascalCase properties (TemplateId).
        // IgnoreUnmatchedProperties enables forward compatibility with future schema additions.
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _logger.LogDebug("PromptTemplateLoader initialized with UnderscoredNamingConvention");
    }

    /// <summary>
    /// Loads a template from a YAML file.
    /// </summary>
    /// <param name="filePath">The path to the YAML template file.</param>
    /// <param name="source">The source classification for the template.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="TemplateEntry"/> if the file was loaded and validated successfully;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method is fault-tolerant. If the file cannot be read or contains invalid YAML,
    /// the error is logged and <c>null</c> is returned instead of throwing an exception.
    /// </remarks>
    public async Task<TemplateEntry?> LoadFromFileAsync(
        string filePath,
        TemplateSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _logger.LogDebug("Loading template from file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Template file not found: {FilePath}", filePath);
            return null;
        }

        try
        {
            var yamlContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            return LoadFromYaml(yamlContent, source, filePath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read template file: {FilePath}", filePath);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied reading template file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Loads all templates from a directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory containing template files.</param>
    /// <param name="source">The source classification for loaded templates.</param>
    /// <param name="extensions">The file extensions to scan (e.g., [".yaml", ".yml"]).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A read-only list of successfully loaded template entries.
    /// Invalid templates are skipped and logged.
    /// </returns>
    public async Task<IReadOnlyList<TemplateEntry>> LoadFromDirectoryAsync(
        string directoryPath,
        TemplateSource source,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var entries = new List<TemplateEntry>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug("Template directory does not exist: {DirectoryPath}", directoryPath);
            return entries.AsReadOnly();
        }

        _logger.LogDebug(
            "Loading templates from directory: {DirectoryPath} (extensions: {Extensions})",
            directoryPath,
            string.Join(", ", extensions));

        try
        {
            var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Any(ext =>
                    f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entry = await LoadFromFileAsync(filePath, source, cancellationToken);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            _logger.LogDebug(
                "Loaded {Count} templates from {DirectoryPath}",
                entries.Count,
                directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to template directory: {DirectoryPath}", directoryPath);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Template directory not found: {DirectoryPath}", directoryPath);
        }

        return entries.AsReadOnly();
    }

    /// <summary>
    /// Loads all built-in templates from embedded resources.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A read-only list of successfully loaded template entries from embedded resources.
    /// </returns>
    /// <remarks>
    /// Embedded templates are shipped with the module assembly and are always available.
    /// They provide baseline functionality that can be overridden by user templates.
    /// </remarks>
    public async Task<IReadOnlyList<TemplateEntry>> LoadEmbeddedTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = new List<TemplateEntry>();
        var assembly = typeof(PromptTemplateLoader).Assembly;

        _logger.LogDebug("Loading embedded templates from assembly: {Assembly}", assembly.FullName);

        // LOGIC: Find all embedded resources that match our naming convention.
        // Resource names follow the pattern: Lexichord.Modules.Agents.Resources.Prompts.{filename}.yaml
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(EmbeddedResourcePrefix, StringComparison.Ordinal) &&
                          (name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                           name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        _logger.LogDebug("Found {Count} embedded template resources", resourceNames.Count);

        foreach (var resourceName in resourceNames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogWarning("Failed to open embedded resource stream: {ResourceName}", resourceName);
                    continue;
                }

                using var reader = new StreamReader(stream);
                var yamlContent = await reader.ReadToEndAsync(cancellationToken);

                var entry = LoadFromYaml(yamlContent, TemplateSource.Embedded, resourceName);
                if (entry != null)
                {
                    entries.Add(entry);
                    _logger.LogDebug(
                        "Loaded embedded template: {TemplateId} from {ResourceName}",
                        entry.Template.TemplateId,
                        resourceName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load embedded template: {ResourceName}", resourceName);
            }
        }

        _logger.LogInformation("Loaded {Count} embedded templates", entries.Count);
        return entries.AsReadOnly();
    }

    /// <summary>
    /// Loads a template from YAML content.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <param name="source">The source classification for the template.</param>
    /// <param name="filePath">The file path or resource name for logging purposes.</param>
    /// <returns>
    /// A <see cref="TemplateEntry"/> if the YAML was parsed and validated successfully;
    /// otherwise, <c>null</c>.
    /// </returns>
    public TemplateEntry? LoadFromYaml(
        string yamlContent,
        TemplateSource source,
        string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            _logger.LogWarning("Empty YAML content provided for template loading");
            return null;
        }

        try
        {
            // Step 1: Deserialize YAML to DTO
            var dto = _deserializer.Deserialize<PromptTemplateYaml>(yamlContent);

            // Step 2: Validate required fields
            var validationErrors = ValidateDto(dto);
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);
                _logger.LogWarning(
                    "Template validation failed for {FilePath}: {Errors}",
                    filePath ?? "(inline)",
                    errorMessage);
                return null;
            }

            // Step 3: Convert to domain object
            var template = ConvertToDomain(dto!);

            // Step 4: Create entry with metadata
            var entry = new TemplateEntry(
                Template: template,
                Category: dto!.Category,
                Tags: (dto.Tags ?? []).AsReadOnly(),
                Source: source,
                LoadedAt: DateTimeOffset.UtcNow,
                FilePath: source == TemplateSource.Embedded ? null : filePath);

            _logger.LogDebug(
                "Successfully loaded template: {TemplateId} (Category={Category}, Tags={TagCount})",
                template.TemplateId,
                entry.Category ?? "(none)",
                entry.Tags.Count);

            return entry;
        }
        catch (YamlException ex)
        {
            _logger.LogWarning(
                ex,
                "YAML parsing failed at line {Line}, column {Column} for {FilePath}: {Message}",
                ex.Start.Line,
                ex.Start.Column,
                filePath ?? "(inline)",
                ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error loading template from {FilePath}", filePath ?? "(inline)");
            return null;
        }
    }

    /// <summary>
    /// Validates the deserialized DTO for required fields.
    /// </summary>
    /// <param name="dto">The DTO to validate.</param>
    /// <returns>A list of validation error messages; empty if valid.</returns>
    private static IReadOnlyList<string> ValidateDto(PromptTemplateYaml? dto)
    {
        var errors = new List<string>();

        if (dto == null)
        {
            errors.Add("Template content is null or empty");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(dto.TemplateId))
        {
            errors.Add("template_id is required");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("name is required");
        }

        // LOGIC: At least one of system_prompt or user_prompt should be present
        // to make the template useful, but we don't enforce this strictly.
        // The renderer will handle empty prompts gracefully.

        return errors;
    }

    /// <summary>
    /// Converts a validated DTO to the domain <see cref="PromptTemplate"/> record.
    /// </summary>
    /// <param name="dto">The validated DTO to convert.</param>
    /// <returns>A new <see cref="PromptTemplate"/> instance.</returns>
    private static PromptTemplate ConvertToDomain(PromptTemplateYaml dto)
    {
        // LOGIC: Use the record constructor directly instead of the factory method
        // because we need to include the description field which the factory method
        // does not expose (it defaults to empty string).
        return new PromptTemplate(
            TemplateId: dto.TemplateId!,
            Name: dto.Name!,
            Description: dto.Description ?? string.Empty,
            SystemPromptTemplate: dto.SystemPrompt ?? string.Empty,
            UserPromptTemplate: dto.UserPrompt ?? string.Empty,
            RequiredVariables: (dto.RequiredVariables ?? []).AsReadOnly(),
            OptionalVariables: (dto.OptionalVariables ?? []).AsReadOnly());
    }
}
