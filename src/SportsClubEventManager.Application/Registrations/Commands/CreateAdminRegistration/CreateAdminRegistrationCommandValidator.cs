using FluentValidation;

namespace SportsClubEventManager.Application.Registrations.Commands.CreateAdminRegistration;

/// <summary>
/// Validator for <see cref="CreateAdminRegistrationCommand"/>.
/// </summary>
public sealed class CreateAdminRegistrationCommandValidator : AbstractValidator<CreateAdminRegistrationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAdminRegistrationCommandValidator"/> class.
    /// </summary>
    public CreateAdminRegistrationCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");
    }
}
