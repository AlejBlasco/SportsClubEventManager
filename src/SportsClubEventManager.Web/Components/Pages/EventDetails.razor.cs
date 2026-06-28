using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Constants;
using SportsClubEventManager.Web.Models;
using SportsClubEventManager.Web.Services;
using System.Text.Json;

namespace SportsClubEventManager.Web.Components.Pages;

/// <summary>
/// Code-behind for the EventDetails page component.
/// Handles event display and user registration/cancellation functionality.
/// </summary>
public sealed partial class EventDetails
{
    private const string LocalStorageKeyPrefix = LocalStorageKeys.EventRegistrationPrefix;

    private EventDetailDto? eventDetail;
    private bool isLoading = true;
    private bool hasError;
    private bool isNotFound;

    private bool isRegistered;
    private Guid? currentUserId;
    private string? currentUserName;
    private string? currentUserEmail;

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
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IGuidProvider GuidProvider { get; set; } = default!;

    /// <summary>
    /// Initializes the component and loads event details.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadEventDetailsAsync();
        await LoadRegistrationStateAsync();
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

    /// <summary>
    /// Loads registration state from browser localStorage.
    /// </summary>
    private async Task LoadRegistrationStateAsync()
    {
        try
        {
            var storageKey = $"{LocalStorageKeyPrefix}{Id}";
            var storedData = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", storageKey);

            if (!string.IsNullOrWhiteSpace(storedData))
            {
                var registrationData = JsonSerializer.Deserialize<RegistrationStorageData>(storedData);
                if (registrationData is not null)
                {
                    currentUserId = registrationData.UserId;
                    currentUserName = registrationData.UserName;
                    currentUserEmail = registrationData.UserEmail;
                    isRegistered = true;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load registration state from localStorage for event {EventId}", Id);
        }
    }

    /// <summary>
    /// Saves registration state to browser localStorage.
    /// </summary>
    private async Task SaveRegistrationStateAsync(Guid userId, string userName, string userEmail)
    {
        try
        {
            var storageKey = $"{LocalStorageKeyPrefix}{Id}";
            var registrationData = new RegistrationStorageData
            {
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail
            };

            var jsonData = JsonSerializer.Serialize(registrationData);
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, jsonData);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save registration state to localStorage for event {EventId}", Id);
        }
    }

    /// <summary>
    /// Removes registration state from browser localStorage.
    /// </summary>
    private async Task ClearRegistrationStateAsync()
    {
        try
        {
            var storageKey = $"{LocalStorageKeyPrefix}{Id}";
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to clear registration state from localStorage for event {EventId}", Id);
        }
    }

    /// <summary>
    /// Shows the registration form dialog.
    /// </summary>
    private void ShowRegistrationForm()
    {
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
        isRegistrationInProgress = true;
        ClearMessages();

        try
        {
            var userId = GuidProvider.NewGuid();
            var result = await EventService.RegisterForEventAsync(Id, userId);

            if (result is not null)
            {
                currentUserId = userId;
                currentUserName = formModel.Name;
                currentUserEmail = formModel.Email;
                isRegistered = true;

                await SaveRegistrationStateAsync(userId, formModel.Name, formModel.Email);

                successMessage = "Registration successful! You are now registered for this event.";
                showRegistrationForm = false;

                await LoadEventDetailsAsync();
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
        if (currentUserId is null)
        {
            errorMessage = "Unable to cancel registration: user information not found.";
            showCancelConfirmation = false;
            return;
        }

        isCancellationInProgress = true;
        ClearMessages();

        try
        {
            var success = await EventService.CancelRegistrationAsync(Id, currentUserId.Value);

            if (success)
            {
                currentUserId = null;
                currentUserName = null;
                currentUserEmail = null;
                isRegistered = false;

                await ClearRegistrationStateAsync();

                successMessage = "Registration cancelled successfully.";
                showCancelConfirmation = false;

                await LoadEventDetailsAsync();
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

    /// <summary>
    /// Data structure for storing registration information in browser localStorage.
    /// </summary>
    private sealed class RegistrationStorageData
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}
