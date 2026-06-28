namespace SportsClubEventManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when an attempt is made to register for an event that has reached its maximum capacity.
/// </summary>
public class CapacityExceededException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CapacityExceededException"/> class.
    /// </summary>
    public CapacityExceededException() : base("The event has reached its maximum capacity.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapacityExceededException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public CapacityExceededException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CapacityExceededException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CapacityExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
