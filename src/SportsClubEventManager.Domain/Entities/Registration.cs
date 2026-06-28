using SportsClubEventManager.Domain.Common;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Domain.Entities;

/// <summary>
/// Represents a user's registration for an event.
/// </summary>
public class Registration : BaseEntity
{
    /// <summary>
    /// Gets or sets the identifier of the event this registration is for.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who registered.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the registration was created.
    /// </summary>
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the status of the registration.
    /// </summary>
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;

    /// <summary>
    /// Gets or sets the event this registration is associated with.
    /// </summary>
    public Event Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who created this registration.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Cancels this registration by setting its status to Cancelled.
    /// </summary>
    public void Cancel()
    {
        Status = RegistrationStatus.Cancelled;
    }

    /// <summary>
    /// Determines whether this registration is currently active.
    /// </summary>
    /// <returns><c>true</c> if the registration is not cancelled; otherwise, <c>false</c>.</returns>
    public bool IsActive()
    {
        return Status != RegistrationStatus.Cancelled;
    }
}
