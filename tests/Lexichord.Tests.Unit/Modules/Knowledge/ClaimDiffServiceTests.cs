// =============================================================================
// File: ClaimDiffServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Claim Diff Service.
// =============================================================================
// LOGIC: Tests data contracts, DiffStats, ClaimDiffResult, ClaimChange,
//   ClaimModification, SemanticMatcher, and ClaimDiffService logic.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Modules.Knowledge.Claims.Diff;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="ClaimDiffService"/> and related types.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6i")]
public class ClaimDiffServiceTests
{
    private readonly Mock<IClaimRepository> _mockRepository;
    private readonly Mock<ILogger<ClaimDiffService>> _mockLogger;
    private readonly ClaimDiffService _service;
    private readonly SemanticMatcher _matcher;

    public ClaimDiffServiceTests()
    {
        _mockRepository = new Mock<IClaimRepository>();
        _mockLogger = new Mock<ILogger<ClaimDiffService>>();
        _service = new ClaimDiffService(_mockRepository.Object, _mockLogger.Object);
        _matcher = new SemanticMatcher();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act
        var service = new ClaimDiffService(
            _mockRepository.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ClaimDiffService(
            null!,
            _mockLogger.Object));

        Assert.Equal("repository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ClaimDiffService(
            _mockRepository.Object,
            null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region DiffOptions Tests

    [Fact]
    public void DiffOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DiffOptions();

        // Assert
        Assert.True(options.UseSemanticMatching);
        Assert.Equal(0.8f, options.SemanticMatchThreshold);
        Assert.Equal(0.05f, options.IgnoreConfidenceChangeBelow);
        Assert.True(options.TrackValidationChanges);
        Assert.True(options.IncludeEvidence);
        Assert.True(options.GroupRelatedChanges);
    }

    [Fact]
    public void DiffOptions_Default_HasRecommendedSettings()
    {
        // Act
        var options = DiffOptions.Default;

        // Assert
        Assert.True(options.UseSemanticMatching);
        Assert.Equal(0.8f, options.SemanticMatchThreshold);
    }

    [Fact]
    public void DiffOptions_Strict_DisablesSemanticMatching()
    {
        // Act
        var options = DiffOptions.Strict;

        // Assert
        Assert.False(options.UseSemanticMatching);
        Assert.Equal(0.0f, options.IgnoreConfidenceChangeBelow);
    }

    [Fact]
    public void DiffOptions_Lenient_HasLowerThreshold()
    {
        // Act
        var options = DiffOptions.Lenient;

        // Assert
        Assert.Equal(0.6f, options.SemanticMatchThreshold);
        Assert.Equal(0.1f, options.IgnoreConfidenceChangeBelow);
    }

    #endregion

    #region DiffStats Tests

    [Fact]
    public void DiffStats_Empty_HasZeroCounts()
    {
        // Act
        var stats = DiffStats.Empty;

        // Assert
        Assert.Equal(0, stats.AddedCount);
        Assert.Equal(0, stats.RemovedCount);
        Assert.Equal(0, stats.ModifiedCount);
        Assert.Equal(0, stats.UnchangedCount);
        Assert.Equal(0, stats.TotalChanges);
    }

    [Fact]
    public void DiffStats_TotalChanges_SumsCorrectly()
    {
        // Arrange
        var stats = new DiffStats
        {
            AddedCount = 3,
            RemovedCount = 2,
            ModifiedCount = 5,
            UnchangedCount = 10
        };

        // Assert
        Assert.Equal(10, stats.TotalChanges); // 3 + 2 + 5
    }

    #endregion

    #region ClaimDiffResult Tests

    [Fact]
    public void ClaimDiffResult_Empty_HasEmptyLists()
    {
        // Act
        var result = ClaimDiffResult.Empty;

        // Assert
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Empty(result.Modified);
        Assert.Empty(result.Unchanged);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public void ClaimDiffResult_NoChanges_CreatesUnchangedResult()
    {
        // Arrange
        var claims = new[] { CreateTestClaim() };

        // Act
        var result = ClaimDiffResult.NoChanges(claims);

        // Assert
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Empty(result.Modified);
        Assert.Single(result.Unchanged);
        Assert.False(result.HasChanges);
        Assert.Equal(1, result.OldClaimCount);
        Assert.Equal(1, result.NewClaimCount);
    }

    [Fact]
    public void ClaimDiffResult_HasChanges_TrueWhenAdded()
    {
        // Arrange
        var result = new ClaimDiffResult
        {
            Added = new[] { CreateClaimChange(ClaimChangeType.Added) },
            Removed = Array.Empty<ClaimChange>(),
            Modified = Array.Empty<ClaimModification>(),
            Unchanged = Array.Empty<Claim>()
        };

        // Assert
        Assert.True(result.HasChanges);
    }

    [Fact]
    public void ClaimDiffResult_HasChanges_TrueWhenRemoved()
    {
        // Arrange
        var result = new ClaimDiffResult
        {
            Added = Array.Empty<ClaimChange>(),
            Removed = new[] { CreateClaimChange(ClaimChangeType.Removed) },
            Modified = Array.Empty<ClaimModification>(),
            Unchanged = Array.Empty<Claim>()
        };

        // Assert
        Assert.True(result.HasChanges);
    }

    [Fact]
    public void ClaimDiffResult_HasChanges_TrueWhenModified()
    {
        // Arrange
        var result = new ClaimDiffResult
        {
            Added = Array.Empty<ClaimChange>(),
            Removed = Array.Empty<ClaimChange>(),
            Modified = new[] { CreateClaimModification() },
            Unchanged = Array.Empty<Claim>()
        };

        // Assert
        Assert.True(result.HasChanges);
    }

    #endregion

    #region ClaimDiffService.Diff Tests

    [Fact]
    public void Diff_EmptyBothLists_ReturnsEmptyResult()
    {
        // Arrange
        var oldClaims = Array.Empty<Claim>();
        var newClaims = Array.Empty<Claim>();

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Empty(result.Modified);
        Assert.Empty(result.Unchanged);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public void Diff_IdenticalClaims_ReturnsNoChanges()
    {
        // Arrange
        var claim = CreateTestClaim();
        var oldClaims = new[] { claim };
        var newClaims = new[] { claim };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Empty(result.Modified);
        Assert.Single(result.Unchanged);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public void Diff_AddedClaim_DetectsAddition()
    {
        // Arrange
        var oldClaims = Array.Empty<Claim>();
        var newClaims = new[] { CreateTestClaim() };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        Assert.Single(result.Added);
        Assert.Empty(result.Removed);
        Assert.Empty(result.Modified);
        Assert.Equal(ClaimChangeType.Added, result.Added[0].ChangeType);
    }

    [Fact]
    public void Diff_RemovedClaim_DetectsRemoval()
    {
        // Arrange
        var oldClaims = new[] { CreateTestClaim() };
        var newClaims = Array.Empty<Claim>();

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        Assert.Empty(result.Added);
        Assert.Single(result.Removed);
        Assert.Empty(result.Modified);
        Assert.Equal(ClaimChangeType.Removed, result.Removed[0].ChangeType);
    }

    [Fact]
    public void Diff_ModifiedConfidence_DetectsModification()
    {
        // Arrange
        var id = Guid.NewGuid();
        var oldClaim = CreateTestClaim(id) with { Confidence = 0.5f };
        var newClaim = CreateTestClaim(id) with { Confidence = 0.95f };

        var oldClaims = new[] { oldClaim };
        var newClaims = new[] { newClaim };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Single(result.Modified);
        Assert.Contains(ClaimChangeType.ConfidenceChanged, result.Modified[0].ChangeTypes);
    }

    [Fact]
    public void Diff_SmallConfidenceChange_IgnoredWithDefaultThreshold()
    {
        // Arrange
        var id = Guid.NewGuid();
        var oldClaim = CreateTestClaim(id) with { Confidence = 0.80f };
        var newClaim = CreateTestClaim(id) with { Confidence = 0.82f }; // 2% change, below 5% threshold

        var oldClaims = new[] { oldClaim };
        var newClaims = new[] { newClaim };

        // Act
        var result = _service.Diff(oldClaims, newClaims);

        // Assert - should be unchanged because 2% is below 5% threshold
        Assert.Empty(result.Modified);
        Assert.Single(result.Unchanged);
    }

    [Fact]
    public void Diff_WithGrouping_CreatesGroups()
    {
        // Arrange
        var oldClaims = Array.Empty<Claim>();
        var newClaims = new[] { CreateTestClaim(), CreateTestClaim() };
        var options = new DiffOptions { GroupRelatedChanges = true };

        // Act
        var result = _service.Diff(oldClaims, newClaims, options);

        // Assert
        Assert.NotNull(result.Groups);
        Assert.NotEmpty(result.Groups);
    }

    [Fact]
    public void Diff_WithoutGrouping_NoGroups()
    {
        // Arrange
        var oldClaims = Array.Empty<Claim>();
        var newClaims = new[] { CreateTestClaim() };
        var options = new DiffOptions { GroupRelatedChanges = false };

        // Act
        var result = _service.Diff(oldClaims, newClaims, options);

        // Assert
        Assert.Null(result.Groups);
    }

    #endregion

    #region SemanticMatcher Tests

    [Fact]
    public void JaroWinkler_ExactMatch_Returns1()
    {
        // Act
        var result = SemanticMatcher.JaroWinkler("test", "test");

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void JaroWinkler_EmptyStrings_Returns1()
    {
        // Act
        var result = SemanticMatcher.JaroWinkler("", "");

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void JaroWinkler_OneEmpty_Returns0()
    {
        // Act
        var result = SemanticMatcher.JaroWinkler("test", "");

        // Assert
        Assert.Equal(0.0f, result);
    }

    [Fact]
    public void JaroWinkler_SimilarStrings_ReturnsHighScore()
    {
        // Act
        var result = SemanticMatcher.JaroWinkler("GetUser", "GetUsers");

        // Assert
        Assert.True(result > 0.9f);
    }

    [Fact]
    public void JaroWinkler_DifferentStrings_ReturnsLowScore()
    {
        // Act
        var result = SemanticMatcher.JaroWinkler("abc", "xyz");

        // Assert
        Assert.True(result < 0.5f);
    }

    [Fact]
    public void ComputeSimilarity_SameId_Returns1()
    {
        // Arrange
        var claim = CreateTestClaim();

        // Act
        var result = _matcher.ComputeSimilarity(claim, claim);

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void ComputeSimilarity_IdenticalContent_ReturnsHighScore()
    {
        // Arrange
        var claim1 = CreateTestClaim(Guid.NewGuid());
        var claim2 = CreateTestClaim(Guid.NewGuid()) with
        {
            Subject = claim1.Subject,
            Predicate = claim1.Predicate,
            Object = claim1.Object
        };

        // Act
        var result = _matcher.ComputeSimilarity(claim1, claim2);

        // Assert
        Assert.True(result > 0.95f);
    }

    [Fact]
    public void FindMatch_NoMatch_ReturnsNull()
    {
        // Arrange
        var target = CreateTestClaim();
        var candidates = Array.Empty<Claim>();

        // Act
        var (match, similarity) = _matcher.FindMatch(target, candidates, 0.8f);

        // Assert
        Assert.Null(match);
        Assert.Equal(0f, similarity);
    }

    [Fact]
    public void FindMatch_ExactMatch_ReturnsClaim()
    {
        // Arrange
        var target = CreateTestClaim();
        var candidates = new[] { target };

        // Act
        var (match, similarity) = _matcher.FindMatch(target, candidates, 0.8f);

        // Assert
        Assert.Same(target, match);
        Assert.Equal(1.0f, similarity);
    }

    #endregion

    #region ClaimSnapshot Tests

    [Fact]
    public void ClaimSnapshot_Properties_SetCorrectly()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var claimIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var snapshot = new ClaimSnapshot
        {
            DocumentId = documentId,
            Label = "v1.0",
            ClaimIds = claimIds
        };

        // Assert
        Assert.Equal(documentId, snapshot.DocumentId);
        Assert.Equal("v1.0", snapshot.Label);
        Assert.Equal(2, snapshot.ClaimCount);
        Assert.NotEqual(Guid.Empty, snapshot.Id);
    }

    #endregion

    #region ClaimContradiction Tests

    [Fact]
    public void ClaimContradiction_DefaultStatus_IsOpen()
    {
        // Arrange
        var contradiction = new ClaimContradiction
        {
            ProjectId = Guid.NewGuid(),
            Claim1 = CreateTestClaim(),
            Claim2 = CreateTestClaim(),
            Type = ContradictionType.DirectContradiction,
            Description = "Test contradiction"
        };

        // Assert
        Assert.Equal(ContradictionStatus.Open, contradiction.Status);
        Assert.Equal(1.0f, contradiction.Confidence);
    }

    #endregion

    #region ChangeImpact Tests

    [Fact]
    public void ChangeImpact_Values_OrderedCorrectly()
    {
        // Assert
        Assert.True(ChangeImpact.Low < ChangeImpact.Medium);
        Assert.True(ChangeImpact.Medium < ChangeImpact.High);
        Assert.True(ChangeImpact.High < ChangeImpact.Critical);
    }

    #endregion

    #region Helper Methods

    private static Claim CreateTestClaim(Guid? id = null)
    {
        return new Claim
        {
            Id = id ?? Guid.NewGuid(),
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

    private static ClaimChange CreateClaimChange(ClaimChangeType type)
    {
        return new ClaimChange
        {
            Claim = CreateTestClaim(),
            ChangeType = type,
            Description = $"{type}: test claim",
            Impact = ChangeImpact.Low
        };
    }

    private static ClaimModification CreateClaimModification()
    {
        var claim = CreateTestClaim();
        return new ClaimModification
        {
            OldClaim = claim with { Confidence = 0.5f },
            NewClaim = claim with { Confidence = 0.9f },
            ChangeTypes = new[] { ClaimChangeType.ConfidenceChanged },
            FieldChanges = new[]
            {
                new FieldChange
                {
                    FieldName = "Confidence",
                    OldValue = 0.5f,
                    NewValue = 0.9f,
                    Description = "Confidence changed from 50% to 90%"
                }
            },
            Description = "Confidence changed from 50% to 90%",
            Impact = ChangeImpact.Low
        };
    }

    #endregion
}
