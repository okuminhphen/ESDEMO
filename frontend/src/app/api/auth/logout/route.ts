import { NextRequest, NextResponse } from "next/server";
import { clearSession, readSession } from "@/lib/auth/session.server";
import { rejectUnexpectedOrigin } from "@/lib/auth/route-helpers.server";
import { backendRequest } from "@/lib/http/backend-client.server";

export async function POST(request: NextRequest) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) {
    return NextResponse.json(blocked.problem, { status: blocked.status });
  }

  const session = await readSession();
  if (session) {
    try {
      await backendRequest<void>({
        method: "POST",
        url: "/api/auth/logout",
        data: { refreshToken: session.refreshToken },
      });
    } catch {
      // Always clear this browser session. The server may already have revoked it.
    }
  }
  const response = new NextResponse(null, { status: 204, headers: { "Cache-Control": "no-store" } });
  clearSession(response);
  return response;
}
