// <copyright file="IResonanceAxisProvider.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides axis definitions for the Resonance spider chart.
/// </summary>
/// <remarks>
/// <para>LOGIC: Strategy pattern for axis configuration.</para>
/// <para>Allows future extension for custom axis sets per voice profile.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public interface IResonanceAxisProvider
{
    /// <summary>
    /// Gets the ordered list of axis definitions.
    /// </summary>
    /// <returns>Axes in clockwise order starting from top.</returns>
    IReadOnlyList<ResonanceAxisDefinition> GetAxes();
}
