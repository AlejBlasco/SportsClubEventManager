namespace SportsClubEventManager.Infrastructure.Import;

/// <summary>
/// Composes the "MODAL." (modality), "CAMPO" (field/range) and "CAT" (category) CSV columns
/// into a single formatted <c>Event.Description</c> string, since the <c>Event</c> entity has
/// no first-class fields for these attributes. Blank segments are omitted.
/// </summary>
internal static class EventDescriptionComposer
{
    /// <summary>
    /// Composes the formatted description string, e.g. "Modality: Trap | Field: Campo 2 | Category: S1".
    /// </summary>
    /// <param name="modality">The raw "MODAL." column value.</param>
    /// <param name="field">The raw "CAMPO" column value.</param>
    /// <param name="category">The raw "CAT" column value.</param>
    /// <returns>The composed description, or <see langword="null"/> when all segments are blank.</returns>
    public static string? Compose(string? modality, string? field, string? category)
    {
        var segments = new List<string>();

        if (!string.IsNullOrWhiteSpace(modality))
        {
            segments.Add($"Modality: {modality.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(field))
        {
            segments.Add($"Field: {field.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            segments.Add($"Category: {category.Trim()}");
        }

        return segments.Count == 0 ? null : string.Join(" | ", segments);
    }
}
