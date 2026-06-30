using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Infrastructure.Common;

/// <summary>
/// System implementation of IDateTimeProvider that returns the actual current UTC time.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;
}
