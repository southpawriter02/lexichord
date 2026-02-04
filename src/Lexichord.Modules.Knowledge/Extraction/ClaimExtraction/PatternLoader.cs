// =============================================================================
// File: PatternLoader.cs
// Project: Lexichord.Modules.Knowledge
// Description: Loads extraction patterns from YAML configuration.
// =============================================================================
// LOGIC: Reads pattern definitions from YAML files and converts them to
//   ExtractionPattern records for use by the PatternClaimExtractor.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Knowledge.Extraction.ClaimExtraction;

/// <summary>
/// Loads extraction patterns from YAML configuration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Reads pattern definitions from embedded resources or
/// external files and converts them to <see cref="ExtractionPattern"/> records.
/// </para>
/// <para>
/// <b>Sources:</b>
/// <list type="bullet">
/// <item><description>Built-in patterns (embedded resource).</description></item>
/// <item><description>Project-specific patterns (file system).</description></item>
/// </list>
/// </para>
/// </remarks>
public class PatternLoader
{
    private readonly ILogger<PatternLoader> _logger;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public PatternLoader(ILogger<PatternLoader> logger)
    {
        _logger = logger;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads built-in patterns.
    /// </summary>
    /// <returns>The built-in extraction patterns.</returns>
    public IReadOnlyList<ExtractionPattern> LoadBuiltInPatterns()
    {
        // Return default patterns when YAML loading is not available
        return GetDefaultPatterns();
    }

    /// <summary>
    /// Loads patterns from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML file.</param>
    /// <returns>The loaded patterns.</returns>
    public IReadOnlyList<ExtractionPattern> LoadFromFile(string filePath)
    {
        try
        {
            var yaml = File.ReadAllText(filePath);
            return ParseYaml(yaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load patterns from {FilePath}", filePath);
            return Array.Empty<ExtractionPattern>();
        }
    }

    /// <summary>
    /// Parses patterns from YAML content.
    /// </summary>
    private IReadOnlyList<ExtractionPattern> ParseYaml(string yaml)
    {
        try
        {
            var config = _deserializer.Deserialize<PatternConfig>(yaml);
            var patterns = config?.Patterns?.Select(ConvertPattern).ToList();
            if (patterns == null || patterns.Count == 0)
            {
                return Array.Empty<ExtractionPattern>();
            }
            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse pattern YAML");
            return Array.Empty<ExtractionPattern>();
        }
    }

    /// <summary>
    /// Converts a YAML pattern to an ExtractionPattern.
    /// </summary>
    private static ExtractionPattern ConvertPattern(YamlPattern p) => new()
    {
        Id = p.Id ?? Guid.NewGuid().ToString(),
        Name = p.Name ?? p.Id ?? "Unknown",
        Description = p.Description,
        Type = Enum.TryParse<PatternType>(p.Type, true, out var t) ? t : PatternType.Template,
        RegexPattern = p.RegexPattern,
        Template = p.Template,
        Predicate = p.Predicate ?? ClaimPredicate.HAS_PROPERTY,
        SubjectType = p.SubjectType ?? "*",
        ObjectType = p.ObjectType ?? "*",
        ObjectIsLiteral = p.ObjectIsLiteral,
        LiteralType = p.LiteralType,
        BaseConfidence = p.BaseConfidence ?? 0.8f,
        Priority = p.Priority ?? 0,
        IsEnabled = p.IsEnabled ?? true,
        Tags = p.Tags
    };

    /// <summary>
    /// Returns default built-in patterns when YAML is not loaded.
    /// </summary>
    private static IReadOnlyList<ExtractionPattern> GetDefaultPatterns() => new[]
    {
        new ExtractionPattern
        {
            Id = "endpoint-accepts-parameter",
            Name = "Endpoint Accepts Parameter",
            Type = PatternType.Template,
            Template = "{SUBJECT} accepts {OBJECT}",
            Predicate = ClaimPredicate.ACCEPTS,
            SubjectType = "Endpoint",
            ObjectType = "Parameter",
            BaseConfidence = 0.85f,
            Priority = 10
        },
        new ExtractionPattern
        {
            Id = "endpoint-returns-response",
            Name = "Endpoint Returns Response",
            Type = PatternType.Template,
            Template = "{SUBJECT} returns {OBJECT}",
            Predicate = ClaimPredicate.RETURNS,
            SubjectType = "Endpoint",
            ObjectType = "Response",
            BaseConfidence = 0.85f,
            Priority = 10
        },
        new ExtractionPattern
        {
            Id = "parameter-defaults-to",
            Name = "Parameter Has Default",
            Type = PatternType.Template,
            Template = "{SUBJECT} defaults to {OBJECT}",
            Predicate = ClaimPredicate.HAS_DEFAULT,
            SubjectType = "Parameter",
            ObjectType = "literal",
            ObjectIsLiteral = true,
            BaseConfidence = 0.9f,
            Priority = 10
        },
        new ExtractionPattern
        {
            Id = "entity-requires",
            Name = "Entity Requires",
            Type = PatternType.Template,
            Template = "{SUBJECT} requires {OBJECT}",
            Predicate = ClaimPredicate.REQUIRES,
            SubjectType = "*",
            ObjectType = "*",
            BaseConfidence = 0.85f,
            Priority = 8
        },
        new ExtractionPattern
        {
            Id = "entity-contains",
            Name = "Entity Contains",
            Type = PatternType.Template,
            Template = "{SUBJECT} contains {OBJECT}",
            Predicate = ClaimPredicate.CONTAINS,
            SubjectType = "*",
            ObjectType = "*",
            BaseConfidence = 0.8f,
            Priority = 5
        },
        new ExtractionPattern
        {
            Id = "deprecated-endpoint",
            Name = "Deprecated Endpoint",
            Type = PatternType.Regex,
            RegexPattern = @"(?<subject>[A-Z]+\s*/[\w/]+|this\s+endpoint)\s+(?:is|has\s+been)\s+(?<object>deprecated)",
            Predicate = ClaimPredicate.IS_DEPRECATED,
            SubjectType = "Endpoint",
            ObjectType = "literal",
            ObjectIsLiteral = true,
            LiteralType = "bool",
            BaseConfidence = 0.95f,
            Priority = 20
        }
    };

    #region YAML Model Classes

    private class PatternConfig
    {
        public List<YamlPattern>? Patterns { get; set; }
    }

    private class YamlPattern
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? Template { get; set; }
        public string? RegexPattern { get; set; }
        public string? Predicate { get; set; }
        public string? SubjectType { get; set; }
        public string? ObjectType { get; set; }
        public bool ObjectIsLiteral { get; set; }
        public string? LiteralType { get; set; }
        public float? BaseConfidence { get; set; }
        public int? Priority { get; set; }
        public bool? IsEnabled { get; set; }
        public List<string>? Tags { get; set; }
    }

    #endregion
}
