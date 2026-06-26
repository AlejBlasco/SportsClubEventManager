using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Pages;

/// <summary>
/// Code-behind for the Events page component.
/// </summary>
public sealed partial class Events : IAsyncDisposable
{
    private const string ViewPreferenceKey = "eventsViewPreference";
    private List<EventDto> events = [];
    private bool isLoading = true;
    private bool hasError;
    private ViewMode currentView = ViewMode.Calendar;
    private IJSObjectReference? sessionStorageModule;

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<Events> Logger { get; set; } = default!;

    /// <summary>
    /// Represents the available view modes for displaying events.
    /// </summary>
    private enum ViewMode
    {
        Calendar,
        List
    }

    /// <summary>
    /// Initializes the component after rendering.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadSessionStorageModuleAsync();
            await RestoreViewPreferenceAsync();
            await LoadEventsAsync();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Loads the session storage JavaScript module.
    /// </summary>
    private async Task LoadSessionStorageModuleAsync()
    {
        try
        {
            sessionStorageModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/sessionStorage.js");
        }
        catch
        {
            // JS module loading failed, continue without view persistence
        }
    }

    /// <summary>
    /// Restores the user's view preference from session storage.
    /// </summary>
    private async Task RestoreViewPreferenceAsync()
    {
        if (sessionStorageModule is null)
        {
            return;
        }

        try
        {
            var savedView = await sessionStorageModule.InvokeAsync<string?>("getItem", ViewPreferenceKey);
            if (Enum.TryParse<ViewMode>(savedView, out var viewMode))
            {
                currentView = viewMode;
            }
        }
        catch
        {
            // Failed to restore preference, use default
        }
    }

    /// <summary>
    /// Loads events from the API.
    /// </summary>
    private async Task LoadEventsAsync()
    {
        isLoading = true;
        hasError = false;

        try
        {
            events = await EventService.GetEventsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load events from API");
            hasError = true;
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Switches the view mode and persists the preference.
    /// </summary>
    /// <param name="viewMode">The view mode to switch to.</param>
    private async Task SwitchView(ViewMode viewMode)
    {
        currentView = viewMode;

        if (sessionStorageModule is not null)
        {
            try
            {
                await sessionStorageModule.InvokeVoidAsync("setItem", ViewPreferenceKey, viewMode.ToString());
            }
            catch
            {
                // Failed to persist preference, continue
            }
        }
    }

    /// <summary>
    /// Disposes the component and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (sessionStorageModule is not null)
        {
            await sessionStorageModule.DisposeAsync();
        }
    }
}
