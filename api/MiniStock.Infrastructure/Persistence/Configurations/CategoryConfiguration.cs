using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        // Único a nivel DB, no solo el chequeo ExistsByNameAsync en el servicio: sin esto,
        // dos requests concurrentes de creación podían pasar ambos el chequeo antes de que
        // cualquiera hiciera SaveChanges (race condition clásica) y quedar dos categorías
        // con el mismo nombre.
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(300);
    }
}
