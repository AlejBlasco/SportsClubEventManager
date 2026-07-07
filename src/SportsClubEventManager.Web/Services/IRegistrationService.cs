using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Contract for authenticated user registration operations.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Retrieves active registrations for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active registrations.</returns>
    Task<List<RegistrationListDto>> GetMyRegistrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels one registration owned by the authenticated user.
    /// </summary>
    /// <param name="registrationId">Registration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when cancellation succeeds.</returns>
    Task<bool> CancelMyRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default);
}
