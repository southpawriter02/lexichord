using Lexichord.Abstractions.Contracts.Navigation;
using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Layout;
using Lexichord.Host.Views.Sections;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Handles navigation requests for section views, creating them on demand.
/// </summary>
/// <remarks>
/// LOGIC: Subscribes to RegionNavigationRequestNotification via MediatR.
/// When a navigation request comes in for a section ID (lexichord.section.*),
/// this handler creates the appropriate section view and registers it as a
/// document in the center region.
/// </remarks>
public sealed class SectionNavigationHandler : INotificationHandler<RegionNavigationRequestNotification>
{
    private readonly IRegionManager _regionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SectionNavigationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionNavigationHandler"/> class.
    /// </summary>
    public SectionNavigationHandler(
        IRegionManager regionManager,
        IServiceProvider serviceProvider,
        ILogger<SectionNavigationHandler> logger)
    {
        _regionManager = regionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(RegionNavigationRequestNotification notification, CancellationToken cancellationToken)
    {
        // LOGIC: Only handle section navigation requests
        if (!SectionNavigationService.TryParseSection(notification.RequestedId, out var section))
        {
            return;
        }

        _logger.LogDebug(
            "Handling navigation request for section {Section} (ID: {RequestedId})",
            section,
            notification.RequestedId);

        // LOGIC: Create the section view and register it as a document
        var (title, viewFactory) = GetSectionViewInfo(section);

        try
        {
            var document = await _regionManager.RegisterDocumentAsync(
                id: notification.RequestedId,
                title: title,
                viewFactory: viewFactory,
                options: new DocumentRegistrationOptions(
                    ActivateOnRegister: true,
                    IsPinned: true // Section views should stay open
                ));

            if (document is not null)
            {
                _logger.LogInformation(
                    "Created section view for {Section} (ID: {DocumentId})",
                    section,
                    notification.RequestedId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create section view for {Section}",
                    section);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating section view for {Section}",
                section);
        }
    }

    /// <summary>
    /// Gets the title and view factory for a section.
    /// </summary>
    private static (string Title, Func<IServiceProvider, object> ViewFactory) GetSectionViewInfo(NavigationSection section)
    {
        return section switch
        {
            NavigationSection.Documents => ("Documents", _ => new DocumentsSectionView()),
            NavigationSection.StyleGuide => ("Style Guide", _ => new StyleGuideSectionView()),
            NavigationSection.Memory => ("Memory", _ => new MemorySectionView()),
            NavigationSection.Agents => ("Agents", _ => new AgentsSectionView()),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
        };
    }
}
