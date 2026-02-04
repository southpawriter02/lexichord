// =============================================================================
// File: ClaimRepositoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ClaimRepository.
// =============================================================================
// LOGIC: Tests constructor validation and static mapping methods.
//   Database operations require integration tests with actual PostgreSQL.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Modules.Knowledge.Claims.Repository;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="ClaimRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6h")]
public class ClaimRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<ClaimRepository>> _mockLogger;

    public ClaimRepositoryTests()
    {
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockCache = new Mock<IMemoryCache>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<ClaimRepository>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var repository = new ClaimRepository(
            _mockConnectionFactory.Object,
            _mockCache.Object,
            _mockMediator.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ClaimRepository(
            null!,
            _mockCache.Object,
            _mockMediator.Object,
            _mockLogger.Object));

        Assert.Equal("connectionFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ClaimRepository(
            _mockConnectionFactory.Object,
            null!,
            _mockMediator.Object,
            _mockLogger.Object));

        Assert.Equal("cache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ClaimRepository(
            _mockConnectionFactory.Object,
            _mockCache.Object,
            null!,
            _mockLogger.Object));

        Assert.Equal("mediator", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ClaimRepository(
            _mockConnectionFactory.Object,
            _mockCache.Object,
            _mockMediator.Object,
            null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region ClaimSearchCriteria Tests

    [Fact]
    public void ClaimSearchCriteria_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var criteria = new ClaimSearchCriteria();

        // Assert
        Assert.Equal(0, criteria.Page);
        Assert.Equal(50, criteria.PageSize);
        Assert.Equal(ClaimSortField.ExtractedAt, criteria.SortBy);
        Assert.Equal(SortDirection.Descending, criteria.SortDirection);
        Assert.Null(criteria.ProjectId);
        Assert.Null(criteria.DocumentId);
    }

    [Fact]
    public void ClaimSearchCriteria_SkipOverridesPageCalculation()
    {
        // Arrange
        var criteria = new ClaimSearchCriteria
        {
            Page = 5,
            PageSize = 10,
            Skip = 100
        };

        // Assert - Skip should override Page * PageSize
        Assert.Equal(100, criteria.Skip);
    }

    [Fact]
    public void ClaimSearchCriteria_TakeOverridesPageSize()
    {
        // Arrange
        var criteria = new ClaimSearchCriteria
        {
            PageSize = 50,
            Take = 25
        };

        // Assert - Take should override PageSize
        Assert.Equal(25, criteria.Take);
    }

    [Theory]
    [InlineData(ClaimSortField.ExtractedAt)]
    [InlineData(ClaimSortField.Confidence)]
    [InlineData(ClaimSortField.Predicate)]
    [InlineData(ClaimSortField.ValidationStatus)]
    public void ClaimSearchCriteria_SortBy_AcceptsAllFields(ClaimSortField field)
    {
        // Arrange & Act
        var criteria = new ClaimSearchCriteria { SortBy = field };

        // Assert
        Assert.Equal(field, criteria.SortBy);
    }

    #endregion

    #region ClaimQueryResult Tests

    [Fact]
    public void ClaimQueryResult_Empty_HasCorrectDefaults()
    {
        // Arrange & Act
        var result = ClaimQueryResult.Empty;

        // Assert
        Assert.Empty(result.Claims);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public void ClaimQueryResult_TotalPages_CalculatedCorrectly()
    {
        // Arrange
        var result = new ClaimQueryResult
        {
            Claims = Array.Empty<Claim>(),
            TotalCount = 103,
            PageSize = 10
        };

        // Assert - 103/10 = 10.3, should ceil to 11
        Assert.Equal(11, result.TotalPages);
    }

    [Fact]
    public void ClaimQueryResult_TotalPages_ZeroCount_ReturnsZero()
    {
        // Arrange
        var result = new ClaimQueryResult
        {
            Claims = Array.Empty<Claim>(),
            TotalCount = 0,
            PageSize = 10
        };

        // Assert
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ClaimQueryResult_HasMore_TrueWhenMorePages()
    {
        // Arrange
        var result = new ClaimQueryResult
        {
            Claims = Array.Empty<Claim>(),
            TotalCount = 100,
            Page = 0,
            PageSize = 50
        };

        // Assert
        Assert.True(result.HasMore);
    }

    [Fact]
    public void ClaimQueryResult_HasMore_FalseOnLastPage()
    {
        // Arrange
        var result = new ClaimQueryResult
        {
            Claims = Array.Empty<Claim>(),
            TotalCount = 50,
            Page = 0,
            PageSize = 50
        };

        // Assert
        Assert.False(result.HasMore);
    }

    [Fact]
    public void ClaimQueryResult_HasPrevious_FalseOnFirstPage()
    {
        // Arrange
        var result = new ClaimQueryResult
        {
            Claims = Array.Empty<Claim>(),
            TotalCount = 100,
            Page = 0,
            PageSize = 50
        };

        // Assert
        Assert.False(result.HasPrevious);
    }

    [Fact]
    public void ClaimQueryResult_HasPrevious_TrueOnLaterPages()
    {
        // Arrange
        var result = new ClaimQueryResult
        {
            Claims = Array.Empty<Claim>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 50
        };

        // Assert
        Assert.True(result.HasPrevious);
    }

    #endregion

    #region BulkUpsertResult Tests

    [Fact]
    public void BulkUpsertResult_Empty_HasZeroCounts()
    {
        // Arrange & Act
        var result = BulkUpsertResult.Empty;

        // Assert
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(0, result.UnchangedCount);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void BulkUpsertResult_TotalProcessed_SumsAllCounts()
    {
        // Arrange
        var result = new BulkUpsertResult
        {
            CreatedCount = 10,
            UpdatedCount = 5,
            UnchangedCount = 3
        };

        // Assert
        Assert.Equal(18, result.TotalProcessed);
    }

    [Fact]
    public void BulkUpsertResult_AllSucceeded_FalseWhenErrorsPresent()
    {
        // Arrange
        var result = new BulkUpsertResult
        {
            Errors = new List<ClaimUpsertError>
            {
                new() { ClaimId = Guid.NewGuid(), Error = "Test error" }
            }
        };

        // Assert
        Assert.False(result.AllSucceeded);
    }

    [Fact]
    public void BulkUpsertResult_AllSucceeded_TrueWhenNoErrors()
    {
        // Arrange
        var result = new BulkUpsertResult();

        // Assert
        Assert.True(result.AllSucceeded);
    }

    #endregion

    #region ClaimRow Mapping Tests

    [Fact]
    public void ClaimRow_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var row = new ClaimRow();

        // Assert
        Assert.True(row.IsActive);
        Assert.Equal(1, row.Version);
        Assert.Equal("Pending", row.ValidationStatus);
    }

    #endregion

    #region Events Tests

    [Fact]
    public void ClaimCreatedEvent_HasTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = new Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events.ClaimCreatedEvent
        {
            Claim = CreateTestClaim()
        };

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.InRange(evt.Timestamp, before, after);
    }

    [Fact]
    public void ClaimUpdatedEvent_IncludesBothStates()
    {
        // Arrange
        var oldClaim = CreateTestClaim();
        var newClaim = CreateTestClaim() with { Confidence = 0.95f };

        // Act
        var evt = new Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events.ClaimUpdatedEvent
        {
            Claim = newClaim,
            PreviousClaim = oldClaim
        };

        // Assert
        Assert.Equal(0.95f, evt.Claim.Confidence);
        Assert.Equal(0.85f, evt.PreviousClaim.Confidence);
    }

    [Fact]
    public void ClaimDeletedEvent_HasClaimAndDocumentId()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // Act
        var evt = new Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events.ClaimDeletedEvent
        {
            ClaimId = claimId,
            DocumentId = documentId
        };

        // Assert
        Assert.Equal(claimId, evt.ClaimId);
        Assert.Equal(documentId, evt.DocumentId);
    }

    #endregion

    #region Helper Methods

    private static Claim CreateTestClaim()
    {
        return new Claim
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            Subject = new ClaimEntity
            {
                EntityType = "Method",
                SurfaceForm = "GetUser",
                NormalizedForm = "getuser",
                StartOffset = 0,
                EndOffset = 7
            },
            Predicate = ClaimPredicate.ACCEPTS,
            Object = ClaimObject.FromEntity(new ClaimEntity
            {
                EntityType = "Parameter",
                SurfaceForm = "userId",
                NormalizedForm = "userid"
            }),
            Confidence = 0.85f,
            Evidence = new ClaimEvidence
            {
                Sentence = "GetUser accepts a userId parameter.",
                StartOffset = 0,
                EndOffset = 35,
                ExtractionMethod = ClaimExtractionMethod.PatternRule,
                PatternId = "method-accepts-parameter"
            }
        };
    }

    #endregion
}
