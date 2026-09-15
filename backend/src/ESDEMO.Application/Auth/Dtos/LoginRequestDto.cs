using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Application.Auth.Dtos;

public sealed class LoginRequestDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}
