using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Components.Admin;

/// <summary>
/// Modal component for creating and editing events.
/// </summary>
public partial class EventFormModal
{
    private bool _isVisible;
    private bool _editMode;
    private bool _saving;
    private string? _errorMessage;
    private EventFormModel _formModel = new();
    private Guid? _editingEventId;
    private byte[]? _rowVersion;
    private int _currentRegistrations;

    /// <summary>
    /// Gets or sets the callback invoked when an event is successfully saved.
    /// </summary>
    [Parameter]
    public EventCallback OnEventSaved { get; set; }

    /// <summary>
    /// Opens the modal for creating a new event.
    /// </summary>
    public void Open()
    {
        _editMode = false;
        _editingEventId = null;
        _rowVersion = null;
        _currentRegistrations = 0;
        _formModel = new EventFormModel
        {
            Date = DateTime.Now.AddDays(7),
            MaxCapacity = 50
        };
        _errorMessage = null;
        _isVisible = true;
        StateHasChanged();
    }

    /// <summary>
    /// Opens the modal for editing an existing event.
    /// </summary>
    /// <param name="eventItem">The event to edit.</param>
    public void Open(EventAdminListDto eventItem)
    {
        _editMode = true;
        _editingEventId = eventItem.Id;
        _rowVersion = eventItem.RowVersion;
        _currentRegistrations = eventItem.CurrentRegistrations;
        _formModel = new EventFormModel
        {
            Title = eventItem.Title,
            Description = null,
            Date = eventItem.Date,
            Location = eventItem.Location,
            MaxCapacity = eventItem.MaxCapacity
        };
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
        _errorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Saves the event (create or update).
    /// </summary>
    private async Task SaveAsync()
    {
        _saving = true;
        _errorMessage = null;

        try
        {
            if (_editMode && _editingEventId.HasValue)
            {
                var updateRequest = new UpdateEventRequest
                {
                    Title = _formModel.Title,
                    Description = _formModel.Description,
                    Date = _formModel.Date,
                    Location = _formModel.Location,
                    MaxCapacity = _formModel.MaxCapacity,
                    RowVersion = _rowVersion
                };

                await EventManagementService.UpdateEventAsync(_editingEventId.Value, updateRequest);
            }
            else
            {
                var createRequest = new CreateEventRequest
                {
                    Title = _formModel.Title,
                    Description = _formModel.Description,
                    Date = _formModel.Date,
                    Location = _formModel.Location,
                    MaxCapacity = _formModel.MaxCapacity
                };

                await EventManagementService.CreateEventAsync(createRequest);
            }

            Close();
            await OnEventSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save event: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>
    /// Form model for event creation and editing.
    /// </summary>
    private class EventFormModel
    {
        /// <summary>
        /// Gets or sets the title of the event.
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the event.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2,000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the event takes place.
        /// </summary>
        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the location where the event takes place.
        /// </summary>
        [Required(ErrorMessage = "Location is required")]
        [StringLength(300, ErrorMessage = "Location cannot exceed 300 characters")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum capacity of the event.
        /// </summary>
        [Required(ErrorMessage = "Max capacity is required")]
        [Range(1, 10000, ErrorMessage = "Capacity must be between 1 and 10,000")]
        public int MaxCapacity { get; set; }
    }
}
