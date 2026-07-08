using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Models;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Pages;

/// <summary>
/// Code-behind for the EventDetails page component.
/// Handles event display and user registration/cancellation functionality.
/// </summary>
public sealed partial class EventDetails
{
    private EventDetailDto? eventDetail;
    private bool isLoading = true;
    private bool hasError;
    private bool isNotFound;

    private bool isRegistered;
    private bool isAuthenticated;
    private Guid? currentRegistrationId;

    private bool showRegistrationForm;
    private bool showCancelConfirmation;
    private bool isRegistrationInProgress;
    private bool isCancellationInProgress;

    private string? successMessage;
    private string? errorMessage;

    /// <summary>
    /// Gets or sets the event ID from the route parameter.
    /// </summary>
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private ILogger<EventDetails> Logger { get; set; } = default!;

    [Inject]
    private IRegistrationService RegistrationService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Initializes the component and loads event details.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

        await LoadEventDetailsAsync();

        if (isAuthenticated)
        {
            await LoadMyRegistrationStateAsync();
        }
    }

    /// <summary>
    /// Loads event details from the API.
    /// </summary>
    private async Task LoadEventDetailsAsync()
    {
        isLoading = true;
        hasError = false;
        isNotFound = false;

        try
        {
            eventDetail = await EventService.GetEventByIdAsync(Id);

            if (eventDetail is null)
            {
                isNotFound = true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load event details for event {EventId}", Id);
            hasError = true;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadMyRegistrationStateAsync()
    {
        try
        {
            var myRegistrations = await RegistrationService.GetMyRegistrationsAsync();
            var matchingRegistration = myRegistrations.FirstOrDefault(r => r.EventId == Id);

            isRegistered = matchingRegistration is not null;
            currentRegistrationId = matchingRegistration?.RegistrationId;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load current user registrations for event {EventId}", Id);
            isRegistered = false;
            currentRegistrationId = null;
        }
    }

    /// <summary>
    /// Shows the registration form dialog.
    /// </summary>
    private void ShowRegistrationForm()
    {
        if (!isAuthenticated)
        {
            NavigationManager.NavigateTo("/login");
            return;
        }

        showRegistrationForm = true;
        ClearMessages();
    }

    /// <summary>
    /// Hides the registration form dialog.
    /// </summary>
    private void HideRegistrationForm()
    {
        showRegistrationForm = false;
    }

    /// <summary>
    /// Shows the cancellation confirmation dialog.
    /// </summary>
    private void ShowCancelConfirmation()
    {
        showCancelConfirmation = true;
        ClearMessages();
    }

    /// <summary>
    /// Hides the cancellation confirmation dialog.
    /// </summary>
    private void HideCancelConfirmation()
    {
        showCancelConfirmation = false;
    }

    /// <summary>
    /// Handles registration form submission.
    /// </summary>
    private async Task HandleRegistrationSubmit(RegistrationFormModel formModel)
    {
        _ = formModel;
        isRegistrationInProgress = true;
        ClearMessages();

        try
        {
            var result = await EventService.RegisterForEventAsync(Id);

            if (result is not null)
            {
                currentRegistrationId = result.RegistrationId;
                isRegistered = true;

                successMessage = "Registration successful! You are now registered for this event.";
                showRegistrationForm = false;

                await LoadEventDetailsAsync();
                await LoadMyRegistrationStateAsync();
            }
            else
            {
                errorMessage = "Registration failed. The event may be full or you may already be registered. Please refresh and try again.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register for event {EventId}", Id);
            errorMessage = "An unexpected error occurred. Please try again later.";
        }
        finally
        {
            isRegistrationInProgress = false;
        }
    }

    /// <summary>
    /// Handles cancellation confirmation.
    /// </summary>
    private async Task HandleCancellationConfirm()
    {
        if (currentRegistrationId is null)
        {
            errorMessage = "Unable to cancel registration: registration information not found.";
            showCancelConfirmation = false;
            return;
        }

        isCancellationInProgress = true;
        ClearMessages();

        try
        {
            var success = await EventService.CancelRegistrationAsync(currentRegistrationId.Value);

            if (success)
            {
                currentRegistrationId = null;
                isRegistered = false;

                successMessage = "Registration cancelled successfully.";
                showCancelConfirmation = false;

                await LoadEventDetailsAsync();
                await LoadMyRegistrationStateAsync();
            }
            else
            {
                errorMessage = "Cancellation failed. Your registration may have already been cancelled. Please refresh the page.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cancel registration for event {EventId}", Id);
            errorMessage = "An unexpected error occurred while cancelling. Please try again later.";
        }
        finally
        {
            isCancellationInProgress = false;
        }
    }

    /// <summary>
    /// Clears all success and error messages.
    /// </summary>
    private void ClearMessages()
    {
        successMessage = null;
        errorMessage = null;
    }

    /// <summary>
    /// Clears the success message.
    /// </summary>
    private void ClearSuccessMessage()
    {
        successMessage = null;
    }

    /// <summary>
    /// Clears the error message.
    /// </summary>
    private void ClearErrorMessage()
    {
        errorMessage = null;
    }
}
