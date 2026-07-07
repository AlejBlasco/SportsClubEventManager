using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Admin;

/// <summary>
/// Code-behind for the User Details Modal component.
/// </summary>
public partial class UserDetailsModal
{
    /// <summary>
    /// Gets or sets the identifier of the user to display in the modal.
    /// </summary>
    [Parameter]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the modal is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after a successful user update.
    /// </summary>
    [Parameter]
    public EventCallback OnUserUpdated { get; set; }

    private bool _isVisible = true;
    private bool _loading = true;
    private bool _submitting = false;
    private bool _editMode = false;
    private bool _editingRole = false;
    private bool _showDeleteConfirm = false;
    private string? _errorMessage;
    private string? _localSuccessMessage;

    private UserDetailsDto? _user;
    private UpdateUserRequest _editRequest = new();
    private Role _newRole;
    private bool _isSelfModification;
    private Guid _currentUserId;

    /// <summary>
    /// Initializes the component and loads user details.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task OnInitializedAsync()
    {
        await LoadUserDetailsAsync();
        await CheckIfSelfModificationAsync();
    }

    /// <summary>
    /// Loads the user details from the API.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadUserDetailsAsync()
    {
        try
        {
            _loading = true;
            _errorMessage = null;
            _user = await UserManagementService.GetUserByIdAsync(UserId);
            _newRole = _user.Role;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load user details: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Checks if the current user is modifying their own account.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CheckIfSelfModificationAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var currentUserId))
        {
            _currentUserId = currentUserId;
            _isSelfModification = currentUserId == UserId;
        }
    }

    /// <summary>
    /// Enables edit mode for user information.
    /// </summary>
    private void EnableEditMode()
    {
        if (_user == null) return;

        _editRequest = new UpdateUserRequest
        {
            Name = _user.Name,
            Email = _user.Email,
            Gender = _user.Gender,
            LicenseNumber = _user.LicenseNumber,
            LicenseCategory = _user.LicenseCategory
        };

        _editMode = true;
    }

    /// <summary>
    /// Cancels edit mode without saving changes.
    /// </summary>
    private void CancelEdit()
    {
        _editMode = false;
        _editRequest = new();
    }

    /// <summary>
    /// Saves the edited user information.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SaveChangesAsync()
    {
        try
        {
            _submitting = true;
            _errorMessage = null;

            _user = await UserManagementService.UpdateUserAsync(UserId, _editRequest);
            _localSuccessMessage = "User information updated successfully.";
            _editMode = false;

            await OnUserUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to update user: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    /// <summary>
    /// Toggles role editing mode.
    /// </summary>
    private void ToggleRoleEdit()
    {
        _editingRole = !_editingRole;
        if (_editingRole && _user != null)
        {
            _newRole = _user.Role;
        }
    }

    /// <summary>
    /// Saves the new role assignment.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SaveRoleAsync()
    {
        if (_user == null) return;

        try
        {
            _submitting = true;
            _errorMessage = null;

            await UserManagementService.UpdateUserRoleAsync(UserId, _newRole);
            _user.Role = _newRole;
            _editingRole = false;
            _localSuccessMessage = "User role updated successfully.";

            await OnUserUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to update role: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    /// <summary>
    /// Toggles the user's active status.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ToggleStatusAsync()
    {
        if (_user == null) return;

        try
        {
            _submitting = true;
            _errorMessage = null;

            var newStatus = !_user.IsActive;
            await UserManagementService.UpdateUserStatusAsync(UserId, newStatus);
            _user.IsActive = newStatus;
            _localSuccessMessage = $"User account {(newStatus ? "activated" : "deactivated")} successfully.";

            await OnUserUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to update status: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    /// <summary>
    /// Deletes the user account permanently.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteUserAsync()
    {
        try
        {
            _submitting = true;
            _errorMessage = null;

            await UserManagementService.DeleteUserAsync(UserId);
            await OnUserUpdated.InvokeAsync();
            await Close();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to delete user: {ex.Message}";
            _showDeleteConfirm = false;
        }
        finally
        {
            _submitting = false;
        }
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task Close()
    {
        _isVisible = false;
        await OnClose.InvokeAsync();
    }
}
