# LCS-DES-047a: Index Status View

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-047a                             |
| **Version**      | v0.4.7a                                  |
| **Title**        | Index Status View                        |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines `IndexStatusView` and supporting components that display comprehensive status of all indexed documents in the Settings dialog. Users can see which documents are indexed, their status, chunk counts, and last indexing timestamps.

### 1.2 Goals

- Display list of all indexed documents with status
- Show status badges (Indexed, Pending, Stale, Failed)
- Display summary statistics (document count, chunk count, storage size)
- Provide last indexed timestamp per document
- Register view in Settings dialog
- Support filtering and sorting

### 1.3 Non-Goals

- Inline editing of document metadata (future)
- Bulk selection operations (v0.4.7b handles individual actions)
- Real-time file watching status (separate from index status)

---

## 2. Design

### 2.1 IIndexStatusService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for retrieving index status and statistics.
/// </summary>
public interface IIndexStatusService
{
    /// <summary>
    /// Gets the status of all indexed documents.
    /// </summary>
    Task<IReadOnlyList<IndexedDocumentInfo>> GetAllDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the status of a specific document.
    /// </summary>
    Task<IndexedDocumentInfo?> GetDocumentStatusAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Gets aggregate statistics for the index.
    /// </summary>
    Task<IndexStatistics> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Refreshes status for documents that may have changed on disk.
    /// </summary>
    Task RefreshStaleStatusAsync(CancellationToken ct = default);
}
```

### 2.2 IndexedDocumentInfo Record

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Information about an indexed document's status.
/// </summary>
public record IndexedDocumentInfo
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// File path relative to workspace root.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Display name (file name without path).
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Current indexing status.
    /// </summary>
    public required IndexingStatus Status { get; init; }

    /// <summary>
    /// Number of chunks generated from this document.
    /// </summary>
    public int ChunkCount { get; init; }

    /// <summary>
    /// Timestamp when document was last indexed.
    /// </summary>
    public DateTimeOffset? IndexedAt { get; init; }

    /// <summary>
    /// File hash at time of indexing.
    /// </summary>
    public string? FileHash { get; init; }

    /// <summary>
    /// Error message if status is Failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Estimated storage size in bytes.
    /// </summary>
    public long EstimatedSizeBytes { get; init; }
}

/// <summary>
/// Indexing status for a document.
/// </summary>
public enum IndexingStatus
{
    /// <summary>
    /// Document has never been indexed.
    /// </summary>
    NotIndexed,

    /// <summary>
    /// Document is queued for indexing.
    /// </summary>
    Pending,

    /// <summary>
    /// Document is currently being indexed.
    /// </summary>
    Indexing,

    /// <summary>
    /// Document is successfully indexed and up-to-date.
    /// </summary>
    Indexed,

    /// <summary>
    /// Document has changed since last indexing.
    /// </summary>
    Stale,

    /// <summary>
    /// Document indexing failed.
    /// </summary>
    Failed
}
```

### 2.3 IndexStatistics Record

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Aggregate statistics for the document index.
/// </summary>
public record IndexStatistics
{
    /// <summary>
    /// Total number of indexed documents.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// Total number of chunks across all documents.
    /// </summary>
    public int ChunkCount { get; init; }

    /// <summary>
    /// Estimated total storage size in bytes.
    /// </summary>
    public long StorageSizeBytes { get; init; }

    /// <summary>
    /// Number of documents in each status.
    /// </summary>
    public IReadOnlyDictionary<IndexingStatus, int> StatusCounts { get; init; }
        = new Dictionary<IndexingStatus, int>();

    /// <summary>
    /// Timestamp of the most recent indexing operation.
    /// </summary>
    public DateTimeOffset? LastIndexedAt { get; init; }

    /// <summary>
    /// Gets a human-readable storage size string.
    /// </summary>
    public string StorageSizeDisplay => FormatBytes(StorageSizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F1} {suffixes[i]}";
    }
}
```

### 2.4 IndexStatusService Implementation

```csharp
namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of index status service.
/// </summary>
public sealed class IndexStatusService : IIndexStatusService
{
    private readonly IDocumentRepository _documentRepo;
    private readonly IChunkRepository _chunkRepo;
    private readonly IFileHashService _hashService;
    private readonly ILogger<IndexStatusService> _logger;

