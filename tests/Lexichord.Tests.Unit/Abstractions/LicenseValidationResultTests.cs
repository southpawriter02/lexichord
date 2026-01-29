using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for LicenseValidationResult record.
/// </summary>
/// <remarks>
/// Tests static factory methods and property behavior.
/// 
/// Version: v0.1.6c
/// </remarks>
public class LicenseValidationResultTests
{
    #region Success Factory Method Tests

    [Fact]
    public void Success_WithAllParameters_CreatesValidResult()
    {
        // Arrange
        var tier = LicenseTier.WriterPro;
        var licenseeName = "John Doe";
        var expiration = DateTime.UtcNow.AddYears(1);

        // Act
        var result = LicenseValidationResult.Success(tier, licenseeName, expiration);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(tier, result.Tier);
        Assert.Equal(licenseeName, result.LicenseeName);
        Assert.Equal(expiration, result.ExpirationDate);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(LicenseErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void Success_WithNullExpiration_CreatesValidResult()
    {
        // Arrange & Act
        var result = LicenseValidationResult.Success(LicenseTier.Enterprise, "Acme Corp", null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ExpirationDate);
    }

    [Fact]
    public void Success_WithEmptyLicenseeName_CreatesValidResult()
    {
        // Arrange & Act (empty string for licensee name)
        var result = LicenseValidationResult.Success(LicenseTier.Teams, "", null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("", result.LicenseeName);
    }

    [Theory]
    [InlineData(LicenseTier.Core)]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public void Success_AllTiers_CreatesCorrectTier(LicenseTier tier)
    {
        // Arrange & Act
        var result = LicenseValidationResult.Success(tier, "Test", null);

        // Assert
        Assert.Equal(tier, result.Tier);
    }

    #endregion

    #region Failure Factory Method Tests

    [Fact]
    public void Failure_WithErrorCode_CreatesInvalidResult()
    {
        // Arrange
        var errorCode = LicenseErrorCode.InvalidFormat;
        var errorMessage = "License key format is invalid";

        // Act
        var result = LicenseValidationResult.Failure(errorCode, errorMessage);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void Failure_SetsCoreTier()
    {
        // Arrange & Act
        var result = LicenseValidationResult.Failure(
            LicenseErrorCode.InvalidSignature,
            "Invalid signature");

        // Assert
        Assert.Equal(LicenseTier.Core, result.Tier);
    }

    [Fact]
    public void Failure_HasNullLicenseeName()
    {
        // Arrange & Act
        var result = LicenseValidationResult.Failure(
            LicenseErrorCode.Expired,
            "License has expired");

        // Assert
        Assert.Null(result.LicenseeName);
    }

    [Fact]
    public void Failure_HasNullExpirationDate()
    {
        // Arrange & Act
        var result = LicenseValidationResult.Failure(
            LicenseErrorCode.Revoked,
            "License has been revoked");

        // Assert
        Assert.Null(result.ExpirationDate);
    }

    [Theory]
    [InlineData(LicenseErrorCode.InvalidFormat)]
    [InlineData(LicenseErrorCode.InvalidSignature)]
    [InlineData(LicenseErrorCode.Expired)]
    [InlineData(LicenseErrorCode.Revoked)]
    [InlineData(LicenseErrorCode.AlreadyActivated)]
    [InlineData(LicenseErrorCode.NetworkError)]
    [InlineData(LicenseErrorCode.ServerError)]
    [InlineData(LicenseErrorCode.ActivationLimitReached)]
    public void Failure_AllErrorCodes_CreatesCorrectErrorCode(LicenseErrorCode errorCode)
    {
        // Arrange & Act
        var result = LicenseValidationResult.Failure(errorCode, "Test error");

        // Assert
        Assert.Equal(errorCode, result.ErrorCode);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void TwoSuccessResults_WithSameValues_AreEqual()
    {
        // Arrange
        var expiration = DateTime.UtcNow.AddYears(1);
        var result1 = LicenseValidationResult.Success(LicenseTier.WriterPro, "John", expiration);
        var result2 = LicenseValidationResult.Success(LicenseTier.WriterPro, "John", expiration);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void TwoFailureResults_WithSameValues_AreEqual()
    {
        // Arrange
        var result1 = LicenseValidationResult.Failure(LicenseErrorCode.InvalidFormat, "Bad format");
        var result2 = LicenseValidationResult.Failure(LicenseErrorCode.InvalidFormat, "Bad format");

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void SuccessAndFailure_AreDifferent()
    {
        // Arrange
        var success = LicenseValidationResult.Success(LicenseTier.Core, "", null);
        var failure = LicenseValidationResult.Failure(LicenseErrorCode.InvalidFormat, "Error");

        // Assert
        Assert.NotEqual(success, failure);
    }

    #endregion
}
