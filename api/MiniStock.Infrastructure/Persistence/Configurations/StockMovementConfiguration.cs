using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Notes).HasMaxLength(300);
        builder.Property(m => m.Type).HasConversion<string>();
        builder.HasOne(m => m.Product).WithMany(p => p.Movements).HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.CreatedBy).WithMany().HasForeignKey(m => m.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
