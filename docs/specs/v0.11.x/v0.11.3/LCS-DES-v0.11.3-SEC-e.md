# LCS-DES-113-SEC-e: Data Classification

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-DES-113-SEC-e                                            |
| **Version**      | v0.11.3                                                      |
| **Codename**     | Data Protection & Encryption - Data Classification           |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Security Architect                                           |
| **Module**       | Lexichord.Security.Classification                            |
| **Est. Hours**   | 6                                                            |
| **License Tier** | Core (detection), Teams+ (policy enforcement)                |

---

## 1. Overview

### 1.1 Purpose

The **Data Classification Service** categorizes sensitive information in the knowledge graph and applies protection policies. This foundational service enables field-level encryption, masking, and audit trails for sensitive properties.

### 1.2 Key Responsibilities

1. **Classify entities** by sensitivity level (Public, Internal, Confidential, Restricted, Secret)
2. **Detect sensitive properties** using pattern matching and PII detection
3. **Apply classification policies** to define encryption and masking requirements
4. **Audit access** to classified data
5. **Support auto-detection** of new sensitive properties

### 1.3 Module Location

```
Lexichord.Security.Classification/
├── Abstractions/
│   └── IDataClassificationService.cs
├── Models/
│   ├── DataClassification.cs
│   ├── PropertyClassification.cs
│   └── DetectedSensitiveProperty.cs
└── Implementation/
    ├── DataClassificationService.cs
    └── SensitivityDetector.cs
```

---

## 2. Interface Definitions

### 2.1 IDataClassificationService

```csharp
namespace Lexichord.Security.Classification.Abstractions;

/// <summary>
/// Service for classifying data sensitivity and managing protection policies.
/// </summary>
/// <remarks>
/// LOGIC: Data classification drives all downstream protection mechanisms.
/// Classifications persist and can be updated by administrators.
/// Changes apply retroactively to already-encrypted data (via re-encryption).
/// </remarks>
public interface IDataClassificationService
{
    /// <summary>
    /// Gets the classification for an entity type.
    /// </summary>
    /// <param name="entityType">The entity type name (e.g., "User", "Credential").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The overall classification for the entity type.</returns>
    /// <remarks>
    /// LOGIC: Returns the highest classification level among all properties
    /// for a quick assessment of entity sensitivity.
    /// </remarks>
    Task<DataClassification> GetClassificationAsync(
        string entityType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets classified properties for an entity type.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of property classifications with encryption/masking rules.</returns>
    /// <remarks>
    /// LOGIC: Used by encryption and masking services to determine
    /// which properties need protection and how.
    /// </remarks>
    Task<IReadOnlyList<PropertyClassification>> GetPropertyClassificationsAsync(
        string entityType,
        CancellationToken ct = default);

    /// <summary>
    /// Sets the overall classification for an entity type.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="classification">The sensitivity level.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Sets default classification for all properties.
    /// Specific properties can override via SetPropertyClassificationAsync.
    /// </remarks>
    Task SetClassificationAsync(
        string entityType,
        DataClassification classification,
        CancellationToken ct = default);

    /// <summary>
    /// Sets classification for a specific property.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="classification">The sensitivity level for this property.</param>
    /// <param name="requiresEncryption">Whether to encrypt this property at rest.</param>
    /// <param name="requiresMasking">Whether to mask this property in UI.</param>
    /// <param name="maskingType">The masking strategy if required.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetPropertyClassificationAsync(
        string entityType,
        string propertyName,
        DataClassification classification,
        bool requiresEncryption,
        bool requiresMasking,
        MaskingType maskingType,
        CancellationToken ct = default);

    /// <summary>
    /// Auto-detects sensitive properties in an entity instance.
    /// </summary>
    /// <param name="entityId">The entity instance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of detected sensitive properties with confidence scores.</returns>
    /// <remarks>
    /// LOGIC: Uses PII detector and pattern matching to find candidate properties.
    /// Returns high-confidence detections for admin review.
    /// </remarks>
    Task<IReadOnlyList<DetectedSensitiveProperty>> DetectSensitivePropertiesAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets classification metadata for audit purposes.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Classification metadata including last changed date and policy version.</returns>
    Task<ClassificationMetadata> GetMetadataAsync(
        string entityType,
        CancellationToken ct = default);

    /// <summary>
    /// Exports classification policy as JSON.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>JSON representation of all classifications.</returns>
    Task<string> ExportPolicyAsync(CancellationToken ct = default);

    /// <summary>
    /// Imports classification policy from JSON.
    /// </summary>
    /// <param name="policyJson">JSON policy document.</param>
    /// <param name="mergeWithExisting">If true, merges; if false, replaces.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ImportPolicyAsync(string policyJson, bool mergeWithExisting = true, CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 DataClassification Enum

```csharp
namespace Lexichord.Security.Classification.Models;

