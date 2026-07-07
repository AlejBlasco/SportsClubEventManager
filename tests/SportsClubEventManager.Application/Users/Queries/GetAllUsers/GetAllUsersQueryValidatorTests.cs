using FluentAssertions;
using FluentValidation;
using Xunit;
using SportsClubEventManager.Application.Users.Queries.GetAllUsers;

namespace SportsClubEventManager.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Unit tests for GetAllUsersQueryValidator verifying pagination parameter validation.
/// </summary>
public sealed class GetAllUsersQueryValidatorTests
{
    private readonly GetAllUsersQueryValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllUsersQueryValidatorTests"/> class.
    /// </summary>
    public GetAllUsersQueryValidatorTests()
    {
        _validator = new GetAllUsersQueryValidator();
    }

    /// <summary>
    /// Verifies that a valid query with default parameters passes validation.
    /// </summary>
    [Fact]
    public void Validate_ValidQueryWithDefaults_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that page number must be greater than zero.
    /// </summary>
    [Fact]
    public void Validate_PageNumberZero_FailsValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 0,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageNumber");
    }

    /// <summary>
    /// Verifies that page number cannot be negative.
    /// </summary>
    [Fact]
    public void Validate_PageNumberNegative_FailsValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = -1,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that page size must be greater than zero.
    /// </summary>
    [Fact]
    public void Validate_PageSizeZero_FailsValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 0,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    /// <summary>
    /// Verifies that page size cannot exceed maximum (100).
    /// </summary>
    [Fact]
    public void Validate_PageSizeExceedsMaximum_FailsValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 101,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    /// <summary>
    /// Verifies that maximum page size (100) is valid.
    /// </summary>
    [Fact]
    public void Validate_PageSizeAtMaximum_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 100,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that minimum page size (1) is valid.
    /// </summary>
    [Fact]
    public void Validate_PageSizeOne_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 1,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that query with null search text is valid.
    /// </summary>
    [Fact]
    public void Validate_NullSearchText_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SearchText = null,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that query with empty search text is valid.
    /// </summary>
    [Fact]
    public void Validate_EmptySearchText_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SearchText = "",
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that query with whitespace-only search text is valid.
    /// </summary>
    [Fact]
    public void Validate_WhitespaceSearchText_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SearchText = "   ",
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that query with valid search text passes validation.
    /// </summary>
    [Fact]
    public void Validate_ValidSearchText_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SearchText = "john@example.com",
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that multiple validation errors are reported together.
    /// </summary>
    [Fact]
    public void Validate_MultipleValidationErrors_ReportsAllErrors()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 0,
            PageSize = 150,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Verifies that default pagination settings are valid.
    /// </summary>
    [Fact]
    public void Validate_DefaultPaginationSettings_PassesValidation()
    {
        // Arrange
        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20 // Default page size per specification
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
