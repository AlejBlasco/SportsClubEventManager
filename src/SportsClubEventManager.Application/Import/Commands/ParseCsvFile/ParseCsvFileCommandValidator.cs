using FluentValidation;

namespace SportsClubEventManager.Application.Import.Commands.ParseCsvFile;

/// <summary>
/// Validates the top-level shape of a <see cref="ParseCsvFileCommand"/>. Row-level field
/// validation (Title/Location/Date/MaxCapacity) is performed separately by the handler for
/// each parsed row, so failures can be reported per row instead of aborting the whole request.
/// </summary>
public sealed class ParseCsvFileCommandValidator : AbstractValidator<ParseCsvFileCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseCsvFileCommandValidator"/> class.
    /// </summary>
    public ParseCsvFileCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("The uploaded file name is required.")
            .Must(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .csv files are supported.");

        RuleFor(x => x.DefaultMaxCapacity)
            .GreaterThan(0)
            .When(x => x.DefaultMaxCapacity.HasValue)
            .WithMessage("Default maximum capacity must be greater than zero.");
    }
}
