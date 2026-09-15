"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LayoutDashboard, Menu, Package, X } from "lucide-react";
import type { AuthUser } from "@/features/auth/types";
import { AppLogo } from "@/components/app-logo";
import { Button } from "@/components/ui/button";
import { AuthMenu } from "@/features/auth/components/auth-menu";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores/ui-store";

const links = [
  { href: "/admin", label: "Overview", icon: LayoutDashboard },
  { href: "/admin/products", label: "Products", icon: Package },
];

export function AdminShell({ user, children }: { user: AuthUser; children: React.ReactNode }) {
  const pathname = usePathname();
  const { isAdminSidebarOpen, setAdminSidebarOpen } = useUiStore();
  return (
    <div className="min-h-screen bg-background lg:grid lg:grid-cols-[264px_1fr]"><aside className={cn("fixed inset-y-0 left-0 z-30 flex w-66 -translate-x-full flex-col bg-forest p-5 text-white transition-transform lg:sticky lg:translate-x-0", isAdminSidebarOpen && "translate-x-0")}><div className="flex items-center justify-between"><AppLogo className="text-white [&>span]:bg-accent [&>span]:text-forest" /><button className="lg:hidden" aria-label="Close menu" onClick={() => setAdminSidebarOpen(false)}><X /></button></div><nav className="mt-12 space-y-1" aria-label="Admin navigation">{links.map(({ href, label, icon: Icon }) => { const isCurrent = href === "/admin" ? pathname === href : pathname.startsWith(href); return <Link key={href} href={href} onClick={() => setAdminSidebarOpen(false)} className={cn("flex items-center gap-3 rounded-lg px-3 py-3 text-sm font-semibold text-white/65 transition hover:bg-white/10 hover:text-white", isCurrent && "bg-white/12 text-white")}><Icon className="size-4" />{label}</Link>; })}</nav><div className="mt-auto rounded-xl border border-white/10 bg-white/5 p-4"><p className="text-sm font-bold">{user.displayName}</p><p className="mt-1 truncate text-xs text-white/55">{user.email}</p><p className="mt-3 font-mono text-[10px] font-bold uppercase tracking-[0.16em] text-accent">Administrator</p></div></aside>{isAdminSidebarOpen ? <button className="fixed inset-0 z-20 bg-forest/35 lg:hidden" aria-label="Close sidebar overlay" onClick={() => setAdminSidebarOpen(false)} /> : null}<div className="min-w-0"><header className="flex h-18 items-center justify-between border-b border-border bg-white px-5 lg:px-8"><Button variant="ghost" size="small" className="lg:hidden" onClick={() => setAdminSidebarOpen(true)} aria-label="Open menu"><Menu className="size-4" /></Button><p className="hidden font-mono text-xs font-bold uppercase tracking-[0.16em] text-forest/55 lg:block">ESDEMO · Admin</p><AuthMenu /></header><main className="mx-auto max-w-7xl px-5 py-9 lg:px-8 lg:py-12">{children}</main></div></div>
  );
}
