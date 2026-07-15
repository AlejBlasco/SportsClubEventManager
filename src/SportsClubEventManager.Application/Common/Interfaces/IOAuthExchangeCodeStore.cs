using SportsClubEventManager.Application.Authentication.Common;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Stores a short-lived, single-use code that hands off OAuth2 tokens from the Api's browser-facing
/// callback (a different origin than Web) to the Web app, without ever placing the tokens themselves
/// in a URL or redirect.
/// </summary>
public interface IOAuthExchangeCodeStore
{
    /// <summary>
    /// Creates a new single-use code bound to the given authentication result.
    /// </summary>
    /// <param name="result">The authentication result to associate with the code.</param>
    /// <returns>An opaque, unguessable code.</returns>
    string CreateCode(AuthenticationResult result);

    /// <summary>
    /// Consumes a previously created code, returning its associated authentication result exactly once.
    /// </summary>
    /// <param name="code">The code to consume.</param>
    /// <returns>The associated authentication result, or <c>null</c> if the code is unknown, expired, or already consumed.</returns>
    AuthenticationResult? ConsumeCode(string code);
}
