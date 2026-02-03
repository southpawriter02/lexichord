// =============================================================================
// File: IContextPreviewViewModelFactory.cs
// Project: Lexichord.Modules.RAG
// Description: Factory interface for creating ContextPreviewViewModel instances.
// =============================================================================
// LOGIC: Abstracts the creation of ContextPreviewViewModel to enable dependency
//   injection and testability. The factory encapsulates the dependencies needed
//   by the ViewModel (license context, expansion service, logging).
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// Factory for creating <see cref="ContextPreviewViewModel"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This factory abstracts the creation of <see cref="ContextPreviewViewModel"/> instances,
/// allowing the <see cref="SearchResultItemViewModel"/> to obtain context preview functionality
/// without directly managing the dependencies required by the ViewModel.
/// </para>
/// <para>
/// The factory pattern is used here because:
/// <list type="bullet">
///   <item><description>
///     <see cref="ContextPreviewViewModel"/> requires services (IContextExpansionService,
///     ILicenseContext) that are registered in DI but not directly available to
///     SearchResultItemViewModel.
///   </description></item>
///   <item><description>
///     Each search result item needs its own ContextPreviewViewModel instance,
///     requiring dynamic creation at runtime.
///   </description></item>
///   <item><description>
///     The factory can be mocked in unit tests to verify SearchResultItemViewModel
///     behavior without exercising the full context expansion logic.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d as part of The Context Window feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In SearchResultItemViewModel constructor:
/// public SearchResultItemViewModel(
///     SearchHit hit,
///     IContextPreviewViewModelFactory? contextPreviewFactory = null,
///     // ... other parameters)
/// {
///     _contextPreview = contextPreviewFactory?.Create(hit);
/// }
/// </code>
/// </example>
public interface IContextPreviewViewModelFactory
{
    /// <summary>
    /// Creates a new <see cref="ContextPreviewViewModel"/> for the specified search hit.
    /// </summary>
    /// <param name="searchHit">
    /// The search hit containing the chunk and document information.
    /// The factory extracts the necessary data to construct a RAG Chunk
    /// for use with <see cref="IContextExpansionService"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="ContextPreviewViewModel"/> instance configured with the
    /// chunk data and appropriate service dependencies.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="searchHit"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The factory performs the following transformations:
    /// <list type="number">
    ///   <item><description>
    ///     Extracts chunk metadata (index, heading, level) from
    ///     <see cref="SearchHit.Chunk"/> (<see cref="TextChunk"/>).
    ///   </description></item>
    ///   <item><description>
    ///     Extracts document ID from <see cref="SearchHit.Document"/>.
    ///   </description></item>
    ///   <item><description>
    ///     Constructs a RAG <see cref="Lexichord.Abstractions.Contracts.RAG.Chunk"/>
    ///     for use with <see cref="IContextExpansionService"/>.
    ///   </description></item>
    ///   <item><description>
    ///     Creates the ViewModel with injected services and the converted chunk.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    ContextPreviewViewModel Create(SearchHit searchHit);
}
