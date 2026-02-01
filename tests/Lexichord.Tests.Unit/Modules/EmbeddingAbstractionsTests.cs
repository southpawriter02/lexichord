// =============================================================================
// File: EmbeddingAbstractionsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Comprehensive unit tests for v0.4.4a embedding abstractions.
//              Tests EmbeddingOptions, EmbeddingResult, and EmbeddingException.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Xunit;
using FluentAssertions;

namespace Lexichord.Tests.Unit.Modules;

/// <summary>
/// Unit tests for embedding abstractions: EmbeddingOptions, EmbeddingResult, and EmbeddingException.
/// </summary>
/// <remarks>
/// Introduced in v0.4.4a as part of the Embedding Abstractions layer.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4a")]
public class EmbeddingAbstractionsTests
{
    #region EmbeddingOptions Tests

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Default_HasCorrectValues()
    {
        // Arrange & Act
        var options = EmbeddingOptions.Default;

        // Assert
        options.Should().NotBeNull();
        options.Model.Should().Be("text-embedding-3-small");
        options.MaxTokens.Should().Be(8191);
        options.Dimensions.Should().Be(1536);
        options.Normalize.Should().BeTrue();
        options.MaxBatchSize.Should().Be(100);
        options.TimeoutSeconds.Should().Be(60);
        options.MaxRetries.Should().Be(3);
        options.ApiBaseUrl.Should().BeNull();
        options.SecretKeyName.Should().Be("openai:api-key");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForInvalidModel()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { Model = "" };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Model")
            .WithMessage("*Model cannot be null or empty*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForNullModel()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { Model = null! };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Model")
            .WithMessage("*Model cannot be null or empty*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForInvalidDimensions()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { Dimensions = 0 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Dimensions")
            .WithMessage("*Dimensions must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForNegativeDimensions()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { Dimensions = -100 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("Dimensions")
            .WithMessage("*Dimensions must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForInvalidMaxTokens()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxTokens = 0 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxTokens")
            .WithMessage("*MaxTokens must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForNegativeMaxTokens()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxTokens = -1 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxTokens")
            .WithMessage("*MaxTokens must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForInvalidBatchSize()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxBatchSize = 0 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxBatchSize")
            .WithMessage("*MaxBatchSize must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForNegativeBatchSize()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxBatchSize = -50 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxBatchSize")
            .WithMessage("*MaxBatchSize must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForInvalidTimeoutSeconds()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { TimeoutSeconds = 0 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("TimeoutSeconds")
            .WithMessage("*TimeoutSeconds must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForNegativeTimeoutSeconds()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { TimeoutSeconds = -30 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("TimeoutSeconds")
            .WithMessage("*TimeoutSeconds must be positive*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_ThrowsForNegativeRetries()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxRetries = -1 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRetries")
            .WithMessage("*MaxRetries cannot be negative*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_SucceedsWithZeroRetries()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxRetries = 0 };

        // Act & Assert
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_Validate_SucceedsWithValidConfiguration()
    {
        // Arrange
        var options = EmbeddingOptions.Default;

        // Act & Assert
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingOptions_CanModifyViaInit()
    {
        // Arrange & Act
        var options = EmbeddingOptions.Default with
        {
            Model = "text-embedding-3-large",
            Dimensions = 3072,
            MaxBatchSize = 50
        };

        // Assert
        options.Model.Should().Be("text-embedding-3-large");
        options.Dimensions.Should().Be(3072);
        options.MaxBatchSize.Should().Be(50);
        // Verify other fields remain unchanged
        options.MaxTokens.Should().Be(8191);
        options.Normalize.Should().BeTrue();
    }

    #endregion

    #region EmbeddingResult.Ok Tests

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_CreatesSuccessfulResult()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var tokenCount = 100;
        var originalLength = 500;
        var wasTruncated = false;
        var latencyMs = 150L;

        // Act
        var result = EmbeddingResult.Ok(embedding, tokenCount, originalLength, wasTruncated, latencyMs);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Embedding.Should().BeSameAs(embedding);
        result.TokenCount.Should().Be(tokenCount);
        result.OriginalLength.Should().Be(originalLength);
        result.WasTruncated.Should().Be(wasTruncated);
        result.LatencyMs.Should().Be(latencyMs);
        result.RetryCount.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_WithRetryCount_PreservesValue()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f };
        var retryCount = 2;

        // Act
        var result = EmbeddingResult.Ok(embedding, 50, 200, false, 200L, retryCount);

        // Assert
        result.RetryCount.Should().Be(retryCount);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_WithTruncation_FlagsIt()
    {
        // Arrange
        var embedding = new float[] { 0.5f };

        // Act
        var result = EmbeddingResult.Ok(embedding, 100, 2000, true, 300L);

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_ThrowsForNullEmbedding()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Ok(null!, 50, 200, false, 100L);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("embedding");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_ThrowsForNegativeTokenCount()
    {
        // Arrange
        var embedding = new float[] { 0.1f };

        // Act & Assert
        var act = () => EmbeddingResult.Ok(embedding, -1, 200, false, 100L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tokenCount")
            .WithMessage("*tokenCount cannot be negative*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_ThrowsForNegativeOriginalLength()
    {
        // Arrange
        var embedding = new float[] { 0.1f };

        // Act & Assert
        var act = () => EmbeddingResult.Ok(embedding, 50, -1, false, 100L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("originalLength")
            .WithMessage("*originalLength cannot be negative*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_ThrowsForNegativeLatency()
    {
        // Arrange
        var embedding = new float[] { 0.1f };

        // Act & Assert
        var act = () => EmbeddingResult.Ok(embedding, 50, 200, false, -1L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("latencyMs")
            .WithMessage("*latencyMs cannot be negative*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Ok_ThrowsForNegativeRetryCount()
    {
        // Arrange
        var embedding = new float[] { 0.1f };

        // Act & Assert
        var act = () => EmbeddingResult.Ok(embedding, 50, 200, false, 100L, -1);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("retryCount")
            .WithMessage("*retryCount cannot be negative*");
    }

    #endregion

    #region EmbeddingResult.Fail Tests

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_CreatesFailedResult()
    {
        // Arrange
        var errorMessage = "API key not found";
        var originalLength = 500;
        var latencyMs = 50L;

        // Act
        var result = EmbeddingResult.Fail(errorMessage, originalLength, latencyMs);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Embedding.Should().BeNull();
        result.TokenCount.Should().Be(0);
        result.ErrorMessage.Should().Be(errorMessage);
        result.OriginalLength.Should().Be(originalLength);
        result.LatencyMs.Should().Be(latencyMs);
        result.RetryCount.Should().Be(0);
        result.WasTruncated.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_WithRetryCount_PreservesValue()
    {
        // Arrange
        var retryCount = 3;

        // Act
        var result = EmbeddingResult.Fail("Timeout", 200, 500L, retryCount);

        // Assert
        result.RetryCount.Should().Be(retryCount);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_ThrowsForNullErrorMessage()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Fail(null!, 200, 100L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage")
            .WithMessage("*errorMessage cannot be null or empty*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_ThrowsForEmptyErrorMessage()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Fail("", 200, 100L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage")
            .WithMessage("*errorMessage cannot be null or empty*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_ThrowsForWhitespaceErrorMessage()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Fail("   ", 200, 100L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage")
            .WithMessage("*errorMessage cannot be null or empty*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_ThrowsForNegativeOriginalLength()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Fail("Error", -1, 100L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("originalLength")
            .WithMessage("*originalLength cannot be negative*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_ThrowsForNegativeLatency()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Fail("Error", 200, -1L);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("latencyMs")
            .WithMessage("*latencyMs cannot be negative*");
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingResult_Fail_ThrowsForNegativeRetryCount()
    {
        // Act & Assert
        var act = () => EmbeddingResult.Fail("Error", 200, 100L, -1);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("retryCount")
            .WithMessage("*retryCount cannot be negative*");
    }

    #endregion

    #region EmbeddingException Tests

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_PreservesStatusCode()
    {
        // Arrange
        var statusCode = 401;
        var message = "Unauthorized";

        // Act
        var ex = new EmbeddingException(message, statusCode, isTransient: false);

        // Assert
        ex.Message.Should().Be(message);
        ex.StatusCode.Should().Be(statusCode);
        ex.IsTransient.Should().BeFalse();
        ex.RetryCount.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_PreservesRetryCount()
    {
        // Arrange
        var retryCount = 3;

        // Act
        var ex = new EmbeddingException("Error", 500, isTransient: true, retryCount);

        // Assert
        ex.RetryCount.Should().Be(retryCount);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_PreservesIsTransient()
    {
        // Act & Assert
        var transientEx = new EmbeddingException("Rate limit", 429, isTransient: true);
        transientEx.IsTransient.Should().BeTrue();

        var permanentEx = new EmbeddingException("Bad request", 400, isTransient: false);
        permanentEx.IsTransient.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_SimpleConstructor_HasDefaults()
    {
        // Act
        var ex = new EmbeddingException("Something went wrong");

        // Assert
        ex.Message.Should().Be("Something went wrong");
        ex.StatusCode.Should().Be(0);
        ex.IsTransient.Should().BeFalse();
        ex.RetryCount.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_WithInnerException_PreservesContext()
    {
        // Arrange
        var innerEx = new HttpRequestException("Network error");

        // Act
        var ex = new EmbeddingException("Embedding failed", innerEx);

        // Assert
        ex.InnerException.Should().BeSameAs(innerEx);
        ex.Message.Should().Be("Embedding failed");
        ex.StatusCode.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_WithAllParameters_HasCompleteInformation()
    {
        // Arrange
        var innerEx = new TimeoutException("Request timeout");

        // Act
        var ex = new EmbeddingException(
            "API request timeout after 3 retries",
            statusCode: 408,
            isTransient: true,
            retryCount: 3,
            innerException: innerEx);

        // Assert
        ex.Message.Should().Be("API request timeout after 3 retries");
        ex.StatusCode.Should().Be(408);
        ex.IsTransient.Should().BeTrue();
        ex.RetryCount.Should().Be(3);
        ex.InnerException.Should().BeSameAs(innerEx);
    }

    [Fact]
    [Trait("Feature", "v0.4.4a")]
    public void EmbeddingException_IsExceptionDerived()
    {
        // Act
        var ex = new EmbeddingException("Test");

        // Assert
        ex.Should().BeAssignableTo<Exception>();
    }

    #endregion
}
