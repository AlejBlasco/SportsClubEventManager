using FluentValidation;

namespace SportsClubEventManager.Application.Events.Commands.DeleteEvent;

/// <summary>
/// Validator for the DeleteEventCommand to ensure all fields meet business rules.
/// </summary>
public class DeleteEventCommandValidator : AbstractValidator<DeleteEventCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEventCommandValidator"/> class.
    /// </summary>
    public DeleteEventCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");
    }
}
