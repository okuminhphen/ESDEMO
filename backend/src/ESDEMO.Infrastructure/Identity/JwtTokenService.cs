using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ESDEMO.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    public AccessTokenResult CreateAccessToken(UserResponseDto user)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(options.Value.AccessTokenMinutes);
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        ];
        claims.AddRange(user.Roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(options.Value.GetSecurityKey(), SecurityAlgorithms.HmacSha256));
        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
