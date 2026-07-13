namespace SportsClubEventManager.Application.Common.Models.Notifications;

/// <summary>
/// A single notification recipient (an actively registered user).
/// </summary>
public sealed record NotificationRecipient
{
    /// <summary>
    /// Gets the recipient's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the recipient's display name.
    /// </summary>
    public required string Name { get; init; }
}
