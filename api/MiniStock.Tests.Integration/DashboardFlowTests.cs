using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MiniStock.Application.DTOs.Dashboard;

namespace MiniStock.Tests.Integration;

/// <summary>Verifica que los endpoints de agregación del dashboard respondan sobre datos reales.</summary>
[Collection(IntegrationCollection.Name)]
public class DashboardFlowTests
{
    private readonly IntegrationTestFixture _factory;

    public DashboardFlowTests(IntegrationTestFixture factory) => _factory = factory;

    [Fact]
    public async Task GetSummary_ReturnsAggregatesFromRealDatabase()
    {
        var token = await _factory.GetAdminTokenAsync();
        var client = _factory.CreateClient().WithBearerToken(token);

        var response = await client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        summary!.TotalProducts.Should().BeGreaterThanOrEqualTo(0);
        summary.TotalCategories.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetSummary_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
