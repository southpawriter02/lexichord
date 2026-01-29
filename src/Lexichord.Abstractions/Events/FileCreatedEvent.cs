using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event published when a file or folder is successfully created.
/// </summary>
/// <param name="FilePath">The absolute path of the created item.</param>
/// <param name="FileName">The name of the created item.</param>
/// <param name="IsDirectory">True if a folder was created; false for files.</param>
/// <remarks>
/// LOGIC: Published by <see cref="Contracts.IFileOperationService"/> after
/// successful file/folder creation. Enables the Project Explorer to update
/// its tree view without a full refresh.
/// </remarks>
public sealed record FileCreatedEvent(
    string FilePath,
    string FileName,
    bool IsDirectory
) : INotification;
