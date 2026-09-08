using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MiniStock.Application.DTOs.Categories;
using MiniStock.Application.DTOs.Products;

namespace MiniStock.Tests.Integration;

/// <summary>
/// CRUD de productos vía HTTP contra Postgres real, con foco en lo que un test unitario con
/// mocks no puede probar: el RBAC de verdad (rol resuelto desde el JWT emitido por la API,
/// no simulado) y el bug histórico de status code (categoría inexistente daba 409 antes del
/// fix — acá se prueba contra el pipeline completo, no contra el servicio aislado).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ProductsFlowTests
{
    private readonly IntegrationTestFixture _factory;

    public ProductsFlowTests(IntegrationTestFixture factory) => _factory = factory;

    private async Task<HttpClient> AdminClientAsync()
    {
        var token = await _factory.GetAdminTokenAsync();
        return _factory.CreateClient().WithBearerToken(token);
    }

    [Fact]
    public async Task Create_GetById_Update_Deactivate_FullFlow_Succeeds()
    {
        var admin = await AdminClientAsync();

        var category = await CreateCategoryAsync(admin, $"Categoría {Guid.NewGuid():N}");

        var createResponse = await admin.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(
            "Producto de test", $"SKU-{Guid.NewGuid():N}"[..12], 99.90m, 10, 2, category.Id, "Descripción"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var getResponse = await admin.GetAsync($"/api/v1/products/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await admin.PutAsJsonAsync($"/api/v1/products/{created.Id}", new UpdateProductRequest(
            "Producto editado", "Nueva desc", 149.90m, 5, category.Id));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Name.Should().Be("Producto editado");

        var deactivateResponse = await admin.DeleteAsync($"/api/v1/products/{created.Id}");
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_NonExistentCategory_ReturnsNotFound()
    {
        // Este es el bug real que motivó ErrorType: antes devolvía 409, no 404.
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(
            "Producto huérfano", $"SKU-{Guid.NewGuid():N}"[..12], 10m, 1, 0, Guid.NewGuid(), null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_DuplicateSku_ReturnsConflict()
    {
        var admin = await AdminClientAsync();
        var category = await CreateCategoryAsync(admin, $"Categoría {Guid.NewGuid():N}");
        var sku = $"SKU-{Guid.NewGuid():N}"[..12];
        var payload = new CreateProductRequest("Producto", sku, 10m, 1, 0, category.Id, null);

        await admin.PostAsJsonAsync("/api/v1/products", payload);
        var second = await admin.PostAsJsonAsync("/api/v1/products", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deactivate_AsNonAdminUser_ReturnsForbidden()
    {
        var admin = await AdminClientAsync();
        var category = await CreateCategoryAsync(admin, $"Categoría {Guid.NewGuid():N}");
        var createResponse = await admin.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(
            "Producto protegido", $"SKU-{Guid.NewGuid():N}"[..12], 10m, 1, 0, category.Id, null));
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var userToken = await AuthTestHelper.RegisterAndLoginAsUserAsync(_factory.CreateClient());
        var userClient = _factory.CreateClient().WithBearerToken(userToken);

        var deleteResponse = await userClient.DeleteAsync($"/api/v1/products/{product!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/categories", new CreateCategoryRequest(name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }
}
