using System.Text.RegularExpressions;
using SportsClubEventManager.Domain.Common;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : BaseEntity
{
    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    private static readonly Regex EmailRegex = new(EmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string _email = string.Empty;

    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gender of the user.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// Must be unique globally and follow a valid email format.
    /// </summary>
    public string Email
    {
        get => _email;
        set
        {
            ValidateEmail(value);
            _email = value;
        }
    }

    /// <summary>
    /// Gets or sets the license number of the user.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Gets or sets the license category of the user.
    /// </summary>
    public string? LicenseCategory { get; set; }

    /// <summary>
    /// Gets or sets the collection of registrations associated with this user.
    /// </summary>
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    /// <summary>
    /// Validates that the provided email address follows a valid format.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <exception cref="Exceptions.DomainException">Thrown when the email format is invalid.</exception>
    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exceptions.DomainException("Email address is required.");
        }

        if (!EmailRegex.IsMatch(email))
        {
            throw new Exceptions.DomainException($"Email address '{email}' is not in a valid format.");
        }
    }
}
