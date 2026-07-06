using FluentValidation;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Validator for UpdateProfileCommand.
/// </summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProfileCommandValidator"/> class.
    /// </summary>
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters long.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s\-']+$")
            .WithMessage("Name can only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .WithMessage("Gender is required.")
            .Must(BeValidGender)
            .WithMessage("Gender must be Male, Female, or Other.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("Email address must be in a valid format.")
            .Must(BeValidEmailFormat)
            .WithMessage("Email address must be in a valid format.")
            .MaximumLength(256)
            .WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.LicenseNumber)
            .MaximumLength(50)
            .WithMessage("License number must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.LicenseNumber));

        RuleFor(x => x.LicenseCategory)
            .MaximumLength(50)
            .WithMessage("License category must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.LicenseCategory));
    }

    private bool BeValidGender(string gender)
    {
        return Enum.TryParse<Gender>(gender, ignoreCase: true, out _);
    }

    private bool BeValidEmailFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex != email.LastIndexOf('@') || atIndex == email.Length - 1)
        {
            return false;
        }

        var domainPart = email[(atIndex + 1)..];
        return domainPart.Contains('.')
            && !domainPart.StartsWith('.')
            && !domainPart.EndsWith('.');
    }
}
