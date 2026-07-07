using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Application.Events.Commands.UpdateEvent;

/// <summary>
/// Validator for the UpdateEventCommand to ensure all fields meet business rules.
/// </summary>
public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEventCommandValidator"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public UpdateEventCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");

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

        // OQ-6: Capacity cannot be less than current registrations
        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
            {
                if (command.EventId == Guid.Empty)
                {
                    return;
                }

                // Single database query to fetch current registration count
                var currentRegistrations = await _context.Events
                    .Where(e => e.Id == command.EventId)
                    .Select(e => e.Registrations.Count(r => r.Status != Domain.Enums.RegistrationStatus.Cancelled))
                    .FirstOrDefaultAsync(cancellationToken);

                if (command.MaxCapacity < currentRegistrations)
                {
                    context.AddFailure(
                        nameof(command.MaxCapacity),
                        $"Event capacity cannot be less than current registrations ({currentRegistrations}). Please cancel some registrations first.");
                }
            });
    }
}
