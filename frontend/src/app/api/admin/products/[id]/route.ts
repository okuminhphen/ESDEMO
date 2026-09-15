import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import { productFormSchema } from "@/features/products/schemas/product-schema";
import type { Product } from "@/features/products/types";
import { authorizedBackendRequest, rejectUnexpectedOrigin, responseFromBackendError } from "@/lib/auth/route-helpers.server";
import { parseRequestBody } from "@/lib/auth/request-validation.server";
import { writeSession } from "@/lib/auth/session.server";

const updateProductSchema = productFormSchema.extend({
  version: z.number().int().positive("Version is required."),
});

export async function GET(_request: NextRequest, context: RouteContext<"/api/admin/products/[id]">) {
  const { id } = await context.params;
  return requestForProduct({ method: "GET", url: `/api/admin/products/${id}` });
}

export async function PUT(request: NextRequest, context: RouteContext<"/api/admin/products/[id]">) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) {
    return NextResponse.json(blocked.problem, { status: blocked.status });
  }
  const { id } = await context.params;
  const parsed = await parseRequestBody(request, updateProductSchema);
  if ("problem" in parsed) {
    return NextResponse.json(parsed.problem, { status: 400 });
  }
  return requestForProduct({
    method: "PUT",
    url: `/api/admin/products/${id}`,
    data: { ...parsed.data, description: parsed.data.description?.trim() || null },
  });
}

export async function DELETE(request: NextRequest, context: RouteContext<"/api/admin/products/[id]">) {
  const blocked = rejectUnexpectedOrigin(request);
  if (blocked) {
    return NextResponse.json(blocked.problem, { status: blocked.status });
  }
  const { id } = await context.params;
  const version = Number(request.nextUrl.searchParams.get("version"));
  if (!Number.isInteger(version) || version <= 0) {
    return NextResponse.json({ status: 400, title: "A positive version query value is required." }, { status: 400 });
  }
  try {
    const result = await authorizedBackendRequest<void>({
      method: "DELETE",
      url: `/api/admin/products/${id}`,
      params: { version },
    });
    const response = new NextResponse(null, { status: 204, headers: { "Cache-Control": "no-store" } });
    if (result.refreshed) {
      await writeSession(response, result.session);
    }
    return response;
  } catch (error) {
    const failure = responseFromBackendError(error);
    return NextResponse.json(failure.body, { status: failure.status });
  }
}

async function requestForProduct(config: { method: "GET" | "PUT"; url: string; data?: unknown }) {
  try {
    const result = await authorizedBackendRequest<Product>(config);
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
