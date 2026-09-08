using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniStock.Api.Extensions;
using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.Interfaces;

namespace MiniStock.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var result = await _productService.GetPagedAsync(page, pageSize, search, categoryId, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _productService.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateAsync(request, ct);
        return result.ToActionResult(value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.UpdateAsync(id, request, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _productService.DeactivateAsync(id, ct);
        return result.ToActionResult();
    }
}
