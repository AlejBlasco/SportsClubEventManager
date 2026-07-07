using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Contract for administrator registration management operations.
/// </summary>
public interface IAdminRegistrationManagementService
{
    /// <summary>
    /// Retrieves paginated registrations with optional filtering.
    /// </summary>
    /// <returns>A paged registration result.</returns>
    Task<PagedResult<RegistrationListDto>> GetRegistrationsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? eventId = null,
        Guid? userId = null,
        RegistrationStatus? status = null,
        DateTime? eventDateFrom = null,
        DateTime? eventDateTo = null,
        string? searchText = null,
        string sortBy = "RegistrationDate",
        string sortOrder = "desc");

    /// <summary>
    /// Creates a registration as administrator.
    /// </summary>
    /// <param name="request">Registration creation request.</param>
    /// <returns>Created registration identifier.</returns>
    Task<Guid> CreateRegistrationAsync(CreateAdminRegistrationRequest request);

    /// <summary>
    /// Cancels a registration by identifier.
    /// </summary>
    /// <param name="registrationId">Registration identifier.</param>
    Task CancelRegistrationAsync(Guid registrationId);
}
