using MediatR;

namespace SportsClubEventManager.Application.Registrations.Commands.CancelRegistrationById;

/// <summary>
/// Command to cancel a registration by identifier.
/// </summary>
public sealed class CancelRegistrationByIdCommand : IRequest
{
    /// <summary>
    /// Gets or sets the registration identifier.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the requesting user.
    /// </summary>
    public Guid RequestingUserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the requester is administrator.
    /// </summary>
    public bool IsAdministrator { get; set; }

    /// <summary>
    /// Gets or sets the requester IP address, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the requester user agent, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
