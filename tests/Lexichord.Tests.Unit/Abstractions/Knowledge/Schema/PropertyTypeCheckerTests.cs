// =============================================================================
// File: PropertyTypeCheckerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Tests for PropertyTypeChecker type validation logic.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Validation.Validators.Schema;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Schema;

/// <summary>
/// Tests for <see cref="PropertyTypeChecker"/> type validation logic.
/// </summary>
public sealed class PropertyTypeCheckerTests
{
    private readonly PropertyTypeChecker _sut;

    public PropertyTypeCheckerTests()
    {
        _sut = new PropertyTypeChecker(new FakeLogger<PropertyTypeChecker>());
    }

    // =========================================================================
    // Null value handling
    // =========================================================================

    [Fact]
    public void CheckType_NullValue_AlwaysValid()
    {
        foreach (PropertyType pt in Enum.GetValues<PropertyType>())
        {
            var result = _sut.CheckType(null, pt);
            result.IsValid.Should().BeTrue($"null should be valid for {pt}");
            result.ActualType.Should().Be("null");
        }
    }

    // =========================================================================
    // String / Text types
    // =========================================================================

    [Theory]
    [InlineData(PropertyType.String)]
    [InlineData(PropertyType.Text)]
    public void CheckType_StringValue_ValidForStringAndText(PropertyType type)
    {
        var result = _sut.CheckType("hello", type);
        result.IsValid.Should().BeTrue();
        result.ActualType.Should().Be("String");
    }

    [Theory]
    [InlineData(PropertyType.String)]
    [InlineData(PropertyType.Text)]
    public void CheckType_IntValue_InvalidForStringAndText(PropertyType type)
    {
        var result = _sut.CheckType(42, type);
        result.IsValid.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    // =========================================================================
    // Number type
    // =========================================================================

    [Theory]
    [InlineData(42)]
    [InlineData(42L)]
    [InlineData(42.0f)]
    [InlineData(42.0d)]
    public void CheckType_NumericValues_ValidForNumber(object value)
    {
        var result = _sut.CheckType(value, PropertyType.Number);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_DecimalValue_ValidForNumber()
    {
        var result = _sut.CheckType(42.0m, PropertyType.Number);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_StringValue_InvalidForNumber()
    {
        var result = _sut.CheckType("42", PropertyType.Number);
        result.IsValid.Should().BeFalse();
    }

    // =========================================================================
    // Boolean type
    // =========================================================================

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckType_BoolValues_ValidForBoolean(bool value)
    {
        var result = _sut.CheckType(value, PropertyType.Boolean);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_StringValue_InvalidForBoolean()
    {
        var result = _sut.CheckType("true", PropertyType.Boolean);
        result.IsValid.Should().BeFalse();
    }

    // =========================================================================
    // DateTime type
    // =========================================================================

    [Fact]
    public void CheckType_DateTime_ValidForDateTime()
    {
        var result = _sut.CheckType(DateTime.Now, PropertyType.DateTime);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_DateTimeOffset_ValidForDateTime()
    {
        var result = _sut.CheckType(DateTimeOffset.UtcNow, PropertyType.DateTime);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_ParseableDateString_ValidForDateTime()
    {
        var result = _sut.CheckType("2026-01-15", PropertyType.DateTime);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_UnparseableString_InvalidForDateTime()
    {
        var result = _sut.CheckType("not-a-date", PropertyType.DateTime);
        result.IsValid.Should().BeFalse();
    }

    // =========================================================================
    // Enum type
    // =========================================================================

    [Fact]
    public void CheckType_StringValue_ValidForEnum()
    {
        var result = _sut.CheckType("GET", PropertyType.Enum);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_IntValue_InvalidForEnum()
    {
        var result = _sut.CheckType(42, PropertyType.Enum);
        result.IsValid.Should().BeFalse();
    }

    // =========================================================================
    // Reference type
    // =========================================================================

    [Fact]
    public void CheckType_Guid_ValidForReference()
    {
        var result = _sut.CheckType(Guid.NewGuid(), PropertyType.Reference);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_String_ValidForReference()
    {
        var result = _sut.CheckType("entity-id-123", PropertyType.Reference);
        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // Array type
    // =========================================================================

    [Fact]
    public void CheckType_List_ValidForArray()
    {
        var result = _sut.CheckType(new List<string> { "a", "b" }, PropertyType.Array);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_CLRArray_ValidForArray()
    {
        var result = _sut.CheckType(new[] { 1, 2, 3 }, PropertyType.Array);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckType_String_InvalidForArray()
    {
        // LOGIC: Strings implement IEnumerable but should NOT be valid arrays.
        var result = _sut.CheckType("not-an-array", PropertyType.Array);
        result.IsValid.Should().BeFalse();
    }
}
