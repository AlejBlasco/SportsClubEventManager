using Microsoft.AspNetCore.Components;

namespace SportsClubEventManager.Web.Components.Shared;

/// <summary>
/// Code-behind for the ConfirmationDialog component.
/// Provides a reusable confirmation modal for destructive or irreversible actions.
/// </summary>
public sealed partial class ConfirmationDialog
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Confirm Action";

    /// <summary>
    /// Gets or sets the confirmation message displayed in the dialog body.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Are you sure you want to proceed?";

    /// <summary>
    /// Gets or sets a value indicating whether the confirmation action is currently being processed.
    /// </summary>
    [Parameter]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user confirms the action.
    /// </summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user cancels the action.
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Handles confirm button click.
    /// </summary>
    private async Task OnConfirmClicked()
    {
        await OnConfirm.InvokeAsync();
    }

    /// <summary>
    /// Handles cancel button click or overlay click.
    /// </summary>
    private async Task OnCancelClicked()
    {
        await OnCancel.InvokeAsync();
    }
}
