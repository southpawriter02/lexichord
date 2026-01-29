using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for LicenseInfo record.
/// </summary>
/// <remarks>
/// Tests computed properties like IsExpired, DaysUntilExpiration, MaskedLicenseKey, and IsActivated.
/// 
/// Version: v0.1.6c
/// </remarks>
public class LicenseInfoTests
{
    #region CoreDefault Tests

    [Fact]
    public void CoreDefault_HasCorrectTier()
    {
        Assert.Equal(LicenseTier.Core, LicenseInfo.CoreDefault.Tier);
    }

    [Fact]
    public void CoreDefault_HasNullLicenseeName()
    {
        Assert.Null(LicenseInfo.CoreDefault.LicenseeName);
    }

    [Fact]
    public void CoreDefault_HasNullExpirationDate()
    {
        Assert.Null(LicenseInfo.CoreDefault.ExpirationDate);
    }

    [Fact]
    public void CoreDefault_HasNullLicenseKey()
    {
        Assert.Null(LicenseInfo.CoreDefault.LicenseKey);
    }

    [Fact]
    public void CoreDefault_IsNotActivated()
    {
        Assert.False(LicenseInfo.CoreDefault.IsActivated);
    }

    #endregion

    #region IsActivated Tests

    [Fact]
    public void IsActivated_WhenFalse_ReturnsFalse()
    {
        // Arrange
        var info = new LicenseInfo(LicenseTier.Core, null, null, null, false);

        // Assert
        Assert.False(info.IsActivated);
    }

    [Fact]
    public void IsActivated_WhenTrue_ReturnsTrue()
    {
        // Arrange
        var info = new LicenseInfo(LicenseTier.WriterPro, "Test", null, null, true);

        // Assert
        Assert.True(info.IsActivated);
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_NoExpirationDate_ReturnsFalse()
    {
        // Arrange
        var info = new LicenseInfo(LicenseTier.WriterPro, "Test", null, null, true);

        // Assert
        Assert.False(info.IsExpired);
    }

    [Fact]
    public void IsExpired_FutureDate_ReturnsFalse()
    {
        // Arrange
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddDays(30),
            null,
            true);

        // Assert
        Assert.False(info.IsExpired);
    }

    [Fact]
    public void IsExpired_PastDate_ReturnsTrue()
    {
        // Arrange
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddDays(-1),
            null,
            true);

        // Assert
        Assert.True(info.IsExpired);
    }

    [Fact]
    public void IsExpired_ExactlyNow_ReturnsFalse()
    {
        // Arrange - Use a date barely in the future to ensure consistency
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddMinutes(1),
            null,
            true);

        // Assert
        Assert.False(info.IsExpired);
    }

    #endregion

    #region DaysUntilExpiration Tests

    [Fact]
    public void DaysUntilExpiration_NoExpirationDate_ReturnsNull()
    {
        // Arrange
        var info = new LicenseInfo(LicenseTier.WriterPro, "Test", null, null, true);

        // Assert
        Assert.Null(info.DaysUntilExpiration);
    }

    [Fact]
    public void DaysUntilExpiration_30DaysFromNow_Returns30()
    {
        // Arrange
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddDays(30),
            null,
            true);

        // Assert - Allow some variance for test execution time
        Assert.NotNull(info.DaysUntilExpiration);
        Assert.InRange(info.DaysUntilExpiration.Value, 29, 31);
    }

    [Fact]
    public void DaysUntilExpiration_ExpiredYesterday_ReturnsNegative()
    {
        // Arrange
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddDays(-1),
            null,
            true);

        // Assert
        Assert.NotNull(info.DaysUntilExpiration);
        Assert.True(info.DaysUntilExpiration.Value < 0);
    }

    #endregion

    #region MaskedLicenseKey Tests

    [Fact]
    public void MaskedLicenseKey_NullKey_ReturnsNull()
    {
        // Arrange
        var info = new LicenseInfo(LicenseTier.Core, null, null, null, false);

        // Assert
        Assert.Null(info.MaskedLicenseKey);
    }

    [Fact]
    public void MaskedLicenseKey_FullKey_MaskesMiddleSegments()
    {
        // Arrange
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            null,
            "ABCD-1234-EFGH-5678",
            true);

        // Assert
        var masked = info.MaskedLicenseKey;
        Assert.NotNull(masked);
        Assert.EndsWith("5678", masked);
        Assert.Contains("****", masked);
    }

    [Fact]
    public void MaskedLicenseKey_ShortKey_ReturnsNull()
    {
        // Arrange - Key that doesn't fit the expected format (less than 4 chars)
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            null,
            "SHO", // Less than 4 characters
            true);

        // Assert
        Assert.Null(info.MaskedLicenseKey);
    }

    [Fact]
    public void MaskedLicenseKey_ExactlyFourChars_ReturnsMasked()
    {
        // Arrange
        var info = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            null,
            "ABCD",
            true);

        // Assert
        var masked = info.MaskedLicenseKey;
        Assert.NotNull(masked);
        Assert.EndsWith("ABCD", masked);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void TwoInfos_WithSameValues_AreEqual()
    {
        // Arrange
        var expiration = DateTime.UtcNow.AddYears(1);
        var info1 = new LicenseInfo(LicenseTier.WriterPro, "John", expiration, "KEY1", true);
        var info2 = new LicenseInfo(LicenseTier.WriterPro, "John", expiration, "KEY1", true);

        // Assert
        Assert.Equal(info1, info2);
    }

    [Fact]
    public void TwoInfos_WithDifferentTiers_AreNotEqual()
    {
        // Arrange
        var info1 = new LicenseInfo(LicenseTier.WriterPro, "John", null, null, true);
        var info2 = new LicenseInfo(LicenseTier.Enterprise, "John", null, null, true);

        // Assert
        Assert.NotEqual(info1, info2);
    }

    [Fact]
    public void TwoInfos_WithDifferentActivation_AreNotEqual()
    {
        // Arrange
        var info1 = new LicenseInfo(LicenseTier.Core, null, null, null, false);
        var info2 = new LicenseInfo(LicenseTier.Core, null, null, null, true);

        // Assert
        Assert.NotEqual(info1, info2);
    }

    #endregion
}
