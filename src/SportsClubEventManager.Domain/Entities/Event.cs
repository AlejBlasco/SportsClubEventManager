using SportsClubEventManager.Domain.Common;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;

namespace SportsClubEventManager.Domain.Entities;

/// <summary>
/// Represents an event in the system.
/// </summary>
public class Event : BaseEntity
{
    private int _maxCapacity;

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
    /// Must be greater than zero.
    /// </summary>
    public int MaxCapacity
    {
        get => _maxCapacity;
        set
        {
            ValidateCapacity(value);
            _maxCapacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the collection of registrations associated with this event.
    /// </summary>
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    /// <summary>
    /// Gets the current number of active registrations for this event.
    /// Only counts registrations that are not cancelled.
    /// </summary>
    public int CurrentRegistrations => Registrations.Count(r => r.Status != RegistrationStatus.Cancelled);

    /// <summary>
    /// Gets a value indicating whether the event has reached its maximum capacity.
    /// </summary>
    public bool IsFull => CurrentRegistrations >= MaxCapacity;

    /// <summary>
    /// Determines whether the event can accept a new registration.
    /// </summary>
    /// <returns><c>true</c> if the event can accept a new registration; otherwise, <c>false</c>.</returns>
    public bool CanAcceptRegistration()
    {
        return !IsFull;
    }

    /// <summary>
    /// Validates that the capacity is greater than zero.
    /// </summary>
    /// <param name="capacity">The capacity to validate.</param>
    /// <exception cref="DomainException">Thrown when the capacity is less than or equal to zero.</exception>
    private void ValidateCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            throw new DomainException("Event capacity must be greater than zero.");
        }
    }

    /// <summary>
    /// Validates that the event date is in the future.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the event date is in the past.</exception>
    public void ValidateFutureDate()
    {
        if (Date < DateTime.UtcNow)
        {
            throw new DomainException("Event date must be in the future.");
        }
    }
}
