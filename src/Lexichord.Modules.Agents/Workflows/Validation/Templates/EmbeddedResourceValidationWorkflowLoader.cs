// -----------------------------------------------------------------------
// <copyright file="EmbeddedResourceValidationWorkflowLoader.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Loads pre-built validation workflow definitions from embedded YAML resources.
/// </summary>
/// <remarks>
/// <para>
/// Embedded resources follow the naming convention:
/// <c>Lexichord.Modules.Agents.Resources.Workflows.Validation.{workflowId}.yaml</c>
/// </para>
/// <para>
/// Deserialization uses YamlDotNet with <see cref="UnderscoredNamingConvention"/>
/// to map snake_case YAML keys to PascalCase C# properties. Internal DTOs provide
/// the mapping layer between YAML and the immutable domain records.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §4
/// </para>
/// </remarks>
internal sealed class EmbeddedResourceValidationWorkflowLoader : IValidationWorkflowLoader
{
    // ── Fields ───────────────────────────────────────────────────────────

    /// <summary>Logger for diagnostic output during resource loading.</summary>
    private readonly ILogger<EmbeddedResourceValidationWorkflowLoader> _logger;

    /// <summary>Pre-configured YamlDotNet deserializer (stateless, thread-safe).</summary>
    private readonly IDeserializer _deserializer;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="EmbeddedResourceValidationWorkflowLoader"/>.
    /// </summary>
    /// <param name="logger">Logger for recording resource loading diagnostics.</param>
    public EmbeddedResourceValidationWorkflowLoader(
        ILogger<EmbeddedResourceValidationWorkflowLoader> logger)
    {
        _logger = logger;

        // LOGIC: Create a single deserializer for all resources.
        // UnderscoredNamingConvention maps snake_case YAML to PascalCase C# properties.
        // IgnoreUnmatchedProperties ensures forward compatibility.
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _logger.LogDebug("EmbeddedResourceValidationWorkflowLoader initialized");
    }

    // ── IValidationWorkflowLoader Implementation ────────────────────────

    /// <inheritdoc />
    public async Task<ValidationWorkflowDefinition?> LoadWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Loading validation workflow from embedded resource: {WorkflowId}", workflowId);

