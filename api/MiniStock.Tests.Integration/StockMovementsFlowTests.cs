using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MiniStock.Application.DTOs.Categories;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Domain.Entities;

namespace MiniStock.Tests.Integration;

/// <summary>
/// Registrar movimientos de stock vía HTTP — en particular la regla de negocio más
/// sensible del sistema (nunca stock negativo), que en el pipeline real depende de que
/// movimiento + actualización de stock se persistan atómicamente (mismo SaveChanges).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class StockMovementsFlowTests
{
    private readonly IntegrationTestFixture _factory;

    public StockMovementsFlowTests(IntegrationTestFixture factory) => _factory = factory;

    private async Task<(HttpClient Client, ProductResponse Product)> SeedProductAsync(int initialStock = 5)
    {
        var token = await _factory.GetAdminTokenAsync();
        var client = _factory.CreateClient().WithBearerToken(token);

        var categoryResponse = await client.PostAsJsonAsync(
            "/api/v1/categories", new CreateCategoryRequest($"Categoría {Guid.NewGuid():N}", null));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var productResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(
            "Producto con stock", $"SKU-{Guid.NewGuid():N}"[..12], 50m, initialStock, 1, category!.Id, null));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();

        return (client, product!);
    }

    [Fact]
    public async Task RegisterEntry_UpdatesProductStock()
    {
        var (client, product) = await SeedProductAsync(initialStock: 5);

        var response = await client.PostAsJsonAsync("/api/v1/stock-movements", new RegisterMovementRequest(
            product.Id, 10, MovementType.Entry, "Compra"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var updatedProduct = await (await client.GetAsync($"/api/v1/products/{product.Id}"))
            .Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct!.Stock.Should().Be(15);
    }

    [Fact]
    public async Task RegisterExit_InsufficientStock_ReturnsBadRequest()
    {
        var (client, product) = await SeedProductAsync(initialStock: 3);

        var response = await client.PostAsJsonAsync("/api/v1/stock-movements", new RegisterMovementRequest(
            product.Id, 100, MovementType.Exit, "Venta imposible"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // El stock no debe haberse tocado — la validación corta antes de aplicar el delta.
        var unchangedProduct = await (await client.GetAsync($"/api/v1/products/{product.Id}"))
            .Content.ReadFromJsonAsync<ProductResponse>();
        unchangedProduct!.Stock.Should().Be(3);
    }

    [Fact]
    public async Task RegisterMovement_NonExistentProduct_ReturnsNotFound()
    {
        var token = await _factory.GetAdminTokenAsync();
        var client = _factory.CreateClient().WithBearerToken(token);

        var response = await client.PostAsJsonAsync("/api/v1/stock-movements", new RegisterMovementRequest(
            Guid.NewGuid(), 1, MovementType.Entry, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
