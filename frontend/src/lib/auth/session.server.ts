import "server-only";

import { compactDecrypt, CompactEncrypt } from "jose";
import { cookies } from "next/headers";
import type { NextResponse } from "next/server";
import { getSessionEncryptionKey } from "@/lib/env";
import type { AuthUser, TokenResponse } from "@/features/auth/types";

const sessionCookieName = "esdemo_session";

export type BffSession = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
};

export function assertSessionConfiguration() {
  getSessionEncryptionKey();
}

export async function readSession(): Promise<BffSession | null> {
  const value = (await cookies()).get(sessionCookieName)?.value;
  if (!value) {
    return null;
  }

  try {
    const decrypted = await compactDecrypt(value, getSessionEncryptionKey());
    const parsed: unknown = JSON.parse(new TextDecoder().decode(decrypted.plaintext));
    return isSession(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

export function sessionFromTokenResponse(response: TokenResponse): BffSession {
  return {
    accessToken: response.accessToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    refreshToken: response.refreshToken,
    refreshTokenExpiresAt: response.refreshTokenExpiresAt,
    user: response.user,
  };
}

export async function writeSession(response: NextResponse, session: BffSession) {
  const payload = new TextEncoder().encode(JSON.stringify(session));
  const value = await new CompactEncrypt(payload)
    .setProtectedHeader({ alg: "dir", enc: "A256GCM" })
    .encrypt(getSessionEncryptionKey());
  const expiresAt = new Date(session.refreshTokenExpiresAt);

  response.cookies.set(sessionCookieName, value, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    expires: Number.isNaN(expiresAt.getTime()) ? undefined : expiresAt,
  });
}

export function clearSession(response: NextResponse) {
  response.cookies.set(sessionCookieName, "", {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  });
}

function isSession(value: unknown): value is BffSession {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const session = value as Partial<BffSession>;
  return typeof session.accessToken === "string"
    && typeof session.accessTokenExpiresAt === "string"
    && typeof session.refreshToken === "string"
    && typeof session.refreshTokenExpiresAt === "string"
    && isUser(session.user);
}

function isUser(value: unknown): value is AuthUser {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const user = value as Partial<AuthUser>;
  return typeof user.id === "string"
    && typeof user.email === "string"
    && typeof user.displayName === "string"
    && Array.isArray(user.roles)
    && user.roles.every((role) => typeof role === "string");
}
