import type { Metadata } from "next";
import { ProductForm } from "@/features/products/components/product-form";

export const metadata: Metadata = { title: "New product" };

export default function NewProductPage() {
  return <section><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Product catalog</p><h1 className="mt-3 text-4xl font-black tracking-[-0.05em] text-forest">Create product</h1><p className="mt-2 text-sm text-forest/65">All fields are validated before reaching the backend.</p><div className="mt-8 max-w-3xl rounded-xl border border-border bg-white p-6 sm:p-8"><ProductForm /></div></section>;
}
