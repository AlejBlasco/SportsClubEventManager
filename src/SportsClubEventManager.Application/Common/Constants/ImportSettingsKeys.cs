namespace SportsClubEventManager.Application.Common.Constants;

/// <summary>
/// Configuration keys and default values for the CSV event import feature
/// ("ImportSettings" section in appsettings.json), shared by the Application and
/// Infrastructure layers so the two do not drift out of sync.
/// </summary>
public static class ImportSettingsKeys
{
    /// <summary>
    /// Configuration key for the default <c>Event.MaxCapacity</c> applied to every imported
    /// row, since the standardized CSV schema has no capacity column.
    /// </summary>
    public const string DefaultMaxCapacity = "ImportSettings:DefaultMaxCapacity";

    /// <summary>
    /// Default value used when <see cref="DefaultMaxCapacity"/> is not configured.
    /// </summary>
    public const int DefaultMaxCapacityFallback = 30;

    /// <summary>
    /// Configuration key for the default event time (format "HH:mm") applied when the CSV
    /// "HORA" column is blank.
    /// </summary>
    public const string DefaultEventTime = "ImportSettings:DefaultEventTime";

    /// <summary>
    /// Default value used when <see cref="DefaultEventTime"/> is not configured.
    /// </summary>
    public const string DefaultEventTimeFallback = "09:00";

    /// <summary>
    /// Configuration key for the maximum accepted upload size, in bytes, for the preview endpoint.
    /// </summary>
    public const string MaxFileSizeBytes = "ImportSettings:MaxFileSizeBytes";

    /// <summary>
    /// Default value used when <see cref="MaxFileSizeBytes"/> is not configured (5 MB).
    /// </summary>
    public const long MaxFileSizeBytesFallback = 5 * 1024 * 1024;

    /// <summary>
    /// Configuration key for the maximum number of data rows accepted in a single import file.
    /// </summary>
    public const string MaxRowCount = "ImportSettings:MaxRowCount";

    /// <summary>
    /// Default value used when <see cref="MaxRowCount"/> is not configured.
    /// </summary>
    public const int MaxRowCountFallback = 5000;

    /// <summary>
    /// Configuration key for whether imported event titles are normalized to title case
    /// (via <see cref="System.Globalization.CultureInfo.InvariantCulture"/>'s <c>TextInfo.ToTitleCase</c>)
    /// before validation and persistence.
    /// </summary>
    public const string NormalizeTitleCapitalization = "ImportSettings:NormalizeTitleCapitalization";

    /// <summary>
    /// Default value used when <see cref="NormalizeTitleCapitalization"/> is not configured.
    /// </summary>
    public const bool NormalizeTitleCapitalizationFallback = true;
}
