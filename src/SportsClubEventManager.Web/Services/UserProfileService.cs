using System.Net.Http.Json;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for user profile operations using HttpClient.
/// </summary>
public sealed class UserProfileService(HttpClient httpClient) : IUserProfileService
{
    /// <summary>
    /// Retrieves the profile of the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user profile data.</returns>
    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var response = await httpClient.GetAsync($"api/users/{userId}/profile");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserProfileDto>();
    }

    /// <summary>
    /// Updates the profile of the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The profile update request.</param>
    /// <returns>The updated user profile data.</returns>
    public async Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/users/{userId}/profile", request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserProfileDto>();
    }

    /// <summary>
    /// Changes the password of the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The password change request.</param>
    /// <returns>The password change response with new tokens.</returns>
    public async Task<ChangePasswordResponse?> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/users/{userId}/password", request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
    }
}
