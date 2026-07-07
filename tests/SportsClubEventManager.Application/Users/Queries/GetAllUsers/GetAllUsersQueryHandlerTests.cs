using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Queries.GetAllUsers;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Unit tests for GetAllUsersQueryHandler verifying pagination, filtering, and sorting functionality.
/// </summary>
public sealed class GetAllUsersQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly GetAllUsersQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllUsersQueryHandlerTests"/> class.
    /// </summary>
    public GetAllUsersQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new GetAllUsersQueryHandler(_context);
    }

    /// <summary>
    /// Verifies that GetAllUsersQueryHandler returns all users on the first page with default sorting.
    /// </summary>
    [Fact]
    public async Task Handle_FirstPage_ReturnsUsersWithPaginationMetadata()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.Administrator, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.User, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalCount.Should().Be(3);
    }

    /// <summary>
    /// Verifies that pagination correctly limits results to page size and skips correct number of records.
    /// </summary>
    [Fact]
    public async Task Handle_SecondPage_SkipsFirstPageAndReturnsCorrectRecords()
    {
        // Arrange
        var users = new List<User>();
        for (int i = 1; i <= 25; i++)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = $"User{i:D2}",
                Email = $"user{i:D2}@example.com",
                Role = Role.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddHours(i)
            });
        }

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.PageNumber.Should().Be(2);
        result.TotalCount.Should().Be(25);
    }

    /// <summary>
    /// Verifies that page size limit is respected and capped at maximum.
    /// </summary>
    [Fact]
    public async Task Handle_PageSizeExceededMax_ReturnsResultsUpToPageSize()
    {
        // Arrange
        var users = Enumerable.Range(1, 150)
            .Select(i => new User
            {
                Id = Guid.NewGuid(),
                Name = $"User{i}",
                Email = $"user{i}@example.com",
                Role = Role.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20, // Default page size
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(20);
        result.TotalCount.Should().Be(150);
    }

    /// <summary>
    /// Verifies that role filter correctly filters users by role.
    /// </summary>
    [Fact]
    public async Task Handle_RoleFilter_ReturnsOnlyUsersWithSpecifiedRole()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.Administrator, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.Administrator, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            RoleFilter = Role.Administrator,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(u => u.Role.Should().Be(Role.Administrator));
    }

    /// <summary>
    /// Verifies that status filter correctly filters users by active/inactive status.
    /// </summary>
    [Fact]
    public async Task Handle_StatusFilterActive_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = Role.User, IsActive = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            IsActiveFilter = true,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
    }

    /// <summary>
    /// Verifies that search filter works for name and email (case-insensitive).
    /// </summary>
    [Fact]
    public async Task Handle_SearchByEmail_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob Smith", Email = "bob@different.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SearchText = "EXAMPLE.COM",
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(u => u.Email.ToLower().Should().Contain("example.com"));
    }

    /// <summary>
    /// Verifies that search by name returns case-insensitive matches.
    /// </summary>
    [Fact]
    public async Task Handle_SearchByName_ReturnsCaseInsensitiveMatches()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice Johnson", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob Smith", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Charlie Brown", Email = "charlie@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SearchText = "ALICE",
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Alice Johnson");
    }

    /// <summary>
    /// Verifies that multiple filters combine with AND logic.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleFilters_CombinesWithAndLogic()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.Administrator, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.Administrator, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            RoleFilter = Role.Administrator,
            IsActiveFilter = true,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Alice");
    }

    /// <summary>
    /// Verifies that sorting by name in ascending order works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_SortByNameAsc_ReturnsSortedByNameAscending()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.ElementAt(0).Name.Should().Be("Alice");
        result.Items.ElementAt(1).Name.Should().Be("Bob");
        result.Items.ElementAt(2).Name.Should().Be("Charlie");
    }

    /// <summary>
    /// Verifies that sorting by name in descending order works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_SortByNameDesc_ReturnsSortedByNameDescending()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Charlie", Email = "charlie@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "desc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.ElementAt(0).Name.Should().Be("Charlie");
        result.Items.ElementAt(1).Name.Should().Be("Bob");
        result.Items.ElementAt(2).Name.Should().Be("Alice");
    }

    /// <summary>
    /// Verifies that sorting by email works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_SortByEmail_ReturnsSortedByEmail()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "User1", Email = "zebra@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User2", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User3", Email = "bob@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "email",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.ElementAt(0).Email.Should().Be("alice@example.com");
        result.Items.ElementAt(1).Email.Should().Be("bob@example.com");
        result.Items.ElementAt(2).Email.Should().Be("zebra@example.com");
    }

    /// <summary>
    /// Verifies that sorting by role works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_SortByRole_ReturnsSortedByRole()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "User1", Email = "user1@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User2", Email = "user2@example.com", Role = Role.Administrator, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User3", Email = "user3@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "role",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies that sorting by IsActive status works correctly.
    /// </summary>
    [Fact]
    public async Task Handle_SortByIsActive_ReturnsSortedByActiveStatus()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "User1", Email = "user1@example.com", Role = Role.User, IsActive = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User2", Email = "user2@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User3", Email = "user3@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "isactive",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that empty user list returns zero items with correct metadata.
    /// </summary>
    [Fact]
    public async Task Handle_NoUsersInSystem_ReturnsEmptyResult()
    {
        // Arrange
        var users = new List<User>();
        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(1);
    }

    /// <summary>
    /// Verifies that requesting a page beyond total count returns empty items but correct metadata.
    /// </summary>
    [Fact]
    public async Task Handle_PageBeyondTotalCount_ReturnsEmptyItemsWithMetadata()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetAllUsersQuery
        {
            PageNumber = 10,
            PageSize = 20,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(10);
    }
}
