using System.Net.Http.Json;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for administrator registration management API operations.
/// </summary>
public sealed class AdminRegistrationManagementService(HttpClient httpClient) : IAdminRegistrationManagementService
{
    /// <summary>
    /// Retrieves paginated registrations with optional filtering.
    /// </summary>
    /// <returns>A paged registration result.</returns>
    public async Task<PagedResult<RegistrationListDto>> GetRegistrationsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? eventId = null,
        Guid? userId = null,
        RegistrationStatus? status = null,
        DateTime? eventDateFrom = null,
        DateTime? eventDateTo = null,
        string? searchText = null,
        string sortBy = "RegistrationDate",
        string sortOrder = "desc")
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}",
            $"sortBy={sortBy}",
            $"sortOrder={sortOrder}"
        };

        if (eventId.HasValue)
        {
            queryParams.Add($"eventId={eventId}");
        }

        if (userId.HasValue)
        {
            queryParams.Add($"userId={userId}");
        }

        if (status.HasValue)
        {
            queryParams.Add($"status={status}");
        }

        if (eventDateFrom.HasValue)
        {
            queryParams.Add($"eventDateFrom={eventDateFrom.Value:yyyy-MM-ddTHH:mm:ss}");
        }

        if (eventDateTo.HasValue)
        {
            queryParams.Add($"eventDateTo={eventDateTo.Value:yyyy-MM-ddTHH:mm:ss}");
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");
        }

        var url = $"api/admin/registrations?{string.Join("&", queryParams)}";
        var result = await httpClient.GetFromJsonAsync<PagedResult<RegistrationListDto>>(url);
        return result ?? new PagedResult<RegistrationListDto>();
    }

    /// <summary>
    /// Creates a registration as administrator.
    /// </summary>
    /// <param name="request">Registration creation request.</param>
    /// <returns>Created registration identifier.</returns>
    public async Task<Guid> CreateRegistrationAsync(CreateAdminRegistrationRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/admin/registrations", request);
        response.EnsureSuccessStatusCode();

        var registrationId = await response.Content.ReadFromJsonAsync<Guid>();
        return registrationId;
    }

    /// <summary>
    /// Cancels a registration by identifier.
    /// </summary>
    /// <param name="registrationId">Registration identifier.</param>
    public async Task CancelRegistrationAsync(Guid registrationId)
    {
        var response = await httpClient.DeleteAsync($"api/admin/registrations/{registrationId}");
        response.EnsureSuccessStatusCode();
    }
}
