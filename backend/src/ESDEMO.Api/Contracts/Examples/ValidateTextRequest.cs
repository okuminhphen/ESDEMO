using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Api.Contracts.Examples;

public sealed class ValidateTextRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Text { get; init; } = string.Empty;
}
