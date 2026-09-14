namespace ESDEMO.Application.Auth.Dtos;

public sealed record UserResponseDto(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