        try
        {
            // LOGIC: Construct the expected resource name from the workflow ID.
            // Resources are compiled into the assembly by the EmbeddedResource glob
            // in the .csproj for Resources/Workflows/Validation/*.yaml.
            var resourceName =
                $"Lexichord.Modules.Agents.Resources.Workflows.Validation.{workflowId}.yaml";
            var assembly = typeof(EmbeddedResourceValidationWorkflowLoader).Assembly;
            var resource = assembly.GetManifestResourceStream(resourceName);

            if (resource == null)
            {
                _logger.LogWarning(
                    "Validation workflow resource not found: {ResourceName}",
                    resourceName);
                return null;
            }

            // LOGIC: Read the YAML content from the embedded resource stream.
            using var reader = new StreamReader(resource);
            var yaml = await reader.ReadToEndAsync(ct);

            // LOGIC: Deserialize the YAML into a DTO, then map to the domain record.
            var definition = ParseYamlWorkflow(yaml, workflowId);

            _logger.LogInformation(
                "Loaded validation workflow: {WorkflowId} ({WorkflowName}, {StepCount} steps, trigger={Trigger})",
                definition.Id,
                definition.Name,
                definition.Steps.Count,
                definition.Trigger);

            return definition;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to load validation workflow: {WorkflowId} — {Error}",
                workflowId,
                ex.Message);
            throw;
        }
    }

    // ── Private Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Parses a YAML string into a <see cref="ValidationWorkflowDefinition"/>.
    /// </summary>
    /// <param name="yaml">The raw YAML content.</param>
    /// <param name="workflowId">The workflow ID for fallback/override.</param>
    /// <returns>The parsed workflow definition.</returns>
    private ValidationWorkflowDefinition ParseYamlWorkflow(string yaml, string workflowId)
    {
        // LOGIC: Deserialize into the internal DTO which has mutable properties
        // compatible with YamlDotNet's property-setting approach.
        var dto = _deserializer.Deserialize<ValidationWorkflowYamlDto>(yaml)
            ?? throw new InvalidOperationException(
                $"Failed to parse validation workflow YAML for '{workflowId}': result was null.");

        _logger.LogDebug(
            "Parsed YAML DTO for {WorkflowId}: name={Name}, steps={StepCount}",
            workflowId,
            dto.Name,
            dto.Steps?.Count ?? 0);

        // LOGIC: Convert the mutable DTO to an immutable domain record.
        return dto.ToDefinition(workflowId);
    }

    // ── Internal DTOs for YAML Deserialization ───────────────────────────
    //
    // These DTOs mirror the validation workflow YAML schema defined in
    // LCS-DES-077-KG-h §2. They use mutable properties because YamlDotNet
    // requires settable properties for deserialization.

    /// <summary>
    /// DTO for the top-level validation workflow YAML structure.
    /// </summary>
    internal class ValidationWorkflowYamlDto
    {
        /// <summary>Workflow identifier (e.g., "on-save-validation").</summary>
        public string Id { get; set; } = "";

        /// <summary>Human-readable workflow name.</summary>
        public string Name { get; set; } = "";

        /// <summary>Workflow description.</summary>
        public string? Description { get; set; }

        /// <summary>Semantic version string.</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Trigger type string (e.g., "OnSave", "PrePublish").</summary>
        public string Trigger { get; set; } = "Manual";

        /// <summary>Whether enabled by default.</summary>
        public bool EnabledByDefault { get; set; } = true;

        /// <summary>Overall timeout in minutes.</summary>
        public int TimeoutMinutes { get; set; } = 10;

        /// <summary>Expected duration in minutes.</summary>
        public int? ExpectedDurationMinutes { get; set; }

        /// <summary>Ordered list of step DTOs.</summary>
        public List<ValidationWorkflowStepYamlDto> Steps { get; set; } = new();

        /// <summary>License requirement DTO.</summary>
        public ValidationWorkflowLicenseYamlDto LicenseRequirement { get; set; } = new();

        /// <summary>Performance targets as key-value pairs.</summary>
        public Dictionary<string, int>? PerformanceTargets { get; set; }

        /// <summary>
        /// Converts this DTO to an immutable <see cref="ValidationWorkflowDefinition"/>.
        /// </summary>
        /// <param name="fallbackId">Fallback ID if the YAML doesn't specify one.</param>
        /// <returns>The domain record.</returns>
        public ValidationWorkflowDefinition ToDefinition(string fallbackId) =>
            new()
            {
                Id = string.IsNullOrWhiteSpace(Id) ? fallbackId : Id,
                Name = Name,
                Description = Description,
                Version = Version,
                Trigger = Enum.TryParse<ValidationWorkflowTrigger>(Trigger, true, out var t)
                    ? t
                    : ValidationWorkflowTrigger.Manual,
                IsPrebuilt = true,
                EnabledByDefault = EnabledByDefault,
                TimeoutMinutes = TimeoutMinutes,
                ExpectedDurationMinutes = ExpectedDurationMinutes,
                Steps = Steps
                    .Select(s => s.ToStepDef())
                    .OrderBy(s => s.Order)
                    .ToList(),
                LicenseRequirement = LicenseRequirement.ToRequirement(),
                PerformanceTargets = PerformanceTargets,
                CreatedAt = DateTime.UtcNow
            };
    }

    /// <summary>
    /// DTO for a validation workflow step in YAML.
    /// </summary>
    internal class ValidationWorkflowStepYamlDto
    {
        /// <summary>Unique step ID.</summary>
        public string Id { get; set; } = "";

        /// <summary>Display name.</summary>
        public string Name { get; set; } = "";

        /// <summary>Step type identifier (e.g., "Schema", "Gating", "Sync").</summary>
        public string Type { get; set; } = "";

        /// <summary>Execution order (1-based).</summary>
        public int Order { get; set; }

        /// <summary>Whether this step is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Per-step timeout in milliseconds.</summary>
        public int? TimeoutMs { get; set; }

        /// <summary>Step-specific configuration options.</summary>
        public Dictionary<string, object>? Options { get; set; }

        /// <summary>Converts to an immutable step definition record.</summary>
        public ValidationWorkflowStepDef ToStepDef() =>
            new()
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Order = Order,
                Enabled = Enabled,
                TimeoutMs = TimeoutMs,
                Options = Options
            };
    }

    /// <summary>
    /// DTO for the license requirement block in YAML.
    /// </summary>
    internal class ValidationWorkflowLicenseYamlDto
    {
        /// <summary>Whether Core (free) tier can execute.</summary>
        public bool Core { get; set; } = false;

        /// <summary>Whether WriterPro tier can execute.</summary>
        public bool WriterPro { get; set; } = false;

        /// <summary>Whether Teams tier can execute.</summary>
        public bool Teams { get; set; } = true;

        /// <summary>Whether Enterprise tier can execute.</summary>
        public bool Enterprise { get; set; } = true;

        /// <summary>Converts to an immutable license requirement record.</summary>
        public ValidationWorkflowLicenseRequirement ToRequirement() =>
            new()
            {
                Core = Core,
                WriterPro = WriterPro,
                Teams = Teams,
                Enterprise = Enterprise
            };
    }
}