    public IndexStatusService(
        IDocumentRepository documentRepo,
        IChunkRepository chunkRepo,
        IFileHashService hashService,
        ILogger<IndexStatusService> logger)
    {
        _documentRepo = documentRepo;
        _chunkRepo = chunkRepo;
        _hashService = hashService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IndexedDocumentInfo>> GetAllDocumentsAsync(CancellationToken ct = default)
    {
        var documents = await _documentRepo.GetAllAsync();
        var result = new List<IndexedDocumentInfo>();

        foreach (var doc in documents)
        {
            var status = await DetermineStatusAsync(doc, ct);
            var chunkCount = await _chunkRepo.GetCountByDocumentAsync(doc.Id, ct);

            result.Add(new IndexedDocumentInfo
            {
                Id = doc.Id,
                FilePath = doc.FilePath,
                Status = status,
                ChunkCount = chunkCount,
                IndexedAt = doc.IndexedAt,
                FileHash = doc.FileHash,
                ErrorMessage = doc.ErrorMessage,
                EstimatedSizeBytes = EstimateSize(chunkCount)
            });
        }

        return result.OrderBy(d => d.FileName).ToList();
    }

    public async Task<IndexedDocumentInfo?> GetDocumentStatusAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _documentRepo.GetByIdAsync(documentId);
        if (doc == null) return null;

        var status = await DetermineStatusAsync(doc, ct);
        var chunkCount = await _chunkRepo.GetCountByDocumentAsync(doc.Id, ct);

        return new IndexedDocumentInfo
        {
            Id = doc.Id,
            FilePath = doc.FilePath,
            Status = status,
            ChunkCount = chunkCount,
            IndexedAt = doc.IndexedAt,
            FileHash = doc.FileHash,
            ErrorMessage = doc.ErrorMessage,
            EstimatedSizeBytes = EstimateSize(chunkCount)
        };
    }

    public async Task<IndexStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var documents = await GetAllDocumentsAsync(ct);

        var statusCounts = documents
            .GroupBy(d => d.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new IndexStatistics
        {
            DocumentCount = documents.Count,
            ChunkCount = documents.Sum(d => d.ChunkCount),
            StorageSizeBytes = documents.Sum(d => d.EstimatedSizeBytes),
            StatusCounts = statusCounts,
            LastIndexedAt = documents
                .Where(d => d.IndexedAt.HasValue)
                .Max(d => d.IndexedAt)
        };
    }

    public async Task RefreshStaleStatusAsync(CancellationToken ct = default)
    {
        var documents = await _documentRepo.GetAllAsync();

        foreach (var doc in documents)
        {
            if (!File.Exists(doc.FilePath))
                continue;

            var currentHash = await _hashService.ComputeHashAsync(doc.FilePath, ct);
            if (currentHash != doc.FileHash)
            {
                _logger.LogDebug("Document marked stale: {Path}", doc.FilePath);
                // Status will be computed as Stale in DetermineStatusAsync
            }
        }
    }

    private async Task<IndexingStatus> DetermineStatusAsync(Document doc, CancellationToken ct)
    {
        // Check for error
        if (!string.IsNullOrEmpty(doc.ErrorMessage))
            return IndexingStatus.Failed;

        // Check if file exists
        if (!File.Exists(doc.FilePath))
            return IndexingStatus.Failed;

        // Check for changes
        var currentHash = await _hashService.ComputeHashAsync(doc.FilePath, ct);
        if (currentHash != doc.FileHash)
            return IndexingStatus.Stale;

        // Check if indexed
        if (doc.IndexedAt.HasValue)
            return IndexingStatus.Indexed;

        return IndexingStatus.Pending;
    }

