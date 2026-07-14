using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Pages.Admin;

/// <summary>
/// Code-behind for the administrator registration management page.
/// </summary>
public partial class RegistrationManagement
{
    private PagedResult<RegistrationListDto>? _registrations;
    private bool _loading = true;
    private bool _creatingRegistration;
    private string? _errorMessage;
    private string? _successMessage;

    private int _currentPage = 1;
    private int _pageSize = 20;
    private string? _searchText;
    private string _sortBy = "RegistrationDate";
    private string _sortOrder = "desc";
    private string _statusFilter = string.Empty;
    private DateTime? _eventDateFrom;
    private DateTime? _eventDateTo;

    private Guid? _manualUserId;
    private Guid? _manualEventId;
    private IReadOnlyList<UserListDto> _activeUsers = [];
    private IReadOnlyList<EventAdminListDto> _upcomingEvents = [];

    /// <summary>
    /// Initializes the page, loading registrations and the dropdown data for manual registration.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadRegistrationsAsync();
        await LoadManualRegistrationOptionsAsync();
    }

    /// <summary>
    /// Loads the active users and upcoming events used to populate the manual registration dropdowns.
    /// Only active users and upcoming events are offered, since both are rejected by
    /// <c>CreateAdminRegistrationCommandHandler</c> anyway.
    /// </summary>
    private async Task LoadManualRegistrationOptionsAsync()
    {
        try
        {
            var usersResult = await UserManagementService.GetAllUsersAsync(
                pageNumber: 1,
                pageSize: 100,
                isActiveFilter: true,
                sortBy: "Name",
                sortOrder: "asc");
            _activeUsers = usersResult.Items.ToList();

            var eventsResult = await EventManagementService.GetAllEventsAsync(
                pageNumber: 1,
                pageSize: 100,
                isUpcoming: true,
                sortBy: "Date",
                sortOrder: "asc");
            _upcomingEvents = eventsResult.Items.ToList();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load users/events for manual registration: {ex.Message}";
        }
    }

    private async Task LoadRegistrationsAsync()
    {
        try
        {
            _loading = true;
            _errorMessage = null;

            RegistrationStatus? status = null;
            if (!string.IsNullOrWhiteSpace(_statusFilter) && Enum.TryParse<RegistrationStatus>(_statusFilter, out var parsed))
            {
                status = parsed;
            }

            _registrations = await AdminRegistrationManagementService.GetRegistrationsAsync(
                pageNumber: _currentPage,
                pageSize: _pageSize,
                status: status,
                eventDateFrom: _eventDateFrom,
                eventDateTo: _eventDateTo,
                searchText: _searchText,
                sortBy: _sortBy,
                sortOrder: _sortOrder);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load registrations: {ex.Message}";
            _registrations = new PagedResult<RegistrationListDto>();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        _currentPage = 1;
        await LoadRegistrationsAsync();
    }

    private async Task ChangePageAsync(int pageNumber)
    {
        if (pageNumber < 1 || (_registrations is not null && pageNumber > _registrations.TotalPages))
        {
            return;
        }

        _currentPage = pageNumber;
        await LoadRegistrationsAsync();
    }

    private async Task CreateManualRegistrationAsync()
    {
        _errorMessage = null;
        _successMessage = null;

        if (_manualUserId is null || _manualEventId is null)
        {
            _errorMessage = "Select both a user and an event for the manual registration.";
            return;
        }

        try
        {
            _creatingRegistration = true;
            await AdminRegistrationManagementService.CreateRegistrationAsync(new CreateAdminRegistrationRequest
            {
                UserId = _manualUserId.Value,
                EventId = _manualEventId.Value
            });

            _manualUserId = null;
            _manualEventId = null;
            _successMessage = "Registration created successfully.";
            await LoadRegistrationsAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to create registration: {ex.Message}";
        }
        finally
        {
            _creatingRegistration = false;
        }
    }

    private async Task ConfirmCancelAsync(Guid registrationId)
    {
        _errorMessage = null;
        _successMessage = null;

        try
        {
            await AdminRegistrationManagementService.CancelRegistrationAsync(registrationId);
            _successMessage = "Registration cancelled successfully.";
            await LoadRegistrationsAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to cancel registration: {ex.Message}";
        }
    }

    private async Task ExportCsvAsync()
    {
        if (_registrations is null || !_registrations.Items.Any())
        {
            _errorMessage = "No data available to export.";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("RegistrationId,EventTitle,EventDate,UserName,UserEmail,RegistrationDate,Status");

        foreach (var item in _registrations.Items)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(item.RegistrationId.ToString()),
                EscapeCsv(item.EventTitle),
                EscapeCsv(item.EventDate.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(item.UserName),
                EscapeCsv(item.UserEmail),
                EscapeCsv(item.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(item.Status.ToString())));
        }

        await JSRuntime.InvokeVoidAsync(
            "downloadFileFromText",
            $"registrations-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            builder.ToString(),
            "text/csv;charset=utf-8");
    }

    private static string EscapeCsv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
