# Lexichord Scope Breakdown Document: v0.19.3-RES
## Recovery & Repair Implementation

---

## 1. DOCUMENT CONTROL

| Field | Value |
|-------|-------|
| **Document ID** | LCS-SBD-v0.19.3-RES |
| **Version** | 0.19.3 |
| **Release Focus** | Recovery & Repair |
| **Total Scope Hours** | 58 hours |
| **Status** | Draft - Specification Phase |
| **Created** | 2026-02-01 |
| **Last Updated** | 2026-02-01 |
| **Target Release Date** | Q2 2026 |
| **Classification** | Internal - Product Development |

### Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 0.19.3-RES-DRAFT-001 | 2026-02-01 | System | Initial comprehensive specification |

---

## 2. EXECUTIVE SUMMARY

### 2.1 Vision & Objectives

The v0.19.3-RES (Recovery & Repair) release represents a critical enhancement to Lexichord's resilience and reliability. This version implements a comprehensive recovery and repair framework that enables Lexichord to:

- **Detect crashes** from multiple failure modes (application, system, power, memory)
- **Validate data integrity** across schemas, relationships, checksums, and consistency
- **Automatically repair** corrupted data and inconsistent state
- **Restore state** from snapshots with minimal data loss
- **Maintain transaction journals** for recovery and audit purposes
- **Detect corruption** before it propagates to end users

### 2.2 Business Impact

**Reliability Improvement**: From 99.5% uptime to 99.95% uptime through automatic recovery
**Data Loss Prevention**: Zero-data-loss recovery for non-catastrophic failures
**Operational Overhead**: Reduction in manual intervention and data restoration procedures
**Customer Trust**: Enhanced confidence in data safety and system stability
**Compliance**: Improved audit trails and recovery documentation

### 2.3 Technical Scope

This release introduces 6 coordinated sub-components totaling **58 development hours**:

| Component | Hours | Focus |
|-----------|-------|-------|
| Crash Detection & Recovery | 12 | Identify and respond to failure events |
| Data Integrity Validation | 10 | Verify database schema and data consistency |
| Automatic Repair Engine | 12 | Execute remediation strategies |
| State Snapshot & Restore | 10 | Capture and restore application state |
| Transaction Journal | 8 | Record and replay operations |
| Corruption Detection | 6 | Proactive corruption identification |
| **TOTAL** | **58** | |

### 2.4 Key Features

1. **Multi-mode Crash Detection**: Application, system, power, and memory failure detection
2. **Comprehensive Data Validation**: Schema, referential, checksum, and consistency validation
3. **Intelligent Repair Strategies**: Automated decision trees for various corruption types
4. **Snapshot Management**: Periodic snapshots with compression and deduplication
5. **Journal Replay**: Transaction-level recovery with idempotency guarantees
6. **Corruption Prevention**: Real-time monitoring and early warning systems

---

## 3. DETAILED SUB-PARTS BREAKDOWN

### 3.1 v0.19.3a: Crash Detection & Recovery (12 Hours)

#### 3.1.1 Overview

Crash detection and recovery implements the foundational mechanisms for identifying when Lexichord has experienced a failure, classifying that failure, and executing appropriate recovery procedures.

#### 3.1.2 Scope

- Implement crash detection for 4 major failure modes
- Build crash classification engine
- Create recovery workflow coordinator
- Integrate with logging and monitoring systems
- Design crash record persistence

#### 3.1.3 Deliverables

- `CrashDetectionService.cs` - Core detection logic
- `CrashClassifier.cs` - Failure mode classification
- `CrashRecoveryOrchestrator.cs` - Recovery workflow coordination
- `CrashRecord` model and repository
- Unit tests (minimum 30 tests)
- Integration tests with simulated crashes

#### 3.1.4 Acceptance Criteria

- [ ] Application crashes are detected within 2 seconds
- [ ] System-level crashes (unhandled exceptions, segmentation faults) are detected
- [ ] Power failure recovery is initiated on application restart
- [ ] OutOfMemory exceptions trigger graceful shutdown and recovery
- [ ] Crash classification accuracy >= 98%
- [ ] All crash events are persisted to database with full context
- [ ] Recovery workflow completes within 30 seconds for non-catastrophic failures
- [ ] Crash records include timestamp, exception data, stack trace, and system state
- [ ] Unit test coverage >= 85%
- [ ] Integration tests validate 4 crash types

#### 3.1.5 Technical Requirements

- **Language**: C# (.NET 7+)
- **Async Pattern**: Full async/await with CancellationToken support
- **Database**: PostgreSQL with connection pooling
- **Logging**: Serilog with structured logging
- **Monitoring**: Prometheus metrics for crash rates and recovery times

#### 3.1.6 Success Metrics

- Time to detect crash: < 2 seconds
- Time to classify crash: < 1 second
- Recovery orchestration time: < 10 seconds
- Crash record write success rate: 99.9%

---

### 3.2 v0.19.3b: Data Integrity Validation (10 Hours)

#### 3.2.1 Overview

Data integrity validation implements comprehensive checking mechanisms to ensure all data in the system conforms to defined rules, constraints, and consistency requirements.

#### 3.2.2 Scope

- Design and implement schema validation engine
- Build referential integrity checker
- Create checksum verification system
- Implement consistency validation rules
- Design validation result reporting

#### 3.2.3 Deliverables

- `DataIntegrityValidator.cs` - Main validation orchestrator
- `SchemaValidator.cs` - Schema compliance checking
- `ReferentialIntegrityChecker.cs` - Foreign key and relationship validation
- `ChecksumVerifier.cs` - Data authenticity verification
- `ConsistencyValidator.cs` - Cross-table consistency rules
- `IntegrityReport` model
- Validation rule configuration system
- Unit tests (minimum 40 tests)

#### 3.2.4 Acceptance Criteria

- [ ] Schema validation identifies all non-nullable constraint violations
- [ ] Referential integrity checks detect orphaned foreign key references
- [ ] Checksum verification detects data bit corruption
- [ ] Consistency checks validate business logic rules
- [ ] Validation can run on tables with 10M+ rows in < 5 minutes
- [ ] Validation is non-blocking to application operations (runs asynchronously)
- [ ] Integrity reports include violation count, severity, and affected records
- [ ] False positive rate < 0.1%
- [ ] Unit test coverage >= 90%
- [ ] Validation rules are configurable and extensible

#### 3.2.5 Technical Requirements

- **Validation Framework**: FluentValidation for rule definition
- **Performance**: Chunked processing for large datasets
- **Reporting**: JSON/CSV export capabilities
- **Concurrency**: Thread-safe concurrent validation

#### 3.2.6 Success Metrics

- Schema validation time for 100K records: < 10 seconds
- Referential integrity check time: < 20 seconds
- Checksum verification overhead: < 5% CPU
- False positive rate: < 0.1%

---

### 3.3 v0.19.3c: Automatic Repair Engine (12 Hours)

#### 3.3.1 Overview

The automatic repair engine intelligently analyzes detected data issues and executes remediation strategies without requiring manual intervention.

#### 3.3.2 Scope

- Design repair strategy decision tree
- Implement repair handlers for corruption types
- Build rollback capabilities
- Create repair logging and auditing
- Design repair progress tracking

#### 3.3.3 Deliverables

- `AutoRepairEngine.cs` - Main repair orchestrator
- `RepairStrategyAnalyzer.cs` - Decision logic
- `RepairHandlers/` directory with:
  - `SchemaRepairHandler.cs`
  - `ReferentialIntegrityRepairHandler.cs`
  - `DataCorruptionRepairHandler.cs`
  - `ConsistencyRepairHandler.cs`
- `RepairOperation` model and repository
- Rollback mechanism with transaction support
- Unit tests (minimum 35 tests)

#### 3.3.4 Acceptance Criteria

- [ ] Repair strategy selection is correct >= 95% of the time
- [ ] Repair operations are transactional and can be rolled back
- [ ] Failed repairs do not cause additional data corruption
- [ ] Repair status is tracked and reported in real-time
- [ ] Repair operations include full audit trail
- [ ] Critical repairs require explicit confirmation
- [ ] Non-critical repairs execute automatically
- [ ] Repair operations are idempotent
- [ ] Unit test coverage >= 85%
- [ ] Repair time for typical corruption patterns < 2 minutes

#### 3.3.5 Technical Requirements

- **Transaction Support**: Full ACID compliance for repair operations
- **Rollback**: Complete undo capability for all repairs
- **Audit Trail**: Comprehensive logging of all repair actions
- **Safety**: Dry-run mode for testing repairs before execution

#### 3.3.6 Success Metrics

- Strategy selection accuracy: >= 95%
- Repair success rate: >= 90%
- Average repair time: < 2 minutes
- Rollback completion: < 30 seconds

---

### 3.4 v0.19.3d: State Snapshot & Restore (10 Hours)

#### 3.4.1 Overview

State snapshot and restore provides a mechanism to capture periodic snapshots of application state and restore from those snapshots when needed.

#### 3.4.2 Scope

- Design snapshot data model
- Implement snapshot capture mechanisms
- Build snapshot compression and deduplication
- Create restoration workflow
- Implement snapshot versioning and retention

#### 3.4.3 Deliverables

- `StateSnapshotManager.cs` - Snapshot lifecycle management
- `SnapshotCaptureService.cs` - Snapshot creation
- `SnapshotRestoreService.cs` - Restoration logic
- `SnapshotCompressor.cs` - Compression implementation
- `SnapshotDeduplicator.cs` - Deduplication logic
- `StateSnapshot` model
- Snapshot scheduling and retention policies
- Unit tests (minimum 30 tests)

#### 3.4.4 Acceptance Criteria

- [ ] Snapshots are created at configurable intervals
- [ ] Snapshot creation for 100MB of state < 10 seconds
- [ ] Snapshot compression reduces size by >= 60% for typical workloads
- [ ] Incremental snapshots reduce storage by >= 80%
- [ ] Restoration completes within 20 seconds for 100MB snapshot
- [ ] Snapshots include metadata (timestamp, version, hash)
- [ ] Corrupted snapshots are detected and skipped
- [ ] Retention policies automatically clean up old snapshots
- [ ] Unit test coverage >= 80%
- [ ] Snapshots support point-in-time recovery

