using Bunit;
using Bunit.JSInterop;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Pages.Admin;
using SportsClubEventManager.Web.Services;
using Xunit;

namespace SportsClubEventManager.Web.Tests.Components.Pages.Admin;

/// <summary>
/// Tests for the <c>ImportEvents</c> page, focused on the duplicate-row indicator introduced
/// alongside server-side duplicate detection: rows flagged <c>IsDuplicate</c> must render a
/// distinct "Duplicate" badge instead of the generic error badge, and must not be preselected
/// for import (the same rule already applied to any other invalid row).
/// </summary>
public sealed class ImportEventsPageTests : TestContext
{
    private readonly IImportManagementService _importManagementService;

    /// <summary>
    /// Initializes the test with a mocked import management service registered into bUnit's
    /// service collection, as required by the page's <c>@inject</c> declaration.
    /// </summary>
    public ImportEventsPageTests()
    {
        _importManagementService = Substitute.For<IImportManagementService>();
        Services.AddSingleton(_importManagementService);

        // The page calls into IJSRuntime only for the template download (a browser file-save
        // trick unrelated to this page's validation/duplicate logic); Loose mode lets any such
        // call resolve without requiring an explicit per-argument setup for every test.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies that a row flagged as a duplicate by the backend renders the distinct
    /// "Duplicate" warning badge rather than the generic "N error(s)" danger badge.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenRowIsDuplicate_DisplaysDuplicateBadge()
    {
        // Arrange
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(CreatePreviewResponse());
        var cut = RenderComponent<ImportEvents>();

        // Act
        await UploadCsvAndPreviewAsync(cut);

        // Assert
        var duplicateBadge = cut.FindAll(".badge.bg-warning").Should().ContainSingle().Subject;
        duplicateBadge.TextContent.Should().Contain("Duplicate");

        var duplicateRowStatusCell = cut.FindAll("tbody tr")[1].QuerySelector("td:last-child");
        duplicateRowStatusCell!.QuerySelector(".bg-danger").Should().BeNull();
    }

    /// <summary>
    /// Verifies that a duplicate row's selection checkbox is not preselected for import and is
    /// disabled, matching the rule already applied to any other row that fails validation.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenRowIsDuplicate_CheckboxIsNotPreselectedAndDisabled()
    {
        // Arrange
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(CreatePreviewResponse());
        var cut = RenderComponent<ImportEvents>();

        // Act
        await UploadCsvAndPreviewAsync(cut);

        // Assert
        var checkboxes = cut.FindAll("tbody tr td input[type=checkbox]");
        var duplicateRowCheckbox = checkboxes[1];
        duplicateRowCheckbox.HasAttribute("checked").Should().BeFalse();
        duplicateRowCheckbox.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a valid, non-duplicate row is still preselected for import (its checkbox
    /// checked and enabled), so the new duplicate handling does not regress existing behavior.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenRowIsValidAndNotDuplicate_CheckboxIsPreselectedAndEnabled()
    {
        // Arrange
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(CreatePreviewResponse());
        var cut = RenderComponent<ImportEvents>();

        // Act
        await UploadCsvAndPreviewAsync(cut);

        // Assert
        var checkboxes = cut.FindAll("tbody tr td input[type=checkbox]");
        var validRowCheckbox = checkboxes[0];
        validRowCheckbox.HasAttribute("checked").Should().BeTrue();
        validRowCheckbox.HasAttribute("disabled").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a row that is invalid for a reason other than duplication (a genuine
    /// field-validation error) still renders the original "N error(s)" danger badge, confirming
    /// the new duplicate-badge branch does not swallow the pre-existing error-badge branch.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenRowIsInvalidButNotDuplicate_DisplaysGenericErrorBadge()
    {
        // Arrange
        var preview = CreatePreviewResponse();
        preview.Rows[2].IsDuplicate.Should().BeFalse();
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(preview);
        var cut = RenderComponent<ImportEvents>();

        // Act
        await UploadCsvAndPreviewAsync(cut);

        // Assert
        var errorBadge = cut.FindAll(".badge.bg-danger").Should().ContainSingle().Subject;
        errorBadge.TextContent.Should().Contain("error");
    }

    /// <summary>
    /// Verifies that confirming an import that succeeds displays a success message and clears the
    /// preview grid, and that the admin-approved row (built from the preview's normalized title)
    /// is the one sent to the confirm-import service call.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenConfirmImportSucceeds_DisplaysSuccessMessageAndClearsPreview()
    {
        // Arrange
        var singleValidRowPreview = new CsvImportPreviewResponse
        {
            TotalRows = 1,
            ValidRowCount = 1,
            InvalidRowCount = 0,
            Rows = [new CsvImportRowDto { RowNumber = 1, Title = "Trap Shooting", Date = DateTime.UtcNow.AddDays(30), Location = "Range 1", MaxCapacity = 30, IsValid = true, IsDuplicate = false, Errors = [] }]
        };
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(singleValidRowPreview);
        _importManagementService
            .ConfirmImportAsync(Arg.Any<IReadOnlyList<ImportEventItemDto>>())
            .Returns(new CsvImportResultDto { ImportedCount = 1, FailedCount = 0, FailedRows = [] });
        var cut = RenderComponent<ImportEvents>();
        await UploadCsvAndPreviewAsync(cut);

        // Act
        var confirmButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Confirm Import"));
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        cut.Find(".alert-success").TextContent.Should().Contain("1 event(s) imported successfully");
        cut.FindAll("table").Should().BeEmpty();
        await _importManagementService.Received(1).ConfirmImportAsync(
            Arg.Is<IReadOnlyList<ImportEventItemDto>>(events => events.Count == 1 && events[0].Title == "Trap Shooting"));
    }

    /// <summary>
    /// Verifies that when the confirm-import call is rejected (re-validation failed server-side),
    /// the rejected rows and their error messages are listed for the admin instead of a success message.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenConfirmImportIsRejected_DisplaysFailedRowsWithErrors()
    {
        // Arrange
        var singleValidRowPreview = new CsvImportPreviewResponse
        {
            TotalRows = 1,
            ValidRowCount = 1,
            InvalidRowCount = 0,
            Rows = [new CsvImportRowDto { RowNumber = 1, Title = "Trap Shooting", Date = DateTime.UtcNow.AddDays(30), Location = "Range 1", MaxCapacity = 30, IsValid = true, IsDuplicate = false, Errors = [] }]
        };
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(singleValidRowPreview);
        _importManagementService
            .ConfirmImportAsync(Arg.Any<IReadOnlyList<ImportEventItemDto>>())
            .Returns(new CsvImportResultDto
            {
                ImportedCount = 0,
                FailedCount = 1,
                FailedRows = [new CsvImportRowDto { RowNumber = 1, Title = "Trap Shooting", IsValid = false, IsDuplicate = true, Errors = ["An event with this title and date already exists"] }]
            });
        var cut = RenderComponent<ImportEvents>();
        await UploadCsvAndPreviewAsync(cut);

        // Act
        var confirmButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Confirm Import"));
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        cut.Find(".alert-warning").TextContent.Should().Contain("Import rejected");
        cut.Find(".alert-warning ul li").TextContent.Should().Contain("An event with this title and date already exists");
    }

    /// <summary>
    /// Verifies that, if the admin deselects the only valid row before confirming, the page
    /// reports the selection error locally and never calls the confirm-import service.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenAdminDeselectsTheOnlyValidRow_ShowsSelectionErrorWithoutCallingService()
    {
        // Arrange
        var singleValidRowPreview = new CsvImportPreviewResponse
        {
            TotalRows = 1,
            ValidRowCount = 1,
            InvalidRowCount = 0,
            Rows = [new CsvImportRowDto { RowNumber = 1, Title = "Trap Shooting", Date = DateTime.UtcNow.AddDays(30), Location = "Range 1", MaxCapacity = 30, IsValid = true, IsDuplicate = false, Errors = [] }]
        };
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(singleValidRowPreview);
        var cut = RenderComponent<ImportEvents>();
        await UploadCsvAndPreviewAsync(cut);

        // Act
        var rowCheckbox = cut.Find("tbody tr td input[type=checkbox]");
        await cut.InvokeAsync(() => rowCheckbox.Change(false));
        var confirmButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Confirm Import"));
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        cut.Find(".alert-danger").TextContent.Should().Contain("Select at least one valid row to import");
        await _importManagementService.DidNotReceive().ConfirmImportAsync(Arg.Any<IReadOnlyList<ImportEventItemDto>>());
    }

    /// <summary>
    /// Verifies that editing a row's Max Capacity input before confirming overrides the value
    /// sent to the confirm-import service, rather than the original previewed capacity.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenAdminEditsRowCapacity_SendsOverriddenCapacityOnConfirm()
    {
        // Arrange
        var singleValidRowPreview = new CsvImportPreviewResponse
        {
            TotalRows = 1,
            ValidRowCount = 1,
            InvalidRowCount = 0,
            Rows = [new CsvImportRowDto { RowNumber = 1, Title = "Trap Shooting", Date = DateTime.UtcNow.AddDays(30), Location = "Range 1", MaxCapacity = 30, IsValid = true, IsDuplicate = false, Errors = [] }]
        };
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(singleValidRowPreview);
        _importManagementService
            .ConfirmImportAsync(Arg.Any<IReadOnlyList<ImportEventItemDto>>())
            .Returns(new CsvImportResultDto { ImportedCount = 1, FailedCount = 0, FailedRows = [] });
        var cut = RenderComponent<ImportEvents>();
        await UploadCsvAndPreviewAsync(cut);

        // Act
        var capacityInput = cut.Find("tbody tr td input[type=number]");
        await cut.InvokeAsync(() => capacityInput.Change("75"));
        var confirmButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Confirm Import"));
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        await _importManagementService.Received(1).ConfirmImportAsync(
            Arg.Is<IReadOnlyList<ImportEventItemDto>>(events => events.Count == 1 && events[0].MaxCapacity == 75));
    }

    /// <summary>
    /// Verifies that selecting a manual column remap and re-running the preview sends the
    /// selected source-to-canonical column mapping to the preview service call.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenAdminRemapsAColumn_ResendsPreviewWithSelectedMapping()
    {
        // Arrange
        var previewWithHeaders = new CsvImportPreviewResponse
        {
            DetectedHeaders = ["Fecha", "Modalidad", "Titulo", "Hora", "Campo", "Lugar", "Categoria"],
            SuggestedMapping = new Dictionary<string, string>(),
            TotalRows = 0,
            ValidRowCount = 0,
            InvalidRowCount = 0,
            Rows = []
        };
        _importManagementService
            .PreviewCsvAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ImportColumnMappingDto>?>(), Arg.Any<int?>())
            .Returns(previewWithHeaders);
        var cut = RenderComponent<ImportEvents>();
        await UploadCsvAndPreviewAsync(cut);

        // Act: remap the "DÍA" canonical column to the "Fecha" detected header, then re-run the preview.
        var diaColumnSelect = cut.FindAll("select").First();
        await cut.InvokeAsync(() => diaColumnSelect.Change("Fecha"));
        var rerunButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Re-run Preview"));
        await cut.InvokeAsync(() => rerunButton.Click());

        // Assert
        await _importManagementService.Received(1).PreviewCsvAsync(
            Arg.Any<byte[]>(),
            Arg.Any<string>(),
            Arg.Is<IReadOnlyList<ImportColumnMappingDto>?>(mapping => mapping != null
                && mapping.Any(m => m.TargetColumn == "DÍA" && m.SourceColumn == "Fecha")),
            Arg.Any<int?>());
    }

    /// <summary>
    /// Verifies that clicking "Download Template" delegates to the import management service to
    /// fetch the template content.
    /// </summary>
    [Fact]
    public async Task ImportEvents_WhenDownloadTemplateClicked_CallsImportManagementService()
    {
        // Arrange
        _importManagementService.DownloadTemplateAsync().Returns("DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT\n");
        var cut = RenderComponent<ImportEvents>();

        // Act
        var downloadButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Download Template"));
        await cut.InvokeAsync(() => downloadButton.Click());

        // Assert
        await _importManagementService.Received(1).DownloadTemplateAsync();
    }

    /// <summary>
    /// Simulates the admin's upload-then-preview flow: selects a CSV file via the file input and
    /// clicks the Preview button, waiting for the mocked service call to resolve and the
    /// component to re-render with the preview rows.
    /// </summary>
    /// <param name="cut">The rendered <see cref="ImportEvents"/> component.</param>
    private static async Task UploadCsvAndPreviewAsync(IRenderedComponent<ImportEvents> cut)
    {
        var inputFileComponent = cut.FindComponent<InputFile>();
        var file = InputFileContent.CreateFromText("DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT\n", "events.csv");
        await cut.InvokeAsync(() => inputFileComponent.UploadFiles(file));

        var previewButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Preview"));
        await cut.InvokeAsync(() => previewButton.Click());
    }

    /// <summary>
    /// Builds a preview response with three rows exercising the three status branches the table
    /// renders: a valid non-duplicate row, a row flagged as a duplicate, and a row that is
    /// invalid for an ordinary field-validation reason (not a duplicate).
    /// </summary>
    private static CsvImportPreviewResponse CreatePreviewResponse() => new()
    {
        TotalRows = 3,
        ValidRowCount = 1,
        InvalidRowCount = 2,
        Rows =
        [
            new CsvImportRowDto
            {
                RowNumber = 1,
                Title = "Trap Shooting",
                Date = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                Location = "Range 1",
                MaxCapacity = 30,
                IsValid = true,
                IsDuplicate = false,
                Errors = []
            },
            new CsvImportRowDto
            {
                RowNumber = 2,
                Title = "Trap Shooting",
                Date = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                Location = "Range 1",
                MaxCapacity = 30,
                IsValid = false,
                IsDuplicate = true,
                Errors = ["Duplicate of row 1"]
            },
            new CsvImportRowDto
            {
                RowNumber = 3,
                Title = string.Empty,
                Date = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc),
                Location = "Range 2",
                MaxCapacity = 30,
                IsValid = false,
                IsDuplicate = false,
                Errors = ["'Title' must not be empty."]
            }
        ]
    };
}
