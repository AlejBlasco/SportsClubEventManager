using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Components.Pages.Admin;

/// <summary>
/// Code-behind for the CSV Event Import page. Implements the stateless, two-step
/// preview/confirm flow: the parsed rows are held client-side (in this component) between
/// the preview and confirm server calls.
/// </summary>
public partial class ImportEvents
{
    /// <summary>
    /// The standardized CSV column names, in display order.
    /// </summary>
    private static readonly string[] CanonicalColumns =
    [
        "DÍA", "MODAL.", "NOMBRE TIRADA", "HORA", "CAMPO", "LUGAR", "CAT"
    ];

    /// <summary>
    /// Maximum file size accepted by the browser-side file reader (10 MB), matching the
    /// generous Kestrel-level ceiling; the API applies the authoritative, configurable limit.
    /// </summary>
    private const long MaxBrowserReadSizeBytes = 10 * 1024 * 1024;

    private byte[]? _selectedFileContent;
    private string? _selectedFileName;
    private int? _defaultMaxCapacity;

    private CsvImportPreviewResponse? _preview;
    private readonly Dictionary<int, bool> _rowSelections = [];
    private readonly Dictionary<int, int> _rowCapacityOverrides = [];
    private readonly Dictionary<string, string> _columnMappingSelections = [];

    private CsvImportResultDto? _importResult;

    private bool _loading;
    private bool _confirming;
    private string? _errorMessage;
    private string? _successMessage;

    /// <summary>
    /// Handles selection of a CSV file from the browser file picker, buffering its content for upload.
    /// </summary>
    /// <param name="e">The file selection event arguments.</param>
    private async Task OnFileSelectedAsync(InputFileChangeEventArgs e)
    {
        _errorMessage = null;
        _successMessage = null;
        _preview = null;
        _importResult = null;
        _rowSelections.Clear();
        _rowCapacityOverrides.Clear();

        try
        {
            using var stream = e.File.OpenReadStream(MaxBrowserReadSizeBytes);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            _selectedFileContent = memoryStream.ToArray();
            _selectedFileName = e.File.Name;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to read the selected file: {ex.Message}";
            _selectedFileContent = null;
            _selectedFileName = null;
        }
    }

    /// <summary>
    /// Downloads the standardized CSV import template to the browser.
    /// </summary>
    private async Task DownloadTemplateAsync()
    {
        _errorMessage = null;

        try
        {
            var content = await ImportManagementService.DownloadTemplateAsync();
            await JSRuntime.InvokeVoidAsync(
                "downloadFileFromText",
                "event-import-template.csv",
                content,
                "text/csv;charset=utf-8");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to download the template: {ex.Message}";
        }
    }

    /// <summary>
    /// Uploads the selected file for a dry-run preview, applying any active column remapping.
    /// </summary>
    private async Task PreviewAsync()
    {
        if (_selectedFileContent is null || _selectedFileName is null)
        {
            _errorMessage = "Select a CSV file before requesting a preview.";
            return;
        }

        _errorMessage = null;
        _successMessage = null;
        _importResult = null;
        _loading = true;

        try
        {
            var mapping = BuildColumnMappingRequest();

            _preview = await ImportManagementService.PreviewCsvAsync(
                _selectedFileContent,
                _selectedFileName,
                mapping,
                _defaultMaxCapacity);

            InitializeRowState();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to preview the file: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Confirms the import, sending only the selected, valid rows with their (possibly edited) capacity.
    /// </summary>
    private async Task ConfirmImportAsync()
    {
        if (_preview is null)
        {
            return;
        }

        var eventsToImport = _preview.Rows
            .Where(r => r.IsValid && _rowSelections.GetValueOrDefault(r.RowNumber))
            .Select(ToImportEventItem)
            .ToList();

        if (eventsToImport.Count == 0)
        {
            _errorMessage = "Select at least one valid row to import.";
            return;
        }

        _errorMessage = null;
        _successMessage = null;
        _confirming = true;

        try
        {
            _importResult = await ImportManagementService.ConfirmImportAsync(eventsToImport);

            if (_importResult.FailedCount == 0)
            {
                _successMessage = $"{_importResult.ImportedCount} event(s) imported successfully.";
                _preview = null;
                _selectedFileContent = null;
                _selectedFileName = null;
                _rowSelections.Clear();
                _rowCapacityOverrides.Clear();
            }
            else
            {
                _errorMessage = $"Import rejected: {_importResult.FailedCount} row(s) failed re-validation. No events were imported.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to confirm the import: {ex.Message}";
        }
        finally
        {
            _confirming = false;
        }
    }

    /// <summary>
    /// Converts a validated preview row (with any admin-edited capacity) into the confirm-step DTO.
    /// </summary>
    /// <param name="row">The preview row to convert.</param>
    /// <returns>The event item ready for the confirm-import request.</returns>
    private ImportEventItemDto ToImportEventItem(CsvImportRowDto row) => new()
    {
        Title = row.Title,
        Date = row.Date ?? default,
        Description = row.Description,
        Location = row.Location,
        MaxCapacity = _rowCapacityOverrides.GetValueOrDefault(row.RowNumber, row.MaxCapacity ?? 0)
    };

    /// <summary>
    /// Initializes per-row selection (valid rows selected by default) and capacity override state.
    /// </summary>
    private void InitializeRowState()
    {
        _rowSelections.Clear();
        _rowCapacityOverrides.Clear();

        if (_preview is null)
        {
            return;
        }

        foreach (var row in _preview.Rows)
        {
            _rowSelections[row.RowNumber] = row.IsValid;

            if (row.MaxCapacity.HasValue)
            {
                _rowCapacityOverrides[row.RowNumber] = row.MaxCapacity.Value;
            }
        }
    }

    /// <summary>
    /// Toggles whether a row is selected for import.
    /// </summary>
    /// <param name="rowNumber">The row number to toggle.</param>
    /// <param name="isSelected">The new selection state.</param>
    private void SetRowSelected(int rowNumber, bool isSelected)
    {
        _rowSelections[rowNumber] = isSelected;
    }

    /// <summary>
    /// Updates the maximum capacity override for a row.
    /// </summary>
    /// <param name="rowNumber">The row number to update.</param>
    /// <param name="value">The new maximum capacity value.</param>
    private void SetRowCapacity(int rowNumber, int value)
    {
        _rowCapacityOverrides[rowNumber] = value;
    }

    /// <summary>
    /// Updates the column remapping selection for a standardized column.
    /// </summary>
    /// <param name="canonicalColumn">The standardized column name being remapped.</param>
    /// <param name="detectedHeader">The detected source header to map it to.</param>
    private void SetColumnMapping(string canonicalColumn, string detectedHeader)
    {
        if (string.IsNullOrWhiteSpace(detectedHeader))
        {
            _columnMappingSelections.Remove(canonicalColumn);
        }
        else
        {
            _columnMappingSelections[canonicalColumn] = detectedHeader;
        }
    }

    /// <summary>
    /// Builds the column mapping payload from the current remap selections, if any are active.
    /// </summary>
    /// <returns>The mapping entries to send with the preview request, or <see langword="null"/> when no remapping is active.</returns>
    private List<ImportColumnMappingDto>? BuildColumnMappingRequest()
    {
        if (_columnMappingSelections.Count == 0)
        {
            return null;
        }

        return _columnMappingSelections
            .Select(kvp => new ImportColumnMappingDto { SourceColumn = kvp.Value, TargetColumn = kvp.Key })
            .ToList();
    }
}
