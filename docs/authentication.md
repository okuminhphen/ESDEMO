# Backend authentication

## Implemented scope

The backend exposes registration, login, rotating refresh tokens, logout and the current-user endpoint. Public registration creates only Customer; Admin is provisioned by the explicit database initializer. Authentication uses ASP.NET Core Identity and signed JWTs. Existing tables support this feature, so no additional migration is needed.

This is a first-party application auth API, not an OAuth/OIDC authorization server. Frontend/BFF sessions, email verification, password recovery and Admin MFA are still pending. Public registration currently permits login before email verification; EmailConfirmed remains false. Do not treat that email address as verified.

## Contracts and code placement

| Endpoint | Request DTO | Success response |
| --- | --- | --- |
| POST /api/auth/register | RegisterRequestDto: email, displayName, password, confirmPassword | 201 UserResponseDto |
| POST /api/auth/login | LoginRequestDto: email, password | 200 TokenResponseDto |
| POST /api/auth/refresh | RefreshTokenRequestDto: refreshToken | 200 TokenResponseDto |
| POST /api/auth/logout | LogoutRequestDto: refreshToken | 204, no body |
| GET /api/auth/me | Authorization: Bearer accessToken | 200 UserResponseDto |

UserResponseDto contains id, email, displayName and roles. TokenResponseDto contains accessToken, tokenType, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt and user. Expiry timestamps are UTC. Identity entities, password hashes and security stamps are never returned.

DTOs live in Application/Auth/Dtos. Application/Auth/Commands contains Register, Login, RefreshToken and Logout; Queries/GetCurrentUser contains the query. Every use case goes through MediatR. API model validation and the MediatR validation behavior share the DTO DataAnnotations, so dispatching a command outside HTTP also validates its input. Identity validates password complexity and database constraints protect uniqueness.

AuthController handles HTTP only. IAuthService and ITokenService are Application abstractions implemented in Infrastructure/Identity. CurrentUser is an API adapter for the authenticated sub claim; /me never accepts a client-supplied user ID.

## Local configuration

Root .env is ignored by Git. Set:

```dotenv
Jwt__Issuer=esdemo-api
Jwt__Audience=esdemo-client
Jwt__SigningKey=<random-base64-key>
Jwt__AccessTokenMinutes=10
Jwt__RefreshTokenDays=7
AuthRateLimit__PermitLimit=30
AuthRateLimit__WindowSeconds=60
```

Generate the key in PowerShell 7:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

Paste the output into Jwt__SigningKey in .env. Never paste it into tracked files, screenshots or messages. The API requires Base64 encoding of at least 32 random bytes. Empty issuer/audience/key or invalid durations fail startup. Issuer/audience are configurable identifiers, not credentials. appsettings.json keeps empty deployment-specific values and safe duration/rate-limit defaults; environment configuration overrides them.

The explicit --initialize-database command does not require a JWT key. Normal API startup requires it. Production loads environment variables/secrets supplied by the deployment platform; it does not read the local .env loader.

Start from the repository root:

```powershell
dotnet run --project backend/src/ESDEMO.Api
```

Existing databases initialized in the previous step need no new migration or seed. For a fresh clone, first run the database initialization instructions in local-development.md.

## Try the endpoints

Use Postman or a REST client against http://localhost:5000. Registration body (the password shown here is a public test example only):

```json
{
  "email": "customer@example.test",
  "displayName": "Demo Customer",
  "password": "Demo_Only_Password!2026",
  "confirmPassword": "Demo_Only_Password!2026"
}
```

Log in using email/password only. The seeded Admin can log in with SeedAdmin__Email and the actual password used when that account was created; changing SeedAdmin__Password afterward does not reset it.

Send accessToken in Authorization: Bearer <accessToken> to GET /api/auth/me. Send refreshToken in the JSON body to /refresh or /logout. After refresh, replace both tokens with the returned values. Logout requires possession of the refresh token, so it also works after the access token expires. Unknown valid-format tokens produce 401 at refresh and an idempotent 204 at logout.

Registration does not return tokens; call login afterward. Supplying role/roles in registration cannot grant Admin: these are not DTO fields and are ignored by the JSON binder.

