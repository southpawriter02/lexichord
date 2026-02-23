// -----------------------------------------------------------------------
// <copyright file="PresetWorkflowRepository.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Repository for preset workflows loaded from embedded YAML resources at construction.
/// </summary>
/// <remarks>
/// <para>
/// This repository scans the assembly for embedded resources matching the pattern
/// <c>*.Workflows.*.yaml</c> and deserializes each into a <see cref="WorkflowDefinition"/>.
/// The resulting list is cached immutably for the lifetime of the application.
/// </para>
/// <para>
/// Deserialization uses YamlDotNet with <see cref="UnderscoredNamingConvention"/> to match
/// the snake_case YAML format defined in the v0.7.7a specification (LCS-DES-077a §5.3).
/// Internal DTOs (<see cref="WorkflowYamlDto"/>, <see cref="WorkflowStepYamlDto"/>, etc.)
/// provide the mapping layer between YAML and domain records.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7c §6.1
/// </para>
/// </remarks>
public class PresetWorkflowRepository : IPresetWorkflowRepository
{
    // ── Fields ───────────────────────────────────────────────────────────

    /// <summary>
    /// Immutable cache of all loaded preset workflow definitions.
    /// Populated once at construction and never modified.
    /// </summary>
    private readonly IReadOnlyList<WorkflowDefinition> _presets;

    /// <summary>
    /// Logger for diagnostic output during resource loading and query operations.
    /// </summary>
    private readonly ILogger<PresetWorkflowRepository> _logger;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="PresetWorkflowRepository"/>
    /// by loading all embedded YAML workflow resources from the assembly.
    /// </summary>
    /// <param name="logger">
    /// Logger instance for recording resource loading diagnostics.
    /// </param>
    /// <remarks>
    /// The constructor eagerly loads all preset YAML resources. Any individual
    /// resource that fails to parse is logged as an error and skipped, so the
    /// remaining presets remain available.
    /// </remarks>
    public PresetWorkflowRepository(ILogger<PresetWorkflowRepository> logger)
    {
        _logger = logger;

        // LOGIC: Eagerly load all embedded YAML workflow resources at construction.
        // This ensures presets are available immediately for GetAll/GetById queries
        // without any lazy-loading or thread-safety concerns.
        _presets = LoadPresets();

        _logger.LogDebug(
            "Loaded {Count} preset workflows from embedded resources",
            _presets.Count);
    }

    // ── IPresetWorkflowRepository Implementation ────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// Returns the cached immutable list. No filtering or transformation is applied.
    /// The list is ordered alphabetically by workflow name.
    /// </remarks>
    public IReadOnlyList<WorkflowDefinition> GetAll() => _presets;

    /// <inheritdoc />
    /// <remarks>
    /// Performs a linear scan of the cached preset list. With only 5 presets
    /// in v0.7.7c, this is more efficient than maintaining a dictionary.
    /// </remarks>
    public WorkflowDefinition? GetById(string workflowId) =>
        _presets.FirstOrDefault(p => p.WorkflowId == workflowId);

    /// <inheritdoc />
    /// <remarks>
    /// Filters the cached preset list by <see cref="WorkflowMetadata.Category"/>.
    /// Returns a new list instance on each call (safe for caller modification).
    /// </remarks>
    public IReadOnlyList<WorkflowDefinition> GetByCategory(WorkflowCategory category) =>
        _presets.Where(p => p.Metadata.Category == category).ToList();

    /// <inheritdoc />
    /// <remarks>
    /// A workflow ID is considered a preset if it starts with the "preset-" prefix
    /// AND exists in the loaded preset list. This prevents false positives for
    /// user-created workflows that coincidentally start with "preset-".
    /// </remarks>
    public bool IsPreset(string workflowId) =>
        workflowId.StartsWith("preset-") && _presets.Any(p => p.WorkflowId == workflowId);

    /// <inheritdoc />
    /// <remarks>
    /// Maps each cached <see cref="WorkflowDefinition"/> to a lightweight
    /// <see cref="PresetWorkflowSummary"/> containing only display-ready information.
    /// Agent IDs are de-duplicated to show the distinct agents used in the pipeline.
    /// </remarks>
    public IReadOnlyList<PresetWorkflowSummary> GetSummaries() =>
        _presets.Select(p => new PresetWorkflowSummary(
            p.WorkflowId,
            p.Name,
            p.Description,
            p.IconName ?? "workflow",
            p.Metadata.Category,
            p.Steps.Count,
            p.Steps.Select(s => s.AgentId).Distinct().ToList(),
            p.Metadata.RequiredTier,
            p.Metadata.Tags.ToList()
        )).ToList();

