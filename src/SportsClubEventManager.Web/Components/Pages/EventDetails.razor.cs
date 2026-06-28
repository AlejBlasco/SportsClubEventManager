using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Pages;

/// <summary>
/// Code-behind for the EventDetails page component.
/// </summary>
public sealed partial class EventDetails
{
    private EventDetailDto? eventDetail;
    private bool isLoading = true;
    private bool hasError;
    private bool isNotFound;

    /// <summary>
    /// Gets or sets the event ID from the route parameter.
    /// </summary>
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private ILogger<EventDetails> Logger { get; set; } = default!;

    /// <summary>
    /// Initializes the component and loads event details.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadEventDetailsAsync();
    }

    /// <summary>
    /// Loads event details from the API.
    /// </summary>
    private async Task LoadEventDetailsAsync()
    {
        isLoading = true;
        hasError = false;
        isNotFound = false;

        try
        {
            eventDetail = await EventService.GetEventByIdAsync(Id);

            if (eventDetail is null)
            {
                isNotFound = true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load event details for event {EventId}", Id);
            hasError = true;
        }
        finally
        {
            isLoading = false;
        }
    }
}
