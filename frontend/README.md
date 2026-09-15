# ESDEMO frontend

Next.js 16 App Router frontend for ESDEMO. It provides the landing page, Customer registration/login and the connected Admin product-management workflow.

## Run locally

Create the ignored local configuration file:

```powershell
Copy-Item .env.example .env.local
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Set the generated value as `BFF_SESSION_SECRET` in `.env.local`, keep `API_BASE_URL=http://localhost:5000`, then run:

```powershell
pnpm install
pnpm dev
```

The .NET API and Docker infrastructure must be running. See the root [local-development guide](../docs/local-development.md).

## Architecture

`src/app` owns routes, layouts and Next.js Route Handlers. `src/features` groups business UI by capability, `src/components` contains reusable layout/UI primitives, `src/lib` contains HTTP/session/query utilities, and `src/stores` is restricted to transient UI state.

- React Hook Form and Zod validate forms before a request.
- Axios calls only same-origin Next.js `/api` BFF endpoints from browser code.
- TanStack Query owns API data, mutations and cache invalidation.
- Zustand owns the responsive Admin sidebar state only; it never stores API data or credentials.
- Server Components are the default. Client Components are used for forms, queries and interactive UI.

## BFF session

Browser JavaScript never receives an access token or refresh token. On login, the Next.js Route Handler calls the .NET API, encrypts the token pair in a compact JWE, and stores the ciphertext in the `esdemo_session` cookie. The cookie is `HttpOnly`, `SameSite=Lax`, scoped to `/`, and becomes `Secure` in production. On a backend 401, the BFF refreshes the session once and updates the cookie before retrying the fixed upstream request.

The BFF exposes only explicit auth and Admin Product routes; it is not a general proxy. It checks an unexpected browser `Origin` on unsafe methods, and all authorization remains enforced by .NET.

This sealed-cookie design is appropriate for the current single application. A multi-instance production deployment needing central session revocation should replace it with an opaque session ID backed by a shared PostgreSQL/Redis store. Do not place JWTs in localStorage, sessionStorage, URLs, Zustand or Postman-like browser variables.

## Current routes

- `/` landing page and backend readiness status
- `/login`, `/register`
- `/admin`, `/admin/products`, `/admin/products/new`, `/admin/products/[id]/edit`
- `/api/auth/register`, `/api/auth/login`, `/api/auth/me`, `/api/auth/logout`
- `/api/admin/products`, `/api/admin/products/[id]`

Public product browsing, checkout and order history intentionally wait for their Customer-facing backend APIs.

## Checks

```powershell
pnpm lint
pnpm build
```