    // ── Private Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Loads all preset workflow definitions from embedded YAML resources.
    /// </summary>
    /// <returns>
    /// An immutable list of parsed <see cref="WorkflowDefinition"/> records,
    /// ordered alphabetically by name. Resources that fail to parse are skipped.
    /// </returns>
    /// <remarks>
    /// Scans the executing assembly for manifest resource names containing
    /// "Workflows" and ending with ".yaml". Each matching resource is read
    /// as UTF-8 text and deserialized via YamlDotNet.
    /// </remarks>
    private IReadOnlyList<WorkflowDefinition> LoadPresets()
    {
        var presets = new List<WorkflowDefinition>();

        // LOGIC: Get the assembly containing this type. Embedded resources are
        // compiled into this assembly by the EmbeddedResource glob in the .csproj.
        var assembly = typeof(PresetWorkflowRepository).Assembly;

        // LOGIC: Filter manifest resource names to only include Workflow YAML files.
        // Resource names follow the pattern: Lexichord.Modules.Agents.Resources.Workflows.<name>.yaml
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.Contains("Workflows") && n.EndsWith(".yaml"));

        // LOGIC: Create a single YamlDotNet deserializer for all resources.
        // UnderscoredNamingConvention maps snake_case YAML keys to PascalCase C# properties.
        // IgnoreUnmatchedProperties ensures forward compatibility if new YAML fields are added.
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var resourceName in resourceNames)
        {
            try
            {
                // LOGIC: Open the embedded resource stream. Null check guards against
                // missing resources (should not happen, but defensive coding).
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    _logger.LogWarning(
                        "Failed to load preset workflow from {Resource}: stream was null",
                        resourceName);
                    continue;
                }

                // LOGIC: Read the entire YAML content as a string for deserialization.
                using var reader = new StreamReader(stream);
                var yaml = reader.ReadToEnd();

                // LOGIC: Deserialize the YAML into a DTO, then map to the domain record.
                var workflow = DeserializeWorkflow(yaml, deserializer);
                presets.Add(workflow);

                _logger.LogDebug(
                    "Loaded preset workflow: {WorkflowId} ({WorkflowName}, {StepCount} steps)",
                    workflow.WorkflowId,
                    workflow.Name,
                    workflow.Steps.Count);
            }
            catch (Exception ex)
            {
                // LOGIC: Log the error but continue loading remaining presets.
                // A single malformed YAML file should not prevent other presets from loading.
                _logger.LogError(
                    ex,
                    "Preset workflow deserialization failed: {Error} (resource: {Resource})",
                    ex.Message,
                    resourceName);
            }
        }

        // LOGIC: Sort alphabetically by name for consistent display ordering.
        return presets.OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    /// Deserializes a YAML string into a <see cref="WorkflowDefinition"/> via the internal DTO layer.
    /// </summary>
    /// <param name="yaml">The YAML content to deserialize.</param>
    /// <param name="deserializer">The pre-configured YamlDotNet deserializer.</param>
    /// <returns>The parsed <see cref="WorkflowDefinition"/>.</returns>
    /// <exception cref="WorkflowImportException">
    /// Thrown when the YAML cannot be deserialized or the resulting DTO is null.
    /// </exception>
    private static WorkflowDefinition DeserializeWorkflow(string yaml, IDeserializer deserializer)
    {
        // LOGIC: Deserialize into the internal DTO which has writable properties
        // compatible with YamlDotNet's property-setting approach.
        var dto = deserializer.Deserialize<WorkflowYamlDto>(yaml)
            ?? throw new WorkflowImportException("Failed to parse preset YAML: result was null.");

        // LOGIC: Convert the mutable DTO into an immutable domain record
        // via the ToWorkflowDefinition() mapping method.
        return dto.ToWorkflowDefinition();
    }

    // ── Internal DTOs for YAML Deserialization ───────────────────────────
    //
    // These DTOs mirror the YAML schema defined in LCS-DES-v0.7.7c §5.
    // They use mutable properties because YamlDotNet requires settable
    // properties for deserialization. The ToXxx() methods convert to
    // immutable domain records.

    /// <summary>
    /// Data transfer object for YAML deserialization of preset workflow definitions.
    /// </summary>
    /// <remarks>
    /// Maps the top-level YAML structure: workflow_id, name, description, icon,
    /// category, steps, and metadata. Converted to <see cref="WorkflowDefinition"/>
    /// via <see cref="ToWorkflowDefinition"/>.
    /// </remarks>
    internal class WorkflowYamlDto
    {
        /// <summary>Unique ID for the preset (e.g., "preset-technical-review").</summary>
        public string WorkflowId { get; set; } = "";

        /// <summary>Human-readable workflow name.</summary>
        public string Name { get; set; } = "";

        /// <summary>Brief description of the workflow's purpose.</summary>
        public string Description { get; set; } = "";

        /// <summary>Optional Lucide icon name for UI display.</summary>
        public string? Icon { get; set; }

        /// <summary>Organizational category (Technical, Marketing, Academic, General).</summary>
        public string Category { get; set; } = "General";

        /// <summary>Ordered list of workflow step DTOs.</summary>
        public List<WorkflowStepYamlDto> Steps { get; set; } = new();

        /// <summary>Metadata DTO containing authorship, versioning, and licensing info.</summary>
        public WorkflowMetadataYamlDto Metadata { get; set; } = new();

        /// <summary>
        /// Converts this DTO to an immutable <see cref="WorkflowDefinition"/> record.
        /// </summary>
        /// <returns>The domain record with all steps and metadata mapped.</returns>
        public WorkflowDefinition ToWorkflowDefinition() =>
            new(
                WorkflowId,
                Name,
                Description,
                Icon,
                Steps.Select(s => s.ToStepDefinition()).ToList(),
                Metadata.ToMetadata(Category)
            );
    }

    /// <summary>
    /// Data transfer object for YAML deserialization of workflow steps.
    /// </summary>
    /// <remarks>
    /// Maps each step entry in the YAML steps array, including optional
    /// persona, prompt override, condition, and data mappings.
    /// </remarks>
    internal class WorkflowStepYamlDto
    {
        /// <summary>Unique ID for this step within the workflow.</summary>
        public string StepId { get; set; } = "";

        /// <summary>ID of the agent to invoke (must exist in the agent registry).</summary>
        public string AgentId { get; set; } = "";

        /// <summary>Optional persona variant for the agent.</summary>
        public string? PersonaId { get; set; }

        /// <summary>Optional custom prompt to use instead of the agent's default.</summary>
        public string? PromptOverride { get; set; }

        /// <summary>Execution order (1-based). Steps execute in ascending order.</summary>
        public int Order { get; set; }

        /// <summary>Optional condition controlling whether this step executes.</summary>
        public WorkflowConditionYamlDto? Condition { get; set; }

        /// <summary>Optional mappings from previous step outputs to this step's inputs.</summary>
        public Dictionary<string, string>? InputMappings { get; set; }

        /// <summary>Optional mappings from this step's outputs to named variables.</summary>
        public Dictionary<string, string>? OutputMappings { get; set; }

        /// <summary>
        /// Converts this DTO to an immutable <see cref="WorkflowStepDefinition"/> record.
        /// </summary>
        /// <returns>The domain record with condition and mappings mapped.</returns>
        public WorkflowStepDefinition ToStepDefinition() =>
            new(
                StepId,
                AgentId,
                PersonaId,
                PromptOverride,
                Order,
                Condition?.ToCondition(),
                InputMappings,
                OutputMappings
            );
    }

    /// <summary>
    /// Data transfer object for YAML deserialization of step conditions.
    /// </summary>
    /// <remarks>
    /// Maps the condition block within a step, supporting condition types
    /// Expression, PreviousSuccess, PreviousFailed, and Always.
    /// </remarks>
    internal class WorkflowConditionYamlDto
    {
        /// <summary>Condition type string (e.g., "Expression", "PreviousSuccess").</summary>
        public string Type { get; set; } = "Always";

        /// <summary>Expression string for Expression-type conditions.</summary>
        public string? Expression { get; set; }

        /// <summary>
        /// Converts this DTO to an immutable <see cref="WorkflowStepCondition"/> record.
        /// </summary>
        /// <returns>The domain record with parsed <see cref="ConditionType"/>.</returns>
        public WorkflowStepCondition ToCondition() =>
            new(
                Expression ?? "",
                Enum.TryParse<ConditionType>(Type, true, out var t) ? t : ConditionType.Always
            );
    }

    /// <summary>
    /// Data transfer object for YAML deserialization of workflow metadata.
    /// </summary>
    /// <remarks>
    /// Maps the metadata block containing authorship, versioning, tags,
    /// built-in flag, and license tier. The <see cref="ToMetadata"/> method
    /// also accepts the top-level category string for enum parsing.
    /// </remarks>
    internal class WorkflowMetadataYamlDto
    {
        /// <summary>Author identifier (always "Lexichord" for built-in presets).</summary>
        public string Author { get; set; } = "Lexichord";

        /// <summary>Semantic version string for the preset definition.</summary>
        public string? Version { get; set; }

        /// <summary>UTC timestamp when the preset was created.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Tags for categorization and search.</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Whether this is a system-provided (built-in) workflow.</summary>
        public bool IsBuiltIn { get; set; } = true;

        /// <summary>Minimum license tier required to execute this preset.</summary>
        public string RequiredTier { get; set; } = "WriterPro";

        /// <summary>
        /// Converts this DTO to an immutable <see cref="WorkflowMetadata"/> record.
        /// </summary>
        /// <param name="categoryStr">
        /// The top-level category string from the workflow YAML (e.g., "Technical").
        /// </param>
        /// <returns>The domain record with parsed enums and timestamps.</returns>
        public WorkflowMetadata ToMetadata(string categoryStr) =>
            new(
                Author,
                CreatedAt ?? DateTime.UtcNow,
                CreatedAt ?? DateTime.UtcNow,
                Version,
                Tags,
                Enum.TryParse<WorkflowCategory>(categoryStr, true, out var c)
                    ? c
                    : WorkflowCategory.General,
                IsBuiltIn,
                Enum.TryParse<LicenseTier>(RequiredTier, true, out var t)
                    ? t
                    : LicenseTier.WriterPro
            );
    }
}
