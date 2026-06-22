namespace SportsClubEventManager.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides a common identifier and audit trail foundation.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
