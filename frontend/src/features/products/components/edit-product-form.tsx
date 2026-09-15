"use client";

import { useQuery } from "@tanstack/react-query";
import { ProductForm } from "@/features/products/components/product-form";
import { getProduct } from "@/features/products/api";
import { queryKeys } from "@/lib/query/query-keys";

export function EditProductForm({ id }: { id: string }) {
  const product = useQuery({ queryKey: queryKeys.products.detail(id), queryFn: () => getProduct(id) });
  if (product.isLoading) {
    return <div className="h-96 animate-pulse rounded-xl border border-border bg-mist" />;
  }
  if (product.isError || !product.data) {
    return <p className="rounded-xl border border-red-200 bg-red-50 p-5 text-sm text-red-800">The product could not be loaded. It may have been removed or your session may have expired.</p>;
  }
  return <ProductForm product={product.data} />;
}
