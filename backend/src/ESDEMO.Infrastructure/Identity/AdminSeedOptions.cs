using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Infrastructure.Identity;

public sealed class AdminSeedOptions
{
    public const string SectionName = "SeedAdmin";

    public bool Enabled { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;
}
