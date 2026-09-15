import { NextResponse } from "next/server";
import type { AuthUser } from "@/features/auth/types";
import { authorizedBackendRequest, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { writeSession } from "@/lib/auth/session.server";

export async function GET() {
  try {
    const result = await authorizedBackendRequest<AuthUser>({ method: "GET", url: "/api/auth/me" });
    const response = NextResponse.json(result.data, { headers: { "Cache-Control": "no-store" } });
    if (result.refreshed || JSON.stringify(result.session.user) !== JSON.stringify(result.data)) {
      await writeSession(response, { ...result.session, user: result.data });
    }
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}
