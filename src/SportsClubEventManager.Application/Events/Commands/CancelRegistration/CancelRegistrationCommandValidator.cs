using FluentValidation;

namespace SportsClubEventManager.Application.Events.Commands.CancelRegistration;

/// <summary>
/// Validator for CancelRegistrationCommand to ensure the identifiers are valid.
/// </summary>
public sealed class CancelRegistrationCommandValidator : AbstractValidator<CancelRegistrationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelRegistrationCommandValidator"/> class.
    /// </summary>
    public CancelRegistrationCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event identifier must not be empty.")
            .WithErrorCode("InvalidEventId");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User identifier must not be empty.")
            .WithErrorCode("InvalidUserId");
    }
}
