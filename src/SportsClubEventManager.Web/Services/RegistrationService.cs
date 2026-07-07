using System.Net;
using System.Net.Http.Json;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for authenticated user registration operations.
/// </summary>
public sealed class RegistrationService(HttpClient httpClient) : IRegistrationService
{
    /// <summary>
    /// Retrieves active registrations for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active registrations.</returns>
    public async Task<List<RegistrationListDto>> GetMyRegistrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<RegistrationListDto>>("api/v1/registrations/me", cancellationToken);
        return result ?? [];
    }

    /// <summary>
    /// Cancels one registration owned by the authenticated user.
    /// </summary>
    /// <param name="registrationId">Registration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when cancellation succeeds.</returns>
    public async Task<bool> CancelMyRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/v1/registrations/{registrationId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return response.StatusCode == HttpStatusCode.NoContent;
    }
}
