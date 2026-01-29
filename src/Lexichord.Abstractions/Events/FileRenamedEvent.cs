using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event published when a file or folder is successfully renamed.
/// </summary>
/// <param name="OldPath">The absolute path before renaming.</param>
/// <param name="NewPath">The absolute path after renaming.</param>
/// <param name="OldName">The name before renaming.</param>
/// <param name="NewName">The name after renaming.</param>
/// <param name="IsDirectory">True if a folder was renamed; false for files.</param>
/// <remarks>
/// LOGIC: Published by <see cref="Contracts.IFileOperationService"/> after
/// successful file/folder renaming. Enables the Project Explorer to update
/// the corresponding node in its tree view.
/// </remarks>
public sealed record FileRenamedEvent(
    string OldPath,
    string NewPath,
    string OldName,
    string NewName,
    bool IsDirectory
) : INotification;
