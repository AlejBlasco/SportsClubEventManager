using FluentValidation;

namespace SportsClubEventManager.Application.Users.Commands.DeleteUser;

/// <summary>
/// Validator for the DeleteUserCommand to ensure required fields are provided.
/// </summary>
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUserCommandValidator"/> class.
    /// </summary>
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
