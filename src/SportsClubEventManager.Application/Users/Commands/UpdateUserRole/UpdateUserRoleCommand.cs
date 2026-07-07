using MediatR;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserRole;

/// <summary>
/// Command to update a user's role.
/// </summary>
public class UpdateUserRoleCommand : IRequest<Unit>
{
    /// <summary>
    /// Gets or sets the ID of the administrator performing the role change.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user whose role is being changed.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the new role to assign to the user.
    /// </summary>
    public Role NewRole { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
