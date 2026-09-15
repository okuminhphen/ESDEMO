import { NextRequest, NextResponse } from "next/server";
import type { Order, PayOrderPayload } from "@/features/orders/types";
import { authorizedBackendRequest, rejectUnexpectedOrigin, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { writeSession } from "@/lib/auth/session.server";

export async function POST(request: NextRequest, context: RouteContext<"/api/orders/[id]/pay">) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) return NextResponse.json(blocked.problem, { status: blocked.status });
  const body = await parseBody(request);
  if (!body) return NextResponse.json({ status: 400, title: "Invalid payment request." }, { status: 400 });
  const { id } = await context.params;
  try {
    const result = await authorizedBackendRequest<Order>({ method: "POST", url: `/api/orders/${id}/pay`, data: body });
    const response = NextResponse.json(result.data, { headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) await writeSession(response, result.session);
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}

async function parseBody(request: NextRequest): Promise<PayOrderPayload | null> {
  try {
    const body: unknown = await request.json();
    if (typeof body !== "object" || body === null) return null;
    const value = body as Partial<PayOrderPayload>;
    return typeof value.amount === "number" && Number.isSafeInteger(value.amount) && value.amount >= 0 && typeof value.idempotencyKey === "string" && value.idempotencyKey.trim().length >= 16 && value.idempotencyKey.trim().length <= 100 ? { amount: value.amount, idempotencyKey: value.idempotencyKey } : null;
  } catch { return null; }
}
