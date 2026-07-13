using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Infrastructure;
using SportsClubEventManager.Infrastructure.Configuration;
using SportsClubEventManager.Infrastructure.Notifications;
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

    private IConfiguration CreateConfigurationWithoutConnectionString()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>());

        return configBuilder.Build();
    }

    private IConfiguration CreateConfigurationWithBlankConnectionString()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "   " }
            });

        return configBuilder.Build();
    }

    /// <summary>
    /// Verifies that AddInfrastructure throws InvalidOperationException when
    /// ConnectionStrings:DefaultConnection is entirely missing from configuration, so the process
    /// fails fast at service registration time instead of failing later inside AddDbContext with a
    /// less clear error.
    /// </summary>
    [Fact]
    public void AddInfrastructure_WithMissingConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfigurationWithoutConnectionString();

        // Act
        var act = () => services.AddInfrastructure(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:DefaultConnection*");
    }

    /// <summary>
    /// Verifies that AddInfrastructure throws InvalidOperationException when
    /// ConnectionStrings:DefaultConnection is present but whitespace-only, treating a blank value
    /// the same as a missing one.
    /// </summary>
    [Fact]
    public void AddInfrastructure_WithBlankConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfigurationWithBlankConnectionString();

        // Act
        var act = () => services.AddInfrastructure(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:DefaultConnection*");
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

    /// <summary>
    /// Tests verifying the "database" health check registration added by AddInfrastructure
    /// (issue #41), covering registration metadata only (name and tags) via the in-memory
    /// IOptions&lt;HealthCheckServiceOptions&gt; snapshot — building the service provider and
    /// reading this registration does not execute the check itself, so no real database
    /// connection is required.
    /// </summary>
    public sealed class WhenRegisteringHealthChecks
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
        /// Verifies that AddInfrastructure registers a health check named "database".
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersHealthCheckNamedDatabase()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // Act
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

            // Assert
            healthCheckOptions.Registrations.Should().Contain(registration => registration.Name == "database");
        }

        /// <summary>
        /// Verifies that the "database" health check is tagged "ready", so it is included in
        /// readiness probes (/health/ready, /health) but excluded from the liveness probe
        /// (/health/live), which runs no checks by design.
        /// </summary>
        [Fact]
        public void AddInfrastructure_TagsDatabaseHealthCheckAsReady()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // Act
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
            var databaseRegistration = healthCheckOptions.Registrations.Single(registration => registration.Name == "database");

            // Assert
            databaseRegistration.Tags.Should().Contain("ready");
        }

        /// <summary>
        /// Verifies that HealthCheckService itself resolves from the container once
        /// AddInfrastructure has run, confirming the health checks middleware is fully wired up
        /// (not just the options), independent of the ASP.NET Core hosting pipeline.
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersResolvableHealthCheckService()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // DefaultHealthCheckService requires ILogger<T> to be resolvable; a bare
            // ServiceCollection has no logging provider registered by default, unlike a real
            // ASP.NET Core host (which always registers one), so it is added explicitly here.
            services.AddLogging();
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();

            // Assert
            healthCheckService.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that calling AddInfrastructure does not register duplicate "database" health
        /// check entries, even though AddHealthChecks() may be invoked multiple times across the
        /// composition root (each host's Program.cs adds its own checks afterwards).
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersExactlyOneDatabaseHealthCheck()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // Act
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

            // Assert
            healthCheckOptions.Registrations.Count(registration => registration.Name == "database").Should().Be(1);
        }
    }

    /// <summary>
    /// Tests verifying the n8n workflow-notification registrations added by AddInfrastructure
    /// (issue #37): N8nOptions binding/validation, the named "N8n" HttpClient, IWorkflowNotifier,
    /// and EventReminderBackgroundService.
    /// </summary>
    public sealed class WhenRegisteringWorkflowNotifications
    {
        private IConfiguration CreateValidConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
                })
                .Build();

        /// <summary>
        /// Verifies that IWorkflowNotifier resolves to an N8nWorkflowNotifier instance.
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersN8nWorkflowNotifierAsIWorkflowNotifier()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();
            services.AddLogging();

            // Act
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var notifier = scope.ServiceProvider.GetService<IWorkflowNotifier>();

            // Assert
            notifier.Should().BeOfType<N8nWorkflowNotifier>();
        }

        /// <summary>
        /// Verifies that IWorkflowNotifier is registered with Scoped lifetime, consistent with
        /// IApplicationDbContext (both resolved once per request/scope).
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersIWorkflowNotifierAsScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IWorkflowNotifier));
            descriptor.Should().NotBeNull();
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Verifies that EventReminderBackgroundService is registered as an IHostedService, so it
        /// starts automatically with the application host.
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersEventReminderBackgroundServiceAsHostedService()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            services.Should().Contain(sd =>
                sd.ServiceType == typeof(IHostedService) &&
                sd.ImplementationType == typeof(EventReminderBackgroundService));
        }

        /// <summary>
        /// Verifies that N8nOptions binds from the "Notifications:N8n" configuration section.
        /// </summary>
        [Fact]
        public void AddInfrastructure_BindsN8nOptionsFromConfiguredSection()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
                    { "Notifications:N8n:Enabled", "true" },
                    { "Notifications:N8n:RegistrationConfirmedWebhookUrl", "https://n8n.example.com/webhook/registration-confirmed" },
                    { "Notifications:N8n:EventUpdatedWebhookUrl", "https://n8n.example.com/webhook/event-updated" },
                    { "Notifications:N8n:EventCancelledWebhookUrl", "https://n8n.example.com/webhook/event-cancelled" },
                    { "Notifications:N8n:EventReminderWebhookUrl", "https://n8n.example.com/webhook/event-reminder" },
                    { "Notifications:N8n:WebhookToken", "shared-secret-token" }
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<N8nOptions>>().Value;

            // Assert
            options.Enabled.Should().BeTrue();
            options.WebhookToken.Should().Be("shared-secret-token");
        }

        /// <summary>
        /// Verifies that an invalid N8nOptions configuration (enabled but missing required
        /// values) fails ValidateOnStart, so a misconfigured production deployment fails fast
        /// instead of silently never sending notifications.
        /// </summary>
        [Fact]
        public void AddInfrastructure_WhenN8nEnabledWithMissingValues_FailsValidateOnStart()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
                    { "Notifications:N8n:Enabled", "true" }
                })
                .Build();

            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var act = () => serviceProvider.GetRequiredService<IOptions<N8nOptions>>().Value;

            // Assert
            act.Should().Throw<OptionsValidationException>();
        }

        /// <summary>
        /// Verifies that the named "N8n" HttpClient is resolvable via IHttpClientFactory, which
        /// N8nWorkflowNotifier depends on to send outbound requests.
        /// </summary>
        [Fact]
        public void AddInfrastructure_RegistersNamedN8nHttpClient()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateValidConfiguration();

            // Act
            services.AddInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Assert
            var act = () => httpClientFactory.CreateClient("N8n");
            act.Should().NotThrow();
        }
    }
}
