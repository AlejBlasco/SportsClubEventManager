using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Pages;

/// <summary>
/// Code-behind for the authenticated user's registrations page.
/// </summary>
public partial class MyRegistrations
{
    private readonly List<RegistrationListDto> _registrations = [];
    private bool _loading = true;
    private Guid? _processingRegistrationId;
    private RegistrationListDto? _pendingCancellation;
    private string? _errorMessage;
    private string? _successMessage;

    /// <summary>
    /// Initializes the page by loading registrations.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _loading = true;
            _errorMessage = null;

            var result = await RegistrationService.GetMyRegistrationsAsync();
            _registrations.Clear();
            _registrations.AddRange(result);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load registrations: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Shows the cancellation confirmation dialog for the given registration.
    /// </summary>
    private void ShowCancelConfirmation(RegistrationListDto registration)
    {
        _pendingCancellation = registration;
        _errorMessage = null;
        _successMessage = null;
    }

    /// <summary>
    /// Hides the cancellation confirmation dialog without cancelling.
    /// </summary>
    private void HideCancelConfirmation()
    {
        _pendingCancellation = null;
    }

    /// <summary>
    /// Confirms and performs the cancellation of the pending registration.
    /// </summary>
    private async Task HandleCancellationConfirmAsync()
    {
        if (_pendingCancellation is null)
        {
            return;
        }

        var registration = _pendingCancellation;
        _errorMessage = null;
        _successMessage = null;

        try
        {
            _processingRegistrationId = registration.RegistrationId;
            var success = await RegistrationService.CancelMyRegistrationAsync(registration.RegistrationId);

            if (!success)
            {
                _errorMessage = "Cancellation failed. The registration may no longer be available.";
                return;
            }

            _successMessage = "Registration cancelled successfully.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to cancel registration: {ex.Message}";
        }
        finally
        {
            _processingRegistrationId = null;
            _pendingCancellation = null;
        }
    }
}
