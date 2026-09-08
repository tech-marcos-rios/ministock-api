using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MiniStock.Domain.Entities;
using MiniStock.Infrastructure.Persistence;
using MiniStock.Infrastructure.Persistence.Repositories;

namespace MiniStock.Tests.Infrastructure;

public class ProductRepositoryTests
{
    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Category SeedCategory(AppDbContext db)
    {
        var category = Category.Create("Electrónica");
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    [Fact]
    public async Task ExistsBySkuAsync_ActiveProductWithSku_ReturnsTrue()
    {
        await using var db = BuildContext();
        var category = SeedCategory(db);
        db.Products.Add(Product.Create("Laptop", "LAP-001", 999m, 10, 3, category.Id));
        await db.SaveChangesAsync();

        var repo = new ProductRepository(db);
        var exists = await repo.ExistsBySkuAsync("LAP-001", default);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsBySkuAsync_DeactivatedProductWithSku_ReturnsFalse()
    {
        // Un producto dado de baja es lógicamente "borrado" — su SKU debe poder reutilizarse.
        await using var db = BuildContext();
        var category = SeedCategory(db);
        var product = Product.Create("Laptop", "LAP-001", 999m, 10, 3, category.Id);
        product.Deactivate();
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var repo = new ProductRepository(db);
        var exists = await repo.ExistsBySkuAsync("LAP-001", default);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsBySkuAsync_UnknownSku_ReturnsFalse()
    {
        await using var db = BuildContext();

        var repo = new ProductRepository(db);
        var exists = await repo.ExistsBySkuAsync("NOPE", default);

        exists.Should().BeFalse();
    }
}
