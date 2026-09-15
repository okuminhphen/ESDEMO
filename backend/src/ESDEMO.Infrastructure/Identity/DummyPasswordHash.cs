using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ESDEMO.Infrastructure.Identity;

// Pay password-verification cost for unknown accounts without retaining a real credential.
public sealed class DummyPasswordHash
{
    public ApplicationUser User { get; } = new() { DisplayName = "Unknown" };
    public string Hash { get; }

    public DummyPasswordHash(IOptions<PasswordHasherOptions> options)
    {
        Hash = new PasswordHasher<ApplicationUser>(options)
            .HashPassword(User, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }
}
