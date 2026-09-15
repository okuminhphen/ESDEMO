import type { Metadata } from "next";
import { Suspense } from "react";
import { AppLogo } from "@/components/app-logo";
import { LoginForm } from "@/features/auth/components/login-form";

export const metadata: Metadata = { title: "Sign in" };

export default function LoginPage() {
  return (
    <main className="grid min-h-screen bg-background lg:grid-cols-[0.9fr_1.1fr]">
      <section className="hidden bg-forest p-12 text-white lg:flex lg:flex-col lg:justify-between"><AppLogo className="text-white [&>span]:bg-accent [&>span]:text-forest" /><div><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-accent">Secure workspace</p><h1 className="mt-5 max-w-md text-5xl font-black leading-none tracking-[-0.06em]">Continue your product workflow.</h1><p className="mt-6 max-w-sm leading-7 text-white/65">Your browser uses an HttpOnly session cookie. Application tokens never enter browser JavaScript.</p></div><p className="text-sm text-white/55">ESDEMO · Next.js BFF</p></section>
      <section className="mx-auto flex w-full max-w-xl flex-col justify-center px-5 py-12 sm:px-8"><AppLogo className="lg:hidden" /><div className="mt-16 rounded-2xl border border-border bg-white p-7 shadow-[0_16px_50px_rgba(6,43,22,0.06)] sm:p-9"><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Welcome back</p><h1 className="mt-4 text-4xl font-black tracking-[-0.05em] text-forest">Sign in to ESDEMO</h1><p className="mt-3 text-sm leading-6 text-forest/65">Use your customer or administrator account to continue.</p><div className="mt-8"><Suspense fallback={<div className="h-52 animate-pulse rounded-lg bg-mist" />}><LoginForm /></Suspense></div></div></section>
    </main>
  );
}
