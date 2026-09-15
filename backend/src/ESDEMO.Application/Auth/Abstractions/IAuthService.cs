using ESDEMO.Application.Auth.Dtos;

namespace ESDEMO.Application.Auth.Abstractions;

public interface IAuthService
{
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
    Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken);
    Task<UserResponseDto> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}
