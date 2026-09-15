import { redirect } from "next/navigation";
import { CheckoutWorkspace } from "@/features/orders/components/checkout-workspace";

export default async function CheckoutPage({ searchParams }: PageProps<"/checkout">) {
  const { productId, orderId } = await searchParams;
  if (typeof productId !== "string" || !productId) redirect("/products");
  return <CheckoutWorkspace productId={productId} orderId={typeof orderId === "string" ? orderId : undefined} />;
}
