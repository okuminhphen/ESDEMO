import type { Metadata } from "next";
import { ProductsWorkspace } from "@/features/products/components/products-workspace";

export const metadata: Metadata = { title: "Products" };

export default function AdminProductsPage() {
  return <ProductsWorkspace />;
}
