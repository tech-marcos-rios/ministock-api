using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.SKU).IsUnique();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
