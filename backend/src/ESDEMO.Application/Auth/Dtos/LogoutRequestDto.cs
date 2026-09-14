using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Application.Auth.Dtos;

public sealed class LogoutRequestDto
{
    [Required, RegularExpression(@"^[A-Za-z0-9_-]{86}$", ErrorMessage = "Invalid refresh token format.")]
    public string RefreshToken { get; init; } = string.Empty;
}
