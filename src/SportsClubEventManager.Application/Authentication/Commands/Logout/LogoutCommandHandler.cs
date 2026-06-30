using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Application.Authentication.Commands.Logout;

/// <summary>
/// Handler for the LogoutCommand.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public LogoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the logout command by revoking the user's refresh token.
    /// </summary>
    /// <param name="request">The logout command containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Unit.Value;
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
