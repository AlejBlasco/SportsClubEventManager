using FluentAssertions;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using Xunit;

namespace SportsClubEventManager.Domain.Tests.Entities;

/// <summary>
/// Tests for User entity behavior including email validation, license fields, and gender handling.
/// </summary>
public sealed class UserTests
{
    /// <summary>
    /// Tests for Email property validation.
    /// </summary>
    public sealed class EmailValidation
    {
        /// <summary>
        /// Verifies that Email throws DomainException when set to null.
        /// </summary>
        [Fact]
        public void Email_WhenNull_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = null!;

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Email address is required.");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when set to empty string.
        /// </summary>
        [Fact]
        public void Email_WhenEmpty_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = string.Empty;

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Email address is required.");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when set to whitespace.
        /// </summary>
        [Fact]
        public void Email_WhenWhitespace_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = "   ";

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("Email address is required.");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when format is missing @ symbol.
        /// </summary>
        [Fact]
        public void Email_WhenMissingAtSymbol_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = "invalidemail.com";

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("*is not in a valid format*");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when format is missing domain.
        /// </summary>
        [Fact]
        public void Email_WhenMissingDomain_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = "user@";

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("*is not in a valid format*");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when format is missing local part.
        /// </summary>
        [Fact]
        public void Email_WhenMissingLocalPart_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = "@domain.com";

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("*is not in a valid format*");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when format has multiple @ symbols.
        /// </summary>
        [Fact]
        public void Email_WhenMultipleAtSymbols_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = "user@@domain.com";

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("*is not in a valid format*");
        }

        /// <summary>
        /// Verifies that Email throws DomainException when format lacks proper domain extension.
        /// </summary>
        [Fact]
        public void Email_WhenMissingDomainExtension_ThrowsDomainException()
        {
            // Arrange
            var sut = new User();

            // Act
            var act = () => sut.Email = "user@domain";

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("*is not in a valid format*");
        }

        /// <summary>
        /// Verifies that Email accepts valid format with standard domain.
        /// </summary>
        [Fact]
        public void Email_WhenValidFormat_Accepted()
        {
            // Arrange
            var sut = new User();
            var validEmail = "user@example.com";

            // Act
            sut.Email = validEmail;

            // Assert
            sut.Email.Should().Be(validEmail);
        }

        /// <summary>
        /// Verifies that Email accepts valid format with subdomain.
        /// </summary>
        [Fact]
        public void Email_WhenValidFormatWithSubdomain_Accepted()
        {
            // Arrange
            var sut = new User();
            var validEmail = "user@mail.example.co.uk";

            // Act
            sut.Email = validEmail;

            // Assert
            sut.Email.Should().Be(validEmail);
        }

        /// <summary>
        /// Verifies that Email accepts valid format with numbers and dots in local part.
        /// </summary>
        [Fact]
        public void Email_WhenValidFormatWithNumbersAndDots_Accepted()
        {
            // Arrange
            var sut = new User();
            var validEmail = "john.doe123@example.com";

            // Act
            sut.Email = validEmail;

            // Assert
            sut.Email.Should().Be(validEmail);
        }

        /// <summary>
        /// Verifies that Email accepts valid format case-insensitively.
        /// </summary>
        [Fact]
        public void Email_WhenValidFormatMixedCase_Accepted()
        {
            // Arrange
            var sut = new User();
            var validEmail = "User@Example.COM";

            // Act
            sut.Email = validEmail;

            // Assert
            sut.Email.Should().Be(validEmail);
        }

