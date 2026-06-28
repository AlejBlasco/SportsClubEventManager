namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Provides abstraction over Guid generation to support testability.
/// </summary>
public interface IGuidProvider
{
    /// <summary>
    /// Generates a new globally unique identifier.
    /// </summary>
    /// <returns>A new Guid value.</returns>
    Guid NewGuid();
}
