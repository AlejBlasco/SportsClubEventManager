using FluentAssertions;
using Xunit;
using SportsClubEventManager.Infrastructure.Authentication;

namespace SportsClubEventManager.Infrastructure.Authentication;

/// <summary>
/// Unit tests for the PasswordHasher.
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordHasherTests"/> class.
    /// </summary>
    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    /// <summary>
    /// Verifies that HashPassword returns a non-empty hash.
    /// </summary>
    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2");
    }

    /// <summary>
    /// Verifies that HashPassword generates unique hashes for same password (due to salt).
    /// </summary>
    [Fact]
    public void HashPassword_WithSamePassword_GeneratesUniqueSalts()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    /// <summary>
    /// Verifies that VerifyPassword returns true for correct password.
    /// </summary>
    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that VerifyPassword returns false for incorrect password.
    /// </summary>
    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that VerifyPassword is case-sensitive.
    /// </summary>
    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var differentCasePassword = "mysecurepassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(differentCasePassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that HashPassword works with various password complexities.
    /// </summary>
    [Theory]
    [InlineData("Simple123!")]
    [InlineData("VeryLongPasswordWithManyCharacters123!@#$%")]
    [InlineData("P@ssw0rd")]
    [InlineData("12345678")]
    public void HashPassword_WithVariousPasswordComplexities_ProducesValidHashes(string password)
    {
        // Arrange
        // (password from theory data)

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        result.Should().BeTrue();
    }
}
