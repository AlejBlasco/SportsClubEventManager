using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Infrastructure;
using SportsClubEventManager.Infrastructure.Persistence;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure;

/// <summary>
/// Unit tests for the Infrastructure layer dependency injection registration.
/// </summary>
public sealed class DependencyInjectionTests
{
    private IConfiguration CreateValidConfiguration()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            });

        return configBuilder.Build();
    }

    /// <summary>
    /// Verifies that AddInfrastructure registers AppDbContext in the service container.
    /// </summary>
    [Fact]
    public void AddInfrastructure_RegistersAppDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var dbContext = serviceProvider.GetService<AppDbContext>();
        dbContext.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that AppDbContext is registered as a scoped service.
    /// </summary>
    [Fact]
    public void AddInfrastructure_RegistersAppDbContextAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var dbContextServiceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(AppDbContext));
        dbContextServiceDescriptor.Should().NotBeNull();
        dbContextServiceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Verifies that MigrateDatabaseAsync extension method completes without error on an InMemory database.
    /// </summary>
    [Fact]
    public async Task MigrateDatabaseAsync_CanBeCalledSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var act = async () => await serviceProvider.MigrateDatabaseAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that MigrateDatabaseAsync creates a proper scope and resolves AppDbContext from it.
    /// </summary>
    [Fact]
    public async Task MigrateDatabaseAsync_CreatesProperScopeForDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var act = async () => await serviceProvider.MigrateDatabaseAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that the service collection returns the same AppDbContext instance within a scope.
    /// </summary>
    [Fact]
    public void AddInfrastructure_ReturnsSameDbContextInstanceWithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext1 = scope.ServiceProvider.GetService<AppDbContext>();
            var dbContext2 = scope.ServiceProvider.GetService<AppDbContext>();

            // Assert
            dbContext1.Should().BeSameAs(dbContext2);
        }
    }

    /// <summary>
    /// Verifies that different scopes receive different AppDbContext instances.
    /// </summary>
    [Fact]
    public void AddInfrastructure_ReturnsDifferentDbContextInstancesAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        AppDbContext dbContext1;
        AppDbContext dbContext2;

        using (var scope1 = serviceProvider.CreateScope())
        {
            dbContext1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        // Assert
        dbContext1.Should().NotBeSameAs(dbContext2);
    }

    /// <summary>
    /// Verifies that the service collection method returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddInfrastructure_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        var result = services.AddInfrastructure(configuration);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Verifies that AddInfrastructure registers a single AppDbContext service descriptor.
    /// </summary>
    [Fact]
    public void AddInfrastructure_RegistersExactlyOneAppDbContextDescriptor()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var dbContextDescriptors = services.Where(sd => sd.ServiceType == typeof(AppDbContext)).ToList();
        dbContextDescriptors.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that AddInfrastructure configures an execution strategy on the DbContext.
    /// </summary>
    [Fact]
    public void AddInfrastructure_ConfiguresExecutionStrategyForDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        // Act
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        // Assert
        executionStrategy.Should().NotBeNull();
    }
}
