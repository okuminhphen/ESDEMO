import { NextRequest, NextResponse } from "next/server";
import { registerSchema } from "@/features/auth/schemas/auth-schemas";
import { backendRequest } from "@/lib/http/backend-client.server";
import { rejectUnexpectedOrigin, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { parseRequestBody } from "@/lib/auth/request-validation.server";
import type { AuthUser } from "@/features/auth/types";

export async function POST(request: NextRequest) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) {
    return NextResponse.json(blocked.problem, { status: blocked.status });
  }
  const parsed = await parseRequestBody(request, registerSchema);
  if ("problem" in parsed) {
    return NextResponse.json(parsed.problem, { status: 400 });
  }
  try {
    const user = await backendRequest<AuthUser>({ method: "POST", url: "/api/auth/register", data: parsed.data });
    return NextResponse.json(user, { status: 201, headers: { "Cache-Control": "no-store" } });
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}
