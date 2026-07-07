using Microsoft.AspNetCore.Components;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Components.Pages.Admin;

/// <summary>
/// Code-behind for the User Management page.
/// </summary>
public partial class UserManagement
{
    private PagedResult<UserListDto>? _users;
    private bool _loading = true;
    private string? _errorMessage;
    private string? _successMessage;

    private int _currentPage = 1;
    private int _pageSize = 20;
    private string _searchText = string.Empty;
    private string _roleFilter = string.Empty;
    private string _statusFilter = string.Empty;
    private string _sortBy = "Name";
    private string _sortOrder = "asc";

    private Guid? _selectedUserId;

    /// <summary>
    /// Initializes the component and loads the initial user list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
    }

    /// <summary>
    /// Loads users from the API with current filter and pagination settings.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadUsersAsync()
    {
        try
        {
            _loading = true;
            _errorMessage = null;

            Role? roleFilterEnum = null;
            if (!string.IsNullOrEmpty(_roleFilter) && Enum.TryParse<Role>(_roleFilter, out var parsedRole))
            {
                roleFilterEnum = parsedRole;
            }

            bool? statusFilterBool = null;
            if (!string.IsNullOrEmpty(_statusFilter) && bool.TryParse(_statusFilter, out var parsedStatus))
            {
                statusFilterBool = parsedStatus;
            }

            _users = await UserManagementService.GetAllUsersAsync(
                _currentPage,
                _pageSize,
                roleFilterEnum,
                statusFilterBool,
                string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                _sortBy,
                _sortOrder);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load users: {ex.Message}";
            _users = new PagedResult<UserListDto>();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Applies the current filter settings and reloads the user list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ApplyFiltersAsync()
    {
        _currentPage = 1;
        await LoadUsersAsync();
    }

    /// <summary>
    /// Changes the current page and reloads the user list.
    /// </summary>
    /// <param name="pageNumber">The page number to navigate to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ChangePage(int pageNumber)
    {
        if (pageNumber < 1 || (_users != null && pageNumber > _users.TotalPages))
        {
            return;
        }

        _currentPage = pageNumber;
        await LoadUsersAsync();
    }

    /// <summary>
    /// Opens the user details modal for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to view.</param>
    private void ViewUserDetails(Guid userId)
    {
        _selectedUserId = userId;
    }

    /// <summary>
    /// Closes the user details modal.
    /// </summary>
    private void CloseUserDetails()
    {
        _selectedUserId = null;
    }

    /// <summary>
    /// Handles the user updated event from the details modal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task OnUserUpdatedAsync()
    {
        _successMessage = "User updated successfully.";
        _selectedUserId = null;
        await LoadUsersAsync();
    }
}
