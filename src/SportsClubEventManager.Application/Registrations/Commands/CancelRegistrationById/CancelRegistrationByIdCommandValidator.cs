using FluentValidation;

namespace SportsClubEventManager.Application.Registrations.Commands.CancelRegistrationById;

/// <summary>
/// Validator for <see cref="CancelRegistrationByIdCommand"/>.
/// </summary>
public sealed class CancelRegistrationByIdCommandValidator : AbstractValidator<CancelRegistrationByIdCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelRegistrationByIdCommandValidator"/> class.
    /// </summary>
    public CancelRegistrationByIdCommandValidator()
    {
        RuleFor(x => x.RegistrationId)
            .NotEmpty()
            .WithMessage("Registration ID is required.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID is required.");
    }
}
