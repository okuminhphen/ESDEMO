using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Api.Security;

public sealed class AuthRateLimitOptions
{
    public const string PolicyName = "auth";
    [Range(1, 1000)]
    public int PermitLimit { get; set; } = 30;
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
