using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Behaviors;
using MediatR;

namespace ESDEMO.Application.Auth.Commands.Logout;

public sealed class LogoutCommand(LogoutRequestDto data) : IRequest, IValidatedRequest
{
    public LogoutRequestDto Data { get; } = data;
    public object Payload => Data;
}

public sealed class LogoutCommandHandler(IAuthService auth) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        auth.LogoutAsync(request.Data, cancellationToken);
}
