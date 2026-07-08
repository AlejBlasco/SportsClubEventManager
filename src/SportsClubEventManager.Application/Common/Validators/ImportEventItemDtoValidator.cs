using FluentValidation;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Common.Validators;

/// <summary>
/// Validates a single <see cref="ImportEventItemDto"/> against the same field-level rules
/// as <c>CreateEventCommandValidator</c> (via <see cref="EventFieldRules"/>). Instantiated
/// and invoked directly by <c>BulkCreateEventsCommandHandler</c> for each row so validation
/// failures can be reported per row instead of short-circuiting the whole request.
/// </summary>
public sealed class ImportEventItemDtoValidator : AbstractValidator<ImportEventItemDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportEventItemDtoValidator"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">Provider for the current UTC time, used for the future-date rule.</param>
    public ImportEventItemDtoValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Title)
            .Must(title => EventFieldRules.ValidateTitle(title) is null)
            .WithMessage(x => EventFieldRules.ValidateTitle(x.Title));

        RuleFor(x => x.Location)
            .Must(location => EventFieldRules.ValidateLocation(location) is null)
            .WithMessage(x => EventFieldRules.ValidateLocation(x.Location));

        RuleFor(x => x.Description)
            .Must(description => EventFieldRules.ValidateDescription(description) is null)
            .WithMessage(x => EventFieldRules.ValidateDescription(x.Description));

        RuleFor(x => x.Date)
            .Must(date => EventFieldRules.ValidateDate(date, dateTimeProvider.UtcNow) is null)
            .WithMessage(x => EventFieldRules.ValidateDate(x.Date, dateTimeProvider.UtcNow));

        RuleFor(x => x.MaxCapacity)
            .Must(maxCapacity => EventFieldRules.ValidateMaxCapacity(maxCapacity) is null)
            .WithMessage(x => EventFieldRules.ValidateMaxCapacity(x.MaxCapacity));
    }
}
