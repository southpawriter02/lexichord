using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Security;

/// <summary>
/// Unit tests for <see cref="SecureVaultException"/> hierarchy.
/// </summary>
[Trait("Category", "Unit")]
public class SecureVaultExceptionTests
{
    [Fact]
    public void SecureVaultException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new SecureVaultException("Test error message");

        // Assert
        ex.Message.Should().Be("Test error message");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void SecureVaultException_WithMessageAndInner_SetsBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var ex = new SecureVaultException("Outer error", inner);

        // Assert
        ex.Message.Should().Be("Outer error");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void SecretNotFoundException_IncludesKeyName()
    {
        // Arrange & Act
        var ex = new SecretNotFoundException("test:api-key");

        // Assert
        ex.KeyName.Should().Be("test:api-key");
        ex.Message.Should().Contain("test:api-key");
    }

    [Fact]
    public void SecretNotFoundException_IsSecureVaultException()
    {
        // Arrange & Act
        var ex = new SecretNotFoundException("test:key");

        // Assert
        ex.Should().BeAssignableTo<SecureVaultException>();
    }

    [Fact]
    public void SecretDecryptionException_IncludesKeyNameAndInnerException()
    {
        // Arrange
        var inner = new Exception("Crypto error");

        // Act
        var ex = new SecretDecryptionException("test:key", inner);

        // Assert
        ex.KeyName.Should().Be("test:key");
        ex.InnerException.Should().Be(inner);
        ex.Message.Should().Contain("test:key");
        ex.Message.Should().Contain("corrupted");
    }

    [Fact]
    public void SecretDecryptionException_IsSecureVaultException()
    {
        // Arrange & Act
        var ex = new SecretDecryptionException("test:key", new Exception());

        // Assert
        ex.Should().BeAssignableTo<SecureVaultException>();
    }

    [Fact]
    public void VaultAccessDeniedException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new VaultAccessDeniedException("Permission denied to /path/to/vault");

        // Assert
        ex.Message.Should().Be("Permission denied to /path/to/vault");
    }

    [Fact]
    public void VaultAccessDeniedException_WithMessageAndInner_SetsBoth()
    {
        // Arrange
        var inner = new UnauthorizedAccessException("Access denied");

        // Act
        var ex = new VaultAccessDeniedException("Cannot access vault", inner);

        // Assert
        ex.Message.Should().Be("Cannot access vault");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void VaultAccessDeniedException_IsSecureVaultException()
    {
        // Arrange & Act
        var ex = new VaultAccessDeniedException("Access denied");

        // Assert
        ex.Should().BeAssignableTo<SecureVaultException>();
    }

    [Fact]
    public void ExceptionHierarchy_CanBeCaughtByBaseType()
    {
        // Arrange
        SecureVaultException? caught = null;

        // Act
        try
        {
            throw new SecretNotFoundException("test:key");
        }
        catch (SecureVaultException ex)
        {
            caught = ex;
        }

        // Assert
        caught.Should().NotBeNull();
        caught.Should().BeOfType<SecretNotFoundException>();
    }
}
