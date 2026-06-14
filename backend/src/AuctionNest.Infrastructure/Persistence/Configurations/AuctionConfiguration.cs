using AuctionNest.Domain.Entities;
using AuctionNest.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(a => a.Description)
            .HasMaxLength(5000)
            .IsRequired();
        
        builder.Property(a => a.ImageUrl)
            .HasMaxLength(500);
        
        builder.Property(a => a.StartPrice)
            .HasPrecision(18, 2);
        
        builder.Property(a => a.CurrentPrice)
            .HasPrecision(18, 2);
        
        builder.Property(a => a.ReservePrice)
            .HasPrecision(18, 2);
        
        builder.Property(a => a.BuyItNowPrice)
            .HasPrecision(18, 2);
        
        builder.Property(a => a.MinBidIncrement)
            .HasPrecision(18, 2);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Navigation(a => a.Bids)
            .HasField("_bids")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        
        builder.HasIndex(a => new { a.Status, a.EndsAt })
            .HasDatabaseName("ix_auctions_status_ends_at");
        
        builder.HasIndex(a => a.SellerId)
            .HasDatabaseName("ix_auctions_seller_id");
        
        builder.HasIndex(a => a.CategoryId)
            .HasDatabaseName("ix_auctions_category_id");

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Auctions)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Bids)
            .WithOne(b => b.Auction)
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.WatchList)
            .WithOne(w => w.Auction)
            .HasForeignKey(w => w.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}