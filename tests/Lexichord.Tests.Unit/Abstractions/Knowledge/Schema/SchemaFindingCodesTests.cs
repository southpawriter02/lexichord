// =============================================================================
// File: SchemaFindingCodesTests.cs
// Project: Lexichord.Tests.Unit
// Description: Tests for SchemaFindingCodes constant values.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Schema;

/// <summary>
/// Tests for <see cref="SchemaFindingCodes"/> constant string values.
/// </summary>
public sealed class SchemaFindingCodesTests
{
    [Fact]
    public void RequiredPropertyMissing_HasExpectedValue()
    {
        SchemaFindingCodes.RequiredPropertyMissing.Should().Be("SCHEMA_REQUIRED_PROPERTY");
    }

    [Fact]
    public void TypeMismatch_HasExpectedValue()
    {
        SchemaFindingCodes.TypeMismatch.Should().Be("SCHEMA_TYPE_MISMATCH");
    }

    [Fact]
    public void InvalidEnumValue_HasExpectedValue()
    {
        SchemaFindingCodes.InvalidEnumValue.Should().Be("SCHEMA_INVALID_ENUM");
    }

    [Fact]
    public void ConstraintViolation_HasExpectedValue()
    {
        SchemaFindingCodes.ConstraintViolation.Should().Be("SCHEMA_CONSTRAINT");
    }

    [Fact]
    public void UnknownProperty_HasExpectedValue()
    {
        SchemaFindingCodes.UnknownProperty.Should().Be("SCHEMA_UNKNOWN_PROPERTY");
    }

    [Fact]
    public void InvalidReference_HasExpectedValue()
    {
        SchemaFindingCodes.InvalidReference.Should().Be("SCHEMA_INVALID_REFERENCE");
    }

    [Fact]
    public void SchemaNotFound_HasExpectedValue()
    {
        SchemaFindingCodes.SchemaNotFound.Should().Be("SCHEMA_NOT_FOUND");
    }

    [Fact]
    public void PatternMismatch_HasExpectedValue()
    {
        SchemaFindingCodes.PatternMismatch.Should().Be("SCHEMA_PATTERN_MISMATCH");
    }

    [Fact]
    public void ValueTooSmall_HasExpectedValue()
    {
        SchemaFindingCodes.ValueTooSmall.Should().Be("SCHEMA_VALUE_TOO_SMALL");
    }

    [Fact]
    public void ValueTooLarge_HasExpectedValue()
    {
        SchemaFindingCodes.ValueTooLarge.Should().Be("SCHEMA_VALUE_TOO_LARGE");
    }

    [Fact]
    public void StringTooLong_HasExpectedValue()
    {
        SchemaFindingCodes.StringTooLong.Should().Be("SCHEMA_STRING_TOO_LONG");
    }

    [Fact]
    public void AllCodes_HaveSchemaPrefix()
    {
        // LOGIC: Verify all codes follow the naming convention.
        var fields = typeof(SchemaFindingCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var value = (string?)field.GetValue(null);
            value.Should().StartWith("SCHEMA_",
                because: $"code {field.Name} should follow the SCHEMA_ prefix convention");
        }
    }
}
