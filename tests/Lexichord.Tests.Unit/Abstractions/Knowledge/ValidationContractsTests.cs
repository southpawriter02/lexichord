// =============================================================================
// File: ValidationContractsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the validation orchestrator data contracts.
// =============================================================================
// LOGIC: Verifies correct construction, default values, computed properties,
//   and factory methods for all validation data contracts: ValidationMode,
//   ValidationSeverity, ValidationFinding, ValidationResult, ValidationOptions,
//   and ValidationContext.
//
// v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge;

/// <summary>
/// Unit tests for validation orchestrator data contracts.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.6.5e")]
public sealed class ValidationContractsTests
{
    #region ValidationMode Tests

    [Fact]
    public void ValidationMode_HasFlagsAttribute_CanCombineModes()
    {
        // Arrange & Act
        var combined = ValidationMode.RealTime | ValidationMode.OnSave;

        // Assert
        combined.HasFlag(ValidationMode.RealTime).Should().BeTrue();
        combined.HasFlag(ValidationMode.OnSave).Should().BeTrue();
        combined.HasFlag(ValidationMode.OnDemand).Should().BeFalse();
    }

    [Fact]
    public void ValidationMode_All_IncludesAllModes()
    {
        // Arrange & Act
        var all = ValidationMode.All;

        // Assert
        all.HasFlag(ValidationMode.RealTime).Should().BeTrue();
        all.HasFlag(ValidationMode.OnSave).Should().BeTrue();
        all.HasFlag(ValidationMode.OnDemand).Should().BeTrue();
        all.HasFlag(ValidationMode.PrePublish).Should().BeTrue();
    }

    [Fact]
    public void ValidationMode_None_HasZeroValue()
    {
        // Assert
        ((int)ValidationMode.None).Should().Be(0);
    }

    #endregion

    #region ValidationSeverity Tests

    [Fact]
    public void ValidationSeverity_OrderIsInfoWarningError()
    {
        // Assert — Info < Warning < Error
        ((int)ValidationSeverity.Info).Should().BeLessThan((int)ValidationSeverity.Warning);
        ((int)ValidationSeverity.Warning).Should().BeLessThan((int)ValidationSeverity.Error);
    }

    #endregion

    #region ValidationFinding Tests

    [Fact]
    public void ValidationFinding_ConstructedWithAllProperties()
    {
        // Arrange & Act
        var finding = new ValidationFinding(
            ValidatorId: "test-validator",
            Severity: ValidationSeverity.Error,
            Code: "TEST_001",
            Message: "Test error message",
            PropertyPath: "metadata.title",
            SuggestedFix: "Add a title");

        // Assert
        finding.ValidatorId.Should().Be("test-validator");
        finding.Severity.Should().Be(ValidationSeverity.Error);
        finding.Code.Should().Be("TEST_001");
        finding.Message.Should().Be("Test error message");
        finding.PropertyPath.Should().Be("metadata.title");
        finding.SuggestedFix.Should().Be("Add a title");
    }

    [Fact]
    public void ValidationFinding_OptionalProperties_DefaultToNull()
    {
        // Arrange & Act
        var finding = new ValidationFinding("v1", ValidationSeverity.Info, "C1", "msg");

        // Assert
        finding.PropertyPath.Should().BeNull();
        finding.SuggestedFix.Should().BeNull();
    }

    [Fact]
    public void ValidationFinding_ErrorFactory_CreatesFindingWithErrorSeverity()
    {
        // Act
        var finding = ValidationFinding.Error("v1", "E001", "Something failed");

        // Assert
        finding.Severity.Should().Be(ValidationSeverity.Error);
        finding.ValidatorId.Should().Be("v1");
        finding.Code.Should().Be("E001");
    }

