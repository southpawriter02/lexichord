namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Event arguments for document state changes.
/// </summary>
/// <remarks>
/// LOGIC: Raised when <see cref="IDocumentTab.IsDirty"/> or <see cref="IDocumentTab.IsPinned"/>
/// changes, enabling UI updates and TabService coordination.
/// </remarks>
/// <param name="PropertyName">The name of the property that changed.</param>
/// <param name="OldValue">The previous value of the property.</param>
/// <param name="NewValue">The new value of the property.</param>
public sealed record DocumentStateChangedEventArgs(
    string PropertyName,
    object? OldValue,
    object? NewValue);
