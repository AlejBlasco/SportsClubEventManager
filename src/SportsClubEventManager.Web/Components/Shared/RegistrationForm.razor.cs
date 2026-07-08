using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Web.Models;

namespace SportsClubEventManager.Web.Components.Shared;

/// <summary>
/// Code-behind for the RegistrationForm component.
/// Provides a modal dialog for collecting user registration information.
/// </summary>
public sealed partial class RegistrationForm
{
    private RegistrationFormModel FormModel { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the form is currently being submitted.
    /// </summary>
    [Parameter]
    public bool IsSubmitting { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user's name, used to prefill the form.
    /// </summary>
    [Parameter]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated user's email, used to prefill the form.
    /// </summary>
    [Parameter]
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback invoked when the form is successfully submitted with valid data.
    /// </summary>
    [Parameter]
    public EventCallback<RegistrationFormModel> OnSubmit { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user cancels the registration.
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        FormModel.Name = UserName;
        FormModel.Email = UserEmail;
    }

    /// <summary>
    /// Handles valid form submission.
    /// </summary>
    private async Task OnValidSubmitAsync()
    {
        await OnSubmit.InvokeAsync(FormModel);
    }

    /// <summary>
    /// Handles cancel button click.
    /// </summary>
    private async Task OnCancelClicked()
    {
        await OnCancel.InvokeAsync();
    }
}
