using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Behaviors;
using MediatR;

namespace ESDEMO.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommand(RefreshTokenRequestDto data) : IRequest<TokenResponseDto>, IValidatedRequest
{
    public RefreshTokenRequestDto Data { get; } = data;
    public object Payload => Data;
}

public sealed class RefreshTokenCommandHandler(IAuthService auth) : IRequestHandler<RefreshTokenCommand, TokenResponseDto>
{
    public Task<TokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        auth.RefreshAsync(request.Data, cancellationToken);
}
