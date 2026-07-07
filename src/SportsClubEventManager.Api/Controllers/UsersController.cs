using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Users.Commands.ChangePassword;
using SportsClubEventManager.Application.Users.Commands.DeleteUser;
using SportsClubEventManager.Application.Users.Commands.UpdateProfile;
using SportsClubEventManager.Application.Users.Commands.UpdateUserAsAdmin;
using SportsClubEventManager.Application.Users.Commands.UpdateUserRole;
using SportsClubEventManager.Application.Users.Commands.UpdateUserStatus;
using SportsClubEventManager.Application.Users.Queries.GetAllUsers;
using SportsClubEventManager.Application.Users.Queries.GetUserById;
using SportsClubEventManager.Application.Users.Queries.GetUserProfile;
using SportsClubEventManager.Shared.DTOs;
using System.Security.Claims;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for user profile operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender for command dispatching.</param>
    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves the profile of the specified user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user profile data.</returns>
    [HttpGet("{id}/profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        var requestingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (requestingUserIdClaim is null || !Guid.TryParse(requestingUserIdClaim, out var requestingUserId))
        {
            return Unauthorized();
        }

        // Users can only view their own profile
        if (requestingUserId != id)
        {
            return Forbid();
        }

        try
        {
            var query = new GetUserProfileQuery { UserId = id };
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates the profile of the specified user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="request">The profile update request containing new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile data.</returns>
    [HttpPut("{id}/profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var requestingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (requestingUserIdClaim is null || !Guid.TryParse(requestingUserIdClaim, out var requestingUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new UpdateProfileCommand
            {
                RequestingUserId = requestingUserId,
                UserId = id,
                Name = request.Name,
                Gender = request.Gender,
                Email = request.Email,
                LicenseNumber = request.LicenseNumber,
                LicenseCategory = request.LicenseCategory
            };

            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Changes the password of the specified user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="request">The password change request containing current and new passwords.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication tokens.</returns>
    [HttpPut("{id}/password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var requestingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (requestingUserIdClaim is null || !Guid.TryParse(requestingUserIdClaim, out var requestingUserId))
        {
            return Unauthorized();
        }

        // Validate password confirmation
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return BadRequest(new { message = "New password and confirmation do not match." });
        }

        try
        {
            var command = new ChangePasswordCommand
            {
                RequestingUserId = requestingUserId,
                UserId = id,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            };

            var result = await _sender.Send(command, cancellationToken);

            var response = new ChangePasswordResponse
            {
                Message = "Password changed successfully.",
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of all users (Administrator only).
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="roleFilter">Optional role filter.</param>
    /// <param name="isActiveFilter">Optional status filter.</param>
    /// <param name="searchText">Optional search text for name or email.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortOrder">The sort order (asc or desc).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of users.</returns>
    [HttpGet("admin")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(PagedResult<UserListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Domain.Enums.Role? roleFilter = null,
        [FromQuery] bool? isActiveFilter = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllUsersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            RoleFilter = roleFilter,
            IsActiveFilter = isActiveFilter,
            SearchText = searchText,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed information about a specific user by ID (Administrator only).
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed user information.</returns>
    [HttpGet("admin/{id}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates user information as an administrator.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user details.</returns>
    [HttpPut("admin/{id}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new UpdateUserAsAdminCommand
            {
                AdminUserId = adminUserId,
                UserId = id,
                Name = request.Name,
                Email = request.Email,
                Gender = request.Gender,
                LicenseNumber = request.LicenseNumber,
                LicenseCategory = request.LicenseCategory,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Activates or deactivates a user account (Administrator only).
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="request">The status update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("admin/{id}/status")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new UpdateUserStatusCommand
            {
                AdminUserId = adminUserId,
                UserId = id,
                IsActive = request.IsActive,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates a user's role (Administrator only).
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="request">The role update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("admin/{id}/role")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new UpdateUserRoleCommand
            {
                AdminUserId = adminUserId,
                UserId = id,
                NewRole = request.Role,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Permanently deletes a user account and cascade deletes related registrations (Administrator only).
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("admin/{id}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new DeleteUserCommand
            {
                AdminUserId = adminUserId,
                UserId = id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
