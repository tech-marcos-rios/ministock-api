using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MiniStock.Domain.Entities;
using MiniStock.Infrastructure.Persistence;
using MiniStock.Infrastructure.Persistence.Repositories;

namespace MiniStock.Tests.Infrastructure;

public class CategoryRepositoryTests
{
    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetActiveProductCountAsync_CountsOnlyActiveProducts()
    {
        await using var db = BuildContext();
        var category = Category.Create("Electrónica");
        db.Categories.Add(category);

        var active1 = Product.Create("A", "SKU-A", 10m, 1, 0, category.Id);
        var active2 = Product.Create("B", "SKU-B", 10m, 1, 0, category.Id);
        var inactive = Product.Create("C", "SKU-C", 10m, 1, 0, category.Id);
        inactive.Deactivate();
        db.Products.AddRange(active1, active2, inactive);
        await db.SaveChangesAsync();

        var repo = new CategoryRepository(db);
        var count = await repo.GetActiveProductCountAsync(category.Id, default);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetActiveProductCountsAsync_ReturnsCountsForMultipleCategories()
    {
        await using var db = BuildContext();
        var withProducts = Category.Create("Con productos");
        var empty = Category.Create("Vacía");
        db.Categories.AddRange(withProducts, empty);
        db.Products.Add(Product.Create("A", "SKU-A", 10m, 1, 0, withProducts.Id));
        await db.SaveChangesAsync();

        var repo = new CategoryRepository(db);
        var counts = await repo.GetActiveProductCountsAsync([withProducts.Id, empty.Id], default);

        counts.Should().ContainKey(withProducts.Id).WhoseValue.Should().Be(1);
        // Categorías sin productos activos no aparecen en el diccionario (contrato documentado).
        counts.Should().NotContainKey(empty.Id);
    }

    // ExistsByNameAsync (EF.Functions.ILike) no se testea acá: ILike es una función
    // específica de Npgsql que el proveedor InMemory no sabe traducir — tirar
    // "'ILike' method is not supported" en vez de ejecutar la query. Se verificó
    // manualmente contra Postgres real (ver verificación en la sección 7 del README).
}
