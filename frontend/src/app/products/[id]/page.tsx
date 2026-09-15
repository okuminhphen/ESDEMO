import { ProductDetail } from "@/features/catalog/components/product-detail";

export default async function ProductPage({ params }: PageProps<"/products/[id]">) {
  const { id } = await params;
  return <ProductDetail id={id} />;
}
