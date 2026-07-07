using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Components.Admin;

/// <summary>
/// Modal component for confirming event deletion.
/// </summary>
public partial class DeleteEventConfirmModal
{
    private bool _isVisible;
    private bool _deleting;
    private string? _errorMessage;
    private EventAdminListDto? _eventToDelete;

    /// <summary>
    /// Gets or sets the callback invoked when an event is successfully deleted.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnEventDeleted { get; set; }

    /// <summary>
    /// Opens the modal with the specified event to delete.
    /// </summary>
    /// <param name="eventItem">The event to delete.</param>
    public void Open(EventAdminListDto eventItem)
    {
        _eventToDelete = eventItem;
        _errorMessage = null;
        _isVisible = true;
        StateHasChanged();
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    private void Close()
    {
        _isVisible = false;
        _eventToDelete = null;
        _errorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Confirms the deletion and deletes the event.
    /// </summary>
    private async Task ConfirmDeleteAsync()
    {
        if (_eventToDelete == null)
        {
            return;
        }

        _deleting = true;
        _errorMessage = null;

        try
        {
            var response = await EventManagementService.DeleteEventAsync(_eventToDelete.Id);
            Close();
            await OnEventDeleted.InvokeAsync(response.Message);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to delete event: {ex.Message}";
        }
        finally
        {
            _deleting = false;
        }
    }
}
