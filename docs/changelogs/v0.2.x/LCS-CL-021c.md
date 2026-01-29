# LCS-CL-021c: YAML Deserializer

## Document Control

| Field            | Value             |
| :--------------- | :---------------- |
| **Document ID**  | LCS-CL-021c       |
| **Version**      | v0.2.1c           |
| **Date**         | 2026-01-29        |
| **Status**       | Complete          |
| **Parent Spec**  | LCS-DES-021c      |
| **Feature Name** | YAML Deserializer |
| **Module**       | Style             |

---

## Summary

Implements the full YAML deserialization capability for the Style Module, transforming `lexichord.yaml` files into `StyleSheet` domain objects. This includes YAML DTOs, schema validation with helpful error messages, embedded default rules, and comprehensive test coverage.

---

## Changes

### New Files

| File                                                              | Purpose                                         |
| :---------------------------------------------------------------- | :---------------------------------------------- |
| `Lexichord.Modules.Style/Yaml/YamlDtos.cs`                        | YAML DTO classes for YamlDotNet deserialization |
| `Lexichord.Modules.Style/Yaml/YamlSchemaValidator.cs`             | Schema validation with detailed error messages  |
| `Lexichord.Modules.Style/Resources/lexichord.yaml`                | Embedded default style sheet (26 rules)         |
| `Lexichord.Tests.Unit/Modules/Style/YamlStyleSheetLoaderTests.cs` | Unit tests for loader functionality             |
| `Lexichord.Tests.Unit/Modules/Style/YamlSchemaValidatorTests.cs`  | Unit tests for schema validation                |

### Modified Files

| File                                                       | Change                                            |
| :--------------------------------------------------------- | :------------------------------------------------ |
| `Lexichord.Modules.Style/Services/YamlStyleSheetLoader.cs` | Replaced stub with full YamlDotNet implementation |

---

## Technical Details

### YAML DTOs

- **YamlStyleSheet**: Maps top-level YAML properties (name, version, author, description, extends, rules)
- **YamlRule**: Maps individual rule properties with YamlMember aliases for snake_case conversion
- All properties nullable to enable validation of required fields

### Schema Validation

- **Required fields**: name (sheet), id/name/description/pattern (rules)
- **ID format**: Kebab-case validation via regex `^[a-z][a-z0-9-]*$`
- **Enum validation**: category, severity, pattern_type with case-insensitive matching
- **Duplicate detection**: Case-insensitive ID uniqueness check
- **Regex validation**: Validates regex patterns before domain conversion

### Default Style Sheet

26 rules across three categories:

| Category    | Count | Examples                                         |
| :---------- | :---- | :----------------------------------------------- |
| Terminology | 10    | no-jargon, no-click-here, prefer-inclusive       |
| Formatting  | 8     | no-trailing-whitespace, heading-space-after-hash |
| Syntax      | 8     | no-passive-voice, no-double-negative             |

### YamlStyleSheetLoader Implementation

| Method                     | Implementation                                           |
| :------------------------- | :------------------------------------------------------- |
| `LoadFromFileAsync`        | File.OpenRead → LoadFromStreamAsync                      |
| `LoadFromStreamAsync`      | YamlDotNet deserialize → Validate → Convert              |
| `LoadEmbeddedDefaultAsync` | Assembly.GetManifestResourceStream → LoadFromStreamAsync |
| `ValidateYaml`             | Parse → YamlSchemaValidator.Validate                     |

---

## Test Coverage

### YamlStyleSheetLoaderTests (19 tests)

- Valid YAML parsing with all field types
- Default value application for optional fields
- All pattern types and severities
- Error handling (missing fields, syntax errors, duplicate IDs, invalid ID format)
- Embedded resource loading verification
- Validation method tests

### YamlSchemaValidatorTests (22 tests)

- Valid input acceptance
- Required field detection (sheet name, rule id/name/description/pattern)
- ID format validation (kebab-case)
- Duplicate ID detection
- Enum validation (category, severity, pattern_type)
- Regex pattern validation

---

## Verification

```bash
# Build verification
dotnet build --no-restore
# Build succeeded with 0 errors, 0 warnings

# Test verification
dotnet test --filter "FullyQualifiedName~Style"
# 113 tests passed
```

---

## Dependencies

| Package    | Version | Purpose                                 |
| :--------- | :------ | :-------------------------------------- |
| YamlDotNet | 15.1.6  | YAML parsing (already added in v0.2.1a) |
