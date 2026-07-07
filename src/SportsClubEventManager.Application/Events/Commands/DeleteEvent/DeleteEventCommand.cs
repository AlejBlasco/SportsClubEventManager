using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Commands.DeleteEvent;

/// <summary>
/// Command to delete an event and cancel all associated registrations.
/// </summary>
public class DeleteEventCommand : IRequest<DeleteEventResponse>
{
    /// <summary>
    /// Gets or sets the ID of the administrator deleting the event.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the event to delete.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
