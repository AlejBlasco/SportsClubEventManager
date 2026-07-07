using MediatR;

namespace SportsClubEventManager.Application.Events.Commands.CreateEvent;

/// <summary>
/// Command to create a new event.
/// </summary>
public class CreateEventCommand : IRequest<Guid>
{
    /// <summary>
    /// Gets or sets the ID of the administrator creating the event.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the title of the event.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the event takes place.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the location where the event takes place.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum capacity of the event.
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
