using System.Net.Http.Json;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for user management operations, communicating with the API.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API communication.</param>
    public UserManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of users.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="roleFilter">Optional role filter.</param>
    /// <param name="isActiveFilter">Optional status filter.</param>
    /// <param name="searchText">Optional search text for name or email.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortOrder">The sort order (asc or desc).</param>
    /// <returns>A paginated result containing user list data.</returns>
    public async Task<PagedResult<UserListDto>> GetAllUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Role? roleFilter = null,
        bool? isActiveFilter = null,
        string? searchText = null,
        string sortBy = "Name",
        string sortOrder = "asc")
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}",
            $"sortBy={sortBy}",
            $"sortOrder={sortOrder}"
        };

        if (roleFilter.HasValue)
        {
            queryParams.Add($"roleFilter={roleFilter.Value}");
        }

        if (isActiveFilter.HasValue)
        {
            queryParams.Add($"isActiveFilter={isActiveFilter.Value}");
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");
        }

        var url = $"api/users/admin?{string.Join("&", queryParams)}";
        var response = await _httpClient.GetFromJsonAsync<PagedResult<UserListDto>>(url);

        return response ?? new PagedResult<UserListDto>();
    }

    /// <summary>
    /// Retrieves detailed information about a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>Detailed user information.</returns>
    public async Task<UserDetailsDto> GetUserByIdAsync(Guid userId)
    {
        var response = await _httpClient.GetFromJsonAsync<UserDetailsDto>($"api/users/admin/{userId}");
        return response ?? throw new InvalidOperationException("Failed to retrieve user details.");
    }

    /// <summary>
    /// Updates user information as an administrator.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <returns>The updated user details.</returns>
    public async Task<UserDetailsDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/admin/{userId}", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UserDetailsDto>();
        return result ?? throw new InvalidOperationException("Failed to update user.");
    }

    /// <summary>
    /// Activates or deactivates a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="isActive">True to activate, false to deactivate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateUserStatusAsync(Guid userId, bool isActive)
    {
        var request = new UpdateUserStatusRequest { IsActive = isActive };
        var response = await _httpClient.PutAsJsonAsync($"api/users/admin/{userId}/status", request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Updates a user's role.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="newRole">The new role to assign.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateUserRoleAsync(Guid userId, Role newRole)
    {
        var request = new UpdateUserRoleRequest { Role = newRole };
        var response = await _httpClient.PutAsJsonAsync($"api/users/admin/{userId}/role", request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Permanently deletes a user account and cascade deletes related registrations.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteUserAsync(Guid userId)
    {
        var response = await _httpClient.DeleteAsync($"api/users/admin/{userId}");
        response.EnsureSuccessStatusCode();
    }
}
