// -----------------------------------------------------------------------
// <copyright file="WorkflowYamlSerializer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Serializes and deserializes <see cref="WorkflowDefinition"/> instances to/from YAML format.
/// </summary>
/// <remarks>
/// <para>
/// Uses YamlDotNet with underscore (snake_case) naming conventions to produce
/// human-readable YAML that can be version-controlled and shared between users.
/// </para>
/// <para>
/// The YAML format follows the schema defined in the v0.7.7a design specification
/// (LCS-DES-077a §5.3), using snake_case keys for all properties.
/// </para>
/// </remarks>
public static class WorkflowYamlSerializer
{
    // ── Lazy Serializer/Deserializer Instances ──────────────────────────

    /// <summary>
    /// Thread-safe lazy-initialized YAML serializer with snake_case naming.
    /// </summary>
    private static readonly Lazy<ISerializer> SerializerInstance = new(() =>
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build());

    /// <summary>
    /// Thread-safe lazy-initialized YAML deserializer with snake_case naming.
    /// </summary>
    private static readonly Lazy<IDeserializer> DeserializerInstance = new(() =>
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build());

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// Serializes a <see cref="WorkflowDefinition"/> to a YAML string.
    /// </summary>
    /// <param name="workflow">The workflow definition to serialize.</param>
    /// <returns>A YAML-formatted string representing the workflow.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workflow"/> is <c>null</c>.
    /// </exception>
    public static string Serialize(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        // Map to a serialization-friendly DTO with snake_case-compatible structure
        var dto = new WorkflowYamlDto
        {
            WorkflowId = workflow.WorkflowId,
            Name = workflow.Name,
            Description = workflow.Description,
            Icon = workflow.IconName,
            Category = workflow.Metadata.Category.ToString(),
            Steps = workflow.Steps.Select(s => new StepYamlDto
            {
                StepId = s.StepId,
                AgentId = s.AgentId,
                PersonaId = s.PersonaId,
                Order = s.Order,
                PromptOverride = s.PromptOverride,
                Condition = s.Condition != null
                    ? new ConditionYamlDto
                    {
                        Type = s.Condition.Type.ToString(),
                        Expression = s.Condition.Type == ConditionType.Expression
                            ? s.Condition.Expression
                            : null
                    }
                    : null
            }).ToList(),
            Metadata = new MetadataYamlDto
            {
                Author = workflow.Metadata.Author,
                CreatedAt = workflow.Metadata.CreatedAt.ToString("o"),
                ModifiedAt = workflow.Metadata.ModifiedAt.ToString("o"),
                Version = workflow.Metadata.Version,
                Tags = workflow.Metadata.Tags.ToList(),
                IsBuiltIn = workflow.Metadata.IsBuiltIn,
                RequiredTier = workflow.Metadata.RequiredTier.ToString()
            }
        };

        return SerializerInstance.Value.Serialize(dto);
    }

    /// <summary>
    /// Deserializes a YAML string into a <see cref="WorkflowDefinition"/>.
    /// </summary>
    /// <param name="yaml">The YAML string to deserialize.</param>
    /// <returns>The parsed <see cref="WorkflowDefinition"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="yaml"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="WorkflowImportException">
    /// Thrown when the YAML cannot be parsed into a valid workflow definition.
    /// </exception>
    public static WorkflowDefinition Deserialize(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new ArgumentNullException(nameof(yaml), "YAML string cannot be null or empty.");
        }

