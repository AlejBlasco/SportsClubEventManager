using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for CSV event import operations, communicating with the API.
/// </summary>
public class ImportManagementService : IImportManagementService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportManagementService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API communication.</param>
    public ImportManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Downloads the standardized CSV import template as UTF-8 text.
    /// </summary>
    /// <returns>The template file content.</returns>
    public async Task<string> DownloadTemplateAsync()
    {
        var response = await _httpClient.GetAsync("api/admin/import/template");
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetString(bytes);
    }

    /// <summary>
    /// Uploads a CSV file for a dry-run preview: parses and maps the rows without writing to the database.
    /// </summary>
    /// <param name="fileContent">The raw uploaded file content.</param>
    /// <param name="fileName">The uploaded file name.</param>
    /// <param name="columnMapping">Optional column remapping entries, used when headers do not match exactly.</param>
    /// <param name="defaultMaxCapacity">Optional override for the default maximum capacity applied to every row.</param>
    /// <returns>The preview response, including per-row validation results.</returns>
    public async Task<CsvImportPreviewResponse> PreviewCsvAsync(
        byte[] fileContent,
        string fileName,
        IReadOnlyList<ImportColumnMappingDto>? columnMapping = null,
        int? defaultMaxCapacity = null)
    {
        using var content = new MultipartFormDataContent();
        using var fileStreamContent = new ByteArrayContent(fileContent);
        fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileStreamContent, "file", fileName);

        if (columnMapping is { Count: > 0 })
        {
            content.Add(new StringContent(JsonSerializer.Serialize(columnMapping)), "columnMapping");
        }

        if (defaultMaxCapacity.HasValue)
        {
            content.Add(new StringContent(defaultMaxCapacity.Value.ToString()), "defaultMaxCapacity");
        }

        var response = await _httpClient.PostAsync("api/admin/import/csv/preview", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CsvImportPreviewResponse>();
        return result ?? throw new InvalidOperationException("Failed to preview the CSV import file.");
    }

    /// <summary>
    /// Confirms a CSV import, inserting all admin-approved rows in a single, all-or-nothing transaction.
    /// </summary>
    /// <param name="events">The admin-approved, already-mapped event rows.</param>
    /// <returns>The import result.</returns>
    public async Task<CsvImportResultDto> ConfirmImportAsync(IReadOnlyList<ImportEventItemDto> events)
    {
        var request = new ConfirmCsvImportRequest { Events = events };
        var response = await _httpClient.PostAsJsonAsync("api/admin/import/csv", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>();
        return result ?? throw new InvalidOperationException("Failed to confirm the CSV import.");
    }
}
