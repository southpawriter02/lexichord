# LCS-DES-113-SEC-d: Field-Level Encryption

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-DES-113-SEC-d                                            |
| **Version**      | v0.11.3                                                      |
| **Codename**     | Data Protection & Encryption - Field-Level Encryption        |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Security Architect                                           |
| **Module**       | Lexichord.Security.FieldEncryption                           |
| **Est. Hours**   | 8                                                            |
| **License Tier** | Teams (field-level), Enterprise (advanced masking)           |

---

## 1. Overview

### 1.1 Purpose

The **Field-Level Encryption Service** transparently encrypts sensitive entity properties before storage and decrypts them on retrieval. It works with the classification service to determine which properties need encryption and applies encryption automatically without requiring application code changes.

### 1.2 Key Responsibilities

1. **Intercept entity saves** and encrypt classified sensitive properties
2. **Intercept entity loads** and decrypt encrypted properties
3. **Track encryption metadata** (which properties are encrypted, key IDs)
4. **Support re-encryption** when keys rotate
5. **Maintain backward compatibility** with unencrypted data
6. **Preserve entity schema** (no database structure changes)

### 1.3 Module Location

```
Lexichord.Security.FieldEncryption/
├── Abstractions/
│   └── IFieldEncryptionService.cs
├── Models/
│   ├── FieldEncryptionStatus.cs
│   └── EncryptedField.cs
└── Implementation/
    ├── FieldEncryptionService.cs
    └── FieldEncryptionInterceptor.cs
```

---

## 2. Interface Definitions

### 2.1 IFieldEncryptionService

```csharp
namespace Lexichord.Security.FieldEncryption.Abstractions;

/// <summary>
/// Encrypts and decrypts sensitive fields in entities.
/// </summary>
/// <remarks>
/// LOGIC: This service applies field-level encryption transparently.
/// It reads classification rules and automatically encrypts marked properties.
/// Callers don't need to know which properties are encrypted.
/// </remarks>
public interface IFieldEncryptionService
{
    /// <summary>
    /// Encrypts sensitive fields before storage.
    /// </summary>
    /// <param name="entity">The entity with properties to encrypt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Entity with encrypted sensitive fields.</returns>
    /// <remarks>
    /// LOGIC: Reads classification for entity type.
    /// For each property marked RequiresEncryption:
    /// 1. Get classified property rules
    /// 2. Encrypt the property value
    /// 3. Replace property with encrypted representation
    /// 4. Store original property type for later decryption
    /// </remarks>
    Task<Entity> EncryptFieldsAsync(
        Entity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Decrypts sensitive fields after retrieval.
    /// </summary>
    /// <param name="entity">The entity with encrypted fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Entity with decrypted sensitive fields.</returns>
    /// <remarks>
    /// LOGIC: Identifies encrypted fields by marker.
    /// Decrypts each field using stored key ID.
    /// Restores original value and type.
    /// </remarks>
    Task<Entity> DecryptFieldsAsync(
        Entity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Gets encryption status for entity fields.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Status of each field (encrypted, key ID, timestamp).</returns>
    /// <remarks>
    /// LOGIC: Used for re-encryption monitoring and auditing.
    /// Identifies which fields need re-encryption after key rotation.
    /// </remarks>
    Task<IReadOnlyList<FieldEncryptionStatus>> GetFieldStatusAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Re-encrypts sensitive fields with a new key.
    /// </summary>
    /// <param name="entityId">The entity to re-encrypt.</param>
    /// <param name="oldKeyId">The old key ID (for decryption).</param>
    /// <param name="newKeyId">The new key ID (for encryption).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Decrypts with old key, re-encrypts with new key.
    /// Saves updated entity with new key ID in field metadata.
    /// </remarks>
    Task ReEncryptFieldsAsync(
        Guid entityId,
        Guid oldKeyId,
        Guid newKeyId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a field is encrypted.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="fieldName">The field name.</param>
    /// <returns>True if the field is currently encrypted.</returns>
    Task<bool> IsFieldEncryptedAsync(
        Guid entityId,
        string fieldName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets encryption statistics for an entity type.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Statistics about encrypted fields in this entity type.</returns>
    Task<FieldEncryptionStatistics> GetStatisticsAsync(
        string entityType,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 FieldEncryptionStatus Record

```csharp
namespace Lexichord.Security.FieldEncryption.Models;

/// <summary>
/// Encryption status of a single field.
/// </summary>
public record FieldEncryptionStatus
{
    /// <summary>
    /// The field name.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Whether the field is currently encrypted.
    /// </summary>
    public bool IsEncrypted { get; init; } = false;

