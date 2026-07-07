using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Admin;

namespace SportsClubEventManager.Web.Components.Pages.Admin;

/// <summary>
/// Code-behind for the Event Management page.
/// </summary>
public partial class EventManagement
{
    private PagedResult<EventAdminListDto>? _events;
    private bool _loading = true;
    private string? _errorMessage;
    private string? _successMessage;

    private string? _searchText;
    private string _statusFilter = "";
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _sortBy = "Date";
    private string _sortOrder = "asc";
    private int _currentPage = 1;
    private int _pageSize = 20;

    private EventFormModal _eventFormModal = null!;
    private DeleteEventConfirmModal _deleteEventModal = null!;

    /// <summary>
    /// Initializes the component by loading the initial event list.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadEventsAsync();
    }

    /// <summary>
    /// Loads events from the API with current filter and pagination settings.
    /// </summary>
    private async Task LoadEventsAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            bool? isUpcoming = _statusFilter switch
            {
                "upcoming" => true,
                "past" => false,
                _ => null
            };

            _events = await EventManagementService.GetAllEventsAsync(
                pageNumber: _currentPage,
                pageSize: _pageSize,
                fromDate: _fromDate,
                toDate: _toDate,
                isUpcoming: isUpcoming,
                searchText: _searchText,
                sortBy: _sortBy,
                sortOrder: _sortOrder);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load events: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Applies the current filter settings and reloads the event list.
    /// </summary>
    private async Task ApplyFiltersAsync()
    {
        _currentPage = 1;
        await LoadEventsAsync();
    }

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    private async Task PreviousPageAsync()
    {
        if (_events?.HasPreviousPage == true)
        {
            _currentPage--;
            await LoadEventsAsync();
        }
    }

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    private async Task NextPageAsync()
    {
        if (_events?.HasNextPage == true)
        {
            _currentPage++;
            await LoadEventsAsync();
        }
    }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    /// <param name="pageNumber">The page number to navigate to.</param>
    private async Task GoToPageAsync(int pageNumber)
    {
        _currentPage = pageNumber;
        await LoadEventsAsync();
    }

    /// <summary>
    /// Opens the create event modal.
    /// </summary>
    private void OpenCreateModal()
    {
        _eventFormModal.Open();
    }

    /// <summary>
    /// Opens the edit event modal with the specified event.
    /// </summary>
    /// <param name="eventItem">The event to edit.</param>
    private void OpenEditModal(EventAdminListDto eventItem)
    {
        _eventFormModal.Open(eventItem);
    }

    /// <summary>
    /// Opens the delete confirmation modal with the specified event.
    /// </summary>
    /// <param name="eventItem">The event to delete.</param>
    private void OpenDeleteModal(EventAdminListDto eventItem)
    {
        _deleteEventModal.Open(eventItem);
    }

    /// <summary>
    /// Handles the event saved callback from the event form modal.
    /// </summary>
    private async Task OnEventSavedAsync()
    {
        _successMessage = "Event saved successfully.";
        await LoadEventsAsync();
    }

    /// <summary>
    /// Handles the event deleted callback from the delete confirmation modal.
    /// </summary>
    /// <param name="message">The deletion result message.</param>
    private async Task OnEventDeletedAsync(string message)
    {
        _successMessage = message;
        await LoadEventsAsync();
    }

    /// <summary>
    /// Determines the Bootstrap badge class based on the event capacity.
    /// </summary>
    /// <param name="eventItem">The event to evaluate.</param>
    /// <returns>The Bootstrap badge class name.</returns>
    private static string GetCapacityBadgeClass(EventAdminListDto eventItem)
    {
        var percentage = (double)eventItem.CurrentRegistrations / eventItem.MaxCapacity;
        return percentage switch
        {
            >= 0.9 => "bg-danger",
            >= 0.7 => "bg-warning",
            _ => "bg-info"
        };
    }
}
