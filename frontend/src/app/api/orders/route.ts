import { NextRequest, NextResponse } from "next/server";
import type { CreateOrderPayload, Order, OrderPage } from "@/features/orders/types";
import { rejectUnexpectedOrigin, authorizedBackendRequest, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { writeSession } from "@/lib/auth/session.server";

export async function GET(request: NextRequest) {
  try {
    const result = await authorizedBackendRequest<OrderPage>({ method: "GET", url: "/api/orders", params: Object.fromEntries(request.nextUrl.searchParams) });
    const response = NextResponse.json(result.data, { headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) await writeSession(response, result.session);
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}

export async function POST(request: NextRequest) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) return NextResponse.json(blocked.problem, { status: blocked.status });
  const body = await parseBody<CreateOrderPayload>(request);
  if (!body || typeof body.productId !== "string" || !isIdempotencyKey(body.idempotencyKey)) return invalidRequest();
  try {
    const result = await authorizedBackendRequest<Order>({ method: "POST", url: "/api/orders", data: body });
    const response = NextResponse.json(result.data, { status: 201, headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) await writeSession(response, result.session);
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}

async function parseBody<T>(request: NextRequest): Promise<T | null> { try { return await request.json() as T; } catch { return null; } }
function isIdempotencyKey(value: unknown): value is string { return typeof value === "string" && value.trim().length >= 16 && value.trim().length <= 100 && !/[\u0000-\u001F\u007F]/.test(value); }
function invalidRequest() { return NextResponse.json({ status: 400, title: "Invalid order request." }, { status: 400 }); }
