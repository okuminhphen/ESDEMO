import { redirect } from "next/navigation";
import { OrderDetail } from "@/features/orders/components/order-detail";
import { readSession } from "@/lib/auth/session.server";

export default async function OrderPage({ params }: PageProps<"/orders/[id]">) {
  const session = await readSession();
  if (!session?.user.roles.includes("Customer")) redirect("/login?next=/orders");
  const { id } = await params;
  return <OrderDetail id={id} />;
}
