"use client";

import { browserRequest } from "@/lib/http/browser-client";
import type { AuthUser, LoginPayload, RegisterPayload } from "@/features/auth/types";

export function getCurrentUser() {
  return browserRequest<AuthUser>({ method: "GET", url: "/auth/me" });
}

export function login(payload: LoginPayload) {
  return browserRequest<AuthUser>({ method: "POST", url: "/auth/login", data: payload });
}

export function register(payload: RegisterPayload) {
  return browserRequest<AuthUser>({ method: "POST", url: "/auth/register", data: payload });
}

export function logout() {
  return browserRequest<void>({ method: "POST", url: "/auth/logout" });
}
