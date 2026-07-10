using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Application.Common.Behaviors;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Import.Services;

namespace SportsClubEventManager.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application layer services including MediatR and FluentValidation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // LoggingBehavior is registered before ValidationBehavior so it is the more outer
            // behavior in the pipeline, logging validation failures too, not just handler failures.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register Application-layer services whose only dependencies (IApplicationDbContext,
        // IValidator<T>) already live in this project.
        services.AddScoped<IEventImportValidationService, EventImportValidationService>();

        return services;
    }
}
