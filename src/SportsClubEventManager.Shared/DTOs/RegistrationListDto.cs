using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object representing a registration in user and administrator views.
/// </summary>
public sealed class RegistrationListDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the registration.
    /// </summary>
    public Guid RegistrationId { get; set; }

    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the event title.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event date in UTC.
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Gets or sets the event location.
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user email.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the registration was created.
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// Gets or sets the registration status.
    /// </summary>
    public RegistrationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the registration can be cancelled by a regular user.
    /// </summary>
    public bool CanBeCancelledByUser { get; set; }
}