## Security behavior and limitations

- Identity V3 PasswordHasher uses PBKDF2-HMAC-SHA512 with random salt and 220,000 iterations, following the linked OWASP guidance. Passwords are 12-128 characters and must contain uppercase/lowercase/digit/symbol and at least four distinct characters. Salt is managed inside the Identity hash. Existing weaker hashes are upgraded on successful verification. Benchmark the cost on the deployment hardware before increasing it.
- Login errors are generic for unknown users, wrong passwords, inactive accounts and lockout. Unknown-account attempts perform a dummy password verification. Identity locks an account for 15 minutes after five failed attempts. Per-user PostgreSQL row locking serializes concurrent login attempts and preserves failed-attempt counts.
- Registration uses a transaction for user creation and Customer assignment. Normalized email uniqueness also handles concurrent attempts. Duplicate registration returns 409; this exposes email availability and is an explicit MVP tradeoff.
- Access JWTs use HS256, strict signature/issuer/audience/expiry/type checks and zero clock skew. Only sub, jti, iat and roles are issued beyond standard issuer/audience/time claims. Keep server clocks synchronized.
- Each authenticated request checks the user is active and not locked out, then loads current roles from PostgreSQL. Role policies Admin and Customer therefore use current database roles even for older valid JWTs. This costs database reads per authenticated request. JWT validation alone is insufficient to access the API while the database is unavailable.
- Refresh tokens contain 64 random bytes, encoded as Base64url. Only their SHA-256 digests are stored. Rotation preserves the family's original absolute expiry (default seven days), with no sliding extension.
- Login/refresh/logout take a lock on the same user row inside a transaction. Replaying any revoked token revokes the remaining active tokens in that family; other logins are unaffected. Revocation commits before returning 401. Two simultaneous refreshes of one token yield one success and one rejection that revokes the replacement. The client/BFF must serialize refreshes; an ambiguous network outcome may require login again.
- Logout revokes the whole refresh family, including when called with an older token. Already-issued access JWTs can still be used until expiry (default ten minutes). Password/security-stamp changes are not checked on every JWT request; password-reset/session-revocation behavior must be added with that feature.
- Auth responses use no-store. Auth body size is limited to 16 KiB. Request logging omits bodies, tokens, cookies and authorization headers. Provider details are not exposed in auth errors, including Development.
- Auth endpoints share a per-IP fixed-window limiter (default 30 requests/minute per process). Failed validation also consumes the limit. This is single-instance protection; configure a gateway/distributed limit and trusted proxy handling before running behind a BFF/reverse proxy or multiple API instances. Untrusted forwarded headers are not used.
- Backend role policies are ready for product/order endpoints. Authorization probes exist only in the test assembly, not the shipped API.
- Require HTTPS outside local development. The future Next.js BFF must keep tokens server-side and give browsers an opaque HttpOnly/Secure cookie with CSRF protection. Do not store tokens in localStorage. Browser/BFF session handling has not been implemented here.

Errors use ProblemDetails with a traceId: 400 validation (with errors), 401 unauthenticated/invalid credentials/session, 403 forbidden, 409 registration conflict, 413 oversized body, 429 rate-limited (Retry-After), and a generic 500 for unexpected auth failures.

## Verification

Run the PostgreSQL integration suite using the environment described in database-model.md. Tests create disposable databases and never register customers in the developer's esdemo database. Auth tests exercise real HTTP middleware, MediatR, Identity and PostgreSQL, including rotation/reuse, concurrent registration/refresh, lockout, JWT validation, current role checks and inactive users.

Verification completed: all 29 backend tests passed. Local Kestrel checks confirmed seeded-Admin login/me/logout, five auth paths in OpenAPI, no test-only routes, and HTTP 413 for oversized bodies. No new migration is required.

On Windows, set ESDEMO_TEST_POSTGRES_HOST=127.0.0.1 when using the Compose IPv4 port binding to avoid localhost connection fallback delays.

## References


- [Microsoft: JWT bearer validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [Microsoft: Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)
- [OWASP: Password storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [RFC 9700: Refresh-token protection](https://www.rfc-editor.org/rfc/rfc9700.html#section-4.14)
