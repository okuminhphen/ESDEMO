using ESDEMO.Application.Products.Dtos;
using ESDEMO.Application.Products.Queries.GetProduct;
using ESDEMO.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ESDEMO.Api.Controllers;

[ApiController]
[Route("api/products")]
[AllowAnonymous]
[RequestSizeLimit(8 * 1024)]
public sealed class PublicProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PublicProductPageResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicProductPageResponseDto>> GetProducts(
        [FromQuery] ProductListRequestDto request, CancellationToken cancellationToken)
    {
        var products = await sender.Send(new GetProductsQuery(new ProductListRequestDto
        {
            Page = request.Page, PageSize = request.PageSize, Search = request.Search,
            IsActive = true, IncludeDeleted = false
        }), cancellationToken);
        return Ok(new PublicProductPageResponseDto(products.Items.Select(ToPublic).ToList(),
            products.TotalCount, products.Page, products.PageSize));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PublicProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicProductResponseDto>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new GetProductQuery(id), cancellationToken);
        if (product.DeletedAt is not null || !product.IsActive)
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Product was not found." });
        return Ok(ToPublic(product));
    }

    private static PublicProductResponseDto ToPublic(ProductResponseDto product) => new(
        product.Id, product.Sku, product.Name, product.Description, product.Price, product.StockQuantity > 0);
}
