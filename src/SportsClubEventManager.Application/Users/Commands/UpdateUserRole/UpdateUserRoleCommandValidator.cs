using FluentValidation;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserRole;

/// <summary>
/// Validator for the UpdateUserRoleCommand to ensure required fields are provided.
/// </summary>
public class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRoleCommandValidator"/> class.
    /// </summary>
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.NewRole)
            .IsInEnum()
            .WithMessage("Role must be a valid value.");
    }
}
