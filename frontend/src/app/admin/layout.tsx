import { redirect } from "next/navigation";
import { AdminShell } from "@/components/layout/admin-shell";
import { readSession } from "@/lib/auth/session.server";

export default async function AdminLayout({ children }: LayoutProps<"/admin">) {
  const session = await readSession();
  if (!session?.user.roles.includes("Admin")) {
    redirect("/login?next=/admin");
  }
  return <AdminShell user={session.user}>{children}</AdminShell>;
}