    [Fact]
    public void ValidationFinding_WarnFactory_CreatesFindingWithWarningSeverity()
    {
        // Act
        var finding = ValidationFinding.Warn("v1", "W001", "Something suspicious");

        // Assert
        finding.Severity.Should().Be(ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidationFinding_InformationFactory_CreatesFindingWithInfoSeverity()
    {
        // Act
        var finding = ValidationFinding.Information("v1", "I001", "Just FYI");

        // Assert
        finding.Severity.Should().Be(ValidationSeverity.Info);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_IsValid_TrueWhenNoErrors()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Warn("v1", "W001", "warning"),
            ValidationFinding.Information("v1", "I001", "info")
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_IsValid_FalseWhenErrorPresent()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("v1", "E001", "error"),
            ValidationFinding.Warn("v1", "W001", "warning")
        });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidationResult_Valid_HasNoFindings()
    {
        // Act
        var result = ValidationResult.Valid(validatorsRun: 3, validatorsSkipped: 1);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Findings.Should().BeEmpty();
        result.ValidatorsRun.Should().Be(3);
        result.ValidatorsSkipped.Should().Be(1);
    }

    [Fact]
    public void ValidationResult_CountProperties_ReturnCorrectCounts()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("v1", "E001", "e1"),
            ValidationFinding.Error("v2", "E002", "e2"),
            ValidationFinding.Warn("v1", "W001", "w1"),
            ValidationFinding.Information("v1", "I001", "i1"),
            ValidationFinding.Information("v1", "I002", "i2"),
            ValidationFinding.Information("v1", "I003", "i3"),
        });

        // Assert
        result.ErrorCount.Should().Be(2);
        result.WarningCount.Should().Be(1);
        result.InfoCount.Should().Be(3);
    }

    [Fact]
    public void ValidationResult_BySeverity_GroupsFindingsCorrectly()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("v1", "E001", "e1"),
            ValidationFinding.Warn("v1", "W001", "w1"),
            ValidationFinding.Warn("v2", "W002", "w2"),
        });

        // Assert
        result.BySeverity.Should().ContainKey(ValidationSeverity.Error);
        result.BySeverity[ValidationSeverity.Error].Should().HaveCount(1);
        result.BySeverity[ValidationSeverity.Warning].Should().HaveCount(2);
    }

    [Fact]
    public void ValidationResult_ByValidator_GroupsFindingsCorrectly()
    {
        // Arrange
        var result = ValidationResult.WithFindings(new[]
        {
            ValidationFinding.Error("v1", "E001", "e1"),
            ValidationFinding.Warn("v1", "W001", "w1"),
            ValidationFinding.Error("v2", "E002", "e2"),
        });

        // Assert
        result.ByValidator.Should().ContainKey("v1");
        result.ByValidator["v1"].Should().HaveCount(2);
        result.ByValidator["v2"].Should().HaveCount(1);
    }

    #endregion

    #region ValidationOptions Tests

    [Fact]
    public void ValidationOptions_Default_HasSensibleDefaults()
    {
        // Act
        var options = ValidationOptions.Default();

        // Assert
        options.Mode.Should().Be(ValidationMode.OnDemand);
        options.Timeout.Should().BeNull();
        options.EffectiveTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.MaxFindings.Should().BeNull();
        options.LicenseTier.Should().Be(LicenseTier.Core);
    }

    [Fact]
    public void ValidationOptions_ForRealTime_HasShortTimeout()
    {
        // Act
        var options = ValidationOptions.ForRealTime(LicenseTier.Teams);

        // Assert
        options.Mode.Should().Be(ValidationMode.RealTime);
        options.EffectiveTimeout.Should().Be(TimeSpan.FromMilliseconds(50));
        options.LicenseTier.Should().Be(LicenseTier.Teams);
    }

    [Fact]
    public void ValidationOptions_ForPrePublish_HasLongTimeout()
    {
        // Act
        var options = ValidationOptions.ForPrePublish(LicenseTier.Enterprise);

        // Assert
        options.Mode.Should().Be(ValidationMode.PrePublish);
        options.EffectiveTimeout.Should().Be(TimeSpan.FromMinutes(2));
        options.LicenseTier.Should().Be(LicenseTier.Enterprise);
    }

    #endregion

    #region ValidationContext Tests

    [Fact]
    public void ValidationContext_Create_SetsDefaultOptionsAndEmptyMetadata()
    {
        // Act
        var context = ValidationContext.Create("doc-1", "markdown", "# Hello");

        // Assert
        context.DocumentId.Should().Be("doc-1");
        context.DocumentType.Should().Be("markdown");
        context.Content.Should().Be("# Hello");
        context.Metadata.Should().BeEmpty();
        context.Options.Mode.Should().Be(ValidationMode.OnDemand);
    }

    [Fact]
    public void ValidationContext_CreateWithOptions_UsesProvidedOptions()
    {
        // Arrange
        var options = new ValidationOptions(Mode: ValidationMode.PrePublish, LicenseTier: LicenseTier.Teams);

        // Act
        var context = ValidationContext.Create("doc-2", "yaml", "key: value", options);

        // Assert
        context.Options.Mode.Should().Be(ValidationMode.PrePublish);
        context.Options.LicenseTier.Should().Be(LicenseTier.Teams);
    }

    #endregion
}
