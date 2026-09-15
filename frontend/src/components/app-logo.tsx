import Link from "next/link";
import { cn } from "@/lib/utils";

export function AppLogo({ className }: { className?: string }) {
  return (
    <Link href="/products" className={cn("inline-flex items-center gap-2 text-lg font-black tracking-[-0.04em] text-forest", className)}>
      <span className="grid size-7 place-items-center rounded-md bg-forest text-sm text-accent">E</span>
      ESDEMO
    </Link>
  );
}
