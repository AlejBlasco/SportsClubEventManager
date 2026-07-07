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

    private string _manualUserId = string.Empty;
    private string _manualEventId = string.Empty;

    /// <summary>
    /// Initializes the page and loads registrations.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadRegistrationsAsync();
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

        if (!Guid.TryParse(_manualUserId, out var userId))
        {
            _errorMessage = "Manual registration requires a valid user GUID.";
            return;
        }

        if (!Guid.TryParse(_manualEventId, out var eventId))
        {
            _errorMessage = "Manual registration requires a valid event GUID.";
            return;
        }

        try
        {
            _creatingRegistration = true;
            await AdminRegistrationManagementService.CreateRegistrationAsync(new CreateAdminRegistrationRequest
            {
                UserId = userId,
                EventId = eventId
            });

            _manualUserId = string.Empty;
            _manualEventId = string.Empty;
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

    private async Task ExportPdfAsync()
    {
        if (_registrations is null || !_registrations.Items.Any())
        {
            _errorMessage = "No data available to export.";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Registration Report");
        builder.AppendLine($"Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();

        foreach (var item in _registrations.Items)
        {
            builder.AppendLine($"Registration: {item.RegistrationId}");
            builder.AppendLine($"Event: {item.EventTitle} ({item.EventDate:yyyy-MM-dd HH:mm})");
            builder.AppendLine($"User: {item.UserName} <{item.UserEmail}>");
            builder.AppendLine($"Status: {item.Status}");
            builder.AppendLine($"Registered On: {item.RegistrationDate:yyyy-MM-dd HH:mm}");
            builder.AppendLine(new string('-', 60));
        }

        await JSRuntime.InvokeVoidAsync(
            "downloadFileFromText",
            $"registrations-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            builder.ToString(),
            "application/pdf");
    }

    private static string EscapeCsv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
