namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Default implementation of IGuidProvider that generates random Guids.
/// </summary>
public sealed class GuidProvider : IGuidProvider
{
    /// <summary>
    /// Generates a new globally unique identifier using Guid.NewGuid().
    /// </summary>
    /// <returns>A new random Guid value.</returns>
    public Guid NewGuid() => Guid.NewGuid();
}
