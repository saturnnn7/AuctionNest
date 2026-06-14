using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionNest.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(200);
        
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("ix_categories_slug");

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}