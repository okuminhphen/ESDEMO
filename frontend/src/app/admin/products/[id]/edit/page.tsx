import type { Metadata } from "next";
import { EditProductForm } from "@/features/products/components/edit-product-form";

export const metadata: Metadata = { title: "Edit product" };

export default async function EditProductPage({ params }: PageProps<"/admin/products/[id]/edit">) {
  const { id } = await params;
  return <section><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Product catalog</p><h1 className="mt-3 text-4xl font-black tracking-[-0.05em] text-forest">Edit product</h1><p className="mt-2 text-sm text-forest/65">Saving uses the current product version to avoid overwriting someone else&apos;s changes.</p><div className="mt-8 max-w-3xl rounded-xl border border-border bg-white p-6 sm:p-8"><EditProductForm id={id} /></div></section>;
}
