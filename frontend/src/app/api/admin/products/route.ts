import { NextRequest, NextResponse } from "next/server";
import { productFormSchema } from "@/features/products/schemas/product-schema";
import type { Product, ProductPage } from "@/features/products/types";
import { authorizedBackendRequest, rejectUnexpectedOrigin, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { parseRequestBody } from "@/lib/auth/request-validation.server";
import { writeSession } from "@/lib/auth/session.server";

export async function GET(request: NextRequest) {
  try {
    const result = await authorizedBackendRequest<ProductPage>({
      method: "GET",
      url: "/api/admin/products",
      params: Object.fromEntries(request.nextUrl.searchParams),
    });
    const response = NextResponse.json(result.data, { headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) {
      await writeSession(response, result.session);
    }
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}

export async function POST(request: NextRequest) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) {
    return NextResponse.json(blocked.problem, { status: blocked.status });
  }
  const parsed = await parseRequestBody(request, productFormSchema);
  if ("problem" in parsed) {
    return NextResponse.json(parsed.problem, { status: 400 });
  }
  try {
    const result = await authorizedBackendRequest<Product>({
      method: "POST",
      url: "/api/admin/products",
      data: normalizeProductBody(parsed.data),
    });
    const response = NextResponse.json(result.data, { status: 201, headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) {
      await writeSession(response, result.session);
    }
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}

function normalizeProductBody(data: { sku: string; name: string; description?: string; price: number; stockQuantity: number; isActive: boolean }) {
  return {
    ...data,
    description: data.description?.trim() || null,
  };
}
