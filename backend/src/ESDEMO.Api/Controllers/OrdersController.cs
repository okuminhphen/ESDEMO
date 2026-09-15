using ESDEMO.Application.Common.Security;
using ESDEMO.Application.Orders.Commands.CreateOrder;
using ESDEMO.Application.Orders.Commands.PayOrder;
using ESDEMO.Application.Orders.Dtos;
using ESDEMO.Application.Orders.Queries.GetMyOrder;
using ESDEMO.Application.Orders.Queries.GetMyOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ESDEMO.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Policy = ApplicationRoleNames.Customer)]
[RequestSizeLimit(16 * 1024)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<OrderPageResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderPageResponseDto>> GetOrders([FromQuery] OrderListRequestDto request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyOrdersQuery(request), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyOrderQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<OrderResponseDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderRequestDto request, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new CreateOrderCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType<OrderResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponseDto>> Pay(Guid id, PayOrderRequestDto request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PayOrderCommand(id, request), cancellationToken));
}
