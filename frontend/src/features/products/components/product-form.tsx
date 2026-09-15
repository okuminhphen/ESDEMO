"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { FieldError } from "@/components/ui/field-error";
import { Input } from "@/components/ui/input";
import { createProduct, updateProduct } from "@/features/products/api";
import { productFormSchema, type ProductFormValues } from "@/features/products/schemas/product-schema";
import type { Product, ProductPayload } from "@/features/products/types";
import { ApiError, getProblemMessage } from "@/lib/http/api-error";
import { queryKeys } from "@/lib/query/query-keys";

const emptyValues: ProductFormValues = {
  sku: "",
  name: "",
  description: "",
  price: 0,
  stockQuantity: 0,
  isActive: true,
};

type ProductFormProps = { product?: Product };

export function ProductForm({ product }: ProductFormProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const form = useForm<ProductFormValues>({ resolver: zodResolver(productFormSchema), defaultValues: emptyValues });

  useEffect(() => {
    form.reset(product ? toFormValues(product) : emptyValues);
  }, [form, product]);

  const mutation = useMutation({
    mutationFn: async (values: ProductFormValues) => {
      const payload = toPayload(values);
      return product ? updateProduct(product.id, { ...payload, version: product.version }) : createProduct(payload);
    },
    onSuccess: (saved) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.lists() });
      queryClient.setQueryData(queryKeys.products.detail(saved.id), saved);
      toast.success(product ? "Product updated." : "Product created.");
      router.replace(`/admin/products/${saved.id}/edit`);
      router.refresh();
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        toast.error("This product changed elsewhere. Reload it before saving again.");
        queryClient.invalidateQueries({ queryKey: product ? queryKeys.products.detail(product.id) : queryKeys.products.all });
        return;
      }
      toast.error(getProblemMessage(error));
    },
  });

  return (
    <form className="space-y-7" onSubmit={form.handleSubmit((values) => mutation.mutate(values))} noValidate>
      <div className="grid gap-5 sm:grid-cols-2">
        <label className="block"><span className="mb-2 block text-sm font-bold text-forest">SKU</span><Input placeholder="DEMO-KEYBOARD-001" {...form.register("sku")} /><FieldError message={form.formState.errors.sku?.message} /></label>
        <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Product name</span><Input placeholder="Demo Keyboard" {...form.register("name")} /><FieldError message={form.formState.errors.name?.message} /></label>
      </div>
      <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Description <span className="font-normal text-forest/55">(optional)</span></span><textarea className="min-h-28 w-full rounded-lg border border-border bg-white px-3 py-3 text-sm text-forest outline-none transition placeholder:text-forest/40 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100" placeholder="Describe the product for your team." {...form.register("description")} /><FieldError message={form.formState.errors.description?.message} /></label>
      <div className="grid gap-5 sm:grid-cols-2">
        <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Price (VND)</span><Input type="number" min="0" step="1" {...form.register("price", { valueAsNumber: true })} /><FieldError message={form.formState.errors.price?.message} /></label>
        <label className="block"><span className="mb-2 block text-sm font-bold text-forest">Stock quantity</span><Input type="number" min="0" step="1" {...form.register("stockQuantity", { valueAsNumber: true })} /><FieldError message={form.formState.errors.stockQuantity?.message} /></label>
      </div>
      <label className="flex cursor-pointer items-center gap-3 rounded-lg border border-border bg-mist px-4 py-4"><input className="size-4 accent-emerald-700" type="checkbox" {...form.register("isActive")} /><span><span className="block text-sm font-bold text-forest">Available for sale</span><span className="mt-0.5 block text-xs text-forest/60">Inactive products remain visible in the Admin console.</span></span></label>
      <div className="flex flex-wrap items-center gap-3 border-t border-border pt-6"><Button type="submit" disabled={mutation.isPending}>{mutation.isPending ? "Saving…" : product ? "Save changes" : "Create product"}</Button><Button type="button" variant="secondary" onClick={() => router.push("/admin/products")}>Cancel</Button>{product ? <p className="ml-auto text-xs text-forest/55">Current version: {product.version}</p> : null}</div>
    </form>
  );
}

function toFormValues(product: Product): ProductFormValues {
  return { sku: product.sku, name: product.name, description: product.description ?? "", price: product.price, stockQuantity: product.stockQuantity, isActive: product.isActive };
}

function toPayload(values: ProductFormValues): ProductPayload {
  return { ...values, sku: values.sku.trim(), name: values.name.trim(), description: values.description?.trim() || null };
}
