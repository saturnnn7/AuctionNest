using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class WatchListConfiguration : IEntityTypeConfiguration<WatchList>
{
    public void Configure(EntityTypeBuilder<WatchList> builder)
    {
        builder.ToTable("watch_lists");

        builder.HasKey(w => w.Id);

        // A user cannot add the same auction to the watchlist twice
        builder.HasIndex(w => new { w.UserId, w.AuctionId })
            .IsUnique()
            .HasDatabaseName("ix_watch_lists_user_id_auction_id");
    }
}