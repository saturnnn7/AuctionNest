using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(b => b.AuctionId)
            .HasDatabaseName("ix_bids_auction_id");

        builder.HasIndex(b => new { b.AuctionId, b.IsWinning })
            .HasDatabaseName("ix_bids_auction_id_is_winning");
    }
}