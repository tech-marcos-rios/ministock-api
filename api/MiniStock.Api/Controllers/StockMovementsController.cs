using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniStock.Api.Extensions;
using MiniStock.Application.Common;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;

namespace MiniStock.Api.Controllers;

[ApiController]
[Route("api/v1/stock-movements")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _service;

    public StockMovementsController(IStockMovementService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Register([FromBody] RegisterMovementRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException());

        var result = await _service.RegisterAsync(request, userId, ct);
        return result.ToActionResult(value => StatusCode(StatusCodes.Status201Created, value));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StockMovementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? productId = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetPagedAsync(page, pageSize, productId, ct);
        return result.ToActionResult();
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _service.GetRecentAsync(count, ct);
        return result.ToActionResult();
    }
}
