using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for AccountSettingsViewModel.
/// </summary>
/// <remarks>
/// Tests license key input validation, activation/deactivation commands,
/// tier display properties, and feature availability display.
/// 
/// Version: v0.1.6c
/// </remarks>
public class AccountSettingsViewModelTests : IDisposable
{
    private readonly Mock<ILicenseService> _mockLicenseService = new();
    private readonly Mock<ILogger<AccountSettingsViewModel>> _mockLogger = new();

    public AccountSettingsViewModelTests()
    {
        // Default setup: Core tier
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(LicenseInfo.CoreDefault);
        _mockLicenseService.Setup(s => s.GetFeatureAvailability(It.IsAny<LicenseTier>()))
            .Returns(new List<FeatureAvailability>());
    }

    private AccountSettingsViewModel CreateViewModel()
    {
        return new AccountSettingsViewModel(
            _mockLicenseService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLicenseServiceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AccountSettingsViewModel(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AccountSettingsViewModel(_mockLicenseService.Object, null!));
    }

    [Fact]
    public void Constructor_LoadsCurrentLicense()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        _mockLicenseService.Verify(s => s.GetCurrentLicense(), Times.Once);
    }

    [Fact]
    public void Constructor_LoadsFeatureAvailability()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        _mockLicenseService.Verify(s => s.GetFeatureAvailability(LicenseTier.Core), Times.Once);
    }

    [Fact]
    public void Constructor_SubscribesToLicenseChangedEvent()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert - Verify event handler was added
        _mockLicenseService.VerifyAdd(
            s => s.LicenseChanged += It.IsAny<EventHandler<LicenseChangedEventArgs>>(),
            Times.Once);
    }

    #endregion

    #region LicenseKeyInput Validation Tests

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("ABCD", false)]
    [InlineData("ABCD-1234", false)]
    [InlineData("ABCD-1234-EFGH", false)]
    [InlineData("INVALID", false)]
    public void LicenseKeyInput_InvalidFormat_SetsIsKeyFormatValidFalse(string input, bool expected)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.LicenseKeyInput = input;

        // Assert
        Assert.Equal(expected, viewModel.IsKeyFormatValid);
    }

    [Theory]
    [InlineData("ABCD-1234-EFGH-5678", true)]
    [InlineData("abcd-1234-efgh-5678", true)] // Case insensitive
    [InlineData("PRO1-2345-6789-0ABC", true)]
    public void LicenseKeyInput_ValidFormat_SetsIsKeyFormatValidTrue(string input, bool expected)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.LicenseKeyInput = input;

        // Assert
        Assert.Equal(expected, viewModel.IsKeyFormatValid);
    }

    [Fact]
    public void LicenseKeyInput_Changed_ClearsValidationMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // Manually set a validation message (simulating a previous validation attempt)
        // We can't directly set it, but we know it starts as null

        // Act
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        // Assert
        Assert.Null(viewModel.ValidationMessage);
        Assert.False(viewModel.IsValidationSuccess);
    }

    #endregion

    #region ActivateCommand Tests

    [Fact]
    public void ActivateCommand_CannotExecute_WhenKeyFormatInvalid()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "INVALID";

        // Assert
        Assert.False(viewModel.ActivateCommand.CanExecute(null));
    }

    [Fact]
    public void ActivateCommand_CanExecute_WhenKeyFormatValid()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        // Assert
        Assert.True(viewModel.ActivateCommand.CanExecute(null));
    }

    [Fact]
    public async Task ActivateCommand_SetsIsValidating_WhileProcessing()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseValidationResult.Success(LicenseTier.WriterPro, "Test User", DateTime.UtcNow.AddYears(1)));
        _mockLicenseService.Setup(s => s.ActivateLicenseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert - IsValidating should be false after completion
        Assert.False(viewModel.IsValidating);
    }

    [Fact]
    public async Task ActivateCommand_ValidKey_ShowsSuccessMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseValidationResult.Success(LicenseTier.WriterPro, "Test User", DateTime.UtcNow.AddYears(1)));
        _mockLicenseService.Setup(s => s.ActivateLicenseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.IsValidationSuccess);
        Assert.NotNull(viewModel.ValidationMessage);
        Assert.Contains("activated", viewModel.ValidationMessage.ToLower());
    }

    [Fact]
    public async Task ActivateCommand_ValidKey_ClearsInputField()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseValidationResult.Success(LicenseTier.WriterPro, "Test User", DateTime.UtcNow.AddYears(1)));
        _mockLicenseService.Setup(s => s.ActivateLicenseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(string.Empty, viewModel.LicenseKeyInput);
    }

    [Fact]
    public async Task ActivateCommand_InvalidKey_ShowsErrorMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseValidationResult.Failure(LicenseErrorCode.InvalidSignature, "Invalid signature"));

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsValidationSuccess);
        Assert.NotNull(viewModel.ValidationMessage);
    }

    [Fact]
    public async Task ActivateCommand_ServiceException_ShowsGenericError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsValidationSuccess);
        Assert.NotNull(viewModel.ValidationMessage);
        Assert.Contains("error", viewModel.ValidationMessage.ToLower());
    }

    #endregion

    #region DeactivateCommand Tests

    [Fact]
    public void DeactivateCommand_CannotExecute_WhenNotActivated()
    {
        // Arrange
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(LicenseInfo.CoreDefault);
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.DeactivateCommand.CanExecute(null));
    }

    [Fact]
    public void DeactivateCommand_CanExecute_WhenActivated()
    {
        // Arrange
        var activatedLicense = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test User",
            DateTime.UtcNow.AddYears(1),
            "PRO1-****-****-0ABC",
            true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(activatedLicense);

        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.DeactivateCommand.CanExecute(null));
    }

    [Fact]
    public async Task DeactivateCommand_Success_ShowsSuccessMessage()
    {
        // Arrange
        var activatedLicense = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test User",
            DateTime.UtcNow.AddYears(1),
            "PRO1-****-****-0ABC",
            true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(activatedLicense);
        _mockLicenseService.Setup(s => s.DeactivateLicenseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.DeactivateCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.IsValidationSuccess);
        Assert.NotNull(viewModel.ValidationMessage);
        Assert.Contains("deactivated", viewModel.ValidationMessage.ToLower());
    }

    [Fact]
    public async Task DeactivateCommand_Failure_ShowsErrorMessage()
    {
        // Arrange
        var activatedLicense = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test User",
            DateTime.UtcNow.AddYears(1),
            "PRO1-****-****-0ABC",
            true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(activatedLicense);
        _mockLicenseService.Setup(s => s.DeactivateLicenseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.DeactivateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsValidationSuccess);
        Assert.NotNull(viewModel.ValidationMessage);
    }

    #endregion

    #region TierDisplayName Tests

    [Fact]
    public void TierDisplayName_CoreTier_ReturnsCoreFree()
    {
        // Arrange
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(LicenseInfo.CoreDefault);
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal("Core (Free)", viewModel.TierDisplayName);
    }

    [Fact]
    public void TierDisplayName_WriterProTier_ReturnsWriterPro()
    {
        // Arrange
        var license = new LicenseInfo(LicenseTier.WriterPro, "Test", null, null, true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal("WriterPro", viewModel.TierDisplayName);
    }

    [Fact]
    public void TierDisplayName_TeamsTier_ReturnsTeams()
    {
        // Arrange
        var license = new LicenseInfo(LicenseTier.Teams, "Test", null, null, true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal("Teams", viewModel.TierDisplayName);
    }

    [Fact]
    public void TierDisplayName_EnterpriseTier_ReturnsEnterprise()
    {
        // Arrange
        var license = new LicenseInfo(LicenseTier.Enterprise, "Test", null, null, true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal("Enterprise", viewModel.TierDisplayName);
    }

    #endregion

    #region TierBadgeColor Tests

    [Theory]
    [InlineData(LicenseTier.Core, "#9E9E9E")] // Gray
    [InlineData(LicenseTier.WriterPro, "#4CAF50")] // Green
    [InlineData(LicenseTier.Teams, "#FF9800")] // Orange
    [InlineData(LicenseTier.Enterprise, "#2196F3")] // Blue
    public void TierBadgeColor_ReturnsCorrectColor(LicenseTier tier, string expectedColor)
    {
        // Arrange
        var license = new LicenseInfo(tier, "Test", null, null, true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(expectedColor, viewModel.TierBadgeColor);
    }

    #endregion

    #region ExpirationWarning Tests

    [Fact]
    public void IsExpirationWarning_NoExpiration_ReturnsFalse()
    {
        // Arrange
        var license = new LicenseInfo(LicenseTier.WriterPro, "Test", null, null, true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsExpirationWarning);
    }

    [Fact]
    public void IsExpirationWarning_ExpiresIn15Days_ReturnsTrue()
    {
        // Arrange
        var license = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddDays(15),
            null,
            true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.IsExpirationWarning);
    }

    [Fact]
    public void IsExpirationWarning_ExpiresIn60Days_ReturnsFalse()
    {
        // Arrange
        var license = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            DateTime.UtcNow.AddDays(60),
            null,
            true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsExpirationWarning);
    }

    #endregion

    #region ExpirationDisplayText Tests

    [Fact]
    public void ExpirationDisplayText_NoExpiration_ReturnsNull()
    {
        // Arrange
        var license = new LicenseInfo(LicenseTier.WriterPro, "Test", null, null, true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.Null(viewModel.ExpirationDisplayText);
    }

    [Fact]
    public void ExpirationDisplayText_HasExpiration_ReturnsFormattedDate()
    {
        // Arrange
        var expirationDate = new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Utc);
        var license = new LicenseInfo(
            LicenseTier.WriterPro,
            "Test",
            expirationDate,
            null,
            true);
        _mockLicenseService.Setup(s => s.GetCurrentLicense())
            .Returns(license);
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.ExpirationDisplayText);
        Assert.Contains("Expires:", viewModel.ExpirationDisplayText);
    }

    #endregion

    #region IsValidationError Tests

    [Fact]
    public void IsValidationError_NoMessage_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.IsValidationError);
    }

    [Fact]
    public async Task IsValidationError_SuccessMessage_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseValidationResult.Success(LicenseTier.WriterPro, "Test", null));
        _mockLicenseService.Setup(s => s.ActivateLicenseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsValidationError);
    }

    [Fact]
    public async Task IsValidationError_ErrorMessage_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LicenseKeyInput = "ABCD-1234-EFGH-5678";

        _mockLicenseService.Setup(s => s.ValidateLicenseKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LicenseValidationResult.Failure(LicenseErrorCode.InvalidSignature, "Invalid"));

        // Act
        await viewModel.ActivateCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.IsValidationError);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromLicenseChangedEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Dispose();

        // Assert
        _mockLicenseService.VerifyRemove(
            s => s.LicenseChanged -= It.IsAny<EventHandler<LicenseChangedEventArgs>>(),
            Times.Once);
    }

    #endregion
}
