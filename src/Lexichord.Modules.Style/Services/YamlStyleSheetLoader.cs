using System.Reflection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Yaml;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// YAML-based implementation of <see cref="IStyleSheetLoader"/>.
/// </summary>
/// <remarks>
/// LOGIC: Loads and validates YAML style sheets using YamlDotNet.
/// 
/// Processing Pipeline:
/// 1. Parse YAML into DTOs (YamlStyleSheet, YamlRule)
/// 2. Validate schema (required fields, valid enums, unique IDs)
/// 3. Convert DTOs to immutable domain objects (StyleSheet, StyleRule)
///
/// Error Handling:
/// - YAML syntax errors: YamlException with line/column info
/// - Schema violations: ValidationError with field context
/// - Both wrapped in InvalidOperationException for consumers
/// </remarks>
public sealed class YamlStyleSheetLoader : IStyleSheetLoader
{
    private readonly ILogger<YamlStyleSheetLoader> _logger;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Resource name for the embedded default style sheet.
    /// </summary>
    private const string EmbeddedResourceName = "Lexichord.Modules.Style.Resources.lexichord.yaml";

    /// <summary>
    /// Initializes a new instance of <see cref="YamlStyleSheetLoader"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public YamlStyleSheetLoader(ILogger<YamlStyleSheetLoader> logger)
    {
        _logger = logger;

        // LOGIC: Configure YamlDotNet with underscore naming convention
        // to map snake_case YAML keys to PascalCase properties
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _logger.LogDebug("YamlStyleSheetLoader initialized");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Reads file content and delegates to LoadFromStreamAsync.
    /// Wraps file I/O errors with helpful context.
    /// </remarks>
    public async Task<StyleSheet> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _logger.LogDebug("Loading style sheet from file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Style sheet file not found: {filePath}", filePath);
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await LoadFromStreamAsync(stream, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read style sheet file: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to read style sheet file: {filePath}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Core deserialization pipeline.
    /// 1. Read YAML from stream
    /// 2. Deserialize to DTO
    /// 3. Validate schema
    /// 4. Convert to domain object
    /// </remarks>
    public async Task<StyleSheet> LoadFromStreamAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _logger.LogDebug("Loading style sheet from stream");

        try
        {
            using var reader = new StreamReader(stream);
            var yamlContent = await reader.ReadToEndAsync(cancellationToken);

            return DeserializeAndConvert(yamlContent);
        }
        catch (YamlException ex)
        {
            _logger.LogError(ex, "YAML parsing failed at line {Line}, column {Column}",
                ex.Start.Line, ex.Start.Column);

            throw new InvalidOperationException(
                $"YAML syntax error at line {ex.Start.Line}, column {ex.Start.Column}: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Loads the embedded lexichord.yaml resource.
    /// This provides the "out of box" style rules.
    /// </remarks>
    public async Task<StyleSheet> LoadEmbeddedDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading embedded default style sheet");

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);

        if (stream is null)
        {
            // LOGIC: This should never happen in production - indicates build issue
            _logger.LogError("Embedded resource not found: {ResourceName}", EmbeddedResourceName);
            throw new InvalidOperationException(
                $"Embedded style sheet resource not found: {EmbeddedResourceName}. " +
                "This is a build configuration error.");
        }

        return await LoadFromStreamAsync(stream, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Validates YAML without fully loading.
    /// Used for syntax checking in editors before save.
    /// </remarks>
    public StyleSheetLoadResult ValidateYaml(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            return StyleSheetLoadResult.Failure("YAML content is empty");
        }

        try
        {
            // Step 1: Parse YAML syntax
            var dto = _deserializer.Deserialize<YamlStyleSheet>(yamlContent);

            // Step 2: Validate schema
            var errors = YamlSchemaValidator.Validate(dto);
            if (errors.Count > 0)
            {
                // Return first error with line context if available
                var firstError = errors[0];
                return StyleSheetLoadResult.Failure(firstError.Message, firstError.Line);
            }

            return StyleSheetLoadResult.Success;
        }
        catch (YamlException ex)
        {
            return StyleSheetLoadResult.Failure(
                $"YAML syntax error: {ex.Message}",
                ex.Start.Line);
        }
    }

    /// <summary>
    /// Deserializes YAML content and converts to domain object.
    /// </summary>
    private StyleSheet DeserializeAndConvert(string yamlContent)
    {
        // Step 1: Deserialize YAML to DTO
        var dto = _deserializer.Deserialize<YamlStyleSheet>(yamlContent);

        // Step 2: Validate schema
        var errors = YamlSchemaValidator.Validate(dto);
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("; ", errors.Select(e => e.Message));
            throw new InvalidOperationException($"Style sheet validation failed: {errorMessages}");
        }

        // Step 3: Convert to domain objects
        return ConvertToDomain(dto!);
    }

    /// <summary>
    /// Converts a YAML DTO to the immutable domain object.
    /// </summary>
    private StyleSheet ConvertToDomain(YamlStyleSheet dto)
    {
        var rules = dto.Rules!
            .Select(ConvertRuleToDomain)
            .ToList()
            .AsReadOnly();

        _logger.LogDebug("Converted style sheet '{Name}' with {RuleCount} rules",
            dto.Name, rules.Count);

        return new StyleSheet(
            Name: dto.Name!,
            Rules: rules,
            Description: dto.Description,
            Version: dto.Version,
            Author: dto.Author,
            Extends: dto.Extends);
    }

    /// <summary>
    /// Converts a YAML rule DTO to the immutable domain object.
    /// </summary>
    private static StyleRule ConvertRuleToDomain(YamlRule dto)
    {
        return new StyleRule(
            Id: dto.Id!,
            Name: dto.Name!,
            Description: dto.Description!,
            Category: ParseCategory(dto.Category),
            DefaultSeverity: ParseSeverity(dto.Severity),
            Pattern: dto.Pattern!,
            PatternType: ParsePatternType(dto.PatternType),
            Suggestion: dto.Suggestion,
            IsEnabled: dto.Enabled ?? true);
    }

    /// <summary>
    /// Parses category string to enum with default fallback.
    /// </summary>
    private static RuleCategory ParseCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return RuleCategory.Terminology; // Default

        return value.ToLowerInvariant() switch
        {
            "terminology" => RuleCategory.Terminology,
            "formatting" => RuleCategory.Formatting,
            "syntax" => RuleCategory.Syntax,
            _ => RuleCategory.Terminology
        };
    }

    /// <summary>
    /// Parses severity string to enum with default fallback.
    /// </summary>
    private static ViolationSeverity ParseSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ViolationSeverity.Warning; // Default

        return value.ToLowerInvariant() switch
        {
            "error" => ViolationSeverity.Error,
            "warning" => ViolationSeverity.Warning,
            "info" => ViolationSeverity.Info,
            "hint" => ViolationSeverity.Hint,
            _ => ViolationSeverity.Warning
        };
    }

    /// <summary>
    /// Parses pattern type string to enum with default fallback.
    /// </summary>
    private static PatternType ParsePatternType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PatternType.Regex; // Default

        return value.ToLowerInvariant() switch
        {
            "regex" => PatternType.Regex,
            "literal" => PatternType.Literal,
            "literal_ignore_case" => PatternType.LiteralIgnoreCase,
            "starts_with" => PatternType.StartsWith,
            "ends_with" => PatternType.EndsWith,
            "contains" => PatternType.Contains,
            _ => PatternType.Regex
        };
    }
}
