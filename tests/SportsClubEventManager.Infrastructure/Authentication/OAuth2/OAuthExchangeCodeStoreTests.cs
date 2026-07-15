using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Authentication.OAuth2;
using Xunit;

namespace SportsClubEventManager.Infrastructure.Authentication.OAuth2;

/// <summary>
/// Unit tests for <see cref="OAuthExchangeCodeStore"/>, the single-use hand-off used between the Api's
/// Google OAuth2 callback and Web's exchange endpoint (issue #125).
/// </summary>
public sealed class OAuthExchangeCodeStoreTests
{
    private readonly OAuthExchangeCodeStore _sut = new(new MemoryCache(new MemoryCacheOptions()));

    private static AuthenticationResult CreateResult() => new()
    {
        UserId = Guid.NewGuid(),
        Email = "user@example.com",
        Name = "Test User",
        Role = Role.User,
        AccessToken = "access-token",
        RefreshToken = "refresh-token",
        ExpiresIn = 1800
    };

    /// <summary>
    /// Verifies that a code created for a given authentication result can be consumed once to retrieve
    /// that exact result.
    /// </summary>
    [Fact]
    public void ConsumeCode_WithFreshlyCreatedCode_ReturnsOriginalResult()
    {
        // Arrange
        var authenticationResult = CreateResult();
        var code = _sut.CreateCode(authenticationResult);

        // Act
        var consumed = _sut.ConsumeCode(code);

        // Assert
        consumed.Should().BeEquivalentTo(authenticationResult);
    }

    /// <summary>
    /// Verifies that a code cannot be redeemed twice — the second attempt must return null, so a
    /// replayed callback URL (e.g. the browser back button) cannot re-establish a session.
    /// </summary>
    [Fact]
    public void ConsumeCode_WhenCalledTwiceWithSameCode_ReturnsNullOnSecondCall()
    {
        // Arrange
        var code = _sut.CreateCode(CreateResult());
        _sut.ConsumeCode(code);

        // Act
        var secondConsume = _sut.ConsumeCode(code);

        // Assert
        secondConsume.Should().BeNull();
    }

    /// <summary>
    /// Verifies that an unknown code (never created, or created by a different process) returns null
    /// rather than throwing.
    /// </summary>
    [Fact]
    public void ConsumeCode_WithUnknownCode_ReturnsNull()
    {
        // Act
        var result = _sut.ConsumeCode("never-issued-code");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that two codes created from two different authentication results stay independent —
    /// consuming one does not affect the other.
    /// </summary>
    [Fact]
    public void CreateCode_CalledTwice_ProducesIndependentCodes()
    {
        // Arrange
        var firstResult = CreateResult();
        var secondResult = CreateResult();
        var firstCode = _sut.CreateCode(firstResult);
        var secondCode = _sut.CreateCode(secondResult);

        // Act
        var consumedFirst = _sut.ConsumeCode(firstCode);
        var consumedSecond = _sut.ConsumeCode(secondCode);

        // Assert
        firstCode.Should().NotBe(secondCode);
        consumedFirst.Should().BeEquivalentTo(firstResult);
        consumedSecond.Should().BeEquivalentTo(secondResult);
    }
}
