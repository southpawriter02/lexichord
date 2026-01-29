using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event published when the file index is rebuilt.
/// </summary>
/// <param name="WorkspacePath">The workspace root path that was indexed.</param>
/// <param name="FileCount">The number of files indexed.</param>
/// <remarks>
/// LOGIC (v0.1.5c): Published by FileIndexService after RebuildIndexAsync completes.
/// Handlers can use this to:
/// - Update UI with file count
/// - Log indexing completion
/// - Trigger dependent operations
/// </remarks>
public record FileIndexRebuiltEvent(
    string WorkspacePath,
    int FileCount
) : INotification;
