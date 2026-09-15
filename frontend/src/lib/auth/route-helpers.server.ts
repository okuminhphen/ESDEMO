import "server-only";

import type { AxiosRequestConfig } from "axios";
import type { NextRequest } from "next/server";
import { BackendApiError, backendRequest } from "@/lib/http/backend-client.server";
import type { ProblemDetails } from "@/lib/http/api-error";
import { readSession, sessionFromTokenResponse, type BffSession } from "@/lib/auth/session.server";
import type { TokenResponse } from "@/features/auth/types";

type AuthorizedResult<T> = {
  data: T;
  session: BffSession;
  refreshed: boolean;
};

export function rejectUnexpectedOrigin(request: NextRequest) {
  const origin = request.headers.get("origin");
  if (origin && origin !== request.nextUrl.origin) {
    return { status: 403, problem: { status: 403, title: "Cross-site request blocked." } satisfies ProblemDetails };
  }
  return null;
}

export async function authorizedBackendRequest<T>(config: AxiosRequestConfig): Promise<AuthorizedResult<T>> {
  let session = await readSession();
  if (!session) {
    throw new BackendApiError(401, { status: 401, title: "Authentication is required." });
  }

  try {
    return {
      data: await requestWithAccessToken<T>(config, session.accessToken),
      session,
      refreshed: false,
    };
  } catch (error) {
    if (!(error instanceof BackendApiError) || error.status !== 401) {
      throw error;
    }
  }

  try {
    const tokens = await backendRequest<TokenResponse>({
      method: "POST",
      url: "/api/auth/refresh",
      data: { refreshToken: session.refreshToken },
    });
    session = sessionFromTokenResponse(tokens);
  } catch {
    throw new BackendApiError(401, { status: 401, title: "Your session has expired. Please sign in again." });
  }

  return {
    data: await requestWithAccessToken<T>(config, session.accessToken),
    session,
    refreshed: true,
  };
}


export function responseFromBackendError(error: unknown) {
  if (error instanceof BackendApiError) {
    return { status: error.status, body: error.problem };
  }
  return { status: 502, body: { status: 502, title: "Backend unavailable." } satisfies ProblemDetails };
}

async function requestWithAccessToken<T>(config: AxiosRequestConfig, accessToken: string) {
  return backendRequest<T>({
    ...config,
    headers: {
      ...config.headers,
      Authorization: `Bearer ${accessToken}`,
    },
  });
}
