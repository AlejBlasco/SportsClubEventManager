using Microsoft.Extensions.Caching.Memory;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Infrastructure.Authentication.OAuth2;

/// <summary>
/// <see cref="IMemoryCache"/>-backed implementation of <see cref="IOAuthExchangeCodeStore"/>. Safe for a
/// single Api instance (the homelab deployment does not run multiple Api replicas); a multi-instance
/// deployment would need a shared store (e.g. a database table) instead.
/// </summary>
public sealed class OAuthExchangeCodeStore : IOAuthExchangeCodeStore
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthExchangeCodeStore"/> class.
    /// </summary>
    /// <param name="cache">The memory cache used to hold pending exchange codes.</param>
    public OAuthExchangeCodeStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public string CreateCode(AuthenticationResult result)
    {
        var code = Guid.NewGuid().ToString("N");

        _cache.Set(BuildKey(code), result, CodeLifetime);

        return code;
    }

    /// <inheritdoc />
    public AuthenticationResult? ConsumeCode(string code)
    {
        var key = BuildKey(code);

        if (!_cache.TryGetValue(key, out AuthenticationResult? result))
        {
            return null;
        }

        // Single-use: remove immediately so a replayed code (e.g. the browser back button) fails.
        _cache.Remove(key);

        return result;
    }

    private static string BuildKey(string code) => $"oauth-exchange:{code}";
}
