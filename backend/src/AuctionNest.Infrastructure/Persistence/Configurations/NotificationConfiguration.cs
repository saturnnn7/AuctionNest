using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Payload)
            .HasMaxLength(2000);

        // Query for a user's unread notifications — the most frequent
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("ix_notifications_user_id_is_read");
    }
}