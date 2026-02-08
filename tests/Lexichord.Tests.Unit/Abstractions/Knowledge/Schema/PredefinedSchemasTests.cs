// =============================================================================
// File: PredefinedSchemasTests.cs
// Project: Lexichord.Tests.Unit
// Description: Tests for PredefinedSchemas built-in entity type schemas.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Validation.Validators.Schema;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Schema;

/// <summary>
/// Tests for <see cref="PredefinedSchemas"/> built-in entity type schemas.
/// </summary>
public sealed class PredefinedSchemasTests
{
    // =========================================================================
    // Endpoint schema
    // =========================================================================

    [Fact]
    public void Endpoint_HasCorrectName()
    {
        PredefinedSchemas.Endpoint.Name.Should().Be("Endpoint");
    }

    [Fact]
    public void Endpoint_HasDescription()
    {
        PredefinedSchemas.Endpoint.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Endpoint_HasFourProperties()
    {
        PredefinedSchemas.Endpoint.Properties.Should().HaveCount(4);
    }

    [Fact]
    public void Endpoint_MethodProperty_IsRequiredEnum()
    {
        var method = PredefinedSchemas.Endpoint.Properties
            .First(p => p.Name == "method");

        method.Type.Should().Be(PropertyType.Enum);
        method.Required.Should().BeTrue();
        method.EnumValues.Should().Contain("GET").And.Contain("POST");
    }

    [Fact]
    public void Endpoint_PathProperty_IsRequiredStringWithPattern()
    {
        var path = PredefinedSchemas.Endpoint.Properties
            .First(p => p.Name == "path");

        path.Type.Should().Be(PropertyType.String);
        path.Required.Should().BeTrue();
        path.Pattern.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Endpoint_RequiredProperties_HasMethodAndPath()
    {
        PredefinedSchemas.Endpoint.RequiredProperties
            .Should().Contain("method").And.Contain("path");
    }

    [Fact]
    public void Endpoint_DeprecatedProperty_IsOptionalBoolean()
    {
        var deprecated = PredefinedSchemas.Endpoint.Properties
            .First(p => p.Name == "deprecated");

        deprecated.Type.Should().Be(PropertyType.Boolean);
        deprecated.Required.Should().BeFalse();
    }

    // =========================================================================
    // Parameter schema
    // =========================================================================

    [Fact]
    public void Parameter_HasCorrectName()
    {
        PredefinedSchemas.Parameter.Name.Should().Be("Parameter");
    }

    [Fact]
    public void Parameter_HasDescription()
    {
        PredefinedSchemas.Parameter.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Parameter_HasFiveProperties()
    {
        PredefinedSchemas.Parameter.Properties.Should().HaveCount(5);
    }

    [Fact]
    public void Parameter_LocationProperty_IsRequiredEnum()
    {
        var location = PredefinedSchemas.Parameter.Properties
            .First(p => p.Name == "location");

        location.Type.Should().Be(PropertyType.Enum);
        location.Required.Should().BeTrue();
        location.EnumValues.Should().Contain("query").And.Contain("path");
    }

    [Fact]
    public void Parameter_RequiredProperties_HasNameLocationType()
    {
        PredefinedSchemas.Parameter.RequiredProperties
            .Should().Contain("name")
            .And.Contain("location")
            .And.Contain("type");
    }

    [Fact]
    public void Parameter_RequiredProperty_IsOptionalBoolean()
    {
        var required = PredefinedSchemas.Parameter.Properties
            .First(p => p.Name == "required");

        required.Type.Should().Be(PropertyType.Boolean);
        required.Required.Should().BeFalse();
    }
}
