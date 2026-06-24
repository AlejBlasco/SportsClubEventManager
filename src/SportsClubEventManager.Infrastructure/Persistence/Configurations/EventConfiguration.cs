using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Event entity.
/// </summary>
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <summary>
    /// Configures the Event entity using Fluent API.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity.</param>
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.HasIndex(e => e.Date);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.MaxCapacity)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        builder.HasMany(e => e.Registrations)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.CurrentRegistrations);
        builder.Ignore(e => e.IsFull);
    }
}
