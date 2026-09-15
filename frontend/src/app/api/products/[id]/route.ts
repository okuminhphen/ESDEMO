import { NextResponse } from "next/server";
import type { CatalogProduct } from "@/features/catalog/types";
import { responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { backendRequest } from "@/lib/http/backend-client.server";

export async function GET(_: Request, context: RouteContext<"/api/products/[id]">) {
  const { id } = await context.params;
  try {
    const data = await backendRequest<CatalogProduct>({ method: "GET", url: `/api/products/${id}` });
    return NextResponse.json(data, { headers: { "Cache-Control": "no-store" } });
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}
