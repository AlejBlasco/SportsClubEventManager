using FluentValidation;

namespace SportsClubEventManager.Application.Events.Commands.RegisterForEvent;

/// <summary>
/// Validator for RegisterForEventCommand to ensure the identifiers are valid.
/// </summary>
public sealed class RegisterForEventCommandValidator : AbstractValidator<RegisterForEventCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterForEventCommandValidator"/> class.
    /// </summary>
    public RegisterForEventCommandValidator()
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