        /// <summary>
        /// Verifies that Email can be changed from one valid address to another.
        /// </summary>
        [Fact]
        public void Email_WhenChangingFromValidToValid_Accepted()
        {
            // Arrange
            var sut = new User { Email = "user1@example.com" };

            // Act
            sut.Email = "user2@example.com";

            // Assert
            sut.Email.Should().Be("user2@example.com");
        }
    }

    /// <summary>
    /// Tests for LicenseNumber and LicenseCategory optional fields.
    /// </summary>
    public sealed class LicenseFieldsHandling
    {
        /// <summary>
        /// Verifies that LicenseNumber can be set to null.
        /// </summary>
        [Fact]
        public void LicenseNumber_WhenNull_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.LicenseNumber = null;

            // Assert
            sut.LicenseNumber.Should().BeNull();
        }

        /// <summary>
        /// Verifies that LicenseNumber can be set to a string value.
        /// </summary>
        [Fact]
        public void LicenseNumber_WhenSet_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.LicenseNumber = "LIC123456";

            // Assert
            sut.LicenseNumber.Should().Be("LIC123456");
        }

        /// <summary>
        /// Verifies that LicenseCategory can be set to null.
        /// </summary>
        [Fact]
        public void LicenseCategory_WhenNull_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.LicenseCategory = null;

            // Assert
            sut.LicenseCategory.Should().BeNull();
        }

        /// <summary>
        /// Verifies that LicenseCategory can be set to a string value.
        /// </summary>
        [Fact]
        public void LicenseCategory_WhenSet_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.LicenseCategory = "CategoryA";

            // Assert
            sut.LicenseCategory.Should().Be("CategoryA");
        }

        /// <summary>
        /// Verifies that both LicenseNumber and LicenseCategory can be null simultaneously.
        /// </summary>
        [Fact]
        public void LicenseFields_WhenBothNull_AreAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.LicenseNumber = null;
            sut.LicenseCategory = null;

            // Assert
            sut.LicenseNumber.Should().BeNull();
            sut.LicenseCategory.Should().BeNull();
        }

        /// <summary>
        /// Verifies that both LicenseNumber and LicenseCategory can be set simultaneously.
        /// </summary>
        [Fact]
        public void LicenseFields_WhenBothSet_AreAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.LicenseNumber = "LIC789";
            sut.LicenseCategory = "CategoryB";

            // Assert
            sut.LicenseNumber.Should().Be("LIC789");
            sut.LicenseCategory.Should().Be("CategoryB");
        }
    }

    /// <summary>
    /// Tests for Gender property handling.
    /// </summary>
    public sealed class GenderProperty
    {
        /// <summary>
        /// Verifies that Gender can be set to Male.
        /// </summary>
        [Fact]
        public void Gender_WhenSetToMale_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.Gender = Gender.Male;

            // Assert
            sut.Gender.Should().Be(Gender.Male);
        }

        /// <summary>
        /// Verifies that Gender can be set to Female.
        /// </summary>
        [Fact]
        public void Gender_WhenSetToFemale_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.Gender = Gender.Female;

            // Assert
            sut.Gender.Should().Be(Gender.Female);
        }

        /// <summary>
        /// Verifies that Gender can be set to Other.
        /// </summary>
        [Fact]
        public void Gender_WhenSetToOther_IsAccepted()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            sut.Gender = Gender.Other;

            // Assert
            sut.Gender.Should().Be(Gender.Other);
        }
    }

    /// <summary>
    /// Tests for User construction and initialization.
    /// </summary>
    public sealed class UserConstruction
    {
        /// <summary>
        /// Verifies that User can be constructed with valid email.
        /// </summary>
        [Fact]
        public void User_WhenConstructedWithValidEmail_IsCreated()
        {
            // Arrange
            // Act
            var sut = new User { Email = "user@example.com" };

            // Assert
            sut.Email.Should().Be("user@example.com");
        }

        /// <summary>
        /// Verifies that User has empty Registrations collection by default.
        /// </summary>
        [Fact]
        public void User_WhenConstructed_HasEmptyRegistrationsCollection()
        {
            // Arrange
            var sut = new User { Email = "user@example.com" };

            // Act
            var registrations = sut.Registrations;

            // Assert
            registrations.Should().BeEmpty();
        }
    }
}
