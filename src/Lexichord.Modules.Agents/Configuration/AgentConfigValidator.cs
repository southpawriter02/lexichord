// Copyright (c) 2025 Ryan Witting. Licensed under the MIT License. See LICENSE in root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Agents;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Configuration;

/// <summary>
/// Validates agent configurations loaded from YAML files against schema rules.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the contract for validating <see cref="AgentConfiguration"/>
/// instances after parsing from YAML. The validator checks both the built-in validation
/// rules from <see cref="AgentConfiguration.Validate"/> and additional YAML-specific rules.
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public interface IAgentConfigValidator
{
    /// <summary>
    /// Validates an agent configuration against all schema rules.
    /// </summary>
    /// <param name="config">The agent configuration to validate.</param>
    /// <param name="sourceName">The source file or resource name for error reporting.</param>
    /// <returns>
    /// An <see cref="AgentConfigValidationResult"/> indicating success or failure
    /// with detailed error and warning messages.
    /// </returns>
    AgentConfigValidationResult Validate(AgentConfiguration config, string sourceName);
}

/// <summary>
/// Default implementation of <see cref="IAgentConfigValidator"/> with comprehensive
/// validation rules for agent configurations loaded from YAML files.
/// </summary>
/// <remarks>
/// <para>
/// This validator extends the built-in <see cref="AgentConfiguration.Validate"/> checks
/// with additional validation for:
/// <list type="bullet">
/// <item><description>Description field (required, non-empty)</description></item>
/// <item><description>Icon field (required, non-empty)</description></item>
/// <item><description>Temperature range (0.0-2.0 for both default options and personas)</description></item>
/// <item><description>MaxTokens positive value with warning threshold (128,000)</description></item>
/// <item><description>Persona validation (kebab-case IDs, no duplicates)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
public sealed partial class AgentConfigValidator : IAgentConfigValidator
{
    private readonly ILogger<AgentConfigValidator> _logger;

    /// <summary>
    /// Compiled regular expression for validating kebab-case identifiers.
    /// </summary>
    /// <remarks>
    /// Pattern: <c>^[a-z0-9]+(-[a-z0-9]+)*$</c><br/>
    /// Matches: lowercase letters and digits, with optional hyphens between segments.<br/>
    /// Valid: "co-pilot", "editor", "research-assistant-v2"<br/>
    /// Invalid: "Co-Pilot", "editor_v1", "-agent", "agent-", "agent--v1"
    /// </remarks>
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCasePattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigValidator"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public AgentConfigValidator(ILogger<AgentConfigValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <strong>Validation Logic:</strong>
    /// </para>
    /// <para>
    /// 1. Invokes <see cref="AgentConfiguration.Validate"/> to check core requirements.<br/>
    /// 2. Validates additional YAML-specific fields (Description, Icon).<br/>
    /// 3. Validates DefaultOptions ranges (Temperature, MaxTokens).<br/>
    /// 4. Validates each Persona (already done by AgentConfiguration.Validate).<br/>
    /// 5. Accumulates all errors and warnings before returning.
    /// </para>
    /// <para>
    /// <strong>Errors:</strong> Prevent the configuration from being loaded.<br/>
    /// <strong>Warnings:</strong> Suggest improvements but allow loading.
    /// </para>
    /// </remarks>
    public AgentConfigValidationResult Validate(AgentConfiguration config, string sourceName)
    {
        if (config is null)
        {
            _logger.LogWarning("Validation called with null configuration for source: {Source}", sourceName);
            return AgentConfigValidationResult.Failure(sourceName, new[]
            {
                new AgentConfigError("config", "Configuration is null")
            });
        }

        var errors = new List<AgentConfigError>();
        var warnings = new List<AgentConfigWarning>();

        // --- Core Validation (from AgentConfiguration.Validate) ---
        var coreErrors = config.Validate();
        foreach (var error in coreErrors)
        {
            // Parse property name from error message if possible
            var parts = error.Split(':');
            var property = parts.Length > 1 ? parts[0].Trim() : "unknown";
            var message = parts.Length > 1 ? parts[1].Trim() : error;

            errors.Add(new AgentConfigError(property.ToLowerInvariant().Replace(" ", "_"), message));
        }

        // --- Additional YAML-Specific Validation ---

        // Description (required in YAML spec, though not enforced by AgentConfiguration)
        if (string.IsNullOrWhiteSpace(config.Description))
            errors.Add(new AgentConfigError("description", "Required field is missing"));

        // Icon (required in YAML spec)
        if (string.IsNullOrWhiteSpace(config.Icon))
            errors.Add(new AgentConfigError("icon", "Required field is missing"));

        // DefaultOptions validation
        if (config.DefaultOptions is not null)
        {
            // Model (required)
            if (string.IsNullOrWhiteSpace(config.DefaultOptions.Model))
                errors.Add(new AgentConfigError("default_options.model", "Required field is missing"));

            // Temperature range (0.0-2.0)
            if (config.DefaultOptions.Temperature.HasValue)
            {
                var temp = config.DefaultOptions.Temperature.Value;
                if (temp < 0 || temp > 2)
                    errors.Add(new AgentConfigError("default_options.temperature", "Must be between 0.0 and 2.0"));
            }

            // MaxTokens validation
            if (config.DefaultOptions.MaxTokens.HasValue)
            {
                var maxTokens = config.DefaultOptions.MaxTokens.Value;
                if (maxTokens <= 0)
                    errors.Add(new AgentConfigError("default_options.max_tokens", "Must be a positive integer"));
                else if (maxTokens > 128000)
                    warnings.Add(new AgentConfigWarning("default_options.max_tokens", "Value is very high (>128k)"));
            }
        }
        else
        {
            errors.Add(new AgentConfigError("default_options", "Required field is missing"));
        }

        // Persona validation (kebab-case IDs already checked by AgentConfiguration.Validate,
        // but we check temperature range here)
        foreach (var persona in config.Personas)
        {
            if (persona.Temperature < 0 || persona.Temperature > 2)
                errors.Add(new AgentConfigError(
                    $"personas[{persona.PersonaId}].temperature",
                    "Must be between 0.0 and 2.0"));
        }

        // --- Return Result ---
        if (errors.Count > 0)
        {
            _logger.LogDebug(
                "Validation failed for {Source}: {ErrorCount} errors, {WarningCount} warnings",
                sourceName, errors.Count, warnings.Count);
            return AgentConfigValidationResult.Failure(sourceName, errors);
        }

        _logger.LogDebug(
            "Validation passed for {Source} ({WarningCount} warnings)",
            sourceName, warnings.Count);
        return AgentConfigValidationResult.Success(config, sourceName, warnings);
    }
}
