using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request model for updating a user's role.
/// </summary>
public class UpdateUserRoleRequest
{
    /// <summary>
    /// Gets or sets the new role to assign to the user.
    /// </summary>
    public Role Role { get; set; }
}
