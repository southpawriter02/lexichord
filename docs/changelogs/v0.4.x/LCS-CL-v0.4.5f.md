# Changelog: v0.4.5f - Schema Registry Service

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-045-KG-b](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-KG-b.md)

---

## Summary

Implements the Schema Registry Service for the Knowledge Graph subsystem (CKVS Phase 1). This version adds YAML-driven entity and relationship type schemas, an in-memory registry with case-insensitive lookups, comprehensive entity and relationship validation with typed error codes, and a built-in technical documentation schema defining Product, Component, Endpoint, Parameter, Response, and Concept entity types.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/

| File                 | Type   | Description                                                              |
| :------------------- | :----- | :----------------------------------------------------------------------- |
| `SchemaRecords.cs`   | Enum   | PropertyType (8 values: String, Text, Number, Boolean, Enum, Array, DateTime, Reference) |
| `SchemaRecords.cs`   | Enum   | Cardinality (4 values: OneToOne, OneToMany, ManyToOne, ManyToMany)       |
| `SchemaRecords.cs`   | Record | PropertySchema with type, constraints (min/max, maxLength, pattern, enum values) |
| `SchemaRecords.cs`   | Record | EntityTypeSchema with properties, hierarchy (Extends, IsAbstract), icon/color |
| `SchemaRecords.cs`   | Record | RelationshipTypeSchema with from/to entity constraints, cardinality      |
| `SchemaRecords.cs`   | Record | SchemaValidationError with Code, Message, PropertyName, ActualValue      |
| `SchemaRecords.cs`   | Record | SchemaValidationWarning with Code, Message, PropertyName                 |
| `SchemaRecords.cs`   | Record | SchemaValidationResult with IsValid, Errors, Warnings, factory methods   |
| `ISchemaRegistry.cs` | Interface | ISchemaRegistry for schema loading, validation, and querying           |

#### Lexichord.Modules.Knowledge/

| File                                                  | Type           | Description                                                    |
| :---------------------------------------------------- | :------------- | :------------------------------------------------------------- |
| `Schema/SchemaLoader.cs`                              | Internal       | YAML parser using YamlDotNet with underscore naming convention |
| `Schema/SchemaValidator.cs`                           | Internal       | Entity and relationship validation with 13 error codes         |
| `Schema/SchemaRegistry.cs`                            | Implementation | ISchemaRegistry with case-insensitive dictionaries and logging |
| `Schema/BuiltInSchemas/technical-docs.yaml`           | Schema         | Built-in schema: 6 entity types, 6 relationship types         |

#### Lexichord.Tests.Unit/

| File                                                        | Tests | Coverage                                                   |
| :---------------------------------------------------------- | ----: | :--------------------------------------------------------- |
| `Abstractions/Knowledge/SchemaRecordsTests.cs`               |    25 | Enums, records, defaults, equality, factory methods        |
| `Modules/Knowledge/SchemaRegistryTests.cs`                   |    37 | Loading, querying, validation delegation, reload, logging  |
| `Modules/Knowledge/SchemaLoaderTests.cs`                     |    16 | YAML parsing, defaults, From/To handling, error handling   |
| `Modules/Knowledge/SchemaValidatorTests.cs`                  |    35 | All error codes, warnings, constraint validation           |

### Modified

| File                                          | Change                                                       |
| :-------------------------------------------- | :----------------------------------------------------------- |
| `Lexichord.Modules.Knowledge.csproj`          | Added YamlDotNet 15.1.6 NuGet + Content include for YAML    |
| `KnowledgeModule.cs`                          | Added SchemaRegistry singleton DI registration (v0.4.5f)     |

---

## Technical Details

### License Gating

| License Tier | Schema Loading | Validation | Schema Querying |
| :----------- | :------------- | :--------- | :-------------- |
| Core         | Denied         | Denied     | Denied          |
| WriterPro    | Read-only      | Read-only  | Allowed         |
| Teams        | Allowed        | Allowed    | Allowed         |
| Enterprise   | Allowed        | Allowed    | Allowed         |

### Validation Error Codes

| Code                     | Context            | Description                                    |
| :----------------------- | :----------------- | :--------------------------------------------- |
| `UNKNOWN_ENTITY_TYPE`    | Entity             | Entity type not registered in schema           |
| `ABSTRACT_TYPE`          | Entity             | Cannot instantiate abstract entity types       |
| `NAME_REQUIRED`          | Entity             | Entity name must be non-empty                  |
| `REQUIRED_PROPERTY_MISSING` | Entity/Relationship | Required property is missing or empty       |
| `TYPE_MISMATCH`          | Entity             | Property value does not match expected type    |
| `INVALID_ENUM_VALUE`     | Entity             | Value not in allowed enum values list          |
| `MAX_LENGTH_EXCEEDED`    | Entity             | String exceeds maximum length constraint       |
| `PATTERN_MISMATCH`       | Entity             | String does not match regex pattern            |
| `BELOW_MINIMUM`          | Entity             | Number is below minimum value constraint       |
| `ABOVE_MAXIMUM`          | Entity             | Number is above maximum value constraint       |
| `UNKNOWN_RELATIONSHIP_TYPE` | Relationship    | Relationship type not registered in schema     |
| `INVALID_FROM_TYPE`      | Relationship       | Source entity type not valid for relationship  |
| `INVALID_TO_TYPE`        | Relationship       | Target entity type not valid for relationship  |

