using ESDEMO.Api.Security;
using ESDEMO.Application.Auth.Commands.Login;
using ESDEMO.Application.Auth.Commands.Logout;
using ESDEMO.Application.Auth.Commands.RefreshToken;
using ESDEMO.Application.Auth.Commands.Register;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ESDEMO.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
[RequestSizeLimit(16 * 1024)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new RegisterCommand(request), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new LoginCommand(request), cancellationToken));

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenRequestDto request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RefreshTokenCommand(request), cancellationToken));

    // The refresh token itself proves possession; logout must also work after access-token expiry.
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(request), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponseDto>> Me(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentUserQuery(), cancellationToken));
}