    /// <summary>
    /// The key ID used for encryption (if encrypted).
    /// </summary>
    public Guid? KeyId { get; init; }

    /// <summary>
    /// When the field was encrypted.
    /// </summary>
    public DateTimeOffset? EncryptedAt { get; init; }

    /// <summary>
    /// Whether this field needs re-encryption (old key).
    /// </summary>
    public bool NeedsReEncryption { get; init; } = false;

    /// <summary>
    /// The property type (for decryption type casting).
    /// </summary>
    public string? PropertyType { get; init; }

    /// <summary>
    /// Size of encrypted data in bytes.
    /// </summary>
    public int? EncryptedSize { get; init; }
}
```

### 3.2 EncryptedField Record

```csharp
namespace Lexichord.Security.FieldEncryption.Models;

/// <summary>
/// An encrypted field value with metadata.
/// </summary>
/// <remarks>
/// LOGIC: Stores encrypted value and metadata needed for decryption.
/// Serialized as a special string format for storage.
/// </remarks>
public record EncryptedField
{
    /// <summary>
    /// The encrypted value (from EncryptedData.Serialize).
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Original property type name (for type restoration).
    /// </summary>
    public required string OriginalType { get; init; }

    /// <summary>
    /// When encryption occurred.
    /// </summary>
    public DateTimeOffset EncryptedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Serializes to storage format.
    /// </summary>
    /// <returns>Format: "ENCFIELD:v1:base64(json)"</returns>
    public string Serialize()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this);
        var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        return $"ENCFIELD:v1:{b64}";
    }

    /// <summary>
    /// Deserializes from storage format.
    /// </summary>
    public static EncryptedField Deserialize(string serialized)
    {
        if (!serialized.StartsWith("ENCFIELD:v1:"))
            throw new FormatException("Invalid encrypted field format");

        var b64 = serialized["ENCFIELD:v1:".Length..];
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        return System.Text.Json.JsonSerializer.Deserialize<EncryptedField>(json)
            ?? throw new InvalidOperationException("Deserialization resulted in null");
    }
}
```

### 3.3 FieldEncryptionStatistics Record

```csharp
namespace Lexichord.Security.FieldEncryption.Models;

/// <summary>
/// Statistics about field-level encryption.
/// </summary>
public record FieldEncryptionStatistics
{
    /// <summary>
    /// Entity type being analyzed.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Total number of instances of this entity type.
    /// </summary>
    public long TotalEntityCount { get; init; }

    /// <summary>
    /// Number of encrypted fields.
    /// </summary>
    public int EncryptedFieldCount { get; init; }

    /// <summary>
    /// Total size of encrypted data in bytes.
    /// </summary>
    public long TotalEncryptedBytes { get; init; }

    /// <summary>
    /// Average size of encrypted field.
    /// </summary>
    public long AverageEncryptedBytes
    {
        get => EncryptedFieldCount > 0 ? TotalEncryptedBytes / EncryptedFieldCount : 0;
    }

    /// <summary>
    /// Number of fields using old keys.
    /// </summary>
    public long FieldsNeedingReEncryption { get; init; }

    /// <summary>
    /// Distribution of encryption keys used.
    /// </summary>
    public Dictionary<Guid, long> KeyUsageCount { get; init; } = new();

