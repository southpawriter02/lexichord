using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event published when a file or folder is successfully deleted.
/// </summary>
/// <param name="FilePath">The absolute path of the deleted item.</param>
/// <param name="FileName">The name of the deleted item.</param>
/// <param name="IsDirectory">True if a folder was deleted; false for files.</param>
/// <remarks>
/// LOGIC: Published by <see cref="Contracts.IFileOperationService"/> after
/// successful file/folder deletion. Enables the Project Explorer to remove
/// the corresponding node from its tree view.
/// </remarks>
public sealed record FileDeletedEvent(
    string FilePath,
    string FileName,
    bool IsDirectory
) : INotification;
