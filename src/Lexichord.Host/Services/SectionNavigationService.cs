using Lexichord.Abstractions.Contracts.Navigation;
using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Layout;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Implementation of <see cref="ISectionNavigationService"/> using the dock region system.
/// </summary>
/// <remarks>
/// LOGIC: Maps navigation sections to dockable IDs and delegates to IRegionManager
/// for actual navigation. Handles dynamic creation of section views when they
/// don't exist by publishing navigation request notifications.
/// </remarks>
public sealed class SectionNavigationService : ISectionNavigationService
{
    private readonly IRegionManager _regionManager;
    private readonly IMediator _mediator;
    private readonly ILogger<SectionNavigationService> _logger;

    private NavigationSection? _activeSection;

    /// <summary>
    /// Section ID prefix for all navigation sections.
    /// </summary>
    private const string SectionPrefix = "lexichord.section.";

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionNavigationService"/> class.
    /// </summary>
    public SectionNavigationService(
        IRegionManager regionManager,
        IMediator mediator,
        ILogger<SectionNavigationService> logger)
    {
        _regionManager = regionManager;
        _mediator = mediator;
        _logger = logger;

        // LOGIC: Subscribe to region changes to track active section
        _regionManager.RegionChanged += OnRegionChanged;
    }

    /// <inheritdoc />
    public NavigationSection? ActiveSection => _activeSection;

    /// <inheritdoc />
    public event EventHandler<NavigationSection?>? ActiveSectionChanged;

    /// <inheritdoc />
    public string GetSectionId(NavigationSection section)
    {
        return section switch
        {
            NavigationSection.Documents => $"{SectionPrefix}documents",
            NavigationSection.StyleGuide => $"{SectionPrefix}styleguide",
            NavigationSection.Memory => $"{SectionPrefix}memory",
            NavigationSection.Agents => $"{SectionPrefix}agents",
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
        };
    }

    /// <inheritdoc />
    public async Task<bool> NavigateToSectionAsync(NavigationSection section)
    {
        var sectionId = GetSectionId(section);
        _logger.LogDebug("Navigating to section {Section} (ID: {SectionId})", section, sectionId);

        try
        {
            // LOGIC: Try to navigate to the section - IRegionManager will handle
            // creation via NavigationRequested event if the dockable doesn't exist
            var result = await _regionManager.NavigateToAsync(sectionId);

            if (result)
            {
                _logger.LogInformation("Successfully navigated to section {Section}", section);
                SetActiveSection(section);
            }
            else
            {
                _logger.LogWarning("Failed to navigate to section {Section}", section);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to section {Section}", section);
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse a section from a dockable ID.
    /// </summary>
    /// <param name="dockableId">The dockable ID to parse.</param>
    /// <param name="section">The parsed section, if successful.</param>
    /// <returns>True if the ID represents a navigation section.</returns>
    public static bool TryParseSection(string dockableId, out NavigationSection section)
    {
        section = default;

        if (!dockableId.StartsWith(SectionPrefix))
            return false;

        var sectionName = dockableId[SectionPrefix.Length..];

        switch (sectionName)
        {
            case "documents":
                section = NavigationSection.Documents;
                return true;
            case "styleguide":
                section = NavigationSection.StyleGuide;
                return true;
            case "memory":
                section = NavigationSection.Memory;
                return true;
            case "agents":
                section = NavigationSection.Agents;
                return true;
            default:
                return false;
        }
    }

    private void SetActiveSection(NavigationSection? section)
    {
        if (_activeSection != section)
        {
            _activeSection = section;
            _logger.LogDebug("Active section changed to {Section}", section);
            ActiveSectionChanged?.Invoke(this, section);
        }
    }

    private void OnRegionChanged(object? sender, RegionChangedEventArgs e)
    {
        // LOGIC: Track which section is active based on region activation events
        if (e.ChangeType == RegionChangeType.Activated)
        {
            if (TryParseSection(e.DockableId, out var section))
            {
                SetActiveSection(section);
            }
        }
    }
}
