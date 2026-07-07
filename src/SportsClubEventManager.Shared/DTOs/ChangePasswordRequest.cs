namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request model for changing user password.
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// Gets the current password for verification.
    /// </summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new password to set.
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the confirmation of the new password.
    /// </summary>
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
