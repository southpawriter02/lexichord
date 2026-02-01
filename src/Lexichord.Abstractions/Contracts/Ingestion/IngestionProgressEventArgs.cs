// =============================================================================
// File: IngestionProgressEventArgs.cs
// Project: Lexichord.Abstractions
// Description: Event arguments for reporting ingestion progress.
// =============================================================================
// LOGIC: Provides real-time feedback during long-running ingestion operations.
//   - Percentage is calculated from ProcessedFiles/TotalFiles.
//   - EstimatedRemaining is optional, computed by the service if timing is available.
//   - Inherits from EventArgs for standard .NET event pattern compatibility.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Provides data for the <see cref="IIngestionService.ProgressChanged"/> event.
/// </summary>
/// <remarks>
/// <para>
/// This class is used to report progress during file and directory ingestion
/// operations. It provides information about the current file being processed,
/// overall progress, and an optional time estimate.
/// </para>
/// <para>
/// The <see cref="Percentage"/> property is calculated automatically based on
/// <see cref="ProcessedFiles"/> and <see cref="TotalFiles"/>.
/// </para>
/// </remarks>
public class IngestionProgressEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="IngestionProgressEventArgs"/>.
    /// </summary>
    /// <param name="currentFile">The file currently being processed.</param>
    /// <param name="totalFiles">The total number of files to process.</param>
    /// <param name="processedFiles">The number of files already processed.</param>
    /// <param name="currentPhase">The current phase of the ingestion pipeline.</param>
    /// <param name="estimatedRemaining">Optional estimated time remaining.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="currentFile"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="totalFiles"/> is less than 1, or
    /// <paramref name="processedFiles"/> is negative.
    /// </exception>
    public IngestionProgressEventArgs(
        string currentFile,
        int totalFiles,
        int processedFiles,
        IngestionPhase currentPhase,
        TimeSpan? estimatedRemaining = null)
    {
        ArgumentNullException.ThrowIfNull(currentFile);

        if (totalFiles < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalFiles),
                totalFiles,
                "Total files must be at least 1.");
        }

        if (processedFiles < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processedFiles),
                processedFiles,
                "Processed files cannot be negative.");
        }

        CurrentFile = currentFile;
        TotalFiles = totalFiles;
        ProcessedFiles = processedFiles;
        CurrentPhase = currentPhase;
        EstimatedRemaining = estimatedRemaining;
    }

    /// <summary>
    /// Gets the path to the file currently being processed.
    /// </summary>
    public string CurrentFile { get; }

    /// <summary>
    /// Gets the total number of files to be processed in this operation.
    /// </summary>
    public int TotalFiles { get; }

    /// <summary>
    /// Gets the number of files that have been processed so far.
    /// </summary>
    public int ProcessedFiles { get; }

    /// <summary>
    /// Gets the current phase of the ingestion pipeline.
    /// </summary>
    public IngestionPhase CurrentPhase { get; }

    /// <summary>
    /// Gets the estimated time remaining for the operation, if available.
    /// </summary>
    /// <remarks>
    /// This value is null until the ingestion service has enough timing data
    /// to produce a reliable estimate (typically after processing at least one file).
    /// </remarks>
    public TimeSpan? EstimatedRemaining { get; }

    /// <summary>
    /// Gets the completion percentage (0-100) based on processed/total files.
    /// </summary>
    /// <remarks>
    /// The percentage is calculated as <c>(ProcessedFiles * 100) / TotalFiles</c>.
    /// During processing of the current file, this represents progress before
    /// that file completes.
    /// </remarks>
    public int Percentage => (ProcessedFiles * 100) / TotalFiles;
}
