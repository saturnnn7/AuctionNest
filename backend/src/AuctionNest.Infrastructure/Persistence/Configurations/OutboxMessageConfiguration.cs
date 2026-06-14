using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        // Hangfire job reads only unprocessed messages
        builder.HasIndex(o => o.ProcessedAt)
            .HasDatabaseName("ix_outbox_messages_processed_at");
    }
}