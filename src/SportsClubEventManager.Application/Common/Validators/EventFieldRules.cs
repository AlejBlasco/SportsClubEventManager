namespace SportsClubEventManager.Application.Common.Validators;

/// <summary>
/// Shared field-level validation rules for event data, mirroring the constraints defined by
/// <c>EventConfiguration</c> (the actual EF Core/database constraints) and
/// <c>CreateEventCommandValidator</c>. Used by the CSV import preview and confirm handlers so
/// both stages apply exactly the same rules against nullable (not-yet-fully-parsed) and
/// non-nullable (already-mapped) field values respectively.
/// </summary>
public static class EventFieldRules
{
    /// <summary>
    /// The maximum allowed length for <c>Event.Title</c>, per <c>EventConfiguration</c>.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    /// The maximum allowed length for <c>Event.Location</c>, per <c>EventConfiguration</c>.
    /// </summary>
    public const int LocationMaxLength = 500;

    /// <summary>
    /// The maximum allowed length for <c>Event.Description</c>, per <c>EventConfiguration</c>.
    /// This also bounds the composite description built from "MODAL."/"CAMPO"/"CAT".
    /// </summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>
    /// Validates the event title.
    /// </summary>
    /// <param name="title">The title to validate.</param>
    /// <returns>An error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Event title is required.";
        }

        return title.Length > TitleMaxLength
            ? $"Event title cannot exceed {TitleMaxLength} characters."
            : null;
    }

    /// <summary>
    /// Validates the event location.
    /// </summary>
    /// <param name="location">The location to validate.</param>
    /// <returns>An error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Event location is required.";
        }

        return location.Length > LocationMaxLength
            ? $"Event location cannot exceed {LocationMaxLength} characters."
            : null;
    }

    /// <summary>
    /// Validates the (optional) event description.
    /// </summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>An error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        return description.Length > DescriptionMaxLength
            ? $"Event description cannot exceed {DescriptionMaxLength} characters."
            : null;
    }

    /// <summary>
    /// Validates the event date.
    /// </summary>
    /// <param name="date">The date to validate.</param>
    /// <param name="utcNow">The current UTC time to compare against.</param>
    /// <returns>An error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateDate(DateTime? date, DateTime utcNow)
    {
        if (date is null)
        {
            return "Event date is required.";
        }

        return date < utcNow
            ? "Event date must be in the future."
            : null;
    }

    /// <summary>
    /// Validates the event maximum capacity.
    /// </summary>
    /// <param name="maxCapacity">The maximum capacity to validate.</param>
    /// <returns>An error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateMaxCapacity(int? maxCapacity)
    {
        if (maxCapacity is null)
        {
            return "Event maximum capacity is required.";
        }

        return maxCapacity <= 0
            ? "Event maximum capacity must be greater than zero."
            : null;
    }
}
