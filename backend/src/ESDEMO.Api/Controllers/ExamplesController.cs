using ESDEMO.Api.Contracts.Examples;
using Microsoft.AspNetCore.Mvc;

namespace ESDEMO.Api.Controllers;

[ApiController]
[Route("api/examples")]
public sealed class ExamplesController : ControllerBase
{
    [HttpPost("validate-text")]
    [ProducesResponseType<ValidateTextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<ValidateTextResponse> ValidateText(ValidateTextRequest request)
    {
        return Ok(new ValidateTextResponse(request.Text, request.Text.Length));
    }
}