#### 3.4.5 Technical Requirements

- **Compression**: GZip or Brotli compression
- **Deduplication**: Content-addressable block storage
- **Storage**: Supports filesystem, S3-compatible, or database storage
- **Parallelism**: Multi-threaded snapshot capture and restoration

#### 3.4.6 Success Metrics

- Snapshot creation: < 10 seconds per 100MB
- Snapshot compression ratio: >= 60%
- Restoration time: < 20 seconds per 100MB
- Incremental snapshot overhead: < 1% of full snapshot

---

### 3.5 v0.19.3e: Transaction Journal (8 Hours)

#### 3.5.1 Overview

Transaction journal records all state-modifying operations, enabling replay-based recovery and providing complete audit trails.

#### 3.5.2 Scope

- Design journal data model
- Implement write-ahead logging (WAL)
- Build journal replay mechanism
- Create journal compaction and archival
- Implement idempotency guarantees

#### 3.5.3 Deliverables

- `TransactionJournal.cs` - Core journal implementation
- `JournalWriter.cs` - Efficient journal writes
- `JournalReplayer.cs` - Replay mechanism
- `JournalCompactor.cs` - Compaction and cleanup
- `JournalEntry` model
- Idempotency key tracking
- Unit tests (minimum 25 tests)

#### 3.5.4 Acceptance Criteria

- [ ] Journal entries are persisted before state changes complete
- [ ] Journal writes achieve >= 10K entries/second throughput
- [ ] Journal replay produces identical state to original execution
- [ ] Replay idempotency prevents duplicate state changes
- [ ] Journal compaction reduces disk usage by >= 70%
- [ ] Journal queries support filtering by date range and operation type
- [ ] Corrupted journal entries are detected and isolated
- [ ] Journal archival supports bulk export and retention policies
- [ ] Unit test coverage >= 85%
- [ ] Replay time for 100K operations < 30 seconds

#### 3.5.5 Technical Requirements

- **Write Pattern**: Write-ahead logging (WAL) for durability
- **Performance**: Lock-free or minimal locking for concurrent writes
- **Compression**: Delta compression for similar consecutive entries
- **Querying**: Efficient indexing for date/operation type filters

#### 3.5.6 Success Metrics

- Journal write throughput: >= 10K entries/second
- Journal compaction ratio: >= 70%
- Replay fidelity: 100%
- Replay performance: >= 3K entries/second

---

### 3.6 v0.19.3f: Corruption Detection (6 Hours)

#### 3.6.1 Overview

Corruption detection implements proactive mechanisms to identify data corruption before it propagates to end users or systems.

#### 3.6.1 Overview

Corruption detection implements proactive mechanisms to identify data corruption before it propagates to end users or systems.

#### 3.6.2 Scope

- Design corruption detection algorithms
- Implement periodic corruption scanning
- Build real-time corruption monitoring
- Create corruption alerts and notifications
- Design corruption pattern analysis

#### 3.6.3 Deliverables

- `CorruptionDetector.cs` - Core detection engine
- `PeriodicCorruptionScan.cs` - Scheduled scanning
- `RealTimeMonitor.cs` - Live monitoring
- `CorruptionAnalyzer.cs` - Pattern analysis
- `CorruptionAlert` model
- Unit tests (minimum 20 tests)

#### 3.6.4 Acceptance Criteria

- [ ] Corruption is detected within 1 minute of occurrence
- [ ] Detection false positive rate < 0.5%
- [ ] Corruption detection overhead < 2% CPU during normal operations
- [ ] Alerts include severity level and recommended actions
- [ ] Alerts support multiple notification channels (email, Slack, PagerDuty)
- [ ] Historical corruption patterns are analyzed for trend detection
- [ ] Undetected corruption is flagged in post-recovery analysis
- [ ] Detection algorithms cover bit flip, truncation, and injection attacks
- [ ] Unit test coverage >= 80%
- [ ] Detection latency < 60 seconds

#### 3.6.5 Technical Requirements

- **Algorithm**: Multi-layer detection (checksums, patterns, statistics)
- **Performance**: Minimal impact on normal operations
- **Alerting**: Integration with monitoring and alerting systems
- **Learning**: ML-based anomaly detection optional for future

#### 3.6.6 Success Metrics

- Detection latency: < 1 minute
- False positive rate: < 0.5%
- Overhead: < 2% CPU
- Alert response time: < 5 minutes

---

## 4. COMPLETE C# INTERFACES

### 4.1 ICrashRecoveryManager

```csharp
namespace Lexichord.Recovery.Core
{
    /// <summary>
    /// Manages crash detection and recovery orchestration.
    /// </summary>
    public interface ICrashRecoveryManager
    {
        /// <summary>
        /// Gets the current crash status.
        /// </summary>
        Task<CrashStatus> GetCrashStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects and records a crash event.
        /// </summary>
        Task<CrashRecord> DetectAndRecordCrashAsync(
            Exception exception,
            CrashType crashType,
            SystemState systemState,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates crash recovery workflow.
        /// </summary>
        Task<RecoveryResult> BeginRecoveryAsync(
            CrashRecord crashRecord,
            RecoveryOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recovery progress for an in-flight recovery operation.
        /// </summary>
        Task<RecoveryProgress> GetRecoveryProgressAsync(
            Guid recoveryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an in-flight recovery operation.
        /// </summary>
        Task<bool> CancelRecoveryAsync(
            Guid recoveryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets crash history for a date range.
        /// </summary>
        Task<IEnumerable<CrashRecord>> GetCrashHistoryAsync(
            DateTime startDate,
            DateTime endDate,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies system readiness for operations after recovery.
        /// </summary>
        Task<HealthCheckResult> VerifyRecoveryReadinessAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when a crash is detected.
        /// </summary>
        event EventHandler<CrashDetectedEventArgs> CrashDetected;

        /// <summary>
        /// Event raised when recovery completes.
        /// </summary>
        event EventHandler<RecoveryCompletedEventArgs> RecoveryCompleted;
    }

    /// <summary>
    /// Represents the type of crash.
    /// </summary>
    public enum CrashType
    {
        ApplicationCrash = 0,
        SystemCrash = 1,
        PowerFailure = 2,
        OutOfMemory = 3,
        StackOverflow = 4,
        Unknown = 99
    }

    /// <summary>
    /// Represents the result of a crash recovery operation.
    /// </summary>
    public class RecoveryResult
    {
        public Guid RecoveryId { get; set; }
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public string Message { get; set; }
        public RecoveryResultDetails Details { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class RecoveryResultDetails
    {
        public int DataFilesRecovered { get; set; }
        public int TransactionsReplayed { get; set; }
        public int ErrorsFixed { get; set; }
        public int WarningsIssued { get; set; }
        public int SnapshotsUsed { get; set; }
    }
}
```

### 4.2 IDataIntegrityValidator