    private static long EstimateSize(int chunkCount)
    {
        // Estimate ~8KB per chunk (content + embedding + metadata)
        const int bytesPerChunk = 8 * 1024;
        return chunkCount * bytesPerChunk;
    }
}
```

### 2.5 IndexStatusViewModel

```csharp
namespace Lexichord.Modules.RAG.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the Index Status View.
/// </summary>
public partial class IndexStatusViewModel : ViewModelBase
{
    private readonly IIndexStatusService _statusService;
    private readonly ILogger<IndexStatusViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private IndexStatistics? _statistics;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private IndexingStatus? _statusFilter;

    public ObservableCollection<IndexedDocumentViewModel> Documents { get; } = new();

    public IReadOnlyList<IndexedDocumentViewModel> FilteredDocuments =>
        Documents
            .Where(d => MatchesFilter(d))
            .ToList();

    public IndexStatusViewModel(
        IIndexStatusService statusService,
        ILogger<IndexStatusViewModel> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;

            var documents = await _statusService.GetAllDocumentsAsync(ct);
            Statistics = await _statusService.GetStatisticsAsync(ct);

            Documents.Clear();
            foreach (var doc in documents)
            {
                Documents.Add(new IndexedDocumentViewModel(doc));
            }

            OnPropertyChanged(nameof(FilteredDocuments));

            _logger.LogInformation("Loaded {Count} indexed documents", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load index status");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        await _statusService.RefreshStaleStatusAsync(ct);
        await LoadAsync(ct);
    }

    partial void OnFilterTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredDocuments));
    }

    partial void OnStatusFilterChanged(IndexingStatus? value)
    {
        OnPropertyChanged(nameof(FilteredDocuments));
    }

    private bool MatchesFilter(IndexedDocumentViewModel doc)
    {
        // Text filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            if (!doc.FileName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                !doc.FilePath.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Status filter
        if (StatusFilter.HasValue && doc.Status != StatusFilter.Value)
        {
            return false;
        }

        return true;
    }
}
```

### 2.6 IndexedDocumentViewModel

```csharp
namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for an individual indexed document in the status list.
/// </summary>
public partial class IndexedDocumentViewModel : ViewModelBase
{
    private readonly IndexedDocumentInfo _info;

    public IndexedDocumentViewModel(IndexedDocumentInfo info)
    {
        _info = info;
    }

    public Guid Id => _info.Id;
    public string FileName => _info.FileName;
    public string FilePath => _info.FilePath;
    public IndexingStatus Status => _info.Status;
    public int ChunkCount => _info.ChunkCount;
    public string? ErrorMessage => _info.ErrorMessage;

    public string StatusDisplay => Status switch
    {
        IndexingStatus.Indexed => "Indexed",
        IndexingStatus.Pending => "Pending",
        IndexingStatus.Indexing => "Indexing...",
        IndexingStatus.Stale => "Stale",
        IndexingStatus.Failed => "Failed",
        _ => "Unknown"
    };

    public string StatusColor => Status switch
    {
        IndexingStatus.Indexed => "Success",
        IndexingStatus.Pending => "Info",
        IndexingStatus.Indexing => "Info",
        IndexingStatus.Stale => "Warning",
        IndexingStatus.Failed => "Error",
        _ => "Secondary"
    };

    public string IndexedAtDisplay => _info.IndexedAt?.LocalDateTime.ToString("g") ?? "Never";

    public string RelativeTime => _info.IndexedAt.HasValue
        ? GetRelativeTime(_info.IndexedAt.Value)
        : "--";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanRetry => Status == IndexingStatus.Failed;
    public bool CanReindex => Status == IndexingStatus.Indexed || Status == IndexingStatus.Stale;