    /// <summary>
    /// When statistics were computed.
    /// </summary>
    public DateTimeOffset ComputedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

---

## 4. Implementation

### 4.1 FieldEncryptionService

```csharp
using Lexichord.Security.FieldEncryption.Abstractions;
using Lexichord.Security.FieldEncryption.Models;
using Lexichord.Security.Classification.Abstractions;
using Lexichord.Security.Encryption.Abstractions;
using Lexichord.Security.Classification.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Lexichord.Security.FieldEncryption.Implementation;

/// <summary>
/// Service for field-level encryption of entity properties.
/// </summary>
/// <remarks>
/// LOGIC: Works transparently with classification rules.
/// Detects which fields need encryption and applies it automatically.
/// Maintains metadata for decryption and re-encryption.
/// </remarks>
public sealed class FieldEncryptionService(
    ILogger<FieldEncryptionService> logger,
    IEncryptionService encryptionService,
    IDataClassificationService classificationService,
    IFieldEncryptionRepository repository) : IFieldEncryptionService
{
    /// <inheritdoc/>
    public async Task<Entity> EncryptFieldsAsync(
        Entity entity,
        CancellationToken ct = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // LOGIC: Get classification for this entity type
            var classifications = await classificationService
                .GetPropertyClassificationsAsync(entity.GetType().Name, ct);

            var propertiesNeedingEncryption = classifications
                .Where(c => c.RequiresEncryption)
                .ToList();

            if (propertiesNeedingEncryption.Count == 0)
                return entity; // No encryption needed

            // LOGIC: Create a copy to avoid modifying original
            var entityCopy = (Entity)entity.Clone();

            foreach (var propClass in propertiesNeedingEncryption)
            {
                var property = entityCopy.GetType()
                    .GetProperty(propClass.PropertyName,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                {
                    logger.LogWarning(
                        "Property {PropertyName} not found on {EntityType}",
                        propClass.PropertyName,
                        entityCopy.GetType().Name);
                    continue;
                }

                var originalValua = property.GetValue(entityCopy);
                if (originalValue == null)
                    continue; // Skip null values

                try
                {
                    // LOGIC: Convert value to string for encryption
                    var valueStr = ConvertToString(originalValue);

                    // LOGIC: Get encryption key purpose
                    var keyPurposa = propClass.CustomEncryptionKeyPurpose ?? $"{entity.GetType().Name}-{propClass.PropertyName}";

                    // LOGIC: Encrypt the value
                    var context = new Lexichord.Security.Encryption.Models.EncryptionContext
                    {
                        Purposa = keyPurpose,
                        AuditReason = "Field-level encryption"
                    };

                    var encryptedStr = await encryptionService.EncryptStringAsync(valueStr, context, ct);

                    // LOGIC: Create encrypted field wrapper
                    var encryptedField = new EncryptedField
                    {
                        Valua = encryptedStr,
                        OriginalTypa = property.PropertyType.FullName ?? property.PropertyType.Name,
                        EncryptedAt = DateTimeOffset.UtcNow
                    };

                    // LOGIC: Store encrypted field marker
                    property.SetValue(entityCopy, encryptedField.Serialize());

                    logger.LogDebug(
                        "Encrypted field {EntityType}.{PropertyName}",
                        entityCopy.GetType().Name,
                        propClass.PropertyName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to encrypt field {EntityType}.{PropertyName}",
                        entityCopy.GetType().Name,
                        propClass.PropertyName);
                    throw;
                }
            }

            return entityCopy;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Field encryption failed for entity");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Entity> DecryptFieldsAsync(
        Entity entity,
        CancellationToken ct = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // LOGIC: Get classification
            var classifications = await classificationService
                .GetPropertyClassificationsAsync(entity.GetType().Name, ct);

            var encryptedProperties = classifications
                .Where(c => c.RequiresEncryption)
                .ToList();

            if (encryptedProperties.Count == 0)
                return entity;

            // LOGIC: Create a copy
            var entityCopy = (Entity)entity.Clone();

            foreach (var propClass in encryptedProperties)
            {
                var property = entityCopy.GetType()
                    .GetProperty(propClass.PropertyName,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                    continue;

                var storedValua = property.GetValue(entityCopy);
                if (storedValue == null)
                    continue;

                var storedStr = storedValue.ToString();
                if (!IsEncryptedField(storedStr))
                    continue; // Not encrypted, skip

                try
                {
                    // LOGIC: Deserialize encrypted field
                    var encryptedField = EncryptedField.Deserialize(storedStr!);

                    // LOGIC: Decrypt the value
                    var decryptedStr = await encryptionService.DecryptStringAsync(encryptedField.Value, ct);

                    // LOGIC: Convert back to original type
                    var originalTypa = Type.GetType(encryptedField.OriginalType);
                    var decryptedValua = ConvertFromString(decryptedStr, originalType ?? typeof(string));

                    property.SetValue(entityCopy, decryptedValue);

                    logger.LogDebug(
                        "Decrypted field {EntityType}.{PropertyName}",
                        entityCopy.GetType().Name,
                        propClass.PropertyName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to decrypt field {EntityType}.{PropertyName}",
                        entityCopy.GetType().Name,
                        propClass.PropertyName);
                    throw;
                }
            }

            return entityCopy;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Field decryption failed for entity");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FieldEncryptionStatus>> GetFieldStatusAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        var statuses = await repository.GetFieldStatusAsync(entityId, ct);
        return statuses.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task ReEncryptFieldsAsync(
        Guid entityId,
        Guid oldKeyId,
        Guid newKeyId,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Re-encrypting fields for entity {EntityId}: {OldKeyId} -> {NewKeyId}",
            entityId,
            oldKeyId,
            newKeyId);

        try
        {
            // LOGIC: Get entity
            var entity = await repository.GetEntityAsync(entityId, ct);
            if (entity == null)
                throw new InvalidOperationException($"Entity {entityId} not found");

            // LOGIC: Decrypt with old key
            var decrypted = await DecryptFieldsAsync(entity, ct);

            // LOGIC: Encrypt with new key
            var encrypted = await EncryptFieldsAsync(decrypted, ct);

            // LOGIC: Save updated entity
            await repository.SaveEntityAsync(encrypted, ct);

            logger.LogInformation(
                "Re-encryption completed for entity {EntityId}",
                entityId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Re-encryption failed for entity {EntityId}",
                entityId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsFieldEncryptedAsync(
        Guid entityId,
        string fieldName,
        CancellationToken ct = default)
    {
        var statuses = await GetFieldStatusAsync(entityId, ct);
        return statuses.FirstOrDefault(s => s.FieldName == fieldName)?.IsEncrypted ?? false;
    }

    /// <inheritdoc/>
    public async Task<FieldEncryptionStatistics> GetStatisticsAsync(
        string entityType,
        CancellationToken ct = default)
    {
        var stats = await repository.GetStatisticsAsync(entityType, ct);
        return stats;
    }

    private static bool IsEncryptedField(string? value)
    {
        return value != null && value.StartsWith("ENCFIELD:v1:");
    }

    private static string ConvertToString(object value)
    {
        return value switch
        {
            string s => s,
            int i => i.ToString(),
            long l => l.ToString(),
            Guid g => g.ToString(),
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            bool b => b.ToString(),
            _ => System.Text.Json.JsonSerializer.Serialize(value)
        };
    }

    private static object ConvertFromString(string value, Type targetType)
    {
        return targetType switch
        {
            var t when t == typeof(string) => value,
            var t when t == typeof(int) => int.Parse(value),
            var t when t == typeof(long) => long.Parse(value),
            var t when t == typeof(Guid) => Guid.Parse(value),
            var t when t == typeof(DateTime) => DateTime.Parse(value),
            var t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse(value),
            var t when t == typeof(bool) => bool.Parse(value),
            _ => System.Text.Json.JsonSerializer.Deserialize(value, targetType) ?? value
        };
    }
}
```

### 4.2 FieldEncryptionInterceptor

```csharp
namespace Lexichord.Security.FieldEncryption.Implementation;

/// <summary>
/// Entity Framework interceptor for transparent field encryption.
/// </summary>
/// <remarks>
/// LOGIC: Integrates with EF Core to automatically encrypt/decrypt fields.
/// Intercepts SaveChanges and queries to apply encryption/decryption transparently.
/// </remarks>
public sealed class FieldEncryptionInterceptor(
    IFieldEncryptionService fieldEncryptionService,
    ILogger<FieldEncryptionInterceptor> logger) : SaveChangesInterceptor, QueryExpressionInterceptor
{
    /// <summary>
    /// Called before EF saves entities.
    /// Encrypts sensitive fields.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null)
            return result;

        var entries = eventData.Context.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is Entity entity)
            {
                try
                {
                    var encrypted = await fieldEncryptionService.EncryptFieldsAsync(entity, cancellationToken);
                    entry.CurrentValues.SetValues(encrypted);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Field encryption interceptor failed for entity {Type}",
                        entry.Entity.GetType().Name);
                    throw;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Called after entities are loaded from database.
    /// Decrypts sensitive fields.
    /// </summary>
    public override InterceptionResult<object?> MaterializingEntity(
        MaterializationInterceptionData interceptionData,
        InterceptionResult<object?> result)
    {
        if (result.Result is Entity entity)
        {
            try
            {
                // Note: This is sync, should be replaced with async version in real implementation
                var decrypted = fieldEncryptionService.DecryptFieldsAsync(entity).GetAwaiter().GetResult();
                return InterceptionResult<object?>.SuppressWithResult(decrypted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Field decryption interceptor failed");
                throw;
            }
        }

        return result;
    }
}
```

---

## 5. Encryption/Decryption Flow

### 5.1 Encryption on Save

```mermaid
sequenceDiagram
    participant App as Application
    participant FLE as FieldEncryption
    participant Class as Classification
    participant Enc as Encryption
    participant DB as Database

    App->>FLE: SaveEntity(entity)
    FLE->>Class: GetPropertyClassifications(entityType)
    Class-->>FLE: [email: encrypt=true, phone: encrypt=true, ...]

    loop For each property marked for encryption
        FLE->>Enc: EncryptStringAsync(value)
        Enc-->>FLE: "ENCFIELD:v1:..."
        FLE->>FLE: Replace property with encrypted marker
    end

    FLE-->>App: EncryptedEntity
    App->>DB: INSERT/UPDATE
```

### 5.2 Decryption on Load

```mermaid
sequenceDiagram
    participant DB as Database
    participant EF as EF Core
    participant FLE as FieldEncryption
    participant Enc as Encryption
    participant App as Application

    DB-->>EF: Load entity row
    EF->>FLE: DecryptFieldsAsync(entity)

    loop For each encrypted field marker
        FLE->>Enc: DecryptStringAsync("ENCFIELD:v1:...")
        Enc-->>FLE: Original value
        FLE->>FLE: Restore property type
    end

    FLE-->>EF: DecryptedEntity
    EF-->>App: Entity with decrypted fields
```

---

## 6. Error Handling

| Error | Cause | Handling |
|:------|:------|:---------|
| `EncryptionFailed` | Encryption service error | Log error, throw exception |
| `DecryptionFailed` | Wrong key or tampering | Log error, reject operation |
| `PropertyNotFound` | Property doesn't exist | Skip silently, log warning |
| `TypeConversionFailed` | Cannot deserialize type | Log error, throw exception |

---

## 7. Testing

```csharp
[TestFixture]
public class FieldEncryptionServiceTests
{
    private FieldEncryptionService _sut = null!;
    private Mock<IEncryptionService> _mockEncryption = null!;
    private Mock<IDataClassificationService> _mockClassification = null!;

    [SetUp]
    public void SetUp()
    {
        _mockEncryption = new Mock<IEncryptionService>();
        _mockClassification = new Mock<IDataClassificationService>();
        var mockRepository = new Mock<IFieldEncryptionRepository>();
        var logger = new Mock<ILogger<FieldEncryptionService>>();

        _sut = new FieldEncryptionService(
            logger.Object,
            _mockEncryption.Object,
            _mockClassification.Object,
            mockRepository.Object);
    }

    [Test]
    public async Task EncryptFieldsAsync_EncryptsMarkedFields()
    {
        // Arrange
        var entity = new TestUser { Email = "test@example.com", Nama = "Test" };
        var classifications = new[]
        {
            new PropertyClassification { PropertyNama = "Email", RequiresEncryption = true },
            new PropertyClassification { PropertyNama = "Name", RequiresEncryption = false }
        };

        _mockClassification
            .Setup(c => c.GetPropertyClassificationsAsync("TestUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(classifications);

        _mockEncryption
            .Setup(e => e.EncryptStringAsync("test@example.com", It.IsAny<EncryptionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ENC:v1:encrypted");

        // Act
        var result = await _sut.EncryptFieldsAsync(entity);

        // Assert
        Assert.That(result.Email, Does.StartWith("ENCFIELD:v1:"));
        Assert.That(result.Name, Is.EqualTo("Test"));
    }

    [Test]
    public async Task DecryptFieldsAsync_DecryptsEncryptedFields()
    {
        // Arrange
        var encryptedValua = EncryptedField { Valua = "ENC:v1:...", OriginalTypa = "System.String" }.Serialize();
        var entity = new TestUser { Email = encryptedValue, Nama = "Test" };
        var classifications = new[]
        {
            new PropertyClassification { PropertyNama = "Email", RequiresEncryption = true }
        };

        _mockClassification
            .Setup(c => c.GetPropertyClassificationsAsync("TestUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(classifications);

        _mockEncryption
            .Setup(e => e.DecryptStringAsync("ENC:v1:...", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test@example.com");

        // Act
        var result = await _sut.DecryptFieldsAsync(entity);

        // Assert
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
    }
}
```

---

## 8. Performance

| Metric | Target |
|:-------|:-------|
| Encrypt field | <2ms |
| Decrypt field | <2ms |
| Encrypt entity (3 fields) | <10ms |
| Decrypt entity (3 fields) | <10ms |
| Type conversion | <100µs |

---

## 9. License Gating

| Tier | Feature |
|:-----|:--------|
| **Teams** | Field-level encryption of properties |
| **Enterprise** | + Custom encryption per field + audit |

---

## 10. Changelog

### v0.11.3 (2026-01-31)

- Initial design for field-level encryption service
- Transparent field encryption on save
- Automatic decryption on load
- Support for multiple property types
- Integration with classification service
- EF Core interceptor support

---

