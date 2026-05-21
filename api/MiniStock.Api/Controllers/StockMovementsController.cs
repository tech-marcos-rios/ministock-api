using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniStock.Application.Common;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Services;

namespace MiniStock.Api.Controllers;

[ApiController]
[Route("api/v1/stock-movements")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly StockMovementService _service;

    public StockMovementsController(StockMovementService service) => _service = service;

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

        if (result.IsFailure)
        {
            if (result.IsNotFound) return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
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
        return Ok(result.Value);
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _service.GetRecentAsync(count, ct);
        return Ok(result.Value);
    }
}