```csharp
namespace Lexichord.Recovery.Validation
{
    /// <summary>
    /// Validates data integrity across multiple dimensions.
    /// </summary>
    public interface IDataIntegrityValidator
    {
        /// <summary>
        /// Performs comprehensive data integrity validation.
        /// </summary>
        Task<IntegrityReport> ValidateAsync(
            IntegrityValidationOptions options,
            IProgress<IntegrityValidationProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates schema conformance.
        /// </summary>
        Task<SchemaValidationResult> ValidateSchemaAsync(
            string tableName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates referential integrity (foreign keys).
        /// </summary>
        Task<ReferentialIntegrityResult> ValidateReferentialIntegrityAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates data checksums.
        /// </summary>
        Task<ChecksumValidationResult> ValidateChecksumsAsync(
            string tableName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates consistency across related tables.
        /// </summary>
        Task<ConsistencyValidationResult> ValidateConsistencyAsync(
            IEnumerable<string> tableNames,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed violation information.
        /// </summary>
        Task<IEnumerable<IntegrityViolation>> GetViolationsAsync(
            string tableName = null,
            IntegrityViolationType type = null,
            int limit = 1000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when violations are detected.
        /// </summary>
        event EventHandler<IntegrityViolationDetectedEventArgs> ViolationDetected;
    }

    public class IntegrityValidationOptions
    {
        public bool ValidateSchema { get; set; } = true;
        public bool ValidateReferentialIntegrity { get; set; } = true;
        public bool ValidateChecksums { get; set; } = true;
        public bool ValidateConsistency { get; set; } = true;
        public int ChunkSize { get; set; } = 10000;
        public int ParallelismDegree { get; set; } = Environment.ProcessorCount;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
        public List<string> TableNamesToValidate { get; set; } = null;
    }

    public class IntegrityReport
    {
        public Guid ReportId { get; set; }
        public DateTime ReportTime { get; set; }
        public TimeSpan Duration { get; set; }
        public SchemaValidationResult SchemaResult { get; set; }
        public ReferentialIntegrityResult ReferentialResult { get; set; }
        public ChecksumValidationResult ChecksumResult { get; set; }
        public ConsistencyValidationResult ConsistencyResult { get; set; }
        public int TotalViolations =>
            (SchemaResult?.ViolationCount ?? 0) +
            (ReferentialResult?.ViolationCount ?? 0) +
            (ChecksumResult?.ViolationCount ?? 0) +
            (ConsistencyResult?.ViolationCount ?? 0);
        public bool IsClean => TotalViolations == 0;
    }

    public class IntegrityViolation
    {
        public Guid ViolationId { get; set; }
        public string TableName { get; set; }
        public IntegrityViolationType Type { get; set; }
        public ViolationSeverity Severity { get; set; }
        public string Description { get; set; }
        public object AffectedData { get; set; }
        public DateTime DetectedAt { get; set; }
        public string RecommendedAction { get; set; }
    }

    public enum IntegrityViolationType
    {
        SchemaViolation,
        ReferentialIntegrityViolation,
        ChecksumMismatch,
        ConsistencyViolation,
        TypeMismatch,
        NullConstraintViolation,
        UniqueConstraintViolation
    }

    public enum ViolationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

### 4.3 IAutoRepairEngine

```csharp
namespace Lexichord.Recovery.Repair
{
    /// <summary>
    /// Orchestrates automatic repair of detected data issues.
    /// </summary>
    public interface IAutoRepairEngine
    {
        /// <summary>
        /// Analyzes violations and executes appropriate repairs.
        /// </summary>
        Task<RepairResult> RepairAsync(
            IEnumerable<IntegrityViolation> violations,
            RepairOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the repair strategy for a specific violation.
        /// </summary>
        Task<RepairStrategy> AnalyzeViolationAsync(
            IntegrityViolation violation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a specific repair operation.
        /// </summary>
        Task<RepairOperation> ExecuteRepairAsync(
            RepairStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back a completed repair operation.
        /// </summary>
        Task<bool> RollbackRepairAsync(
            Guid repairOperationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets repair history.
        /// </summary>
        Task<IEnumerable<RepairOperation>> GetRepairHistoryAsync(
            DateTime startDate,
            DateTime endDate,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a repair will not cause additional damage.
        /// </summary>
        Task<RepairValidationResult> ValidateRepairAsync(
            RepairStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when a repair completes.
        /// </summary>
        event EventHandler<RepairCompletedEventArgs> RepairCompleted;
    }

    public class RepairStrategy
    {
        public Guid StrategyId { get; set; }
        public IntegrityViolation Violation { get; set; }
        public RepairType RepairType { get; set; }
        public RepairSeverity Severity { get; set; }
        public bool RequiresApproval { get; set; }
        public string Description { get; set; }
        public List<string> PreConditions { get; set; } = new();
        public List<string> PostConditions { get; set; } = new();
        public TimeSpan EstimatedDuration { get; set; }
        public string RollbackStrategy { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum RepairType
    {
        DeleteInvalidRecord,
        RestoreFromSnapshot,
        RebuildIndex,
        UpdateInvalidData,
        RemoveOrphanedRecord,
        FixConstraintViolation,
        ReplayTransaction,
        TruncateAndReload
    }

    public enum RepairSeverity
    {
        Automatic,
        RequiresApproval,
        ManualOnly
    }

    public class RepairResult
    {
        public Guid RepairBatchId { get; set; }
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<RepairOperation> Operations { get; set; } = new();
        public int SuccessfulRepairs { get; set; }
        public int FailedRepairs { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class RepairOperation
    {
        public Guid OperationId { get; set; }
        public Guid? StrategyId { get; set; }
        public RepairType Type { get; set; }
        public RepairStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExecutionLog { get; set; }
        public string RollbackLog { get; set; }
        public object Details { get; set; }
    }

    public enum RepairStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        RolledBack
    }
}
```

### 4.4 IStateSnapshotManager

```csharp
namespace Lexichord.Recovery.Snapshots
{
    /// <summary>
    /// Manages snapshot lifecycle: creation, storage, retrieval, and restoration.
    /// </summary>
    public interface IStateSnapshotManager
    {
        /// <summary>
        /// Creates a snapshot of current application state.
        /// </summary>
        Task<StateSnapshot> CreateSnapshotAsync(
            SnapshotOptions options,
            IProgress<SnapshotProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores application state from a snapshot.
        /// </summary>
        Task<RestoreResult> RestoreFromSnapshotAsync(
            Guid snapshotId,
            RestoreOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific snapshot by ID.
        /// </summary>
        Task<StateSnapshot> GetSnapshotAsync(
            Guid snapshotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists snapshots within a date range.
        /// </summary>
        Task<IEnumerable<StateSnapshot>> ListSnapshotsAsync(
            DateTime startDate,
            DateTime endDate,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most recent snapshot.
        /// </summary>
        Task<StateSnapshot> GetLatestSnapshotAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a snapshot and its associated data.
        /// </summary>
        Task<bool> DeleteSnapshotAsync(
            Guid snapshotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates snapshot integrity.
        /// </summary>
        Task<SnapshotValidationResult> ValidateSnapshotAsync(
            Guid snapshotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage statistics.
        /// </summary>
        Task<SnapshotStorageStats> GetStorageStatsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when a snapshot is created.
        /// </summary>
        event EventHandler<SnapshotCreatedEventArgs> SnapshotCreated;

        /// <summary>
        /// Event raised when restoration completes.
        /// </summary>
        event EventHandler<RestoreCompletedEventArgs> RestoreCompleted;
    }

    public class StateSnapshot
    {
        public Guid SnapshotId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public long CompressedSizeBytes { get; set; }
        public long UncompressedSizeBytes { get; set; }
        public string CompressionAlgorithm { get; set; }
        public string StorageLocation { get; set; }
        public string SnapshotHash { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<SnapshotBlock> Blocks { get; set; } = new();
        public bool IsIncremental { get; set; }
        public Guid? BasedOnSnapshotId { get; set; }
        public SnapshotStatus Status { get; set; }
    }

    public enum SnapshotStatus
    {
        Creating,
        Ready,
        Validating,
        Corrupted,
        Archived,
        Deleted
    }

    public class SnapshotOptions
    {
        public bool Incremental { get; set; } = true;
        public string CompressionAlgorithm { get; set; } = "gzip";
        public int CompressionLevel { get; set; } = 6;
        public int ParallelismDegree { get; set; } = Environment.ProcessorCount;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class RestoreResult
    {
        public Guid RestoreId { get; set; }
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public Guid SnapshotId { get; set; }
        public int BlocksRestored { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class SnapshotBlock
    {
        public string BlockHash { get; set; }
        public long SizeBytes { get; set; }
        public string StoragePath { get; set; }
    }
}
```

### 4.5 ITransactionJournal

```csharp
namespace Lexichord.Recovery.Journal
{
    /// <summary>
    /// Records and replays state-modifying transactions.
    /// </summary>
    public interface ITransactionJournal
    {
        /// <summary>
        /// Records a transaction in the journal.
        /// </summary>
        Task<JournalEntry> RecordTransactionAsync(
            JournalTransaction transaction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Replays transactions from the journal.
        /// </summary>
        Task<JournalReplayResult> ReplayTransactionsAsync(
            DateTime startTime,
            DateTime endTime,
            JournalReplayOptions options,
            IProgress<JournalReplayProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets journal entries within a date range.
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetEntriesAsync(
            DateTime startDate,
            DateTime endDate,
            int limit = 10000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets journal entries filtered by operation type.
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetEntriesByOperationAsync(
            string operationType,
            DateTime startDate,
            DateTime endDate,
            int limit = 10000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Compacts the journal to reduce storage.
        /// </summary>
        Task<JournalCompactionResult> CompactAsync(
            JournalCompactionOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Archives old journal entries.
        /// </summary>
        Task<bool> ArchiveAsync(
            DateTime beforeDate,
            string archiveLocation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets journal statistics.
        /// </summary>
        Task<JournalStatistics> GetStatisticsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates journal integrity.
        /// </summary>
        Task<JournalValidationResult> ValidateAsync(
            CancellationToken cancellationToken = default);
    }

    public class JournalTransaction
    {
        public string TransactionId { get; set; }
        public string OperationType { get; set; }
        public DateTime Timestamp { get; set; }
        public string IdempotencyKey { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new();
        public string UserId { get; set; }
        public string SourceSystem { get; set; }
    }

    public class JournalEntry
    {
        public Guid EntryId { get; set; }
        public string TransactionId { get; set; }
        public string OperationType { get; set; }
        public DateTime Timestamp { get; set; }
        public string IdempotencyKey { get; set; }
        public byte[] Payload { get; set; }
        public string PayloadHash { get; set; }
        public JournalEntryStatus Status { get; set; }
        public long SequenceNumber { get; set; }
    }

    public enum JournalEntryStatus
    {
        Recorded,
        Replayed,
        Archived,
        Purged
    }

    public class JournalReplayOptions
    {
        public bool ValidateIdempotency { get; set; } = true;
        public int ParallelismDegree { get; set; } = 1;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(60);
        public bool DryRun { get; set; } = false;
        public List<string> OperationTypesToReplay { get; set; } = null;
    }

    public class JournalReplayResult
    {
        public Guid ReplayId { get; set; }
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long EntriesReplayed { get; set; }
        public long EntriesSkipped { get; set; }
        public long EntriesFailed { get; set; }
        public List<JournalReplayError> Errors { get; set; } = new();
    }

    public class JournalReplayError
    {
        public Guid EntryId { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
    }
}
```

### 4.6 ICorruptionDetector

```csharp
namespace Lexichord.Recovery.Detection
{
    /// <summary>
    /// Detects data corruption through multiple mechanisms.
    /// </summary>
    public interface ICorruptionDetector
    {
        /// <summary>
        /// Performs real-time corruption monitoring.
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops real-time corruption monitoring.
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Performs a scan for corruption in a table.
        /// </summary>
        Task<CorruptionScanResult> ScanTableAsync(
            string tableName,
            CorruptionScanOptions options,
            IProgress<CorruptionScanProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans all tables for corruption.
        /// </summary>
        Task<CorruptionScanResult> ScanAllTablesAsync(
            CorruptionScanOptions options,
            IProgress<CorruptionScanProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detected corruption issues.
        /// </summary>
        Task<IEnumerable<CorruptionIssue>> GetIssuesAsync(
            DateTime startDate,
            DateTime endDate,
            int limit = 1000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets corruption trend analysis.
        /// </summary>
        Task<CorruptionTrendAnalysis> AnalyzeTrendsAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when corruption is detected.
        /// </summary>
        event EventHandler<CorruptionDetectedEventArgs> CorruptionDetected;
    }

    public class CorruptionScanResult
    {
        public Guid ScanId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public string TableName { get; set; }
        public long RecordsScanned { get; set; }
        public long CorruptedRecords { get; set; }
        public List<CorruptionIssue> Issues { get; set; } = new();
    }

    public class CorruptionIssue
    {
        public Guid IssueId { get; set; }
        public string TableName { get; set; }
        public CorruptionType Type { get; set; }
        public CorruptionSeverity Severity { get; set; }
        public string Description { get; set; }
        public object AffectedData { get; set; }
        public DateTime DetectedAt { get; set; }
        public string RecommendedAction { get; set; }
    }

    public enum CorruptionType
    {
        BitFlip,
        Truncation,
        Injection,
        Metadata,
        Index,
        Constraint,
        Unknown
    }

    public enum CorruptionSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class CorruptionScanOptions
    {
        public int ChunkSize { get; set; } = 10000;
        public int ParallelismDegree { get; set; } = Environment.ProcessorCount;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(60);
        public bool ScanIndexes { get; set; } = true;
        public bool ScanMetadata { get; set; } = true;
    }
}
```

---

## 5. ASCII ARCHITECTURE DIAGRAMS

### 5.1 Recovery Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         APPLICATION RUNNING                          │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │   CRASH DETECTION SERVICE   │
                    └──────┬───────────────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
    ▼────▼─────┐      ▼────▼─────┐      ▼────▼──────┐
  ┌─────────┐ ┌─────────────┐ ┌──────────────┐
  │ App     │ │ System      │ │ Power        │
  │ Crash   │ │ Crash       │ │ Failure      │
  │ Handler │ │ Handler     │ │ Handler      │
  └────┬────┘ └──────┬──────┘ └───────┬──────┘
       │             │                 │
       └─────────────┼─────────────────┘
                     │
              ┌──────▼──────────┐
              │ CRASH CLASSIFIER│
              └──────┬──────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
    ▼───▼──┐  ▼──────▼──┐  ▼─────▼───┐
  ┌─────────┐ ┌───────────┐ ┌──────────┐
  │ Severity│ │Crash Type │ │ Context  │
  │Analysis │ │Detection  │ │Capture   │
  └────┬────┘ └─────┬─────┘ └────┬─────┘
       │            │            │
       └────────────┼────────────┘
                    │
           ┌────────▼────────┐
           │CRASH RECORD SAVE│
           └────────┬────────┘
                    │
      ┌─────────────┼─────────────┐
      │             │             │
  ▼───▼──────┐  ▼──▼────────┐  ▼──▼───────┐
┌──────────┐┌───────────┐┌─────────────┐
│ Database ││Data Integ-││Prepare Snap-│
│ Persist  ││rity Check ││ shot Restore│
└────┬─────┘└─────┬──────┘└──────┬──────┘
     │            │              │
     └────────────┼──────────────┘
                  │
          ┌───────▼────────┐
          │ REPAIR ENGINE  │
          └───────┬────────┘
                  │
          ┌───────▼────────┐
          │ RESTORE STATE  │
          └───────┬────────┘
                  │
          ┌───────▼────────┐
          │ HEALTH CHECK   │
          └───────┬────────┘
                  │
         ┌────────▼────────┐
         │RECOVERY COMPLETE│
         └─────────────────┘
```

### 5.2 Snapshot & Restore Process

```
┌──────────────────────────────────────────────────────────┐
│          SNAPSHOT CREATION WORKFLOW                       │
└────────────────────────┬─────────────────────────────────┘
                         │
              ┌──────────▼───────────┐
              │ SNAPSHOT MANAGER     │
              └──────────┬───────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
    ▼────▼───┐      ▼────▼────┐     ▼───▼────┐
  ┌────────┐ ┌──────────────┐ ┌────────────┐
  │ Capture│ │ Compression  │ │Deduplicat- │
  │ State  │ │ (GZip/Brot)  │ │ion (SHA256)│
  └────┬───┘ └──────┬───────┘ └────┬───────┘
       │            │              │
       └────────────┼──────────────┘
                    │
         ┌──────────▼──────────┐
         │ SNAPSHOT BLOCKS     │
         │ ┌─────────────────┐ │
         │ │Block 1: Hash    │ │
         │ │Block 2: Hash    │ │
         │ │Block N: Hash    │ │
         │ └─────────────────┘ │
         └──────────┬──────────┘
                    │
         ┌──────────▼──────────┐
         │ STORAGE ENGINE      │
         ├─────────────────────┤
         │ FS / S3 / DB        │
         └──────────┬──────────┘
                    │
         ┌──────────▼──────────┐
         │ SNAPSHOT METADATA   │
         │ ┌─────────────────┐ │
         │ │ID, Timestamp    │ │
         │ │Size, Hash       │ │
         │ │Retention, Blocks│ │
         │ └─────────────────┘ │
         └─────────────────────┘

┌──────────────────────────────────────────────────────────┐
│          RESTORE FROM SNAPSHOT                           │
└────────────────────────┬─────────────────────────────────┘
                         │
              ┌──────────▼───────────┐
              │ SELECT SNAPSHOT      │
              └──────────┬───────────┘
                         │
          ┌──────────────▼──────────────┐
          │ VERIFY SNAPSHOT INTEGRITY   │
          │ ┌────────────────────────┐  │
          │ │- Hash check            │  │
          │ │- Block availability    │  │
          │ │- Metadata validation   │  │
          │ └────────────────────────┘  │
          └──────────────┬───────────────┘
                         │
              ┌──────────▼───────────┐
              │ LOAD BLOCKS          │
              └──────────┬───────────┘
                         │
              ┌──────────▼───────────┐
              │ DECOMPRESS DATA      │
              └──────────┬───────────┘
                         │
              ┌──────────▼───────────┐
              │ RESTORE TO DATABASE  │
              │ Chunked transactions │
              └──────────┬───────────┘
                         │
              ┌──────────▼───────────┐
              │ INTEGRITY CHECK      │
              └──────────┬───────────┘
                         │
              ┌──────────▼───────────┐
              │ RESTORE COMPLETE     │
              └──────────────────────┘
```

### 5.3 Journal Replay Process

```
┌──────────────────────────────────────────────────────────┐
│                  TRANSACTION JOURNAL                      │
├──────────────────────────────────────────────────────────┤
│ Entry 1: INSERT user_id=101, timestamp=T0               │
│ Entry 2: UPDATE product_id=5, stock=40, timestamp=T1    │
│ Entry 3: DELETE order_id=789, timestamp=T2              │
│ ...                                                       │
│ Entry N: INSERT audit_log, timestamp=TN                 │
└─────────────────────┬──────────────────────────────────┘
                      │
           ┌──────────▼──────────┐
           │ JOURNAL SELECTOR    │
           │ Pick start/end time │
           └──────────┬──────────┘
                      │
           ┌──────────▼──────────┐
           │ IDEMPOTENCY CHECK   │
           │ Prevent duplicates  │
           └──────────┬──────────┘
                      │
         ┌────────────▼────────────┐
         │ VALIDATE PREREQUISITES  │
         │ Dependencies met?       │
         └────────────┬────────────┘
                      │
         ┌────────────▼────────────┐
         │ EXECUTE TRANSACTIONS    │
         │ (parallel or sequential)│
         └────────────┬────────────┘
                      │
      ┌───────────────┼───────────────┐
      │               │               │
  ▼───▼────┐      ▼───▼────┐     ▼───▼────┐
┌────────┐ ┌──────────┐ ┌────────────┐
│ INSERT │ │ UPDATE   │ │ DELETE     │
└────┬───┘ └────┬─────┘ └────┬───────┘
     │          │            │
     └──────────┼────────────┘
                │
       ┌────────▼────────┐
       │ TRANSACTION LOG │
       └────────┬────────┘
                │
       ┌────────▼────────┐
       │ REPLAY COMPLETE │
       └─────────────────┘
```

---

## 6. CRASH TYPES AND RECOVERY STRATEGIES

### 6.1 ApplicationCrash Recovery

**Symptoms**:
- Unhandled exception in application code
- NullReferenceException, ArgumentException, etc.
- Stack overflow in recursive call
- Deadlock in multi-threaded code

**Detection**:
- AppDomain.CurrentDomain.UnhandledException
- TaskScheduler.UnobservedTaskException
- Custom exception handlers in service boundaries

**Recovery Strategy**:
```
1. Log exception with full context (stack trace, thread state, variables)
2. Capture application state snapshot
3. Terminate problematic threads gracefully
4. Reset application state from most recent snapshot
5. Replay transaction journal from snapshot timestamp
6. Verify data integrity
7. Restart affected components
8. Resume normal operations
```

**Recovery Time**: 30-60 seconds
**Data Loss**: None (transaction journal provides durability)
**Success Rate**: 95%+

### 6.2 SystemCrash Recovery

**Symptoms**:
- OS-level exceptions (segmentation fault)
- System resource exhaustion
- OS signaling (SIGSEGV, SIGABRT)
- Kernel panic indication

**Detection**:
- Application fails to start after reboot
- System event log analysis on startup
- Database consistency check on recovery
- Incomplete transaction detection

**Recovery Strategy**:
```
1. Check for unfinished transactions in journal
2. Load most recent complete snapshot
3. Replay committed transactions from journal
4. Verify database integrity
5. Roll back incomplete transactions
6. Rebuild indexes if corrupted
7. Verify application state consistency
8. Resume normal operations
```

**Recovery Time**: 60-120 seconds
**Data Loss**: Minimal (in-flight transactions may be lost)
**Success Rate**: 85-90%

### 6.3 PowerFailure Recovery

**Symptoms**:
- Sudden termination of all processes
- Unclean database shutdown
- Incomplete writes to disk
- Lost in-memory state

**Detection**:
- Application fails to start
- Database recovery log analysis
- Unexpected process termination log
- System uptime reset

**Recovery Strategy**:
```
1. Check database for recovery state
2. Run database WAL (Write-Ahead Log) recovery
3. Load most recent stable snapshot
4. Identify and skip uncommitted transactions
5. Replay committed transactions from journal
6. Perform full data integrity check
7. Rebuild corrupted indexes
8. Resume operations
```

**Recovery Time**: 120-300 seconds
**Data Loss**: Transactions after last checkpoint
**Success Rate**: 90-95%

### 6.4 OutOfMemory Recovery

**Symptoms**:
- OutOfMemoryException thrown
- Application becomes unresponsive
- GC unable to free memory
- Managed heap corruption possible

**Detection**:
- OutOfMemoryException caught
- Memory pressure monitoring
- GC collection frequency increases
- Heap size monitoring

**Recovery Strategy**:
```
1. Catch OutOfMemoryException
2. Force garbage collection
3. Clear in-memory caches
4. Release temporary objects
5. Flush pending I/O operations
6. Snapshot current state immediately
7. Graceful shutdown if unable to recover
8. On restart: load snapshot and replay journal
```

**Recovery Time**: 10-30 seconds (if successful)
**Data Loss**: Depends on exception location
**Success Rate**: 60-70%

---

## 7. DATA INTEGRITY CHECK CATEGORIES

### 7.1 Schema Validation

**Purpose**: Verify that stored data conforms to table schemas

**Checks**:
- Column data types match schema definition
- NULL constraints are enforced
- String length constraints are met
- Numeric ranges are valid
- Enum values are within allowed set
- Foreign key columns reference existing rows
- Primary key columns are non-null and unique
- Datetime columns contain valid dates

**Algorithm**:
```sql
-- Pseudo-code
FOR EACH table
  FOR EACH column
    IF column.type = VARCHAR
      VERIFY MAX(LENGTH(column)) <= column.maxLength
    ELSE IF column.type = NUMERIC
      VERIFY MIN(column) >= column.minValue
      VERIFY MAX(column) <= column.maxValue
    ELSE IF column.notNull = TRUE
      VERIFY COUNT(column IS NULL) = 0
    END IF
  END FOR
END FOR
```

**Performance**: < 2 seconds for 100K rows
**Coverage**: 100% of schema constraints

### 7.2 Referential Integrity

**Purpose**: Verify relationships between tables are maintained

**Checks**:
- Foreign key values reference existing primary keys
- No orphaned child records
- Cascading deletes completed properly
- Update cascades applied correctly
- Circular references detected
- Reference counts match join results

**Algorithm**:
```
FOR EACH foreign_key_constraint (parent_table, child_table, fk_column)
  IDENTIFY orphaned records:
    SELECT child_table.*
    FROM child_table
    LEFT JOIN parent_table
      ON child_table.fk_column = parent_table.pk_column
    WHERE parent_table.pk_column IS NULL
  IF orphaned records exist
    FLAG AS VIOLATION
  END IF
END FOR
```

**Performance**: < 5 seconds for 100K rows
**Coverage**: All foreign key constraints

### 7.3 Checksum Verification

**Purpose**: Detect bit-level data corruption

**Checks**:
- Row checksums match computed hashes
- Record modification times are consistent
- Stored vs. computed checksums match
- Hash chain integrity (if implemented)

**Algorithm**:
```csharp
// Pseudo-code
SHA256 stored_checksum = row["checksum"];
string row_data = ConcatenateAllColumns(row);
SHA256 computed_checksum = SHA256.ComputeHash(row_data);

if (stored_checksum != computed_checksum)
  RECORD_VIOLATION("Checksum mismatch");
```

**Performance**: < 30 seconds for 100K rows
**Coverage**: 100% of rows with checksums

### 7.4 Consistency Validation

**Purpose**: Verify business logic consistency across tables

**Checks**:
- Order totals match sum of line items
- Account balances match transaction history
- Inventory counts match warehouse records
- User permissions consistent with roles
- Date relationships are logical (end_date > start_date)
- Status transitions are valid
- Aggregate totals match detailed records

**Example Rules**:
```
Rule: Order.total_amount = SUM(OrderLine.amount)
Rule: Account.balance = SUM(Transaction.amount) WHERE account_id = Account.id
Rule: Inventory.on_hand = Warehouse.stock_count
Rule: User.role_id EXISTS IN Role table
```

**Performance**: Variable (10-60 seconds for 100K records)
**Coverage**: Business logic requirements

---

## 8. POSTGRESQL SCHEMA

### 8.1 crash_records Table

```sql
CREATE TABLE crash_records (
    crash_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    crash_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    crash_type VARCHAR(50) NOT NULL,
    exception_type VARCHAR(255),
    exception_message TEXT,
    stack_trace TEXT,
    system_state JSONB,
    affected_components TEXT[],
    severity VARCHAR(20),
    recovery_started_at TIMESTAMP WITH TIME ZONE,
    recovery_completed_at TIMESTAMP WITH TIME ZONE,
    recovery_successful BOOLEAN,
    recovery_notes TEXT,
    diagnostics JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT crash_type_check CHECK (crash_type IN (
        'ApplicationCrash', 'SystemCrash', 'PowerFailure', 'OutOfMemory'
    )),
    CONSTRAINT severity_check CHECK (severity IN (
        'Low', 'Medium', 'High', 'Critical'
    ))
);

CREATE INDEX idx_crash_records_timestamp
    ON crash_records(crash_timestamp DESC);
CREATE INDEX idx_crash_records_type
    ON crash_records(crash_type);
CREATE INDEX idx_crash_records_successful
    ON crash_records(recovery_successful);
```

### 8.2 integrity_checks Table

```sql
CREATE TABLE integrity_checks (
    check_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    check_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    table_name VARCHAR(255) NOT NULL,
    check_type VARCHAR(50) NOT NULL,
    total_records_checked BIGINT,
    violations_found INTEGER DEFAULT 0,
    check_duration_ms INTEGER,
    status VARCHAR(20) DEFAULT 'completed',
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT check_type_check CHECK (check_type IN (
        'SchemaValidation', 'ReferentialIntegrity',
        'ChecksumVerification', 'ConsistencyValidation'
    )),
    CONSTRAINT status_check CHECK (status IN (
        'pending', 'in_progress', 'completed', 'failed'
    ))
);

CREATE INDEX idx_integrity_checks_timestamp
    ON integrity_checks(check_timestamp DESC);
CREATE INDEX idx_integrity_checks_table
    ON integrity_checks(table_name);
CREATE INDEX idx_integrity_checks_violations
    ON integrity_checks(violations_found)
    WHERE violations_found > 0;
```

### 8.3 repair_operations Table

```sql
CREATE TABLE repair_operations (
    repair_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    violation_id UUID,
    repair_type VARCHAR(50) NOT NULL,
    repair_status VARCHAR(20) DEFAULT 'pending',
    severity VARCHAR(20),
    affected_records INTEGER,
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    execution_log TEXT,
    rollback_log TEXT,
    dry_run BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT repair_type_check CHECK (repair_type IN (
        'DeleteInvalidRecord', 'RestoreFromSnapshot', 'RebuildIndex',
        'UpdateInvalidData', 'RemoveOrphanedRecord',
        'FixConstraintViolation', 'ReplayTransaction', 'TruncateAndReload'
    )),
    CONSTRAINT repair_status_check CHECK (repair_status IN (
        'pending', 'in_progress', 'completed', 'failed', 'rolled_back'
    )),
    CONSTRAINT severity_check CHECK (severity IN (
        'Low', 'Medium', 'High', 'Critical'
    ))
);

CREATE INDEX idx_repair_operations_status
    ON repair_operations(repair_status);
CREATE INDEX idx_repair_operations_timestamp
    ON repair_operations(created_at DESC);
CREATE INDEX idx_repair_operations_type
    ON repair_operations(repair_type);
```

### 8.4 state_snapshots Table

```sql
CREATE TABLE state_snapshots (
    snapshot_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,
    compressed_size_bytes BIGINT,
    uncompressed_size_bytes BIGINT,
    compression_algorithm VARCHAR(50),
    storage_location VARCHAR(500),
    snapshot_hash VARCHAR(64),
    is_incremental BOOLEAN DEFAULT FALSE,
    based_on_snapshot_id UUID,
    status VARCHAR(20) DEFAULT 'ready',
    metadata JSONB,
    validation_result JSONB,
    last_accessed_at TIMESTAMP WITH TIME ZONE,
    CONSTRAINT status_check CHECK (status IN (
        'creating', 'ready', 'validating', 'corrupted', 'archived', 'deleted'
    )),
    CONSTRAINT size_check CHECK (
        compressed_size_bytes IS NULL OR compressed_size_bytes >= 0
    ),
    FOREIGN KEY (based_on_snapshot_id) REFERENCES state_snapshots(snapshot_id)
);

CREATE INDEX idx_state_snapshots_created
    ON state_snapshots(created_at DESC);
CREATE INDEX idx_state_snapshots_expires
    ON state_snapshots(expires_at);
CREATE INDEX idx_state_snapshots_status
    ON state_snapshots(status);
```

### 8.5 transaction_journal Table

```sql
CREATE TABLE transaction_journal (
    entry_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id VARCHAR(255) NOT NULL UNIQUE,
    operation_type VARCHAR(100) NOT NULL,
    entry_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    idempotency_key VARCHAR(255),
    payload BYTEA NOT NULL,
    payload_hash VARCHAR(64),
    entry_status VARCHAR(20) DEFAULT 'recorded',
    sequence_number BIGSERIAL UNIQUE,
    user_id VARCHAR(255),
    source_system VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT entry_status_check CHECK (entry_status IN (
        'recorded', 'replayed', 'archived', 'purged'
    ))
);

CREATE INDEX idx_transaction_journal_timestamp
    ON transaction_journal(entry_timestamp DESC);
CREATE INDEX idx_transaction_journal_operation_type
    ON transaction_journal(operation_type);
CREATE INDEX idx_transaction_journal_idempotency
    ON transaction_journal(idempotency_key)
    WHERE idempotency_key IS NOT NULL;
CREATE INDEX idx_transaction_journal_sequence
    ON transaction_journal(sequence_number DESC);
```

### 8.6 corruption_issues Table

```sql
CREATE TABLE corruption_issues (
    issue_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    table_name VARCHAR(255) NOT NULL,
    corruption_type VARCHAR(50) NOT NULL,
    severity VARCHAR(20),
    detected_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    description TEXT,
    affected_data JSONB,
    recommended_action TEXT,
    resolved_at TIMESTAMP WITH TIME ZONE,
    resolution_details JSONB,
    CONSTRAINT corruption_type_check CHECK (corruption_type IN (
        'BitFlip', 'Truncation', 'Injection', 'Metadata',
        'Index', 'Constraint', 'Unknown'
    )),
    CONSTRAINT severity_check CHECK (severity IN (
        'Low', 'Medium', 'High', 'Critical'
    ))
);

CREATE INDEX idx_corruption_issues_detected
    ON corruption_issues(detected_at DESC);
CREATE INDEX idx_corruption_issues_table
    ON corruption_issues(table_name);
CREATE INDEX idx_corruption_issues_unresolved
    ON corruption_issues(severity)
    WHERE resolved_at IS NULL;
```

---

## 9. UI MOCKUPS

### 9.1 Recovery Wizard

```
┌─────────────────────────────────────────────────────────────┐
│ LEXICHORD RECOVERY WIZARD                            [x]    │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Step 3 of 5: Select Recovery Strategy                      │
│  ════════════════════════════════════════════════════════   │
│                                                               │
│  Crash Detected:  ApplicationCrash in UserService          │
│  Time:            2026-02-01 14:35:22 UTC                  │
│  Severity:        High                                       │
│                                                               │
│  ┌─ Recovery Options ───────────────────────────────────┐  │
│  │                                                        │  │
│  │ ◉ Restore from Latest Snapshot (Recommended)         │  │
│  │   • Snapshot: 2026-02-01 14:30:00 UTC               │  │
│  │   • Data Loss: ~5 minutes                            │  │
│  │   • Est. Time: 15 seconds                            │  │
│  │                                                        │  │
│  │ ○ Replay Transaction Journal                         │  │
│  │   • Data Loss: None                                  │  │
│  │   • Est. Time: 45 seconds                            │  │
│  │   • Performance: Medium impact during recovery       │  │
│  │                                                        │  │
│  │ ○ Automatic Repair Only                              │  │
│  │   • Fix detected issues without full restore         │  │
│  │   • Data Loss: None                                  │  │
│  │   • Est. Time: 60 seconds                            │  │
│  │                                                        │  │
│  │ ○ Manual Review (Experts Only)                       │  │
│  │   • Requires manual intervention                     │  │
│  │   • Full diagnostic data available                   │  │
│  │                                                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  Available Snapshots:                                        │
│  • 2026-02-01 14:30:00 UTC (1.2 GB) ✓ Validated           │
│  • 2026-02-01 14:20:00 UTC (1.1 GB) ✓ Validated           │
│  • 2026-02-01 14:10:00 UTC (1.2 GB) ✓ Validated           │
│                                                               │
│ ┌──────────────────────────────────────────────────────┐   │
│ │ ▢ Automatically start recovery if approved            │   │
│ │ ▢ Send notification when complete                    │   │
│ │ ▢ Create backup before starting recovery             │   │
│ └──────────────────────────────────────────────────────┘   │
│                                                               │
│                                                               │
│  [Previous] [Cancel] [Start Recovery >]                     │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### 9.2 Integrity Report

```
┌─────────────────────────────────────────────────────────────┐
│ DATA INTEGRITY REPORT                               [Share] │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│ Report ID:  INT-2026-0201-001                               │
│ Generated:  2026-02-01 15:45:30 UTC                         │
│ Duration:   2m 34s                                           │
│ Status:     ⚠ ISSUES DETECTED                              │
│                                                               │
│ ┌─ Summary ─────────────────────────────────────────────┐  │
│ │                                                         │  │
│ │ Tables Checked:      127                              │  │
│ │ Records Scanned:     4.2M                             │  │
│ │ Issues Found:        8                                │  │
│ │ Critical Issues:     2                                │  │
│ │ High Issues:         3                                │  │
│ │ Medium Issues:       2                                │  │
│ │ Low Issues:          1                                │  │
│ │                                                         │  │
│ └─────────────────────────────────────────────────────────┘  │
│                                                               │
│ ┌─ Validation Results ──────────────────────────────────┐   │
│ │                                                         │   │
│ │ ✓ Schema Validation:        PASS (127 tables)        │   │
│ │ ⚠ Referential Integrity:    ISSUES (2 violations)    │   │
│ │ ⚠ Checksum Verification:    ISSUES (3 violations)    │   │
│ │ ⚠ Consistency Validation:   ISSUES (3 violations)    │   │
│ │                                                         │   │
│ └─────────────────────────────────────────────────────────┘   │
│                                                               │
│ ┌─ Critical Issues ─────────────────────────────────────┐   │
│ │                                                         │   │
│ │ 1. Orphaned Order Records                             │   │
│ │    Table: orders                                       │   │
│ │    Count: 47 records                                  │   │
│ │    Issue: Customer_ID references non-existent rows   │   │
│ │    Status: Ready for Repair                           │   │
│ │    [View Details] [Preview Repair] [Approve Repair]  │   │
│ │                                                         │   │
│ │ 2. Checksum Mismatches                                │   │
│ │    Table: products                                     │   │
│ │    Count: 12 records                                  │   │
│ │    Issue: Stored checksum != calculated checksum    │   │
│ │    Status: Ready for Repair                           │   │
│ │    [View Details] [Preview Repair] [Approve Repair]  │   │
│ │                                                         │   │
│ └─────────────────────────────────────────────────────────┘   │
│                                                               │
│ [Export as PDF] [Export as CSV] [Autofix Issues]            │
│ [Schedule Repair] [Archive Report] [Close]                  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### 9.3 Snapshot Manager

```
┌─────────────────────────────────────────────────────────────┐
│ SNAPSHOT MANAGER                                   [Refresh] │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│ Storage Overview:                                            │
│ ████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ 45.2 / 100 GB│
│                                                               │
│ Filter:  [Snapshot Type ▼] [Status ▼] [Date Range ▼]      │
│ Sort By: [Created Date ▼]                                  │
│                                                               │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ Snapshot ID      │ Created       │ Type    │ Size  │ Act│
│ ├────────────────────────────────────────────────────────┤  │
│ │ snap-002a3f...   │ Today 14:30   │ Full    │ 1.2G  │ ⋯ │
│ │ • Hash: 7a4e...  │               │ Status: │ Ready │   │
│ │ • Dedup: 68%     │               │ ✓       │       │   │
│ │                                                         │  │
│ │ snap-001b2c...   │ Today 14:00   │ Incr.   │ 120M  │ ⋯ │
│ │ • Hash: 5f2d...  │               │ Status: │ Ready │   │
│ │ • Dedup: 71%     │               │ ✓       │       │   │
│ │                                                         │  │
│ │ snap-0019ef...   │ Yesterday     │ Full    │ 1.2G  │ ⋯ │
│ │ • Hash: 3c1a...  │ 22:00         │ Status: │ Ready │   │
│ │ • Dedup: 65%     │               │ ✓       │       │   │
│ │                                                         │  │
│ │ snap-0018dd...   │ Yesterday     │ Incr.   │ 98M   │ ⋯ │
│ │ • Hash: 2e09...  │ 18:00         │ Status: │ Ready │   │
│ │ • Dedup: 72%     │               │ ✓       │       │   │
│ │                                                         │  │
│ └────────────────────────────────────────────────────────┘  │
│                                                               │
│ Selected Snapshot: snap-002a3f                               │
│ ┌─ Actions ─────────────────────────────────────────────┐  │
│ │ [Restore from Snapshot]  [Validate]                   │  │
│ │ [View Details]  [Download]  [Delete]  [Clone]         │  │
│ │ [Copy to S3]  [Create Backup]  [Schedule for Archive] │  │
│ └───────────────────────────────────────────────────────┘  │
│                                                               │
│ Retention Policy: Keep latest 10 full + all from last 7 days│
│ Next Cleanup: 2026-02-08                                    │
│                                                               │
│ [Create New Snapshot] [Cleanup Now] [Settings]              │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 10. DEPENDENCY CHAIN

### 10.1 Internal Dependencies

```
v0.19.3a (Crash Detection)
├── Requires: Logging infrastructure (v0.17.0+)
├── Requires: Database connectivity (v0.18.0+)
├── Provides: Crash records and detection events

v0.19.3b (Data Integrity Validation)
├── Requires: Database schema definitions
├── Requires: Validation framework (FluentValidation)
├── Provides: Integrity reports and violation data

v0.19.3c (Automatic Repair Engine)
├── Requires: v0.19.3a (crash context)
├── Requires: v0.19.3b (violation data)
├── Requires: Transaction support
├── Provides: Repair operations and rollback capabilities

v0.19.3d (State Snapshot & Restore)
├── Requires: Data serialization layer
├── Requires: Storage abstraction (filesystem/S3/DB)
├── Requires: Compression libraries
├── Provides: Snapshot management and point-in-time recovery

v0.19.3e (Transaction Journal)
├── Requires: Write-ahead logging infrastructure
├── Requires: Serialization framework
├── Provides: Transaction record persistence

v0.19.3f (Corruption Detection)
├── Requires: v0.19.3b (validation)
├── Requires: Monitoring infrastructure
├── Provides: Corruption alerts and pattern analysis
```

### 10.2 External Dependencies

**NuGet Packages**:
- FluentValidation 11.8.0+
- Serilog 3.0.0+
- Npgsql 8.0.0+
- MediatR 12.0.0+
- Polly 8.0.0+
- GZipStream (System.IO.Compression)
- Brotli (System.IO.Compression.Brotli)

**System Requirements**:
- .NET 7.0 or higher
- PostgreSQL 13+
- 2GB minimum RAM
- 10GB minimum disk for snapshots

**Integration Points**:
- Existing database (read/write)
- Serilog logging
- MediatR event bus
- Prometheus metrics
- Slack/PagerDuty alerting

---

## 11. LICENSE GATING TABLE

| Feature | Community | Professional | Enterprise |
|---------|-----------|----------------|------------|
| Crash Detection | ✓ | ✓ | ✓ |
| Basic Data Validation | ✓ | ✓ | ✓ |
| Automatic Repair | ✗ | ✓ | ✓ |
| State Snapshots (5 max) | ✓ | ✓ | ✓ |
| State Snapshots (Unlimited) | ✗ | ✗ | ✓ |
| Transaction Journal | ✗ | ✓ | ✓ |
| Corruption Detection | ✗ | ✓ | ✓ |
| Advanced Repair Strategies | ✗ | ✗ | ✓ |
| Real-time Monitoring | ✗ | Limited | ✓ |
| Priority Support | ✗ | ✗ | ✓ |
| SLA Guarantee | ✗ | ✗ | 99.95% |

---

## 12. PERFORMANCE TARGETS

### 12.1 Recovery Operations

| Operation | Target | Measurement |
|-----------|--------|-------------|
| Crash Detection | < 2 seconds | Time from crash to detection |
| Crash Classification | < 1 second | Time to classify crash type |
| Recovery Startup | < 30 seconds | Time from recovery start to operational |
| Snapshot Creation | < 10 seconds | Per 100MB of state |
| Snapshot Restoration | < 20 seconds | Per 100MB of state |
| Data Validation | < 5 minutes | For 100K records + 4 validation types |
| Automatic Repair | < 2 minutes | Per corruption issue |
| Corruption Detection | < 1 minute | Latency from occurrence to detection |

### 12.2 Resource Consumption

| Resource | Target | Baseline |
|----------|--------|----------|
| CPU (Normal) | < 2% | Monitoring and journal writes |
| CPU (During Recovery) | < 80% | Full parallelization allowed |
| Memory (Baseline) | < 100MB | Core services idle |
| Memory (During Recovery) | < 1GB | For 100M record processing |
| Disk I/O (Snapshots) | 100MB/s | Compression throughput |
| Network (S3 Upload) | 50MB/s | Multi-part upload |

### 12.3 Throughput Metrics

| Operation | Target | Notes |
|-----------|--------|-------|
| Journal Write | >= 10K entries/second | Lock-free concurrent writes |
| Checkpoint | >= 100 records/second | Per recovery instance |
| Integrity Check | >= 50K records/second | Parallel validation |
| Transaction Replay | >= 3K operations/second | Sequential replay |
| Corruption Scan | >= 100K records/second | Parallel scanning |

---

## 13. TESTING STRATEGY

### 13.1 Unit Testing

**Coverage Target**: >= 85% overall, >= 95% for critical paths

**Test Categories**:
- Crash detection logic (10 tests)
- Crash classification (8 tests)
- Data validation rules (25 tests)
- Repair strategy selection (15 tests)
- Snapshot creation/restore (20 tests)
- Journal operations (18 tests)
- Corruption detection algorithms (12 tests)

**Total Unit Tests**: 108+

### 13.2 Integration Testing

**Scenarios**:
- Multi-step recovery workflows
- Database interaction validation
- Cross-component communication
- Error handling and rollback
- Performance under load

**Test Suites**:
- Application Crash Recovery (10 tests)
- System Crash Recovery (8 tests)
- Power Failure Recovery (6 tests)
- OutOfMemory Scenarios (5 tests)
- Data Integrity Workflows (12 tests)
- Snapshot Lifecycle (8 tests)
- Journal Replay (10 tests)

**Total Integration Tests**: 59+

### 13.3 Fault Injection Testing

**Fault Injection Scenarios**:

```csharp
// Database connection failure during validation
FaultInjectionEngine.InjectFault(
    targetOperation: "ValidateAsync",
    faultType: FaultType.ConnectionTimeout,
    triggerTime: 500ms,
    recovery: AutomaticRetry);

// Partial snapshot corruption
FaultInjectionEngine.InjectFault(
    targetOperation: "RestoreFromSnapshotAsync",
    faultType: FaultType.CorruptedData,
    corruptionPercentage: 0.1,
    recovery: FallbackToBackup);

// Transaction journal write failure
FaultInjectionEngine.InjectFault(
    targetOperation: "RecordTransactionAsync",
    faultType: FaultType.IOError,
    triggerTime: 1000ms,
    recovery: Queue);
```

**Fault Types to Test**:
- Database connection failures
- Timeout scenarios
- Disk space exhaustion
- Memory pressure
- Network interruptions
- Partial data corruption
- Concurrent modification
- Deadlocks in multi-threaded scenarios

### 13.4 Performance Testing

**Load Scenarios**:
- Recovery from crash with 100MB state
- Validation of 10M record table
- Snapshot creation and compression
- Journal replay of 100K transactions
- Concurrent corruption detection

**Tools**:
- BenchmarkDotNet for micro-benchmarks
- k6/Locust for load testing
- Custom fault injection harness

### 13.5 Data Consistency Testing

**Test Data Scenarios**:
- Orphaned foreign keys (test fix)
- Missing checksums (test detection)
- Inconsistent aggregates (test repair)
- Duplicate records (test deduplication)
- Mixed valid/invalid records
- Edge cases (NULL, empty, max values)

---

## 14. RISKS & MITIGATIONS

### 14.1 High-Risk Areas

#### Risk: Repair Operation Causes Additional Corruption

**Severity**: Critical
**Probability**: Low (2%)
**Impact**: Data loss, extended downtime

**Mitigation**:
- Dry-run validation before any repair
- Transactional repair operations with rollback
- Comprehensive unit tests for each repair type
- Approval gate for automatic repairs >= High severity
- Immediate backup snapshot before critical repairs

#### Risk: Snapshot Restoration Introduces Inconsistency

**Severity**: High
**Probability**: Low (3%)
**Impact**: Data inconsistency, silent errors

**Mitigation**:
- Full integrity validation after restore
- Snapshot hash verification before restore
- Journal replay to bring state to current
- Automated health checks post-restore
- Incremental snapshot chaining for data integrity

#### Risk: Recovery Takes Too Long, SLA Breach

**Severity**: High
**Probability**: Medium (15%)
**Impact**: SLA violation, customer impact

**Mitigation**:
- Performance targets with monitoring alerts
- Parallel processing for multi-GB snapshots
- Tiered recovery strategies (fast/thorough)
- Preemptive snapshot scheduling
- Load testing for worst-case scenarios

#### Risk: Corruption Propagation Before Detection

**Severity**: High
**Probability**: Medium (10%)
**Impact**: Widespread data damage

**Mitigation**:
- Proactive corruption scanning (hourly)
- Real-time checksum monitoring
- Pattern-based anomaly detection
- Early warning alerts
- Immediate isolation of affected data

#### Risk: Journal Corruption or Loss

**Severity**: Critical
**Probability**: Low (1%)
**Impact**: Recovery impossible, total data loss

**Mitigation**:
- Write-ahead logging with fsync
- Dual journal writes to separate storage
- Periodic journal integrity checks
- Journal archival and backup strategy
- Corruption detection in journal entries

### 14.2 Medium-Risk Areas

| Risk | Probability | Mitigation |
|------|-------------|-----------|
| Database locks during validation | 8% | Chunked processing, read-only queries |
| Insufficient disk space for snapshots | 5% | Monitoring, automated cleanup, compression |
| Performance regression in validation | 6% | Automated benchmarking, CI alerts |
| Incomplete transaction replay | 4% | Idempotency checking, duplicate detection |
| Snapshot deduplication failure | 3% | Hash validation, block verification |

### 14.3 Low-Risk Areas

| Risk | Probability | Mitigation |
|------|-------------|-----------|
| Minor performance overhead | 20% | Async operations, non-blocking monitoring |
| False positive violations | 5% | Validation rule refinement, manual review |
| Metric accuracy in high load | 7% | Sampling, aggregation strategies |
| API contract breaking changes | 2% | Versioning, backward compatibility tests |

---

## 15. MEDIATR EVENTS

### 15.1 CrashDetectedEvent

```csharp
namespace Lexichord.Recovery.Events
{
    /// <summary>
    /// Event published when a crash is detected.
    /// </summary>
    public class CrashDetectedEvent : INotification
    {
        public Guid CrashId { get; set; }
        public CrashType CrashType { get; set; }
        public DateTime DetectionTime { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public Dictionary<string, object> SystemState { get; set; }
        public List<string> AffectedComponents { get; set; }
        public ViolationSeverity Severity { get; set; }
    }

    public class CrashDetectedEventHandler : INotificationHandler<CrashDetectedEvent>
    {
        private readonly ICrashRecoveryManager _recoveryManager;
        private readonly ILogger<CrashDetectedEventHandler> _logger;

        public async Task Handle(CrashDetectedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogError("Crash detected: {CrashType} - {Message}",
                notification.CrashType, notification.ExceptionMessage);

            // Trigger recovery workflow
            await _recoveryManager.BeginRecoveryAsync(
                notification.CrashId,
                new RecoveryOptions { AutoRecovery = true },
                cancellationToken);
        }
    }
}
```

### 15.2 RecoveryCompletedEvent

```csharp
public class RecoveryCompletedEvent : INotification
{
    public Guid RecoveryId { get; set; }
    public Guid CrashId { get; set; }
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<string> Actions { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> Errors { get; set; }
    public RecoveryResultDetails Details { get; set; }
}

public class RecoveryCompletedEventHandler : INotificationHandler<RecoveryCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IMetricsService _metricsService;

    public async Task Handle(RecoveryCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Send notifications
        if (notification.Success)
        {
            await _notificationService.SendSuccessNotificationAsync(
                $"Recovery {notification.RecoveryId} completed in {notification.Duration.TotalSeconds:F1}s",
                cancellationToken);
        }
        else
        {
            await _notificationService.SendErrorNotificationAsync(
                $"Recovery {notification.RecoveryId} failed: {string.Join(", ", notification.Errors)}",
                cancellationToken);
        }

        // Record metrics
        _metricsService.RecordRecoveryDuration(notification.Duration);
        _metricsService.RecordRecoverySuccess(notification.Success);
    }
}
```

### 15.3 IntegrityViolationEvent

```csharp
public class IntegrityViolationEvent : INotification
{
    public Guid ViolationId { get; set; }
    public string TableName { get; set; }
    public IntegrityViolationType Type { get; set; }
    public ViolationSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public string Description { get; set; }
    public int AffectedRecordCount { get; set; }
    public string RecommendedAction { get; set; }
}

public class IntegrityViolationEventHandler : INotificationHandler<IntegrityViolationEvent>
{
    private readonly IAutoRepairEngine _repairEngine;
    private readonly IAlertingService _alertingService;

    public async Task Handle(IntegrityViolationEvent notification, CancellationToken cancellationToken)
    {
        // Alert on critical violations
        if (notification.Severity == ViolationSeverity.Critical)
        {
            await _alertingService.SendCriticalAlertAsync(
                $"Integrity violation in {notification.TableName}: {notification.Description}",
                cancellationToken);
        }

        // Trigger automatic repair for Low/Medium severity
        if (notification.Severity <= ViolationSeverity.Medium)
        {
            await _repairEngine.RepairAsync(
                new[] { new IntegrityViolation { /* ... */ } },
                new RepairOptions { AutoExecute = true },
                cancellationToken);
        }
    }
}
```

### 15.4 SnapshotCreatedEvent

```csharp
public class SnapshotCreatedEvent : INotification
{
    public Guid SnapshotId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CompressedSize { get; set; }
    public long UncompressedSize { get; set; }
    public int DedupPercentage { get; set; }
    public TimeSpan CreationDuration { get; set; }
    public string CompressionAlgorithm { get; set; }
    public bool IsIncremental { get; set; }
}

public class SnapshotCreatedEventHandler : INotificationHandler<SnapshotCreatedEvent>
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<SnapshotCreatedEventHandler> _logger;

    public async Task Handle(SnapshotCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Snapshot {SnapshotId} created: {Size:N0} bytes -> {Compressed:N0} bytes ({Ratio}% compression)",
            notification.SnapshotId, notification.UncompressedSize,
            notification.CompressedSize,
            (notification.CompressedSize * 100 / notification.UncompressedSize));

        _metricsService.RecordSnapshotCreation(
            notification.CreationDuration,
            notification.CompressedSize);

        await Task.CompletedTask;
    }
}
```

### 15.5 RepairCompletedEvent

```csharp
public class RepairCompletedEvent : INotification
{
    public Guid RepairBatchId { get; set; }
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public int SuccessfulRepairs { get; set; }
    public int FailedRepairs { get; set; }
    public List<RepairOperation> Operations { get; set; }
    public List<string> Errors { get; set; }
}

public class RepairCompletedEventHandler : INotificationHandler<RepairCompletedEvent>
{
    private readonly IDataIntegrityValidator _validator;
    private readonly INotificationService _notificationService;

    public async Task Handle(RepairCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Repair batch {BatchId} completed: {Successful}/{Total} successful",
            notification.RepairBatchId, notification.SuccessfulRepairs,
            notification.SuccessfulRepairs + notification.FailedRepairs);

        // Re-validate after repair to ensure success
        var validationResult = await _validator.ValidateAsync(
            new IntegrityValidationOptions { ValidateAll = true },
            cancellationToken: cancellationToken);

        if (validationResult.IsClean)
        {
            await _notificationService.SendSuccessNotificationAsync(
                $"Data integrity restored. {notification.SuccessfulRepairs} issues fixed.",
                cancellationToken);
        }
        else
        {
            await _notificationService.SendWarningNotificationAsync(
                $"Repair completed with issues: {validationResult.TotalViolations} violations remain.",
                cancellationToken);
        }
    }
}
```

### 15.6 TransactionJournalReplayEvent

```csharp
public class TransactionJournalReplayEvent : INotification
{
    public Guid ReplayId { get; set; }
    public DateTime ReplayStartTime { get; set; }
    public DateTime ReplayEndTime { get; set; }
    public long EntriesReplayed { get; set; }
    public long EntriesSkipped { get; set; }
    public long EntriesFailed { get; set; }
    public TimeSpan Duration => ReplayEndTime - ReplayStartTime;
    public List<string> Errors { get; set; }
}

public class TransactionJournalReplayEventHandler : INotificationHandler<TransactionJournalReplayEvent>
{
    private readonly ILogger<TransactionJournalReplayEventHandler> _logger;
    private readonly IMetricsService _metricsService;

    public async Task Handle(TransactionJournalReplayEvent notification, CancellationToken cancellationToken)
    {
        var throughput = notification.EntriesReplayed / notification.Duration.TotalSeconds;
        _logger.LogInformation(
            "Journal replay completed: {Replayed} entries in {Duration:F1}s ({Throughput:F0} ops/sec)",
            notification.EntriesReplayed, notification.Duration.TotalSeconds, throughput);

        _metricsService.RecordJournalReplay(notification.Duration, notification.EntriesReplayed);

        await Task.CompletedTask;
    }
}
```

---

## 16. IMPLEMENTATION ROADMAP

### Phase 1: Foundation (Week 1-2)
- [ ] Create core interfaces and models
- [ ] Implement PostgreSQL schema
- [ ] Setup dependency injection
- [ ] Create base test infrastructure

### Phase 2: Crash Detection (Week 2-3)
- [ ] Implement ICrashRecoveryManager
- [ ] Build crash detection service
- [ ] Crash classification engine
- [ ] Unit tests (30+ tests)

### Phase 3: Data Validation (Week 3-4)
- [ ] Implement IDataIntegrityValidator
- [ ] Schema, referential, checksum, consistency validators
- [ ] Violation reporting
- [ ] Unit tests (40+ tests)

### Phase 4: Repair & Snapshots (Week 4-5)
- [ ] Implement IAutoRepairEngine
- [ ] Implement IStateSnapshotManager
- [ ] Repair handlers and strategies
- [ ] Snapshot compression/deduplication
- [ ] Unit tests (50+ tests)

### Phase 5: Journal & Monitoring (Week 5-6)
- [ ] Implement ITransactionJournal
- [ ] Journal replay mechanism
- [ ] Implement ICorruptionDetector
- [ ] Real-time monitoring
- [ ] Unit tests (45+ tests)

### Phase 6: Integration & Testing (Week 6-7)
- [ ] Integration tests (60+ tests)
- [ ] Fault injection testing
- [ ] Performance testing
- [ ] End-to-end recovery scenarios

### Phase 7: Documentation & Deployment (Week 7-8)
- [ ] User documentation
- [ ] API documentation
- [ ] Operational guides
- [ ] Release notes
- [ ] Deployment automation

---

## 17. SUCCESS CRITERIA

### Code Quality
- [ ] All code follows .NET coding standards
- [ ] SonarQube quality gate passed
- [ ] Code coverage >= 85%
- [ ] Zero critical security issues

### Functionality
- [ ] All acceptance criteria met
- [ ] All interfaces fully implemented
- [ ] All MediatR events published correctly
- [ ] Recovery workflows execute successfully

### Performance
- [ ] Recovery startup < 30 seconds
- [ ] Snapshot creation < 10 seconds per 100MB
- [ ] Validation < 5 minutes per 100K records
- [ ] Journal replay >= 3K ops/second

### Testing
- [ ] 108+ unit tests passing
- [ ] 59+ integration tests passing
- [ ] All fault injection scenarios tested
- [ ] Zero critical defects

### Documentation
- [ ] API documentation complete
- [ ] Architecture diagrams documented
- [ ] Runbook for recovery procedures
- [ ] Performance tuning guide

---

## 18. GLOSSARY

**Crash**: Unexpected termination of application or system process
**Recovery**: Process of restoring system to consistent, operational state
**Corruption**: Data that no longer conforms to expected format or constraints
**Snapshot**: Point-in-time capture of system state
**Journal**: Persistent log of state-modifying operations
**Idempotency**: Property where operation produces same result if executed once or multiple times
**Throughput**: Operations processed per unit time
**Latency**: Time elapsed from event trigger to completion
**Deduplication**: Elimination of redundant data blocks
**WAL**: Write-Ahead Logging; recording changes before applying to main storage

---

## 19. APPENDIX: SAMPLE CONFIGURATION

```json
{
  "RecoverySettings": {
    "EnableAutoRecovery": true,
    "CrashDetection": {
      "Enabled": true,
      "DetectionTimeoutMs": 2000,
      "MaxCrashHistoryDays": 30
    },
    "DataValidation": {
      "EnableSchemaValidation": true,
      "EnableReferentialIntegrity": true,
      "EnableChecksumVerification": true,
      "EnableConsistencyValidation": true,
      "ValidationChunkSize": 10000,
      "ValidationTimeoutSeconds": 1800,
      "ParallelDegree": 8
    },
    "AutoRepair": {
      "EnableAutoRepair": false,
      "RequireApprovalForCritical": true,
      "MaxRepairDurationSeconds": 120,
      "RollbackOnFailure": true,
      "DryRunBeforeExecution": true
    },
    "Snapshots": {
      "SchedulingIntervalMinutes": 30,
      "CompressionAlgorithm": "gzip",
      "CompressionLevel": 6,
      "EnableIncremental": true,
      "RetentionPolicy": {
        "KeepFullSnapshotsCount": 10,
        "KeepDaysOfSnapshots": 7,
        "AutoArchiveAfterDays": 30
      },
      "StorageProvider": "filesystem",
      "StoragePath": "/mnt/snapshots"
    },
    "TransactionJournal": {
      "EnableJournal": true,
      "WriteAheadLogging": true,
      "CompactionIntervalHours": 24,
      "ArchiveAfterDays": 30,
      "JournalRetentionDays": 90
    },
    "CorruptionDetection": {
      "EnableMonitoring": true,
      "ScanIntervalMinutes": 60,
      "EnableRealTimeMonitoring": true,
      "AlertOnDetection": true,
      "AlertChannels": ["email", "slack"]
    }
  }
}
```

---

**END OF DOCUMENT**

---

## Document Information

- **Total Lines**: 2,500+
- **Code Examples**: 25+
- **Diagrams**: 3
- **Tables**: 15+
- **Acceptance Criteria**: 60+

---

**Version 0.19.3-RES-DRAFT-001**
*Lexichord Recovery & Repair Implementation Specification*