        try
        {
            var dto = DeserializerInstance.Value.Deserialize<WorkflowYamlDto>(yaml);
            if (dto == null)
            {
                throw new WorkflowImportException("Failed to parse YAML: result was null.");
            }

            // Parse the category enum, defaulting to General if unrecognized
            var category = Enum.TryParse<WorkflowCategory>(dto.Category, ignoreCase: true, out var cat)
                ? cat
                : WorkflowCategory.General;

            // Parse the license tier enum, defaulting to Teams if unrecognized
            var requiredTier = Enum.TryParse<LicenseTier>(
                dto.Metadata?.RequiredTier, ignoreCase: true, out var tier)
                ? tier
                : LicenseTier.Teams;

            // Map steps from DTO to domain records
            var steps = (dto.Steps ?? new List<StepYamlDto>())
                .Select(s =>
                {
                    WorkflowStepCondition? condition = null;
                    if (s.Condition != null)
                    {
                        var condType = Enum.TryParse<ConditionType>(
                            s.Condition.Type, ignoreCase: true, out var ct)
                            ? ct
                            : ConditionType.Always;
                        condition = new WorkflowStepCondition(
                            s.Condition.Expression ?? string.Empty,
                            condType);
                    }

                    return new WorkflowStepDefinition(
                        StepId: s.StepId ?? $"step-{Guid.NewGuid():N}"[..12],
                        AgentId: s.AgentId ?? string.Empty,
                        PersonaId: s.PersonaId,
                        PromptOverride: s.PromptOverride,
                        Order: s.Order,
                        Condition: condition,
                        InputMappings: null,
                        OutputMappings: null);
                })
                .ToList()
                .AsReadOnly();

            // Parse metadata timestamps
            var createdAt = DateTime.TryParse(dto.Metadata?.CreatedAt, out var ca)
                ? ca.ToUniversalTime()
                : DateTime.UtcNow;
            var modifiedAt = DateTime.TryParse(dto.Metadata?.ModifiedAt, out var ma)
                ? ma.ToUniversalTime()
                : DateTime.UtcNow;

            var metadata = new WorkflowMetadata(
                Author: dto.Metadata?.Author ?? "unknown",
                CreatedAt: createdAt,
                ModifiedAt: modifiedAt,
                Version: dto.Metadata?.Version,
                Tags: (dto.Metadata?.Tags ?? new List<string>()).AsReadOnly(),
                Category: category,
                IsBuiltIn: dto.Metadata?.IsBuiltIn ?? false,
                RequiredTier: requiredTier);

            return new WorkflowDefinition(
                WorkflowId: dto.WorkflowId ?? $"wf-{Guid.NewGuid():N}"[..12],
                Name: dto.Name ?? string.Empty,
                Description: dto.Description ?? string.Empty,
                IconName: dto.Icon,
                Steps: steps,
                Metadata: metadata);
        }
        catch (WorkflowImportException)
        {
            throw; // Re-throw our own exception type
        }
        catch (Exception ex)
        {
            throw new WorkflowImportException(
                $"Failed to import workflow from YAML: {ex.Message}", ex);
        }
    }

    // ── Internal DTOs for YAML Serialization ────────────────────────────

    /// <summary>
    /// Data transfer object for YAML serialization of workflow definitions.
    /// </summary>
    private sealed class WorkflowYamlDto
    {
        public string? WorkflowId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public List<StepYamlDto>? Steps { get; set; }
        public MetadataYamlDto? Metadata { get; set; }
    }

    /// <summary>
    /// Data transfer object for YAML serialization of workflow steps.
    /// </summary>
    private sealed class StepYamlDto
    {
        public string? StepId { get; set; }
        public string? AgentId { get; set; }
        public string? PersonaId { get; set; }
        public int Order { get; set; }
        public string? PromptOverride { get; set; }
        public ConditionYamlDto? Condition { get; set; }
    }

    /// <summary>
    /// Data transfer object for YAML serialization of step conditions.
    /// </summary>
    private sealed class ConditionYamlDto
    {
        public string? Type { get; set; }
        public string? Expression { get; set; }
    }

    /// <summary>
    /// Data transfer object for YAML serialization of workflow metadata.
    /// </summary>
    private sealed class MetadataYamlDto
    {
        public string? Author { get; set; }
        public string? CreatedAt { get; set; }
        public string? ModifiedAt { get; set; }
        public string? Version { get; set; }
        public List<string>? Tags { get; set; }
        public bool IsBuiltIn { get; set; }
        public string? RequiredTier { get; set; }
    }
}
