using FluentValidation;

namespace SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;

/// <summary>
/// Validates the top-level shape of a <see cref="BulkCreateEventsCommand"/>. Per-row field
/// validation (Title/Location/Date/MaxCapacity) is re-applied separately by the handler for
/// each event, so a single invalid row can be reported without a generic pipeline failure.
/// </summary>
public sealed class BulkCreateEventsCommandValidator : AbstractValidator<BulkCreateEventsCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkCreateEventsCommandValidator"/> class.
    /// </summary>
    public BulkCreateEventsCommandValidator()
    {
        RuleFor(x => x.AdminUserId)
            .NotEmpty()
            .WithMessage("Administrator user ID is required.");

        RuleFor(x => x.Events)
            .NotEmpty()
            .WithMessage("At least one event row is required to confirm an import.");
    }
}
