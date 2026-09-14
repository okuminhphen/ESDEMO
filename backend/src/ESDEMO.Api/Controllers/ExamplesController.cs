using ESDEMO.Api.Contracts.Examples;
using ESDEMO.Application.Examples.Queries.ValidateText;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ESDEMO.Api.Controllers;

[ApiController]
[Route("api/examples")]
public sealed class ExamplesController(ISender sender) : ControllerBase
{
    [HttpPost("validate-text")]
    [ProducesResponseType<ValidateTextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ValidateTextResponse>> ValidateText(
        ValidateTextRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ValidateTextQuery(request.Text),
            cancellationToken);

        return Ok(new ValidateTextResponse(result.Text, result.Length));
    }
}
