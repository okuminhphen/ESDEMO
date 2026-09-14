using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Common.Security;
using ESDEMO.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ESDEMO.Infrastructure.Identity;

public sealed class IdentityAuthService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    ITokenService tokens,
    IPasswordHasher<ApplicationUser> passwordHasher,
    DummyPasswordHash dummyPassword,
    IOptions<JwtOptions> options,
    TimeProvider clock) : IAuthService
{
    public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        if (displayName.Length < 2)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["DisplayName"] = ["Display name must contain at least two characters after trimming."]
            });
        }

        try
        {
            return await InTransactionAsync(async () =>
            {
                var user = new ApplicationUser
                {
                    Email = request.Email.Trim(),
                    UserName = request.Email.Trim(),
                    DisplayName = displayName,
                    EmailConfirmed = false
                };
                var result = await users.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
                    {
                        throw RegistrationConflict();
                    }

                    // Do not expose Identity errors containing submitted values.
                    throw new RequestValidationException(new Dictionary<string, string[]>
                    {
                        [result.Errors.All(error => error.Code.StartsWith("Password", StringComparison.Ordinal)) ? "Password" : "Email"] =
                            [result.Errors.All(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
                                ? "Password must have 12-128 characters, uppercase, lowercase, a digit, a symbol and at least four distinct characters."
                                : "This email address is not supported."]
                    });
                }

                EnsureSucceeded(await users.AddToRoleAsync(user, ApplicationRoleNames.Customer));
                return await ToUserDtoAsync(user);
            }, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "EmailIndex" or "UserNameIndex" })
        {
            throw RegistrationConflict();
        }
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var normalizedEmail = users.NormalizeEmail(request.Email.Trim());
        var userId = await db.Users.AsNoTracking().Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => (Guid?)user.Id).SingleOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            passwordHasher.VerifyHashedPassword(dummyPassword.User, dummyPassword.Hash, request.Password);
            throw new AuthenticationFailedException();
        }

        var response = await InTransactionAsync<TokenResponseDto?>(async () =>
        {
            var user = await LockUserAsync(userId.Value, cancellationToken);
            if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
            {
                passwordHasher.VerifyHashedPassword(dummyPassword.User, dummyPassword.Hash, request.Password);
                return null;
            }

            if (!await users.CheckPasswordAsync(user, request.Password))
            {
                EnsureSucceeded(await users.AccessFailedAsync(user));
                return null; // Commit the failed-attempt counter before returning 401.
            }

            EnsureSucceeded(await users.ResetAccessFailedCountAsync(user));
            return await IssueTokensAsync(user, Guid.NewGuid(),
                clock.GetUtcNow().AddDays(options.Value.RefreshTokenDays), cancellationToken);
        }, cancellationToken);

        return response ?? throw new AuthenticationFailedException();
    }

    public async Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var ownerId = await db.RefreshSessions.AsNoTracking().Where(session => session.TokenHash == hash)
            .Select(session => (Guid?)session.UserId).SingleOrDefaultAsync(cancellationToken);
        if (ownerId is null) { throw new AuthenticationFailedException(); }

        var response = await InTransactionAsync<TokenResponseDto?>(async () =>
        {
            // Every login/refresh/logout for a user takes this lock first. A competing
            // request reads the committed rotation before it can issue another token.
            var user = await LockUserAsync(ownerId.Value, cancellationToken);
            var session = await db.RefreshSessions.SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
            if (user is null || session is null) { return null; }

            if (session.RevokedAt is not null || session.ExpiresAt <= clock.GetUtcNow()
                || !user.IsActive || await users.IsLockedOutAsync(user))
            {
                await RevokeFamilyAsync(session.UserId, session.FamilyId, cancellationToken);
                return null; // Revocation must commit, even though the response is 401.
            }

            session.RevokedAt = clock.GetUtcNow();
            var replacementId = Guid.NewGuid();
            var result = await IssueTokensAsync(user, session.FamilyId, session.ExpiresAt, cancellationToken, replacementId);
            session.ReplacedById = replacementId;
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }, cancellationToken);

        return response ?? throw new AuthenticationFailedException();
    }

    public async Task LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var session = await db.RefreshSessions.AsNoTracking().SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
        if (session is null) { return; }

        await InTransactionAsync(async () =>
        {
            await LockUserAsync(session.UserId, cancellationToken);
            await RevokeFamilyAsync(session.UserId, session.FamilyId, cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<UserResponseDto> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
        {
            throw new AuthenticationFailedException();
        }
        return await ToUserDtoAsync(user);
    }

    private async Task<TokenResponseDto> IssueTokensAsync(ApplicationUser user, Guid familyId,
        DateTimeOffset expiresAt, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        var refreshToken = tokens.CreateRefreshToken();
        var dto = await ToUserDtoAsync(user);
        var accessToken = tokens.CreateAccessToken(dto);
        db.RefreshSessions.Add(new RefreshSession
        {
            Id = sessionId ?? Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = tokens.HashRefreshToken(refreshToken),
            CreatedAt = clock.GetUtcNow(),
            ExpiresAt = expiresAt
        });
        await db.SaveChangesAsync(cancellationToken);
        return new TokenResponseDto
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = expiresAt,
            User = dto
        };
    }

    private Task<ApplicationUser?> LockUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.FromSqlInterpolated($"SELECT * FROM \"AspNetUsers\" WHERE \"Id\" = {userId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<int> RevokeFamilyAsync(Guid userId, Guid familyId, CancellationToken cancellationToken) =>
        db.RefreshSessions.Where(session => session.UserId == userId
                && session.FamilyId == familyId && session.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt,
                (DateTimeOffset?)clock.GetUtcNow()), cancellationToken);

    private async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Identity writes share this DbContext; discard stale state before any retry.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task<UserResponseDto> ToUserDtoAsync(ApplicationUser user) =>
        new(user.Id, user.Email!, user.DisplayName, (await users.GetRolesAsync(user)).ToArray());

    private static ConflictException RegistrationConflict() => new("Unable to register with this email.");

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) { throw new InvalidOperationException("Unable to update account security state."); }
    }
}
