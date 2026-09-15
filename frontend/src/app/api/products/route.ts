import { NextRequest, NextResponse } from "next/server";
import type { CatalogProductPage } from "@/features/catalog/types";
import { responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { backendRequest } from "@/lib/http/backend-client.server";

export async function GET(request: NextRequest) {
  try {
    const data = await backendRequest<CatalogProductPage>({ method: "GET", url: "/api/products", params: Object.fromEntries(request.nextUrl.searchParams) });
    return NextResponse.json(data, { headers: { "Cache-Control": "no-store" } });
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}
