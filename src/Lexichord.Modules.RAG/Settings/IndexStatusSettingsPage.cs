// =============================================================================
// File: IndexStatusSettingsPage.cs
// Project: Lexichord.Modules.RAG
// Description: Settings page registration for the Index Status View.
// Version: v0.4.7a
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.ViewModels;
using Lexichord.Modules.RAG.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.RAG.Settings;

/// <summary>
/// Settings page for viewing indexed document status.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexStatusSettingsPage"/> registers the Index Status View
/// in the Settings dialog under the RAG/Index category.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public sealed class IndexStatusSettingsPage : ISettingsPage
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexStatusSettingsPage"/>.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public IndexStatusSettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public string CategoryId => "rag.index.status";

    /// <inheritdoc />
    public string DisplayName => "Index Status";

    /// <inheritdoc />
    public string? ParentCategoryId => "rag.index";

    /// <inheritdoc />
    public string? Icon => "Database";

    /// <inheritdoc />
    public int SortOrder => 100;

    /// <inheritdoc />
    public LicenseTier RequiredTier => LicenseTier.WriterPro;

    /// <inheritdoc />
    public IReadOnlyList<string> SearchKeywords => new[]
    {
        "index",
        "status",
        "documents",
        "indexed",
        "chunks",
        "rag",
        "retrieval",
        "vector",
        "embedding"
    };

    /// <inheritdoc />
    public object CreateView()
    {
        var viewModel = _serviceProvider.GetRequiredService<IndexStatusViewModel>();
        return new IndexStatusView(viewModel);
    }
}
