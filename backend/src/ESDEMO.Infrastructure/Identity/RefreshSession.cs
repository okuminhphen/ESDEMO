namespace ESDEMO.Infrastructure.Identity;

public sealed class RefreshSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid FamilyId { get; init; }
    // SHA-256 hexadecimal digest of a high-entropy token; never store the raw token.
    public required string TokenHash { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }
}
