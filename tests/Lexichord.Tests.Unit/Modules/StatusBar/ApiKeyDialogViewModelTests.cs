using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Modules.StatusBar.Services;
using Lexichord.Modules.StatusBar.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for ApiKeyDialogViewModel.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the API key dialog ViewModel correctly:
/// - Stores keys when input is valid
/// - Shows error when input is empty
/// - Shows error when storage fails
/// - Closes with false on cancel
/// </remarks>
public class ApiKeyDialogViewModelTests
{
    private readonly IVaultStatusService _vaultStatusService;
    private readonly ILogger _logger;
    private readonly ApiKeyDialogViewModel _sut;

    public ApiKeyDialogViewModelTests()
    {
        _vaultStatusService = Substitute.For<IVaultStatusService>();
        _logger = Substitute.For<ILogger>();

        _sut = new ApiKeyDialogViewModel(_vaultStatusService, _logger);
    }

    #region SaveCommand Tests

    [Fact]
    public async Task SaveCommand_StoresKey_WhenInputValid()
    {
        // Arrange
        _vaultStatusService.StoreApiKeyAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        bool? closeResult = null;
        _sut.CloseRequested += (_, result) => closeResult = result;
        _sut.ApiKey = "sk-test-key-12345";

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        await _vaultStatusService.Received(1)
            .StoreApiKeyAsync(VaultKeys.TestApiKey, "sk-test-key-12345");
        closeResult.Should().BeTrue();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_ShowsError_WhenInputEmpty()
    {
        // Arrange
        _sut.ApiKey = "";

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Be("Please enter an API key");
        await _vaultStatusService.DidNotReceive()
            .StoreApiKeyAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SaveCommand_ShowsError_WhenInputWhitespace()
    {
        // Arrange
        _sut.ApiKey = "   ";

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Be("Please enter an API key");
    }

    [Fact]
    public async Task SaveCommand_ShowsError_WhenStorageFails()
    {
        // Arrange
        _vaultStatusService.StoreApiKeyAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        _sut.ApiKey = "sk-test-key-12345";

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Be("Failed to store key in vault");
    }

    [Fact]
    public async Task SaveCommand_ShowsError_WhenExceptionThrown()
    {
        // Arrange
        _vaultStatusService.StoreApiKeyAsync(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("Vault error"));

        _sut.ApiKey = "sk-test-key-12345";

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("Failed to save key");
    }

    [Fact]
    public async Task SaveCommand_SetsIsSaving_DuringOperation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _vaultStatusService.StoreApiKeyAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(tcs.Task);

        _sut.ApiKey = "sk-test-key-12345";

        // Act
        var saveTask = _sut.SaveCommand.ExecuteAsync(null);

        // Assert - IsSaving should be true during operation
        _sut.IsSaving.Should().BeTrue();

        // Complete the operation
        tcs.SetResult(true);
        await saveTask;

        // Assert - IsSaving should be false after operation
        _sut.IsSaving.Should().BeFalse();
    }

    #endregion

    #region CancelCommand Tests

    [Fact]
    public void CancelCommand_ClosesWithFalse()
    {
        // Arrange
        bool? closeResult = null;
        _sut.CloseRequested += (_, result) => closeResult = result;

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        closeResult.Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ApiKey_IsEmptyByDefault()
    {
        _sut.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void HasError_IsFalseByDefault()
    {
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public void IsSaving_IsFalseByDefault()
    {
        _sut.IsSaving.Should().BeFalse();
    }

    #endregion
}
