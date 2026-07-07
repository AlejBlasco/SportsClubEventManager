using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Users.Commands.ChangePassword;
using SportsClubEventManager.Application.Users.Commands.UpdateProfile;
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
}
