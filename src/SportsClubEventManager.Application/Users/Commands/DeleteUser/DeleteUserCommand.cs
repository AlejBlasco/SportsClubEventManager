using MediatR;

namespace SportsClubEventManager.Application.Users.Commands.DeleteUser;

/// <summary>
/// Command to permanently delete a user account and cascade delete related registrations.
/// </summary>
public class DeleteUserCommand : IRequest<Unit>
{
    /// <summary>
    /// Gets or sets the ID of the administrator performing the deletion.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user to delete.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
