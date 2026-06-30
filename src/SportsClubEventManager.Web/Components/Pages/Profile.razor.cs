using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace SportsClubEventManager.Web.Components.Pages;

/// <summary>
/// Code-behind for the user Profile page component.
/// </summary>
public sealed partial class Profile
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _initials = string.Empty;
    private bool _isLoading = true;

    /// <summary>
    /// Loads the authenticated user's claims on component initialization.
    /// Redirects to the login page if the user is not authenticated.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            Navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        _name = user.Identity.Name ?? string.Empty;
        _email = FindClaim(user, ClaimTypes.Email, "email") ?? string.Empty;
        _initials = BuildInitials(_name);
        _isLoading = false;
    }

    private static string? FindClaim(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirstValue(claimType);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    private static string BuildInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
            : char.ToUpperInvariant(name[0]).ToString();
    }
}
