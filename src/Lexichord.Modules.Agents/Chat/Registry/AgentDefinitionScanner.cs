// -----------------------------------------------------------------------
// <copyright file="AgentDefinitionScanner.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Scans assemblies for types marked with [AgentDefinition] attribute
//   and extracts registration metadata. Validates that decorated types implement
//   IAgent interface and are public. Orders results by priority (lower values first)
//   to control registration order.
// -----------------------------------------------------------------------

using System.Reflection;
using Lexichord.Abstractions.Agents;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Registry;

/// <summary>
/// Scans assemblies for types marked with <see cref="AgentDefinitionAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The scanner performs assembly introspection to discover agent implementations
/// decorated with <see cref="AgentDefinitionAttribute"/>. This enables declarative
/// agent registration where agents are automatically discovered at startup.
/// </para>
/// <para>
/// <b>Scanning Logic:</b>
/// </para>
/// <list type="number">
///   <item>Iterate through each assembly's exported types</item>
///   <item>Check for <see cref="AgentDefinitionAttribute"/> presence</item>
///   <item>Validate type implements <see cref="IAgent"/> interface</item>
///   <item>Extract agent ID and priority from attribute</item>
///   <item>Return ordered by priority (lower values first)</item>
/// </list>
/// <para>
/// <b>Validation Rules:</b>
/// </para>
/// <list type="bullet">
///   <item>Type must be public</item>
///   <item>Type must implement <see cref="IAgent"/></item>
///   <item>Type must have <see cref="AgentDefinitionAttribute"/></item>
///   <item>Agent ID must be non-null (validated by attribute constructor)</item>
/// </list>
/// <para>
/// <b>Invalid Definitions:</b> Types that fail validation are logged at Warning
/// level and excluded from results. Common failures include:
/// </para>
/// <list type="bullet">
///   <item>Non-public types</item>
///   <item>Abstract classes</item>
///   <item>Types not implementing IAgent</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.1b for declarative agent registration.
/// </para>
/// </remarks>
/// <seealso cref="AgentDefinitionAttribute"/>
/// <seealso cref="AgentDefinitionInfo"/>
/// <seealso cref="IAgent"/>
public sealed class AgentDefinitionScanner
{
    private readonly ILogger<AgentDefinitionScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDefinitionScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AgentDefinitionScanner(ILogger<AgentDefinitionScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scans the specified assemblies for agent definitions.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>
    /// An enumerable of <see cref="AgentDefinitionInfo"/> representing discovered agents,
    /// ordered by priority (lower values first).
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method performs the following:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <b>Logging:</b> Log assembly count at Info level
    ///   </item>
    ///   <item>
    ///     <b>Iteration:</b> For each assembly, get exported types
    ///   </item>
    ///   <item>
    ///     <b>Attribute Check:</b> Use reflection to find <see cref="AgentDefinitionAttribute"/>
    ///   </item>
    ///   <item>
    ///     <b>Interface Validation:</b> Ensure type implements <see cref="IAgent"/>
    ///   </item>
    ///   <item>
    ///     <b>Result Construction:</b> Create <see cref="AgentDefinitionInfo"/> with
    ///     agent ID, type, and priority
    ///   </item>
    ///   <item>
    ///     <b>Debug Logging:</b> Log each discovered definition
    ///   </item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> This operation uses reflection and should be called
    /// once at startup, not repeatedly. Results should be cached by the caller
    /// (typically <see cref="AgentRegistry"/>).
    /// </para>
    /// <para>
    /// <b>Exception Handling:</b> If an assembly cannot be scanned (e.g., security
    /// restrictions), the exception is logged and scanning continues with remaining
    /// assemblies.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="assemblies"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// var scanner = new AgentDefinitionScanner(logger);
    /// var assemblies = new[] { typeof(EditorAgent).Assembly };
    /// var definitions = scanner.ScanAssemblies(assemblies);
    ///
    /// foreach (var def in definitions)
    /// {
    ///     Console.WriteLine($"Found {def.AgentId} with priority {def.Priority}");
    /// }
    /// </code>
    /// </example>
    public IEnumerable<AgentDefinitionInfo> ScanAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        _logger.LogInformation(
            "Scanning {Count} assemblies for agent definitions",
            assemblies.Length);

        var definitions = new List<AgentDefinitionInfo>();

        foreach (var assembly in assemblies)
        {
            _logger.LogDebug("Scanning assembly: {AssemblyName}", assembly.FullName);

            try
            {
                // LOGIC: GetExportedTypes returns only public types, which is what we want
                // since agents must be publicly accessible for DI resolution.
                foreach (var type in assembly.GetExportedTypes())
                {
                    // LOGIC: Check for AgentDefinitionAttribute presence
                    var attribute = type.GetCustomAttribute<AgentDefinitionAttribute>();
                    if (attribute is null)
                        continue;

                    // LOGIC: Validate type implements IAgent interface
                    if (!typeof(IAgent).IsAssignableFrom(type))
                    {
                        _logger.LogWarning(
                            "Skipped invalid agent definition: {Type} has [AgentDefinition] but does not implement IAgent",
                            type.FullName);
                        continue;
                    }

                    // LOGIC: Validate type is instantiable (not abstract, not interface)
                    if (type.IsAbstract || type.IsInterface)
                    {
                        _logger.LogWarning(
                            "Skipped abstract agent definition: {Type} cannot be instantiated",
                            type.FullName);
                        continue;
                    }

                    _logger.LogDebug(
                        "Found agent definition: {AgentId} (Priority={Priority}, Type={Type})",
                        attribute.AgentId,
                        attribute.Priority,
                        type.FullName);

                    definitions.Add(new AgentDefinitionInfo(
                        attribute.AgentId,
                        type,
                        attribute.Priority));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to scan assembly {AssemblyName} for agent definitions",
                    assembly.FullName);
            }
        }

        // LOGIC: Order by priority (lower values register first)
        var ordered = definitions.OrderBy(d => d.Priority).ToList();

        _logger.LogInformation(
            "Found {Count} agent definitions across {AssemblyCount} assemblies",
            ordered.Count,
            assemblies.Length);

        return ordered;
    }
}

/// <summary>
/// Represents metadata extracted from an <see cref="AgentDefinitionAttribute"/>.
/// </summary>
/// <param name="AgentId">The unique identifier for the agent.</param>
/// <param name="ImplementationType">The concrete type that implements <see cref="IAgent"/>.</param>
/// <param name="Priority">
/// The registration priority (lower values register first). Defaults to 100.
/// </param>
/// <remarks>
/// <para>
/// This record is the output of <see cref="AgentDefinitionScanner.ScanAssemblies"/>
/// and provides all information needed to register the agent with the registry.
/// </para>
/// <para>
/// <b>Usage Flow:</b>
/// </para>
/// <list type="number">
///   <item>Scanner creates <see cref="AgentDefinitionInfo"/> from attribute</item>
///   <item>Registry receives sorted list of definitions</item>
///   <item>Registry resolves <see cref="ImplementationType"/> from DI</item>
///   <item>Registry registers agent with <see cref="AgentId"/></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.1b.
/// </para>
/// </remarks>
/// <seealso cref="AgentDefinitionScanner"/>
/// <seealso cref="AgentDefinitionAttribute"/>
public record AgentDefinitionInfo(
    string AgentId,
    Type ImplementationType,
    int Priority
);
