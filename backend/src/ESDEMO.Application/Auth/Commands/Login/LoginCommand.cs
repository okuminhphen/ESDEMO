using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Behaviors;
using MediatR;

namespace ESDEMO.Application.Auth.Commands.Login;

public sealed class LoginCommand(LoginRequestDto data) : IRequest<TokenResponseDto>, IValidatedRequest
{
    public LoginRequestDto Data { get; } = data;
    public object Payload => Data;
}

public sealed class LoginCommandHandler(IAuthService auth) : IRequestHandler<LoginCommand, TokenResponseDto>
{
    public Task<TokenResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        auth.LoginAsync(request.Data, cancellationToken);
}
