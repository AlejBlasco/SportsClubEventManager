using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Defines the contract for user management operations (administrator only).
/// </summary>
public interface IUserManagementService
{
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
    Task<PagedResult<UserListDto>> GetAllUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Role? roleFilter = null,
        bool? isActiveFilter = null,
        string? searchText = null,
        string sortBy = "Name",
        string sortOrder = "asc");

    /// <summary>
    /// Retrieves detailed information about a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>Detailed user information.</returns>
    Task<UserDetailsDto> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Updates user information as an administrator.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <returns>The updated user details.</returns>
    Task<UserDetailsDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);

    /// <summary>
    /// Activates or deactivates a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="isActive">True to activate, false to deactivate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateUserStatusAsync(Guid userId, bool isActive);

    /// <summary>
    /// Updates a user's role.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="newRole">The new role to assign.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateUserRoleAsync(Guid userId, Role newRole);

    /// <summary>
    /// Permanently deletes a user account and cascade deletes related registrations.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteUserAsync(Guid userId);
}
