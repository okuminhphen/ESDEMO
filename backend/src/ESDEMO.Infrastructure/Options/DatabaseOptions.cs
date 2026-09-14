using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Infrastructure.Options;

public sealed class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
