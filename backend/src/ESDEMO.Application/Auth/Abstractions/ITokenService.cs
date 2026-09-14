using ESDEMO.Application.Auth.Dtos;

namespace ESDEMO.Application.Auth.Abstractions;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(UserResponseDto user);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}

public sealed class AccessTokenResult(string value, DateTimeOffset expiresAt)
{
    public string Value { get; } = value;
    public DateTimeOffset ExpiresAt { get; } = expiresAt;
}
