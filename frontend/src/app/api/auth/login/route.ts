import { NextRequest, NextResponse } from "next/server";
import { loginSchema } from "@/features/auth/schemas/auth-schemas";
import type { TokenResponse } from "@/features/auth/types";
import { assertSessionConfiguration, sessionFromTokenResponse, writeSession } from "@/lib/auth/session.server";
import { rejectUnexpectedOrigin, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { parseRequestBody } from "@/lib/auth/request-validation.server";
import { backendRequest } from "@/lib/http/backend-client.server";

export async function POST(request: NextRequest) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) {
    return NextResponse.json(blocked.problem, { status: blocked.status });
  }
  const parsed = await parseRequestBody(request, loginSchema);
  if ("problem" in parsed) {
    return NextResponse.json(parsed.problem, { status: 400 });
  }

  try {
    assertSessionConfiguration();
    const tokens = await backendRequest<TokenResponse>({ method: "POST", url: "/api/auth/login", data: parsed.data });
    const response = NextResponse.json(tokens.user, { headers: { "Cache-Control": "no-store" } });
    await writeSession(response, sessionFromTokenResponse(tokens));
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}
