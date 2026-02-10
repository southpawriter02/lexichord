// -----------------------------------------------------------------------
// <copyright file="IKeyBindingConfiguration.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Interface for classes that configure keyboard bindings during startup.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface are registered as
/// <see cref="IKeyBindingConfiguration"/> in the DI container and are
/// resolved during module initialization to apply default keyboard bindings.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyKeyBindings : IKeyBindingConfiguration
/// {
///     public void Configure(IKeyBindingService keyBindingService)
///     {
///         keyBindingService.SetBinding("myCommand", "Ctrl+Shift+M");
///     }
/// }
/// </code>
/// </example>
public interface IKeyBindingConfiguration
{
    /// <summary>
    /// Configures keyboard bindings using the provided service.
    /// </summary>
    /// <param name="keyBindingService">The key binding service to register bindings with.</param>
    void Configure(IKeyBindingService keyBindingService);
}
