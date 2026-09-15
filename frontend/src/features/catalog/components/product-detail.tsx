"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, ShoppingBag } from "lucide-react";
import { Button } from "@/components/ui/button";
import { getCurrentUser } from "@/features/auth/api";
import { getCatalogProduct } from "@/features/catalog/api";
import { queryKeys } from "@/lib/query/query-keys";
import { formatVnd } from "@/lib/utils";

export function ProductDetail({ id }: { id: string }) {
  const product = useQuery({ queryKey: queryKeys.catalog.detail(id), queryFn: () => getCatalogProduct(id) });
  const user = useQuery({ queryKey: queryKeys.auth.currentUser, queryFn: getCurrentUser, retry: false });
  if (product.isLoading) return <main className="mx-auto max-w-5xl px-5 py-20 text-forest/60">Loading product…</main>;
  if (product.isError || !product.data) return <main className="mx-auto max-w-5xl px-5 py-20"><p className="text-forest/60">This product is unavailable.</p><Link href="/products" className="mt-5 inline-flex font-bold text-forest underline">Back to catalog</Link></main>;
  const item = product.data;
  const isCustomer = user.data?.roles.includes("Customer");
  const buyHref = user.data ? isCustomer ? `/checkout?productId=${item.id}` : "#" : `/login?next=${encodeURIComponent(`/products/${item.id}`)}`;
  return <main className="mx-auto max-w-5xl px-5 py-12 lg:px-8 lg:py-20"><Link href="/products" className="inline-flex items-center gap-2 text-sm font-bold text-forest/70 hover:text-forest"><ArrowLeft className="size-4" />Catalog</Link><article className="mt-10 grid gap-10 rounded-2xl border border-border bg-white p-7 shadow-[0_16px_50px_rgba(6,43,22,0.06)] md:grid-cols-[1fr_0.8fr] md:p-10"><div className="min-h-72 rounded-xl bg-mist p-7"><p className="font-mono text-xs font-bold tracking-[0.16em] text-forest/50">{item.sku}</p><div className="mt-24 flex size-16 items-center justify-center rounded-2xl bg-forest text-3xl font-black text-white">{item.name.slice(0, 1).toUpperCase()}</div></div><div><span className={`rounded-full px-2.5 py-1 text-xs font-bold ${item.isInStock ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}`}>{item.isInStock ? "In stock" : "Out of stock"}</span><h1 className="mt-5 text-5xl font-black tracking-[-0.06em] text-forest">{item.name}</h1><p className="mt-5 text-base leading-7 text-forest/65">{item.description ?? "No additional description is available for this product."}</p><p className="mt-9 text-2xl font-black text-forest">{formatVnd(item.price)}</p>{item.isInStock ? isCustomer || !user.data ? <Link href={buyHref} className="mt-8 inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-forest px-5 text-sm font-bold text-white hover:bg-forest/90"><ShoppingBag className="size-4" />{user.data ? "Buy now" : "Sign in to buy"}</Link> : <p className="mt-8 text-sm font-semibold text-forest/65">Use a Customer account to purchase products.</p> : <Button className="mt-8" disabled>Currently out of stock</Button>}<p className="mt-5 text-xs leading-5 text-forest/50">Checkout uses a mock payment. The backend recalculates the final amount and stock.</p></div></article></main>;
}
