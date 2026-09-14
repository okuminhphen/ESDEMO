using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Behaviors;
using MediatR;

namespace ESDEMO.Application.Auth.Commands.Register;

public sealed class RegisterCommand(RegisterRequestDto data) : IRequest<UserResponseDto>, IValidatedRequest
{
    public RegisterRequestDto Data { get; } = data;
    public object Payload => Data;
}

public sealed class RegisterCommandHandler(IAuthService auth) : IRequestHandler<RegisterCommand, UserResponseDto>
{
    public Task<UserResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        auth.RegisterAsync(request.Data, cancellationToken);
}
