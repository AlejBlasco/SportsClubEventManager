using MediatR;

namespace SportsClubEventManager.Application.Registrations.Commands.CreateAdminRegistration;

/// <summary>
/// Command to manually create a registration as administrator.
/// </summary>
public sealed class CreateAdminRegistrationCommand : IRequest<Guid>
{
    /// <summary>
    /// Gets or sets the administrator user identifier.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the target user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the target event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the actor, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent of the actor, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
