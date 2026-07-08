using System.ComponentModel.DataAnnotations;

namespace SportsClubEventManager.Web.Models;

/// <summary>
/// Model for the event registration confirmation form.
/// Name and email are prefilled read-only from the authenticated user's account.
/// </summary>
public sealed class RegistrationFormModel
{
    /// <summary>
    /// Gets or sets the user's name.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(200, ErrorMessage = "Email must not exceed 200 characters")]
    public string Email { get; set; } = string.Empty;
}
