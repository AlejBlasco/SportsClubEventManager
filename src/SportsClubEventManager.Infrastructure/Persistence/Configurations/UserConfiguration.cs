using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the User entity.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Configures the User entity using Fluent API.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity.</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(u => u.LicenseCategory)
            .HasMaxLength(50);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500);

        builder.Property(u => u.ExternalProviderId)
            .HasMaxLength(256);

        builder.HasIndex(u => u.ExternalProviderId);

        builder.Property(u => u.ProviderName)
            .HasMaxLength(50);

        builder.HasIndex(u => new { u.ProviderName, u.ExternalProviderId })
            .IsUnique()
            .HasFilter("[ExternalProviderId] IS NOT NULL AND [ProviderName] IS NOT NULL");

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.HasIndex(u => u.RefreshToken);

        builder.Property(u => u.RefreshTokenExpiryTime);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.LastLoginAt);

        builder.HasMany(u => u.Registrations)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
