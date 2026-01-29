using System.Diagnostics;
using System.Text;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// File service with atomic save support.
/// </summary>
/// <remarks>
/// LOGIC: Implements three-phase atomic write:
/// 1. Write to temp file (.tmp extension)
/// 2. Delete original file
/// 3. Rename temp to original
///
/// Error Handling:
/// - Phase 1 failure: Clean up temp, original untouched
/// - Phase 2 failure: Clean up temp, original preserved
/// - Phase 3 failure: Critical - attempt recovery, report to user
/// </remarks>
public sealed class FileService(
    IMediator mediator,
    ILogger<FileService> logger) : IFileService
{
    private const string TempExtension = ".tmp";
    private const int MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
    private const int BufferSize = 65536; // 64 KB write buffer

    /// <inheritdoc/>
    public async Task<SaveResult> SaveAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        encoding ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        logger.LogInformation("Starting atomic save: {FilePath}", filePath);

        try
        {
            // PHASE 0: Validation
            var validationError = ValidatePath(filePath);
            if (validationError is not null)
            {
                return SaveResult.Failed(filePath, validationError);
            }

            var tempPath = filePath + TempExtension;
            var bytes = encoding.GetBytes(content);

            // PHASE 1: Write to temp file
            logger.LogDebug("Phase 1: Writing {Bytes} bytes to temp file: {TempPath}", bytes.Length, tempPath);

            try
            {
                await WriteWithFlushAsync(tempPath, bytes, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Phase 1 failed: Could not write temp file");
                CleanupTempFile(tempPath);

                return SaveResult.Failed(filePath, CreateSaveError(ex, SaveErrorCode.TempWriteFailed,
                    "Failed to write temporary file. Your original file is preserved."));
            }

            // PHASE 2: Delete original (if exists)
            if (File.Exists(filePath))
            {
                logger.LogDebug("Phase 2: Deleting original file: {FilePath}", filePath);

                try
                {
                    // Check for read-only
                    var attributes = File.GetAttributes(filePath);
                    if ((attributes & FileAttributes.ReadOnly) != 0)
                    {
                        CleanupTempFile(tempPath);
                        return SaveResult.Failed(filePath, new SaveError(
                            SaveErrorCode.ReadOnly,
                            "File is read-only and cannot be overwritten.",
                            RecoveryHint: "Use Save As to save to a different location."));
                    }

                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Phase 2 failed: Could not delete original file");
                    CleanupTempFile(tempPath);

                    return SaveResult.Failed(filePath, CreateSaveError(ex, SaveErrorCode.DeleteFailed,
                        "Could not replace original file. Your original file is preserved."));
                }
            }

            // PHASE 3: Rename temp to original
            logger.LogDebug("Phase 3: Renaming {TempPath} to {FilePath}", tempPath, filePath);

            try
            {
                File.Move(tempPath, filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Phase 3 CRITICAL: Rename failed");

                // CRITICAL: Original deleted but rename failed
                // Attempt recovery
                var recovered = await AttemptRecoveryAsync(tempPath, filePath, cancellationToken);

                if (!recovered)
                {
                    return SaveResult.Failed(filePath, new SaveError(
                        SaveErrorCode.RenameFailed,
                        "Critical error during save. Your content is preserved in a temporary file.",
                        ex,
                        $"Recovery: Your file may be at {tempPath}"));
                }

                logger.LogWarning("Recovery successful after rename failure");
            }

            stopwatch.Stop();

            logger.LogInformation(
                "Atomic save completed: {FilePath} ({Bytes} bytes in {Duration}ms)",
                filePath, bytes.Length, stopwatch.ElapsedMilliseconds);

            // Publish success event
            await mediator.Publish(new DocumentSavedEvent(
                DocumentId: filePath,
                FilePath: filePath,
                BytesWritten: bytes.Length,
                Duration: stopwatch.Elapsed,
                SavedAt: DateTimeOffset.UtcNow
            ), cancellationToken);

            return SaveResult.Succeeded(filePath, bytes.Length, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Save cancelled: {FilePath}", filePath);
            return SaveResult.Failed(filePath, new SaveError(
                SaveErrorCode.Cancelled,
                "Save operation was cancelled."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during save: {FilePath}", filePath);

            await mediator.Publish(new DocumentSaveFailedEvent(
                DocumentId: filePath,
                FilePath: filePath,
                ErrorCode: SaveErrorCode.Unknown,
                ErrorMessage: ex.Message,
                FailedAt: DateTimeOffset.UtcNow
            ), cancellationToken);

            return SaveResult.Failed(filePath, CreateSaveError(ex, SaveErrorCode.Unknown,
                "An unexpected error occurred during save."));
        }
    }

    /// <inheritdoc/>
    public Task<SaveResult> SaveAsAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        // SaveAs uses the same atomic strategy
        return SaveAsync(filePath, content, encoding, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LoadResult> LoadAsync(
        string filePath,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading file: {FilePath}", filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("File not found: {FilePath}", filePath);
                return LoadResult.Failed(filePath, new LoadError(
                    LoadErrorCode.FileNotFound,
                    $"File not found: {filePath}"));
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                logger.LogWarning("File too large: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
                return LoadResult.Failed(filePath, new LoadError(
                    LoadErrorCode.FileTooLarge,
                    $"File exceeds maximum size ({MaxFileSizeBytes / 1024 / 1024} MB)"));
            }

            // Auto-detect encoding if not specified
            encoding ??= await DetectEncodingAsync(filePath, cancellationToken);

            var content = await File.ReadAllTextAsync(filePath, encoding, cancellationToken);

            logger.LogInformation(
                "File loaded: {FilePath} ({Length} chars, {Encoding})",
                filePath, content.Length, encoding.EncodingName);

            return LoadResult.Succeeded(filePath, content, encoding);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access denied: {FilePath}", filePath);
            return LoadResult.Failed(filePath, new LoadError(
                LoadErrorCode.AccessDenied,
                $"Access denied: {filePath}",
                ex));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "IO error loading: {FilePath}", filePath);
            return LoadResult.Failed(filePath, new LoadError(
                LoadErrorCode.IoError,
                ex.Message,
                ex));
        }
    }

    /// <inheritdoc/>
    public bool CanWrite(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                return false;

            if (!Directory.Exists(directory))
                return false;

            // Check if file exists and is read-only
            if (File.Exists(filePath))
            {
                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    return false;

                // Try to open for write to check lock
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
                    return true;
                }
                catch (IOException)
                {
                    return false; // File is locked
                }
            }

            // For new file, check directory writability
            var testFile = Path.Combine(directory, $".lexichord_write_test_{Guid.NewGuid()}");
            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool Exists(string filePath) => File.Exists(filePath);

    /// <inheritdoc/>
    public FileServiceMetadata? GetMetadata(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var info = new FileInfo(filePath);
        return new FileServiceMetadata(
            info.FullName,
            info.Name,
            info.Length,
            info.IsReadOnly,
            info.LastWriteTimeUtc,
            info.CreationTimeUtc);
    }

    #region Private Methods

    private SaveError? ValidatePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new SaveError(SaveErrorCode.InvalidPath, "File path cannot be empty.");
        }

        var invalidChars = Path.GetInvalidPathChars();
        if (filePath.IndexOfAny(invalidChars) >= 0)
        {
            return new SaveError(SaveErrorCode.InvalidPath, "File path contains invalid characters.");
        }

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                return new SaveError(
                    SaveErrorCode.DirectoryNotFound,
                    $"Directory does not exist: {directory}",
                    RecoveryHint: "Create the directory or choose a different location.");
            }
        }
        catch (PathTooLongException)
        {
            return new SaveError(SaveErrorCode.PathTooLong, "File path exceeds maximum length.");
        }

        return null;
    }

    private async Task WriteWithFlushAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        // LOGIC: Write with explicit flush to ensure data is on disk
        // FileOptions.WriteThrough bypasses OS cache for durability
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.WriteThrough | FileOptions.Asynchronous);

        await stream.WriteAsync(content, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private void CleanupTempFile(string filePath)
    {
        var tempPath = filePath.EndsWith(TempExtension)
            ? filePath
            : filePath + TempExtension;

        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
                logger.LogDebug("Cleaned up temp file: {TempPath}", tempPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up temp file: {TempPath}", tempPath);
        }
    }

    private async Task<bool> AttemptRecoveryAsync(
        string tempPath,
        string targetPath,
        CancellationToken cancellationToken)
    {
        // LOGIC: If rename failed, try alternative approaches
        logger.LogWarning("Attempting recovery: {TempPath} -> {TargetPath}", tempPath, targetPath);

        // Approach 1: Simple retry
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(100 * (i + 1), cancellationToken);
            try
            {
                File.Move(tempPath, targetPath);
                return true;
            }
            catch
            {
                // Continue to next attempt
            }
        }

        // Approach 2: Copy and delete
        try
        {
            File.Copy(tempPath, targetPath, overwrite: true);
            File.Delete(tempPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recovery failed");
            return false;
        }
    }

    private static async Task<Encoding> DetectEncodingAsync(string filePath, CancellationToken cancellationToken)
    {
        var buffer = new byte[4];
        await using var fs = File.OpenRead(filePath);
        var bytesRead = await fs.ReadAsync(buffer, cancellationToken);

        // Check for BOM (Byte Order Mark)
        if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            return Encoding.UTF8;

        if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            return Encoding.Unicode; // UTF-16 LE

        if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            return Encoding.BigEndianUnicode; // UTF-16 BE

        // Default to UTF-8 without BOM
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }

    private static SaveError CreateSaveError(Exception ex, SaveErrorCode defaultCode, string defaultMessage)
    {
        return ex switch
        {
            UnauthorizedAccessException => new SaveError(
                SaveErrorCode.AccessDenied,
                "Access denied. The file may be open in another application.",
                ex,
                "Close any other applications that may have the file open."),

            DirectoryNotFoundException => new SaveError(
                SaveErrorCode.DirectoryNotFound,
                "The directory does not exist.",
                ex,
                "Check the file path or create the directory."),

            PathTooLongException => new SaveError(
                SaveErrorCode.PathTooLong,
                "The file path is too long.",
                ex,
                "Choose a shorter file name or path."),

            IOException ioEx when ioEx.HResult == -2147024816 => new SaveError(
                SaveErrorCode.DiskFull,
                "Not enough disk space.",
                ex,
                "Free up disk space and try again."),

            IOException ioEx when ioEx.Message.Contains("being used", StringComparison.OrdinalIgnoreCase) => new SaveError(
                SaveErrorCode.FileInUse,
                "The file is in use by another process.",
                ex,
                "Close the file in other applications and try again."),

            _ => new SaveError(defaultCode, defaultMessage, ex)
        };
    }

    #endregion
}
