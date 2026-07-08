using MediatR;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserStatus;

/// <summary>
/// Command to activate or deactivate a user account.
/// </summary>
public class UpdateUserStatusCommand : IRequest<Unit>
{
    /// <summary>
    /// Gets or sets the ID of the administrator performing the status change.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user whose status is being changed.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account should be active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