    private static string GetRelativeTime(DateTimeOffset timestamp)
    {
        var span = DateTimeOffset.UtcNow - timestamp;

        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return timestamp.LocalDateTime.ToString("MMM d");
    }
}
```

### 2.7 IndexStatusView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Modules.RAG.ViewModels"
             x:Class="Lexichord.Modules.RAG.Views.IndexStatusView"
             x:DataType="vm:IndexStatusViewModel">

    <Grid RowDefinitions="Auto,Auto,*">
        <!-- Header with Statistics -->
        <Border Grid.Row="0"
                Padding="16"
                Background="{DynamicResource SurfaceBackground}">
            <Grid ColumnDefinitions="*,Auto">
                <StackPanel Grid.Column="0" Spacing="4">
                    <TextBlock Text="Index Status"
                               FontSize="18"
                               FontWeight="SemiBold"/>
                    <TextBlock IsVisible="{Binding Statistics, Converter={x:Static ObjectConverters.IsNotNull}}"
                               Foreground="{DynamicResource SecondaryTextBrush}">
                        <Run Text="{Binding Statistics.DocumentCount}"/>
                        <Run Text="documents"/>
                        <Run Text="•"/>
                        <Run Text="{Binding Statistics.ChunkCount}"/>
                        <Run Text="chunks"/>
                        <Run Text="•"/>
                        <Run Text="{Binding Statistics.StorageSizeDisplay}"/>
                    </TextBlock>
                </StackPanel>

                <Button Grid.Column="1"
                        Command="{Binding RefreshCommand}"
                        ToolTip.Tip="Refresh status">
                    <PathIcon Data="{StaticResource RefreshIcon}" Width="16" Height="16"/>
                </Button>
            </Grid>
        </Border>

        <!-- Filter Bar -->
        <Border Grid.Row="1"
                Padding="16,8"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,0,0,1">
            <Grid ColumnDefinitions="*,Auto">
                <TextBox Grid.Column="0"
                         Text="{Binding FilterText, Mode=TwoWay}"
                         Watermark="Filter documents..."
                         Margin="0,0,8,0"/>

                <ComboBox Grid.Column="1"
                          SelectedItem="{Binding StatusFilter}"
                          PlaceholderText="All statuses"
                          Width="150">
                    <ComboBoxItem Content="All" Tag="{x:Null}"/>
                    <ComboBoxItem Content="Indexed"/>
                    <ComboBoxItem Content="Pending"/>
                    <ComboBoxItem Content="Stale"/>
                    <ComboBoxItem Content="Failed"/>
                </ComboBox>
            </Grid>
        </Border>

        <!-- Document List -->
        <Grid Grid.Row="2">
            <!-- Loading -->
            <ProgressRing IsVisible="{Binding IsLoading}"
                          IsIndeterminate="True"
                          Width="32" Height="32"/>

            <!-- Empty State -->
            <StackPanel IsVisible="{Binding !Documents.Count}"
                        HorizontalAlignment="Center"
                        VerticalAlignment="Center"
                        Spacing="8">
                <PathIcon Data="{StaticResource EmptyFolderIcon}"
                          Width="48" Height="48"
                          Foreground="{DynamicResource SecondaryTextBrush}"/>
                <TextBlock Text="No documents indexed"
                           Foreground="{DynamicResource SecondaryTextBrush}"/>
            </StackPanel>

            <!-- Document List -->
            <ScrollViewer IsVisible="{Binding Documents.Count}">
                <ItemsControl ItemsSource="{Binding FilteredDocuments}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType="vm:IndexedDocumentViewModel">
                            <Border Padding="16,12"
                                    BorderBrush="{DynamicResource BorderBrush}"
                                    BorderThickness="0,0,0,1">
                                <Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto,Auto">
                                    <!-- Document Icon -->
                                    <PathIcon Grid.Column="0"
                                              Data="{StaticResource DocumentIcon}"
                                              Width="20" Height="20"
                                              Margin="0,0,12,0"/>

                                    <!-- Document Info -->
                                    <StackPanel Grid.Column="1" Spacing="2">
                                        <TextBlock Text="{Binding FileName}"
                                                   FontWeight="Medium"/>
                                        <TextBlock Text="{Binding FilePath}"
                                                   FontSize="12"
                                                   Foreground="{DynamicResource SecondaryTextBrush}"
                                                   TextTrimming="CharacterEllipsis"/>
                                        <!-- Error Message -->
                                        <TextBlock IsVisible="{Binding HasError}"
                                                   Text="{Binding ErrorMessage}"
                                                   FontSize="12"
                                                   Foreground="{DynamicResource ErrorForeground}"
                                                   TextWrapping="Wrap"
                                                   Margin="0,4,0,0"/>
                                    </StackPanel>

                                    <!-- Chunk Count -->
                                    <TextBlock Grid.Column="2"
                                               Text="{Binding ChunkCount, StringFormat='{}{0} chunks'}"
                                               Foreground="{DynamicResource SecondaryTextBrush}"
                                               Margin="16,0"/>

                                    <!-- Indexed Time -->
                                    <TextBlock Grid.Column="3"
                                               Text="{Binding RelativeTime}"
                                               Foreground="{DynamicResource SecondaryTextBrush}"
                                               Width="60"/>

                                    <!-- Status Badge -->
                                    <Border Grid.Column="4"
                                            Classes="status-badge"
                                            Classes.Success="{Binding StatusColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=Success}"
                                            Classes.Warning="{Binding StatusColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=Warning}"
                                            Classes.Error="{Binding StatusColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=Error}"
                                            Classes.Info="{Binding StatusColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=Info}"
                                            Margin="16,0">
                                        <TextBlock Text="{Binding StatusDisplay}"
                                                   FontSize="12"/>
                                    </Border>

                                    <!-- Actions -->
                                    <StackPanel Grid.Column="5"
                                                Orientation="Horizontal"
                                                Spacing="4">
                                        <Button Classes="icon-button"
                                                IsVisible="{Binding CanReindex}"
                                                ToolTip.Tip="Re-index"
                                                Command="{Binding $parent[ItemsControl].DataContext.ReindexDocumentCommand}"
                                                CommandParameter="{Binding Id}">
                                            <PathIcon Data="{StaticResource RefreshIcon}" Width="14" Height="14"/>
                                        </Button>
                                        <Button Classes="icon-button"
                                                IsVisible="{Binding CanRetry}"
                                                ToolTip.Tip="Retry"
                                                Command="{Binding $parent[ItemsControl].DataContext.RetryDocumentCommand}"
                                                CommandParameter="{Binding Id}">
                                            <PathIcon Data="{StaticResource RetryIcon}" Width="14" Height="14"/>
                                        </Button>
                                        <Button Classes="icon-button"
                                                ToolTip.Tip="Remove from index"
                                                Command="{Binding $parent[ItemsControl].DataContext.RemoveDocumentCommand}"
                                                CommandParameter="{Binding Id}">
                                            <PathIcon Data="{StaticResource TrashIcon}" Width="14" Height="14"/>
                                        </Button>
                                    </StackPanel>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Grid>
    </Grid>
</UserControl>
```

