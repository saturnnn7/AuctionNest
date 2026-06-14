using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .HasMaxLength(30)
            .IsRequired();
        
        builder.Property(u => u.Email)
            .HasMaxLength(254)
            .IsRequired();
        
        builder.Property(u => u.PasswordHash)
            .IsRequired();
        
        builder.Property(u => u.DisplayName)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .IsRequired();
        
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");
        
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("ix_users_username");

        // Soft Delete
        builder.HasQueryFilter(u => u.DeletedAt == null);

        builder.HasMany(u => u.Auctions)
            .WithOne(a => a.Seller)
            .HasForeignKey(a => a.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(u => u.Bids)
            .WithOne(b => b.Bidder)
            .HasForeignKey(b => b.BidderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.WatchList)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}