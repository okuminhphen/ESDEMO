namespace ESDEMO.Application.Auth.Dtos;

// A class intentionally avoids record-generated ToString() exposing credentials.
public sealed class TokenResponseDto
{
    public required string AccessToken { get; init; }
    public string TokenType => "Bearer";
    public required DateTimeOffset AccessTokenExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
    public required UserResponseDto User { get; init; }
}
