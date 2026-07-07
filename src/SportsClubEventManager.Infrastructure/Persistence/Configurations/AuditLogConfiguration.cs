using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the AuditLog entity.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <summary>
    /// Configures the AuditLog entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.PerformedByUserId)
            .IsRequired();

        builder.Property(a => a.TargetUserId)
            .IsRequired();

        builder.Property(a => a.TargetUserEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        // Configure relationship with PerformedByUser
        // ON DELETE NO ACTION to preserve audit trail even if the admin user is deleted
        builder.HasOne(a => a.PerformedByUser)
            .WithMany()
            .HasForeignKey(a => a.PerformedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Create indexes for common query patterns
        builder.HasIndex(a => a.PerformedByUserId)
            .HasDatabaseName("IX_AuditLogs_PerformedByUserId");

        builder.HasIndex(a => a.TargetUserId)
            .HasDatabaseName("IX_AuditLogs_TargetUserId");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");
    }
}
