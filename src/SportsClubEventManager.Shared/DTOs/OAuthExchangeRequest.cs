namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for redeeming a one-time OAuth2 exchange code for the real tokens.
/// </summary>
public sealed class OAuthExchangeRequest
{
    /// <summary>
    /// Gets or sets the opaque, single-use code issued by the Api's OAuth2 callback.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
