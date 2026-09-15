"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, PackageCheck } from "lucide-react";
import { getProducts } from "@/features/products/api";
import { queryKeys } from "@/lib/query/query-keys";

export function AdminOverview() {
  const products = useQuery({ queryKey: queryKeys.products.list({ page: 1, pageSize: 1 }), queryFn: () => getProducts({ page: 1, pageSize: 1 }) });
  return (
    <section className="space-y-6"><div><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Admin workspace</p><h1 className="mt-3 text-4xl font-black tracking-[-0.05em] text-forest">A clear view of product operations.</h1><p className="mt-2 text-sm text-forest/65">The first connected frontend workflow is ready for review.</p></div><div className="grid gap-4 md:grid-cols-3"><article className="rounded-xl border border-border bg-white p-6"><PackageCheck className="size-6 text-forest" /><p className="mt-10 text-sm font-semibold text-forest/65">Catalog products</p><p className="mt-1 text-4xl font-black tracking-[-0.05em] text-forest">{products.isLoading ? "—" : products.data?.totalCount ?? "—"}</p><Link className="mt-6 inline-flex items-center gap-2 text-sm font-bold text-forest underline underline-offset-4" href="/admin/products">Manage products <ArrowRight className="size-4" /></Link></article><article className="rounded-xl border border-border bg-mist p-6 md:col-span-2"><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Next workflow</p><h2 className="mt-5 max-w-lg text-3xl font-black tracking-[-0.05em] text-forest">Customer catalog, checkout and order history wait for their public backend APIs.</h2><p className="mt-4 max-w-xl text-sm leading-6 text-forest/65">The Admin Product API is now connected through the Next.js BFF. It does not expose these management endpoints to customers.</p></article></div></section>
  );
}
