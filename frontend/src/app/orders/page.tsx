import { redirect } from "next/navigation";
import { OrdersWorkspace } from "@/features/orders/components/orders-workspace";
import { readSession } from "@/lib/auth/session.server";

export default async function OrdersPage() {
  const session = await readSession();
  if (!session?.user.roles.includes("Customer")) redirect("/login?next=/orders");
  return <OrdersWorkspace />;
}