### 2.8 DI Registration

```csharp
// In RAGModule.cs
services.AddSingleton<IIndexStatusService, IndexStatusService>();
services.AddTransient<IndexStatusViewModel>();

// Register as settings page
containerRegistry.RegisterForNavigation<IndexStatusView, IndexStatusViewModel>("IndexSettings");
```

---

## 3. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7a")]
public class IndexStatusServiceTests
{
    private readonly Mock<IDocumentRepository> _docRepoMock;
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IFileHashService> _hashServiceMock;
    private readonly IndexStatusService _sut;

    public IndexStatusServiceTests()
    {
        _docRepoMock = new Mock<IDocumentRepository>();
        _chunkRepoMock = new Mock<IChunkRepository>();
        _hashServiceMock = new Mock<IFileHashService>();

        _sut = new IndexStatusService(
            _docRepoMock.Object,
            _chunkRepoMock.Object,
            _hashServiceMock.Object,
            NullLogger<IndexStatusService>.Instance);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_ReturnsAllDocuments()
    {
        // Arrange
        var docs = new List<Document>
        {
            CreateDocument("doc1.md"),
            CreateDocument("doc2.md")
        };
        _docRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(docs);
        _chunkRepoMock.Setup(r => r.GetCountByDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _hashServiceMock.Setup(h => h.ComputeHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("hash123");

        // Act
        var result = await _sut.GetAllDocumentsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStatisticsAsync_CalculatesTotals()
    {
        // Arrange
        var docs = new List<Document>
        {
            CreateDocument("doc1.md", chunkCount: 10),
            CreateDocument("doc2.md", chunkCount: 20)
        };
        _docRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(docs);
        _chunkRepoMock.SetupSequence(r => r.GetCountByDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .ReturnsAsync(20);

        // Act
        var stats = await _sut.GetStatisticsAsync();

        // Assert
        stats.DocumentCount.Should().Be(2);
        stats.ChunkCount.Should().Be(30);
    }

    private static Document CreateDocument(string path, int chunkCount = 0)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            FilePath = $"/workspace/{path}",
            FileHash = "hash123",
            IndexedAt = DateTimeOffset.UtcNow,
            ChunkCount = chunkCount
        };
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7a")]
public class IndexStatusViewModelTests
{
    [Fact]
    public async Task LoadAsync_PopulatesDocumentsAndStatistics()
    {
        // Arrange
        var mockService = new Mock<IIndexStatusService>();
        mockService.Setup(s => s.GetAllDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IndexedDocumentInfo>
            {
                new() { Id = Guid.NewGuid(), FilePath = "/test.md", Status = IndexingStatus.Indexed }
            });
        mockService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexStatistics { DocumentCount = 1, ChunkCount = 10 });

        var sut = new IndexStatusViewModel(mockService.Object, NullLogger<IndexStatusViewModel>.Instance);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Documents.Should().HaveCount(1);
        sut.Statistics.Should().NotBeNull();
        sut.Statistics!.DocumentCount.Should().Be(1);
    }

    [Fact]
    public void FilteredDocuments_FiltersbyText()
    {
        var sut = CreateViewModelWithDocuments("chapter-01.md", "notes.md", "outline.md");

        sut.FilterText = "chapter";

        sut.FilteredDocuments.Should().HaveCount(1);
        sut.FilteredDocuments[0].FileName.Should().Be("chapter-01.md");
    }

    [Fact]
    public void FilteredDocuments_FiltersByStatus()
    {
        var sut = CreateViewModelWithMixedStatus();

        sut.StatusFilter = IndexingStatus.Failed;

        sut.FilteredDocuments.Should().OnlyContain(d => d.Status == IndexingStatus.Failed);
    }
}
```

---

## 4. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Information | "Loaded {Count} indexed documents" | After load |
| Debug | "Document marked stale: {Path}" | During refresh |
| Debug | "Filtering documents: text='{Text}', status={Status}" | On filter change |
| Error | "Failed to load index status" | Load exception |

---

## 5. File Locations

| File | Path |
| :--- | :--- |
| IIndexStatusService | `src/Lexichord.Abstractions/Contracts/IIndexStatusService.cs` |
| IndexedDocumentInfo | `src/Lexichord.Modules.RAG/Models/IndexedDocumentInfo.cs` |
| IndexStatistics | `src/Lexichord.Modules.RAG/Models/IndexStatistics.cs` |
| IndexStatusService | `src/Lexichord.Modules.RAG/Services/IndexStatusService.cs` |
| IndexStatusViewModel | `src/Lexichord.Modules.RAG/ViewModels/IndexStatusViewModel.cs` |
| IndexedDocumentViewModel | `src/Lexichord.Modules.RAG/ViewModels/IndexedDocumentViewModel.cs` |
| IndexStatusView | `src/Lexichord.Modules.RAG/Views/IndexStatusView.axaml` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Services/IndexStatusServiceTests.cs` |

---

## 6. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | View displays all indexed documents | [ ] |
| 2 | Status badges show correct state | [ ] |
| 3 | Summary statistics display correctly | [ ] |
| 4 | Text filter works on filename and path | [ ] |
| 5 | Status filter limits displayed documents | [ ] |
| 6 | Relative time displays correctly | [ ] |
| 7 | Error messages display for failed documents | [ ] |
| 8 | Refresh button updates status | [ ] |
| 9 | All unit tests pass | [ ] |

---

## 7. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
