using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Exceptions;
using MediatR;

namespace ESDEMO.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<UserResponseDto>;

public sealed class GetCurrentUserQueryHandler(IAuthService auth, ICurrentUser currentUser)
    : IRequestHandler<GetCurrentUserQuery, UserResponseDto>
{
    public Task<UserResponseDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        auth.GetUserAsync(currentUser.UserId ?? throw new AuthenticationFailedException(), cancellationToken);
}
