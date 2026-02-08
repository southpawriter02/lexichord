// =============================================================================
// File: PredefinedSchemas.cs
// Project: Lexichord.Modules.Knowledge
// Description: Built-in entity type schemas for common API documentation types.
// =============================================================================
// LOGIC: Provides ready-to-use EntityTypeSchema definitions for standard
//   technical documentation entities. These schemas can be registered with
//   ISchemaRegistry as seed data when no custom schemas are loaded.
//
// Predefined types:
//   - Endpoint: HTTP API endpoint (method, path, description, deprecated)
//   - Parameter: API parameter (name, location, type, required, default)
//
// v0.6.5f: Schema Validator (CKVS Phase 3a)
// Dependencies: EntityTypeSchema, PropertySchema, PropertyType (v0.4.5f)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Schema;

/// <summary>
/// Predefined <see cref="EntityTypeSchema"/> definitions for common API documentation entities.
/// </summary>
/// <remarks>
/// <para>
/// These schemas define the standard structure for technical documentation
/// entity types. They serve as seed data when no custom schemas are loaded
/// from the schema directory.
/// </para>
/// <para>
/// <b>Usage:</b> Register with <see cref="ISchemaRegistry"/> or use directly
/// in tests and schema validation demonstrations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5f as part of the Schema Validator.
/// </para>
/// </remarks>
public static class PredefinedSchemas
{
    /// <summary>
    /// Schema for an HTTP API endpoint entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defines the required and optional properties for API endpoint entities.
    /// Required properties: <c>method</c> (enum), <c>path</c> (string with pattern).
    /// Optional properties: <c>description</c> (string), <c>deprecated</c> (boolean).
    /// </para>
    /// </remarks>
    public static EntityTypeSchema Endpoint => new()
    {
        Name = "Endpoint",
        Description = "An HTTP API endpoint",
        Properties =
        [
            new PropertySchema
            {
                Name = "method",
                Type = PropertyType.Enum,
                Required = true,
                Description = "HTTP method (GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS)",
                EnumValues = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"]
            },
            new PropertySchema
            {
                Name = "path",
                Type = PropertyType.String,
                Required = true,
                Description = "The URL path for the endpoint",
                Pattern = @"^/.*$"
            },
            new PropertySchema
            {
                Name = "description",
                Type = PropertyType.String,
                Required = false,
                Description = "Human-readable description of the endpoint"
            },
            new PropertySchema
            {
                Name = "deprecated",
                Type = PropertyType.Boolean,
                Required = false,
                Description = "Whether the endpoint is deprecated",
                DefaultValue = "false"
            }
        ],
        RequiredProperties = ["method", "path"]
    };

    /// <summary>
    /// Schema for an API parameter entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defines the required and optional properties for API parameter entities.
    /// Required properties: <c>name</c> (string), <c>location</c> (enum), <c>type</c> (enum).
    /// Optional properties: <c>required</c> (boolean), <c>default</c> (string).
    /// </para>
    /// </remarks>
    public static EntityTypeSchema Parameter => new()
    {
        Name = "Parameter",
        Description = "A parameter for an API endpoint",
        Properties =
        [
            new PropertySchema
            {
                Name = "name",
                Type = PropertyType.String,
                Required = true,
                Description = "Parameter name"
            },
            new PropertySchema
            {
                Name = "location",
                Type = PropertyType.Enum,
                Required = true,
                Description = "Where the parameter appears in the request",
                EnumValues = ["path", "query", "header", "cookie", "body"]
            },
            new PropertySchema
            {
                Name = "type",
                Type = PropertyType.Enum,
                Required = true,
                Description = "Data type of the parameter",
                EnumValues = ["string", "integer", "number", "boolean", "array", "object"]
            },
            new PropertySchema
            {
                Name = "required",
                Type = PropertyType.Boolean,
                Required = false,
                Description = "Whether the parameter is required",
                DefaultValue = "false"
            },
            new PropertySchema
            {
                Name = "default",
                Type = PropertyType.String,
                Required = false,
                Description = "Default value for the parameter"
            }
        ],
        RequiredProperties = ["name", "location", "type"]
    };
}
