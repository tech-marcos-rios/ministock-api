using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniStock.Api.Extensions;
using MiniStock.Application.DTOs.Dashboard;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;

namespace MiniStock.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _service.GetSummaryAsync(ct);
        return result.ToActionResult();
    }

    [HttpGet("stock-by-category")]
    [ProducesResponseType(typeof(IReadOnlyList<StockByCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockByCategory(CancellationToken ct)
    {
        var result = await _service.GetStockByCategoryAsync(ct);
        return result.ToActionResult();
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock(CancellationToken ct)
    {
        var result = await _service.GetLowStockProductsAsync(ct);
        return result.ToActionResult();
    }

    [HttpGet("recent-movements")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentMovements([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _service.GetRecentMovementsAsync(count, ct);
        return result.ToActionResult();
    }
}
