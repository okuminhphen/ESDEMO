import { Suspense } from "react";
import { CatalogWorkspace } from "@/features/catalog/components/catalog-workspace";

export default function ProductsPage() {
  return <Suspense fallback={<main className="mx-auto max-w-7xl px-5 py-20 text-forest/60">Loading catalog…</main>}><CatalogWorkspace /></Suspense>;
}
