"use client";

import { browserRequest } from "@/lib/http/browser-client";
import type { Product, ProductListInput, ProductPage, ProductPayload, UpdateProductPayload } from "@/features/products/types";

export function getProducts(input: ProductListInput) {
  return browserRequest<ProductPage>({ method: "GET", url: "/admin/products", params: input });
}

export function getProduct(id: string) {
  return browserRequest<Product>({ method: "GET", url: `/admin/products/${id}` });
}

export function createProduct(payload: ProductPayload) {
  return browserRequest<Product>({ method: "POST", url: "/admin/products", data: payload });
}

export function updateProduct(id: string, payload: UpdateProductPayload) {
  return browserRequest<Product>({ method: "PUT", url: `/admin/products/${id}`, data: payload });
}

export function deleteProduct(id: string, version: number) {
  return browserRequest<void>({ method: "DELETE", url: `/admin/products/${id}`, params: { version } });
}
