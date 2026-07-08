namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Response model for event deletion operations.
/// </summary>
public class DeleteEventResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the deletion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of registrations that were cancelled as part of the deletion.
    /// </summary>
    public int CancelledRegistrationsCount { get; set; }

    /// <summary>
    /// Gets or sets a message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
