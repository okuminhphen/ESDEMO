using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace ESDEMO.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Required]
    public string SigningKey { get; set; } = string.Empty;
    [Range(1, 30)]
    public int AccessTokenMinutes { get; set; } = 10;
    [Range(1, 30)]
    public int RefreshTokenDays { get; set; } = 7;

    public bool HasValidSigningKey()
    {
        try { return Convert.FromBase64String(SigningKey).Length >= 32; }
        catch (FormatException) { return false; }
    }

    public SymmetricSecurityKey GetSecurityKey() => new(Convert.FromBase64String(SigningKey));

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = GetSecurityKey(),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidTypes = ["JWT"],
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
}
