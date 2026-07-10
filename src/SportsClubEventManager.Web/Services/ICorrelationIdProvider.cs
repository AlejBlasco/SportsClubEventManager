namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Provides the correlation id for the current Blazor Server circuit.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the correlation id assigned to the current circuit.
    /// </summary>
    string CorrelationId { get; }
}
