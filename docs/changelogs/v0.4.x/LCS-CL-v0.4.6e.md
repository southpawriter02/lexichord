# Changelog: v0.4.6e - Axiom Data Model

**Version:** v0.4.6e  
**Codename:** Knowledge-Graph Constrained Value Store (CKVS Phase 1b)  
**Status:** ✅ Complete  
**Date:** 2026-02-01

## Summary

Implemented the foundational data model for the Axiom Store, defining record types and enums for domain rules (Axioms), validation constraints (AxiomRule), and validation results (AxiomViolation, AxiomValidationResult).

## Changes

### Lexichord.Abstractions

#### `Contracts/Knowledge/AxiomSeverity.cs` [NEW]

- **Added** `AxiomSeverity` enum with `Error`, `Warning`, `Info` levels

#### `Contracts/Knowledge/AxiomTargetKind.cs` [NEW]

- **Added** `AxiomTargetKind` enum with `Entity`, `Relationship`, `Claim` targets

#### `Contracts/Knowledge/AxiomConstraintType.cs` [NEW]

- **Added** `AxiomConstraintType` enum with 14 constraint types: `Required`, `OneOf`, `NotOneOf`, `Range`, `Pattern`, `Cardinality`, `NotBoth`, `RequiresTogether`, `Equals`, `NotEquals`, `Unique`, `ReferenceExists`, `TypeValid`, `Custom`

#### `Contracts/Knowledge/ConditionOperator.cs` [NEW]

- **Added** `ConditionOperator` enum with 9 operators for conditional rule evaluation

#### `Contracts/Knowledge/TextSpan.cs` [NEW]

- **Added** `TextSpan` record for document location tracking with computed `Length` property

#### `Contracts/Knowledge/AxiomCondition.cs` [NEW]

- **Added** `AxiomCondition` record for conditional clause evaluation

#### `Contracts/Knowledge/AxiomFix.cs` [NEW]

- **Added** `AxiomFix` record for suggested violation fixes with `Confidence` and `CanAutoApply` properties

#### `Contracts/Knowledge/AxiomRule.cs` [NEW]

- **Added** `AxiomRule` record with support for single-property, multi-property, range, cardinality, pattern, and conditional constraints

#### `Contracts/Knowledge/Axiom.cs` [NEW]

- **Added** `Axiom` record as the primary domain rule definition with Id, Name, TargetType, Rules, Severity, Category, Tags, and source tracking

#### `Contracts/Knowledge/AxiomViolation.cs` [NEW]

- **Added** `AxiomViolation` record capturing violation context with Entity/Relationship/Claim IDs, location, and suggested fixes

#### `Contracts/Knowledge/AxiomValidationResult.cs` [NEW]

- **Added** `AxiomValidationResult` record with `IsValid` computed property (only errors affect validity)
- **Added** `Valid()` and `WithViolations()` factory methods
- **Added** `BySeverity` grouping, `ErrorCount`, `WarningCount`, `InfoCount` computed properties

### Lexichord.Tests.Unit

#### `Abstractions/Knowledge/AxiomDataModelTests.cs` [NEW]

- **Added** 32 unit tests covering all enums and records
- **Added** Enum value and ordinal verification tests
- **Added** Record default value and equality tests
- **Added** Computed property tests (`TextSpan.Length`, `AxiomValidationResult.IsValid`)
- **Added** Factory method tests

## Technical Notes

- All 11 types placed in `Lexichord.Abstractions.Contracts.Knowledge` namespace
- Follows immutable record pattern with init-only properties
- XML documentation on all public types and members
- `AxiomValidationResult.IsValid` returns true even with warnings/info (only errors block)

## Test Results

- **New tests:** 32 (59 including theory cases)
- **Total suite:** 4,589 passed, 0 failed, 80 skipped
