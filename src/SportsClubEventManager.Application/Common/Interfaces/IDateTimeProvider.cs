namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Provides the current date and time in UTC.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }
}
