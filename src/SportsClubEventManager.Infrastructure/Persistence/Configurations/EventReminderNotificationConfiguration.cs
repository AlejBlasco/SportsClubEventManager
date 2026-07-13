using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the EventReminderNotification entity.
/// </summary>
internal sealed class EventReminderNotificationConfiguration : IEntityTypeConfiguration<EventReminderNotification>
{
    /// <summary>
    /// Configures the EventReminderNotification entity using Fluent API.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity.</param>
    public void Configure(EntityTypeBuilder<EventReminderNotification> builder)
    {
        builder.ToTable("EventReminderNotifications");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.EventId).IsRequired();
        builder.Property(r => r.IntervalHours).IsRequired();
        builder.Property(r => r.SentAtUtc).IsRequired();

        // Second, database-level barrier against duplicate reminders (the primary one is the
        // "has a matching row" check in EventReminderBackgroundService itself).
        builder.HasIndex(r => new { r.EventId, r.IntervalHours }).IsUnique();

        builder.HasOne(r => r.Event)
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
