"use client";

import { browserRequest } from "@/lib/http/browser-client";
import type { CatalogProduct, CatalogProductListInput, CatalogProductPage } from "@/features/catalog/types";

export function getCatalogProducts(input: CatalogProductListInput) {
  return browserRequest<CatalogProductPage>({ method: "GET", url: "/products", params: input });
}

export function getCatalogProduct(id: string) {
  return browserRequest<CatalogProduct>({ method: "GET", url: `/products/${id}` });
}
