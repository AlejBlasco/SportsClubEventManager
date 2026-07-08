using FluentValidation;

namespace SportsClubEventManager.Application.Events.Commands.CreateEvent;

/// <summary>
/// Validator for the CreateEventCommand to ensure all fields meet business rules.
/// </summary>
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventCommandValidator"/> class.
    /// </summary>
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Event title is required.")
            .MaximumLength(200)
            .WithMessage("Event title cannot exceed 200 characters.");

        RuleFor(x => x.Date)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Event date must be in the future.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Event location is required.")
            .MaximumLength(300)
            .WithMessage("Event location cannot exceed 300 characters.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0)
            .WithMessage("Event capacity must be greater than zero.")
            .LessThanOrEqualTo(10000)
            .WithMessage("Event capacity cannot exceed 10,000.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Event description cannot exceed 2,000 characters.");
    }
}