### Warning Codes

| Code               | Context | Description                                 |
| :----------------- | :------ | :------------------------------------------ |
| `UNKNOWN_PROPERTY` | Entity  | Property not defined in schema (extra data) |

### Built-In Schema: Technical Documentation

**Entity Types:**

| Type      | Required Properties | Optional Properties                           |
| :-------- | :------------------ | :-------------------------------------------- |
| Product   | name                | version, description                          |
| Component | name                | type (enum), description                      |
| Endpoint  | path, method        | description, deprecated, authentication       |
| Parameter | name, type, location | required, default_value, description, example |
| Response  | status_code         | description, content_type                     |
| Concept   | name, definition    | aliases, category                             |

**Relationship Types:**

| Type       | From               | To                    | Cardinality  | Directional |
| :--------- | :----------------- | :-------------------- | :----------- | :---------- |
| CONTAINS   | Product            | Component, Endpoint   | OneToMany    | Yes         |
| EXPOSES    | Component          | Endpoint              | OneToMany    | Yes         |
| ACCEPTS    | Endpoint           | Parameter             | OneToMany    | Yes         |
| RETURNS    | Endpoint           | Response              | OneToMany    | Yes         |
| REQUIRES   | Endpoint, Parameter | Parameter, Concept   | ManyToMany   | Yes         |
| RELATED_TO | Concept            | Concept               | ManyToMany   | No          |

---

## Verification

```bash
# Build Knowledge module
dotnet build src/Lexichord.Modules.Knowledge
# Result: Build succeeded

# Run v0.4.5f unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.5f"
# Result: 113 tests passed

# Run full regression
dotnet test tests/Lexichord.Tests.Unit
# Result: 4247 tests passed, 0 failures
```

---

## Test Coverage

| Category                                   | Tests |
| :----------------------------------------- | ----: |
| PropertyType enum values/ordinals          |     3 |
| Cardinality enum values/ordinals           |     3 |
| PropertySchema defaults/fields/equality    |     5 |
| EntityTypeSchema defaults/fields/equality  |     5 |
| RelationshipTypeSchema defaults/fields     |     3 |
| SchemaValidationError fields/equality      |     3 |
| SchemaValidationWarning fields             |     2 |
| SchemaValidationResult factory/validity    |     6 |
| SchemaLoader YAML parsing                  |     7 |
| SchemaLoader defaults/error handling       |     5 |
| SchemaLoader relationship properties       |     4 |
| SchemaRegistry loading/merging/versions    |    11 |
| SchemaRegistry lookups (case-insensitive)  |     6 |
| SchemaRegistry reload/cancellation/logging |     5 |
| SchemaRegistry validation delegation       |     2 |
| SchemaRegistry multiple files              |     1 |
| SchemaValidator entity type checks         |     4 |
| SchemaValidator required properties        |     4 |
| SchemaValidator property types             |     7 |
| SchemaValidator constraints                |     6 |
| SchemaValidator unknown property warnings  |     2 |
| SchemaValidator valid entities             |     2 |
| SchemaValidator relationship validation    |     7 |
| SchemaValidator constructor                |     2 |
| **Total**                                  | **113** |

---

## Dependencies

- v0.4.5e: KnowledgeEntity, KnowledgeRelationship, IGraphConnectionFactory
- v0.0.4c: ILicenseContext, LicenseTier
- v0.0.3b: ILogger<T> (structured logging)
- v0.2.1c: YamlDotNet (YAML parsing, version 15.1.6)

## Dependents

- v0.4.5g: Entity Abstraction Layer (uses ISchemaRegistry for validation)
- v0.4.7+: Entity Browser (uses schema metadata for display)

---

## NuGet Packages Added

| Package    | Version | Project                       |
| :--------- | :------ | :---------------------------- |
| `YamlDotNet` | 15.1.6 | `Lexichord.Modules.Knowledge` |

> **Note:** YamlDotNet was already in use by Lexichord.Modules.Style (v0.2.1c). The Knowledge module reuses the same version for consistency.

---

## Related Documents

- [LCS-DES-045-KG-b](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-KG-b.md) - Design specification
- [LCS-SBD-045-KG §4.2](../../specs/v0.4.x/v0.4.5/LCS-SBD-v0.4.5-KG.md#42-v045f-schema-registry-service) - Scope breakdown
- [LCS-DES-045-KG-INDEX](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-KG-INDEX.md) - Knowledge Graph specs index
- [LCS-CL-v0.4.5e](./LCS-CL-v0.4.5e.md) - Previous version (Graph Database Integration)
