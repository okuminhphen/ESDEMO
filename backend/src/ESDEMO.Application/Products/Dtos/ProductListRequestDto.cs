using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Application.Products.Dtos;

public sealed class ProductListRequestDto : IValidatableObject
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [StringLength(200)]
    public string? Search { get; init; }

    public bool? IsActive { get; init; }
    public bool IncludeDeleted { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Search?.Contains('\0') == true)
        {
            yield return new ValidationResult("Search cannot contain null characters.", [nameof(Search)]);
        }
    }
}
