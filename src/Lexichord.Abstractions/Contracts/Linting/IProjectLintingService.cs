using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Abstractions.Contracts.Linting;

#region Result Records

/// <summary>
/// Result of a project-wide linting operation.
/// </summary>
/// <param name="ViolationsByFile">Violations indexed by file path.</param>
/// <param name="TotalFiles">Total number of files to process.</param>
/// <param name="ScannedFiles">Number of files actually scanned.</param>
/// <param name="ProblemCount">Total violation count across all files.</param>
/// <param name="Duration">Time taken for the operation.</param>
/// <param name="WasCancelled">True if the operation was cancelled.</param>
/// <param name="Errors">List of file-level errors encountered.</param>
/// <remarks>
/// LOGIC: Returned from LintProjectAsync to provide comprehensive
/// scan results including statistics for progress UI.
///
/// Version: v0.2.6d
/// </remarks>
public record ProjectLintResult(
    IReadOnlyDictionary<string, IReadOnlyList<StyleViolation>> ViolationsByFile,
    int TotalFiles,
    int ScannedFiles,
    int ProblemCount,
    TimeSpan Duration,
    bool WasCancelled,
    IReadOnlyList<string> Errors);

/// <summary>
/// Result of linting multiple open documents.
/// </summary>
/// <param name="ViolationsByDocument">Violations indexed by document ID.</param>
/// <param name="TotalDocuments">Number of documents scanned.</param>
/// <param name="ProblemCount">Total violation count across all documents.</param>
/// <param name="Duration">Time taken for the operation.</param>
/// <remarks>
/// LOGIC: Returned from LintOpenDocumentsAsync for the "Open Files" scope.
///
/// Version: v0.2.6d
/// </remarks>
public record MultiLintResult(
    IReadOnlyDictionary<string, IReadOnlyList<StyleViolation>> ViolationsByDocument,
    int TotalDocuments,
    int ProblemCount,
    TimeSpan Duration);

/// <summary>
/// Progress update during a project-wide linting operation.
/// </summary>
/// <param name="CurrentFile">Index of the file currently being processed (1-based).</param>
/// <param name="TotalFiles">Total number of files to process.</param>
/// <param name="CurrentFilePath">Path of the file currently being processed.</param>
/// <remarks>
/// LOGIC: Used with IProgress{T} for UI progress updates.
///
/// Version: v0.2.6d
/// </remarks>
public record ProjectLintProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFilePath);

#endregion

#region Service Interface

/// <summary>
/// Service for project-wide and multi-document linting operations.
/// </summary>
/// <remarks>
/// LOGIC: IProjectLintingService orchestrates background linting across
/// multiple files or documents. It provides:
/// 
/// - Open Files scope: Scans all currently open documents
/// - Project scope: Scans all matching files in a project directory
/// - Caching: Avoids re-scanning unchanged files
/// - Progress reporting: For UI progress indicators
///
/// Threading:
/// - All methods are thread-safe
/// - Background operations run on thread pool
/// - Progress callbacks may be on any thread
///
/// Dependencies:
/// - IStyleEngine for content analysis
/// - IEditorService for open document access
/// - IFileSystemAccess for project file enumeration
///
/// Version: v0.2.6d
/// </remarks>
public interface IProjectLintingService
{
    /// <summary>
    /// Lints all currently open documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results containing violations for each open document.</returns>
    /// <remarks>
    /// LOGIC: Iterates through IEditorService.GetOpenDocuments(),
    /// linting each document's current content. Uses cached results
    /// where available.
    /// </remarks>
    Task<MultiLintResult> LintOpenDocumentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lints all files in a project directory.
    /// </summary>
    /// <param name="projectPath">Root path of the project.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results containing violations for each scanned file.</returns>
    /// <remarks>
    /// LOGIC: Enumerates files matching target extensions (.md, .txt),
    /// filters using .lexichordignore patterns, and scans each file.
    /// Uses cached results for unchanged files.
    ///
    /// Runs on background thread via Task.Run to avoid blocking UI.
    /// </remarks>
    Task<ProjectLintResult> LintProjectAsync(
        string projectPath,
        IProgress<ProjectLintProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached results for a specific file.
    /// </summary>
    /// <param name="filePath">Path to the file that changed.</param>
    /// <remarks>
    /// LOGIC: Call when a file is saved to ensure the next scan
    /// reads fresh content. Safe to call for non-cached files.
    /// </remarks>
    void InvalidateFile(string filePath);

    /// <summary>
    /// Invalidates all cached results.
    /// </summary>
    /// <remarks>
    /// LOGIC: Use when style rules change or for forced full rescan.
    /// </remarks>
    void InvalidateAll();

    /// <summary>
    /// Event raised during project linting to report progress.
    /// </summary>
    /// <remarks>
    /// LOGIC: Alternative to IProgress{T} for reactive subscribers.
    /// May be raised from background thread.
    /// </remarks>
    event EventHandler<ProjectLintProgress>? ProgressChanged;
}

#endregion
