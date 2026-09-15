using ESDEMO.Application.Common.Security;
using ESDEMO.Application.Products.Commands.CreateProduct;
using ESDEMO.Application.Products.Commands.DeleteProduct;
using ESDEMO.Application.Products.Commands.UpdateProduct;
using ESDEMO.Application.Products.Dtos;
using ESDEMO.Application.Products.Queries.GetProduct;
using ESDEMO.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ESDEMO.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = ApplicationRoleNames.Admin)]
[RequestSizeLimit(32 * 1024)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
public sealed class AdminProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProductPageResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductPageResponseDto>> GetProducts(
        [FromQuery] ProductListRequestDto request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProductsQuery(request), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProductQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequestDto request, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new CreateProductCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponseDto>> Update(
        Guid id, UpdateProductRequestDto request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateProductCommand(id, request), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] DeleteProductRequestDto request, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id, request), cancellationToken);
        return NoContent();
    }
}
