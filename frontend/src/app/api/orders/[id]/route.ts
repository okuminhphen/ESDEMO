import { NextResponse } from "next/server";
import type { Order } from "@/features/orders/types";
import { authorizedBackendRequest, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { writeSession } from "@/lib/auth/session.server";

export async function GET(_: Request, context: RouteContext<"/api/orders/[id]">) {
  const { id } = await context.params;
  try {
    const result = await authorizedBackendRequest<Order>({ method: "GET", url: `/api/orders/${id}` });
    const response = NextResponse.json(result.data, { headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) await writeSession(response, result.session);
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}
