using FluentValidation;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserAsAdmin;

/// <summary>
/// Validator for the UpdateUserAsAdminCommand to ensure all fields meet business rules.
/// </summary>
public class UpdateUserAsAdminCommandValidator : AbstractValidator<UpdateUserAsAdminCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserAsAdminCommandValidator"/> class.
    /// </summary>
    public UpdateUserAsAdminCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be in a valid format.")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Gender)
            .IsInEnum()
            .WithMessage("Gender must be a valid value.");
    }
}
