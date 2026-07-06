using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service interface for user profile operations.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Retrieves the profile of the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user profile data.</returns>
    Task<UserProfileDto?> GetProfileAsync(Guid userId);

    /// <summary>
    /// Updates the profile of the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The profile update request.</param>
    /// <returns>The updated user profile data.</returns>
    Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

    /// <summary>
    /// Changes the password of the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The password change request.</param>
    /// <returns>The password change response with new tokens.</returns>
    Task<ChangePasswordResponse?> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
