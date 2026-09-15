"use client";

import Link from "next/link";
import { useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getCatalogProducts } from "@/features/catalog/api";
import { queryKeys } from "@/lib/query/query-keys";
import { formatVnd } from "@/lib/utils";

const pageSize = 12;

export function CatalogWorkspace() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const search = searchParams.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const input = { page, pageSize, ...(search ? { search } : {}) };
  const products = useQuery({ queryKey: queryKeys.catalog.list(input), queryFn: () => getCatalogProducts(input) });
  const data = products.data;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1;

  function updateSearch(value: string, nextPage = 1) {
    const params = new URLSearchParams(searchParams.toString());
    if (value) {
      params.set("search", value);
    } else {
      params.delete("search");
    }
    params.set("page", String(nextPage));
    router.replace(`${pathname}?${params.toString()}`);
  }

  return <section className="mx-auto max-w-7xl px-5 py-12 lg:px-8 lg:py-16">
    <div className="flex flex-col gap-6 border-b border-border pb-10 lg:flex-row lg:items-end lg:justify-between"><div><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">ESDEMO catalog</p><h1 className="mt-3 text-5xl font-black tracking-[-0.06em] text-forest sm:text-6xl">Products for your next build.</h1><p className="mt-4 max-w-xl text-base leading-7 text-forest/65">Browse active products. Final price and availability are always confirmed when you create an order.</p></div><form className="flex w-full max-w-md gap-2" onSubmit={(event) => { event.preventDefault(); updateSearch(searchInput.trim()); }}><div className="relative min-w-0 flex-1"><Search className="pointer-events-none absolute left-3 top-3 size-4 text-forest/45" /><Input className="pl-9" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Search products" /></div><Button type="submit">Search</Button></form></div>
    {products.isLoading ? <CatalogMessage message="Loading catalog…" /> : products.isError ? <CatalogMessage message="The catalog could not be loaded. Please try again shortly." /> : data?.items.length ? <><div className="grid gap-4 py-10 sm:grid-cols-2 lg:grid-cols-3">{data.items.map((product) => <Link key={product.id} href={`/products/${product.id}`} className="group rounded-2xl border border-border bg-white p-6 transition hover:-translate-y-1 hover:border-forest/30 hover:shadow-[0_18px_50px_rgba(6,43,22,0.09)]"><div className="flex items-start justify-between gap-4"><span className={`rounded-full px-2.5 py-1 text-xs font-bold ${product.isInStock ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}`}>{product.isInStock ? "In stock" : "Out of stock"}</span><ArrowRight className="size-5 text-forest/40 transition group-hover:translate-x-1 group-hover:text-forest" /></div><p className="mt-10 font-mono text-xs font-bold tracking-[0.12em] text-forest/50">{product.sku}</p><h2 className="mt-2 text-2xl font-black tracking-[-0.04em] text-forest">{product.name}</h2><p className="mt-3 line-clamp-2 min-h-12 text-sm leading-6 text-forest/65">{product.description ?? "Product details are confirmed at checkout."}</p><p className="mt-7 text-lg font-black text-forest">{formatVnd(product.price)}</p></Link>)}</div><div className="flex items-center justify-between border-t border-border pt-6 text-sm"><span className="text-forest/60">{data.totalCount} product{data.totalCount === 1 ? "" : "s"}</span><div className="flex items-center gap-3"><Button size="small" variant="secondary" disabled={page <= 1} onClick={() => updateSearch(search, page - 1)}>Previous</Button><span className="font-semibold text-forest">{page} / {totalPages}</span><Button size="small" variant="secondary" disabled={page >= totalPages} onClick={() => updateSearch(search, page + 1)}>Next</Button></div></div></> : <CatalogMessage message="No active products match your search." />}
  </section>;
}

function CatalogMessage({ message }: { message: string }) { return <div className="mx-auto max-w-7xl px-5 py-24 text-center text-forest/60">{message}</div>; }
