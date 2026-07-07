namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request model for changing a user's account status (active/inactive).
/// </summary>
public class UpdateUserStatusRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the user account should be active.
    /// </summary>
    public bool IsActive { get; set; }
}
