using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Application.Products.Dtos;

public sealed class DeleteProductRequestDto
{
    [Required, Range(typeof(uint), "1", "4294967295", ParseLimitsInInvariantCulture = true)]
    public uint? Version { get; init; }
}