/// <summary>
/// Sensitivity levels for data classification.
/// </summary>
/// <remarks>
/// LOGIC: Classification levels form a hierarchy.
/// Higher levels require more protection.
/// Secret > Restricted > Confidential > Internal > Public
/// </remarks>
public enum DataClassification
{
    /// <summary>
    /// No restrictions. Can be shared freely.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Internal use only. No encryption required.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Business sensitive. Recommended encryption for certain properties.
    /// </summary>
    Confidential = 2,

    /// <summary>
    /// Highly sensitive. PII, credentials. Requires encryption and masking.
    /// </summary>
    Restricted = 3,

    /// <summary>
    /// Maximum protection. Encryption mandatory, audit all access.
    /// </summary>
    Secret = 4
}
```

### 3.2 PropertyClassification Record

```csharp
namespace Lexichord.Security.Classification.Models;

/// <summary>
/// Classification rules for an individual property.
/// </summary>
/// <remarks>
/// LOGIC: Properties can have different classifications than their parent entity.
/// For example, a User entity (Internal) may have email (Restricted) and ssn (Secret).
/// </remarks>
public record PropertyClassification
{
    /// <summary>
    /// The property name.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The sensitivity classification.
    /// </summary>
    public DataClassification Classification { get; init; } = DataClassification.Public;

    /// <summary>
    /// Whether this property should be encrypted at rest.
    /// </summary>
    public bool RequiresEncryption { get; init; } = false;

    /// <summary>
    /// Whether this property should be masked for display.
    /// </summary>
    public bool RequiresMasking { get; init; } = false;

    /// <summary>
    /// The masking strategy if RequiresMasking is true.
    /// </summary>
    public MaskingType MaskingType { get; init; } = MaskingType.Full;

    /// <summary>
    /// Whether access to this property should be audited.
    /// </summary>
    public bool RequiresAuditOnAccess { get; init; } = false;

    /// <summary>
    /// Custom encryption key for this property (if null, uses default for entity).
    /// </summary>
    public string? CustomEncryptionKeyPurpose { get; init; } = null;

