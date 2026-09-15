"use client";

import Link from "next/link";
import { useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Edit3, PackagePlus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { deleteProduct, getProducts } from "@/features/products/api";
import { ApiError, getProblemMessage } from "@/lib/http/api-error";
import { queryKeys } from "@/lib/query/query-keys";
import { formatVnd } from "@/lib/utils";

const pageSize = 10;

export function ProductsWorkspace() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const search = searchParams.get("search") ?? "";
  const activeValue = searchParams.get("isActive");
  const isActive = activeValue === "true" ? true : activeValue === "false" ? false : undefined;
  const [searchInput, setSearchInput] = useState(search);

  const input = { page, pageSize, ...(search ? { search } : {}), ...(isActive === undefined ? {} : { isActive }) };
  const products = useQuery({ queryKey: queryKeys.products.list(input), queryFn: () => getProducts(input) });
  const deleteMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => deleteProduct(id, version),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.lists() });
      toast.success("Product discontinued.");
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        toast.error("This product changed elsewhere. Refresh and try again.");
        queryClient.invalidateQueries({ queryKey: queryKeys.products.lists() });
        return;
      }
      toast.error(getProblemMessage(error));
    },
  });

  function updateFilters(next: Record<string, string | undefined>) {
    const params = new URLSearchParams(searchParams.toString());
    Object.entries(next).forEach(([key, value]) => value ? params.set(key, value) : params.delete(key));
    params.set("page", "1");
    router.replace(`${pathname}?${params.toString()}`);
  }

  const data = products.data;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1;
  return (
    <section className="space-y-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><div><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Product catalog</p><h1 className="mt-3 text-4xl font-black tracking-[-0.05em] text-forest">Manage products</h1><p className="mt-2 text-sm text-forest/65">Create, update and discontinue catalog entries with version protection.</p></div><Link href="/admin/products/new" className="inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-forest px-4 text-sm font-bold text-white hover:bg-forest/90"><PackagePlus className="size-4" />New product</Link></div>
      <div className="rounded-xl border border-border bg-white p-4"><form className="flex flex-col gap-3 sm:flex-row" onSubmit={(event) => { event.preventDefault(); updateFilters({ search: searchInput.trim() || undefined }); }}><div className="relative flex-1"><Search className="pointer-events-none absolute left-3 top-3 size-4 text-forest/45" /><Input className="pl-9" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Search SKU or name" /></div><select className="min-h-11 rounded-lg border border-border bg-white px-3 text-sm font-semibold text-forest" value={activeValue ?? ""} onChange={(event) => updateFilters({ isActive: event.target.value || undefined })}><option value="">All availability</option><option value="true">Active</option><option value="false">Inactive</option></select><Button variant="secondary" type="submit">Search</Button>{search || activeValue ? <Button type="button" variant="ghost" onClick={() => { setSearchInput(""); router.replace(pathname); }}>Reset</Button> : null}</form></div>
      <div className="overflow-hidden rounded-xl border border-border bg-white"><div className="overflow-x-auto"><table className="w-full min-w-180 text-left text-sm"><thead className="bg-mist text-xs uppercase tracking-wide text-forest/60"><tr><th className="px-5 py-4 font-bold">Product</th><th className="px-5 py-4 font-bold">Price</th><th className="px-5 py-4 font-bold">Stock</th><th className="px-5 py-4 font-bold">Status</th><th className="px-5 py-4 text-right font-bold">Actions</th></tr></thead><tbody className="divide-y divide-border">{products.isLoading ? <TableMessage message="Loading products…" /> : products.isError ? <TableMessage message="Could not load products. Refresh the page or check your session." /> : data?.items.length ? data.items.map((product) => <tr key={product.id} className="hover:bg-mist/50"><td className="px-5 py-4"><p className="font-bold text-forest">{product.name}</p><p className="mt-1 font-mono text-xs text-forest/55">{product.sku}</p></td><td className="px-5 py-4 font-semibold text-forest">{formatVnd(product.price)}</td><td className="px-5 py-4 text-forest/70">{product.stockQuantity}</td><td className="px-5 py-4"><span className={`rounded-full px-2.5 py-1 text-xs font-bold ${product.deletedAt ? "bg-red-100 text-red-700" : product.isActive ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}`}>{product.deletedAt ? "Discontinued" : product.isActive ? "Active" : "Inactive"}</span></td><td className="px-5 py-4"><div className="flex justify-end gap-2"><Link className="inline-flex size-9 items-center justify-center rounded-lg border border-border text-forest hover:bg-mist" href={`/admin/products/${product.id}/edit`} aria-label={`Edit ${product.name}`}><Edit3 className="size-4" /></Link><button className="inline-flex size-9 items-center justify-center rounded-lg border border-border text-red-700 hover:bg-red-50 disabled:opacity-40" type="button" disabled={Boolean(product.deletedAt) || deleteMutation.isPending} onClick={() => { if (window.confirm(`Discontinue ${product.name}? This keeps its history but removes it from the default list.`)) deleteMutation.mutate({ id: product.id, version: product.version }); }} aria-label={`Discontinue ${product.name}`}><Trash2 className="size-4" /></button></div></td></tr>) : <TableMessage message="No products match the current filters." />}</tbody></table></div>{data ? <div className="flex items-center justify-between border-t border-border px-5 py-4 text-sm"><span className="text-forest/60">{data.totalCount} product{data.totalCount === 1 ? "" : "s"}</span><div className="flex items-center gap-3"><Button size="small" variant="secondary" disabled={page <= 1} onClick={() => updateFilters({ page: String(page - 1) })}>Previous</Button><span className="font-semibold text-forest">{page} / {totalPages}</span><Button size="small" variant="secondary" disabled={page >= totalPages} onClick={() => updateFilters({ page: String(page + 1) })}>Next</Button></div></div> : null}</div>
    </section>
  );
}

function TableMessage({ message }: { message: string }) {
  return <tr><td colSpan={5} className="px-5 py-12 text-center text-sm text-forest/60">{message}</td></tr>;
}
