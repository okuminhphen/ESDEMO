using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Application.Products.Dtos;

public abstract class ProductInputDto : IValidatableObject
{
    [Required, StringLength(64)]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [Required, Range(typeof(decimal), "0", "999999999999999999", ParseLimitsInInvariantCulture = true)]
    public decimal? Price { get; init; }

    [Required, Range(0, int.MaxValue)]
    public int? StockQuantity { get; init; }

    [Required]
    public bool? IsActive { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var sku = Sku.Trim();
        if (sku.Length == 0 || !char.IsAsciiLetterOrDigit(sku[0])
            || sku.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            yield return new ValidationResult("SKU must start with a letter or digit and contain only ASCII letters, digits, underscores or hyphens.", [nameof(Sku)]);
        }

        if (Name.Trim().Length < 2)
        {
            yield return new ValidationResult("Name must contain at least two characters after trimming.", [nameof(Name)]);
        }

        if (Price is { } price && price != decimal.Truncate(price))
        {
            yield return new ValidationResult("Price must be a whole number of VND.", [nameof(Price)]);
        }

        if (Name.Contains('\0') || Description?.Contains('\0') == true)
        {
            yield return new ValidationResult("Text cannot contain null characters.", [nameof(Name), nameof(Description)]);
        }
    }
}