    /// <summary>
    /// When this classification was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### 3.3 MaskingType Enum

```csharp
namespace Lexichord.Security.Classification.Models;

/// <summary>
/// Strategies for masking sensitive data in UI.
/// </summary>
/// <remarks>
/// LOGIC: Each type applies a specific pattern to hide sensitive information
/// while preserving enough context for identification.
/// </remarks>
public enum MaskingType
{
    /// <summary>
    /// Full masking: ******** (all characters replaced)
    /// </summary>
    Full,

    /// <summary>
    /// Partial masking: ****5678 (keep last 4 chars)
    /// </summary>
    Partial,

    /// <summary>
    /// Email masking: j***@***.com
    /// </summary>
    Email,

    /// <summary>
    /// Phone masking: ***-***-1234 (keep last 4)
    /// </summary>
    Phone,

    /// <summary>
    /// Credit card masking: ****-****-****-5678
    /// </summary>
    CreditCard,

    /// <summary>
    /// SSN masking: ***-**-6789
    /// </summary>
    SSN,

    /// <summary>
    /// Custom regex pattern
    /// </summary>
    Custom,

    /// <summary>
    /// No masking (user has permission to view)
    /// </summary>
    None
}
```

### 3.4 DetectedSensitiveProperty Record

```csharp
namespace Lexichord.Security.Classification.Models;

/// <summary>
/// A property detected as potentially sensitive.
/// </summary>
/// <remarks>
/// LOGIC: Auto-detection returns high-confidence matches for admin review.
/// Confidence score helps prioritize which detections to apply.
/// </remarks>
public record DetectedSensitiveProperty
{
    /// <summary>
    /// The property name.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Suggested classification based on detection.
    /// </summary>
    public DataClassification SuggestedClassification { get; init; }

    /// <summary>
    /// Confidence (0.0 to 1.0) that this is sensitive.
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// Reason for detection (e.g., "Matches SSN pattern").
    /// </summary>
    public required string DetectionReason { get; init; }

    /// <summary>
    /// Suggested masking type if encrypted.
    /// </summary>
    public MaskingType SuggestedMaskingType { get; init; } = MaskingType.Full;

    /// <summary>
    /// Sample value that triggered detection (redacted).
    /// </summary>
    public string? SampleValue { get; init; } = null;
}
```

### 3.5 ClassificationMetadata Record

```csharp
namespace Lexichord.Security.Classification.Models;

/// <summary>
/// Metadata about a classification.
/// </summary>
public record ClassificationMetadata
{
    /// <summary>
    /// The entity type name.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Overall classification level.
    /// </summary>
    public DataClassification Classification { get; init; }

    /// <summary>
    /// Number of encrypted properties.
    /// </summary>
    public int EncryptedPropertyCount { get; init; } = 0;

    /// <summary>
    /// Number of masked properties.
    /// </summary>
    public int MaskedPropertyCount { get; init; } = 0;

    /// <summary>
    /// When policy was last modified.
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Who last modified the policy.
    /// </summary>
    public string? LastModifiedBy { get; init; } = null;

    /// <summary>
    /// Version/hash of the classification policy.
    /// </summary>
    public string PolicyVersion { get; init; } = "1.0";
}
```

---

## 4. Implementation

### 4.1 DataClassificationService

```csharp
using Lexichord.Security.Classification.Abstractions;
using Lexichord.Security.Classification.Models;
using Lexichord.Abstractions.PII;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Lexichord.Security.Classification.Implementation;

/// <summary>
/// Service for managing data classification and sensitivity policies.
/// </summary>
/// <remarks>
/// LOGIC: This service maintains a policy database of entity and property classifications.
/// Policies are cached in memory for performance and persist to storage for durability.
/// </remarks>
public sealed class DataClassificationService(
    ILogger<DataClassificationService> logger,
    IPiiDetector piiDetector,
    IClassificationRepository repository) : IDataClassificationService
{
    private readonly ConcurrentDictionary<string, DataClassification> _entityClassifications = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<PropertyClassification>> _propertyClassifications = new();
    private bool _initialized = false;

    /// <summary>
    /// Initializes the service by loading policies from storage.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Initializing DataClassificationService");

        try
        {
            var policies = await repository.LoadAllPoliciesAsync(ct);

            foreach (var policy in policies)
            {
                _entityClassifications[policy.EntityType] = policy.Classification;
                _propertyClassifications[policy.EntityType] = policy.Properties;
            }

            _initialized = true;
            logger.LogInformation("Loaded {Count} classification policies", policies.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize classification service");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<DataClassification> GetClassificationAsync(
        string entityType,
        CancellationToken ct = default)
    {
        if (!_initialized)
            throw new InvalidOperationException("Service not initialized");

        var classification = _entityClassifications.TryGetValue(entityType, out var value)
            ? value
            : DataClassification.Public;

        return Task.FromResult(classification);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PropertyClassification>> GetPropertyClassificationsAsync(
        string entityType,
        CancellationToken ct = default)
    {
        if (!_initialized)
            throw new InvalidOperationException("Service not initialized");

        if (_propertyClassifications.TryGetValue(entityType, out var classifications))
            return Task.FromResult(classifications);

        return Task.FromResult<IReadOnlyList<PropertyClassification>>(
            Array.Empty<PropertyClassification>());
    }

    /// <inheritdoc/>
    public async Task SetClassificationAsync(
        string entityType,
        DataClassification classification,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Setting classification for entity type {EntityType} to {Classification}",
            entityType,
            classification);

        _entityClassifications[entityType] = classification;

        await repository.SaveEntityClassificationAsync(entityType, classification, ct);
        logger.LogDebug("Classification saved for {EntityType}", entityType);
    }

    /// <inheritdoc/>
    public async Task SetPropertyClassificationAsync(
        string entityType,
        string propertyName,
        DataClassification classification,
        bool requiresEncryption,
        bool requiresMasking,
        MaskingType maskingType,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Setting classification for {EntityType}.{PropertyName}",
            entityType,
            propertyName);

        var propClassification = new PropertyClassification
        {
            PropertyName = propertyName,
            Classification = classification,
            RequiresEncryption = requiresEncryption,
            RequiresMasking = requiresMasking,
            MaskingType = maskingType,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var current = _propertyClassifications.TryGetValue(entityType, out var props)
            ? props.ToList()
            : new List<PropertyClassification>();

        // Remove existing classification for this property
        current.RemoveAll(p => p.PropertyName == propertyName);
        current.Add(propClassification);

        _propertyClassifications[entityType] = current.AsReadOnly();

        await repository.SavePropertyClassificationAsync(
            entityType,
            propClassification,
            ct);

        logger.LogDebug(
            "Property classification saved: {EntityType}.{PropertyName} ({Encryption}E, {Masking}M)",
            entityType,
            propertyName,
            requiresEncryption ? "+" : "-",
            requiresMasking ? "+" : "-");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DetectedSensitiveProperty>> DetectSensitivePropertiesAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        logger.LogInformation("Auto-detecting sensitive properties for entity {EntityId}", entityId);

        var detections = new List<DetectedSensitiveProperty>();

        // Get entity data (would need entity service to fetch actual data)
        // For now, this demonstrates the flow
        var piiMatches = await piiDetector.DetectAsync(
            entityId,
            ct);

        foreach (var match in piiMatches)
        {
            var detection = new DetectedSensitiveProperty
            {
                PropertyName = match.FieldName,
                SuggestedClassification = match.Type switch
                {
                    PiiType.SSN => DataClassification.Secret,
                    PiiType.CreditCard => DataClassification.Restricted,
                    PiiType.Email => DataClassification.Restricted,
                    PiiType.Phone => DataClassification.Restricted,
                    PiiType.Address => DataClassification.Confidential,
                    _ => DataClassification.Internal
                },
                ConfidenceScore = match.Confidence,
                DetectionReason = $"PII detector matched {match.Type}",
                SuggestedMaskingType = match.Type switch
                {
                    PiiType.Email => MaskingType.Email,
                    PiiType.Phone => MaskingType.Phone,
                    PiiType.SSN => MaskingType.SSN,
                    PiiType.CreditCard => MaskingType.CreditCard,
                    _ => MaskingType.Full
                }
            };

            detections.Add(detection);
        }

        logger.LogDebug("Detected {Count} sensitive properties", detections.Count);
        return detections.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<ClassificationMetadata> GetMetadataAsync(
        string entityType,
        CancellationToken ct = default)
    {
        var classification = await GetClassificationAsync(entityType, ct);
        var properties = await GetPropertyClassificationsAsync(entityType, ct);

        var metadata = new ClassificationMetadata
        {
            EntityType = entityType,
            Classification = classification,
            EncryptedPropertyCount = properties.Count(p => p.RequiresEncryption),
            MaskedPropertyCount = properties.Count(p => p.RequiresMasking),
            LastModifiedAt = DateTimeOffset.UtcNow,
            PolicyVersion = "1.0"
        };

        return metadata;
    }

    /// <inheritdoc/>
    public async Task<string> ExportPolicyAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Exporting classification policy");

        var policy = new
        {
            version = "1.0",
            exportedAt = DateTimeOffset.UtcNow,
            entityClassifications = _entityClassifications.ToDictionary(x => x.Key, x => x.Value),
            propertyClassifications = _propertyClassifications.ToDictionary(x => x.Key, x => x.Value)
        };

        var json = JsonSerializer.Serialize(policy, new JsonSerializerOptions { WriteIndented = true });
        return json;
    }

    /// <inheritdoc/>
    public async Task ImportPolicyAsync(
        string policyJson,
        bool mergeWithExisting = true,
        CancellationToken ct = default)
    {
        logger.LogInformation("Importing classification policy (merge={Merge})", mergeWithExisting);

        try
        {
            var policy = JsonSerializer.Deserialize<Dictionary<string, object>>(policyJson);
            if (policy == null)
                throw new InvalidOperationException("Invalid policy JSON");

            // Clear existing if not merging
            if (!mergeWithExisting)
            {
                _entityClassifications.Clear();
                _propertyClassifications.Clear();
            }

            // Import entity classifications
            if (policy.TryGetValue("entityClassifications", out var entityClassObj))
            {
                var entities = JsonSerializer.Deserialize<Dictionary<string, int>>(
                    JsonSerializer.Serialize(entityClassObj));

                if (entities != null)
                {
                    foreach (var (entityType, classVal) in entities)
                    {
                        var classification = (DataClassification)classVal;
                        await SetClassificationAsync(entityType, classification, ct);
                    }
                }
            }

            logger.LogInformation("Policy import completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import classification policy");
            throw;
        }
    }
}
```

### 4.2 SensitivityDetector

```csharp
namespace Lexichord.Security.Classification.Implementation;

/// <summary>
/// Pattern-based detector for common sensitive data types.
/// </summary>
/// <remarks>
/// LOGIC: Complements the PII detector with regex patterns for
/// detecting common sensitive values without external calls.
/// </remarks>
public sealed class SensitivityDetector
{
    private static readonly Dictionary<string, (string Pattern, DataClassification Classification, MaskingType Masking)> Patterns = new()
    {
        // Credit card patterns
        { "CreditCard", (@"^\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}$", DataClassification.Secret, MaskingType.CreditCard) },

        // SSN pattern
        { "SSN", (@"^\d{3}-\d{2}-\d{4}$", DataClassification.Secret, MaskingType.SSN) },

        // Email pattern
        { "Email", (@"^[^@\s]+@[^@\s]+\.[^@\s]+$", DataClassification.Restricted, MaskingType.Email) },

        // Phone patterns
        { "Phone", (@"^[\+]?[0-9\-\(\)]{10,}$", DataClassification.Restricted, MaskingType.Phone) },

        // API Key patterns
        { "APIKey", (@"^[a-zA-Z0-9\-_]{32,}$", DataClassification.Secret, MaskingType.Full) },

        // Password field names
        { "PasswordField", (null!, DataClassification.Secret, MaskingType.Full) }, // Handled by property name matching
    };

    /// <summary>
    /// Detects sensitivity based on property name patterns.
    /// </summary>
    public static DataClassification DetectByPropertyName(string propertyName)
    {
        var lower = propertyName.ToLowerInvariant();

        return lower switch
        {
            var n when n.Contains("password") => DataClassification.Secret,
            var n when n.Contains("apikey") => DataClassification.Secret,
            var n when n.Contains("token") => DataClassification.Secret,
            var n when n.Contains("secret") => DataClassification.Secret,
            var n when n.Contains("credential") => DataClassification.Secret,
            var n when n.Contains("email") => DataClassification.Restricted,
            var n when n.Contains("phone") => DataClassification.Restricted,
            var n when n.Contains("ssn") => DataClassification.Secret,
            var n when n.Contains("creditcard") => DataClassification.Secret,
            var n when n.Contains("social") => DataClassification.Restricted,
            var n when n.Contains("address") => DataClassification.Confidential,
            _ => DataClassification.Public
        };
    }

    /// <summary>
    /// Detects sensitivity based on value content.
    /// </summary>
    public static bool TryDetectByValue(string value, out DataClassification classification)
    {
        classification = DataClassification.Public;

        foreach (var (name, (pattern, classification_val, masking)) in Patterns)
        {
            if (string.IsNullOrEmpty(pattern))
                continue;

            if (System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
            {
                classification = classification_val;
                return true;
            }
        }

        return false;
    }
}
```

---

## 5. Classification Policies

### 5.1 Built-in Classifications

```
User (Internal)
├── id (Public, no encryption)
├── username (Internal, no encryption)
├── email (Restricted, encrypt, mask: Email)
├── phone (Restricted, encrypt, mask: Phone)
├── passwordHash (Secret, encrypt, mask: Full)
└── ssn (Secret, encrypt, mask: SSN)

Credential (Secret)
├── id (Public)
├── name (Internal)
├── secret (Secret, encrypt, mask: Full)
├── apiKey (Secret, encrypt, mask: Full)
└── expiresAt (Confidential)

APIKey (Restricted)
├── id (Public)
├── name (Internal)
├── key (Secret, encrypt, mask: Full)
├── secret (Secret, encrypt, mask: Full)
└── lastUsedAt (Internal)

Service (Confidential)
├── id (Public)
├── name (Internal)
├── endpoint (Confidential)
└── apiVersion (Public)
```

---

## 6. Error Handling

### 6.1 Classification Errors

| Error | Cause | Handling |
|:------|:------|:---------|
| `ClassificationNotFound` | Entity type has no policy | Default to Public, log warning |
| `InvalidClassificationLevel` | Out-of-range enum value | Reject with validation error |
| `PolicyValidationFailed` | Conflicting rules in policy | Reject import with details |
| `DetectionFailed` | PII detector error | Log error, continue without auto-detection |

### 6.2 Error Messages

```csharp
public class ClassificationException : Exception
{
    public string EntityType { get; }
    public string? PropertyName { get; }

    public ClassificationException(string message, string entityType, string? propertyName = null)
        : base(message)
    {
        EntityType = entityType;
        PropertyName = propertyName;
    }
}
```

---

## 7. Testing

### 7.1 Unit Tests

```csharp
using NUnit.Framework;
using Moq;
using Lexichord.Security.Classification;
using Lexichord.Security.Classification.Models;

namespace Lexichord.Tests.Security;

[TestFixture]
public class DataClassificationServiceTests
{
    private DataClassificationService _sut = null!;
    private Mock<IClassificationRepository> _mockRepository = null!;
    private Mock<IPiiDetector> _mockPiiDetector = null!;

    [SetUp]
    public async Task SetUp()
    {
        _mockRepository = new Mock<IClassificationRepository>();
        _mockPiiDetector = new Mock<IPiiDetector>();

        var logger = new Mock<ILogger<DataClassificationService>>();
        _sut = new DataClassificationService(logger.Object, _mockPiiDetector.Object, _mockRepository.Object);

        _mockRepository
            .Setup(r => r.LoadAllPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClassificationPolicy>());

        await _sut.InitializeAsync();
    }

    [Test]
    public async Task GetClassificationAsync_EntityTypeNotFound_ReturnsPublic()
    {
        // Act
        var result = await _sut.GetClassificationAsync("NonExistent");

        // Assert
        Assert.That(result, Is.EqualTo(DataClassification.Public));
    }

    [Test]
    public async Task SetClassificationAsync_SavesAndReturns()
    {
        // Act
        await _sut.SetClassificationAsync("User", DataClassification.Restricted);

        // Assert
        _mockRepository.Verify(
            r => r.SaveEntityClassificationAsync("User", DataClassification.Restricted, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SetPropertyClassificationAsync_WithEncryption()
    {
        // Act
        await _sut.SetPropertyClassificationAsync(
            "User", "email", DataClassification.Restricted, true, true, MaskingType.Email);

        // Assert
        var props = await _sut.GetPropertyClassificationsAsync("User");
        Assert.That(props, Has.Count.EqualTo(1));
        Assert.That(props[0].PropertyName, Is.EqualTo("email"));
        Assert.That(props[0].RequiresEncryption, Is.True);
    }

    [Test]
    public async Task DetectSensitivePropertiesAsync_CallsPiiDetector()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var piiMatches = new[] { /* PII matches */ };
        _mockPiiDetector.Setup(d => d.DetectAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(piiMatches);

        // Act
        var result = await _sut.DetectSensitivePropertiesAsync(entityId);

        // Assert
        _mockPiiDetector.Verify(d => d.DetectAsync(entityId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExportPolicyAsync_ReturnsValidJson()
    {
        // Arrange
        await _sut.SetClassificationAsync("User", DataClassification.Restricted);

        // Act
        var json = await _sut.ExportPolicyAsync();

        // Assert
        Assert.That(json, Does.Contain("User"));
        Assert.That(json, Does.Contain("Restricted"));
    }

    [Test]
    public async Task ImportPolicyAsync_MergesFalse_ClearsExisting()
    {
        // Arrange
        await _sut.SetClassificationAsync("OldType", DataClassification.Public);
        var newPolicy = @"{ ""entityClassifications"": { ""NewType"": 1 } }";

        // Act
        await _sut.ImportPolicyAsync(newPolicy, mergeWithExisting: false);

        // Assert
        var oldClassification = await _sut.GetClassificationAsync("OldType");
        Assert.That(oldClassification, Is.EqualTo(DataClassification.Public)); // Cleared
    }
}
```

---

## 8. Performance

### 8.1 Targets

| Metric | Target |
|:-------|:-------|
| Classify property | <1ms |
| Get classifications for type | <2ms |
| Auto-detect (1000 entities) | <5s |
| Export policy | <100ms |
| Import policy | <500ms |

### 8.2 Optimizations

- Policies cached in memory (ConcurrentDictionary)
- Property classifications indexed by entity type
- Auto-detection parallelized across entities
- Lazy-loading of detection results

---

## 9. License Gating

| Tier | Feature |
|:-----|:--------|
| **Core** | Public/Internal classifications only |
| **WriterPro** | All classification levels |
| **Teams** | + Auto-detection of sensitive properties |
| **Enterprise** | + Custom detection rules + audit logging |

---

## 10. Changelog

### v0.11.3 (2026-01-31)

- Initial design for data classification service
- Support for 5 classification levels
- Property-level classification rules
- Auto-detection using PII detector
- Policy import/export in JSON format

---

## Appendix A: Classification Decision Tree

```
START: "How sensitive is this property?"
│
├─ Is it a password, API key, token?
│  └─ YES → Secret (encryption + audit)
│
├─ Is it PII (SSN, credit card)?
│  └─ YES → Secret or Restricted (encryption + masking)
│
├─ Is it contact info (email, phone)?
│  └─ YES → Restricted (encryption + email masking)
│
├─ Is it business info (address, department)?
│  └─ YES → Confidential (optional encryption)
│
├─ Is it internal-only info?
│  └─ YES → Internal (no encryption)
│
└─ Is it public (timestamps, IDs)?
   └─ YES → Public (no protection)
```

---

