import type { ReactNode } from "react";
import { CommerceShell } from "@/components/layout/commerce-shell";

export default function ProductsLayout({ children }: { children: ReactNode }) { return <CommerceShell>{children}</CommerceShell>; }
