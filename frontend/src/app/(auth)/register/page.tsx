import type { Metadata } from "next";
import { AppLogo } from "@/components/app-logo";
import { RegisterForm } from "@/features/auth/components/register-form";

export const metadata: Metadata = { title: "Create account" };

export default function RegisterPage() {
  return (
    <main className="grid min-h-screen bg-background lg:grid-cols-[0.9fr_1.1fr]">
      <section className="hidden bg-accent p-12 lg:flex lg:flex-col lg:justify-between"><AppLogo /><div><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Customer account</p><h1 className="mt-5 max-w-md text-5xl font-black leading-none tracking-[-0.06em] text-forest">Start with a clean product experience.</h1><p className="mt-6 max-w-sm leading-7 text-forest/70">Public registration always creates a Customer account. Administrator access is provisioned by the operator.</p></div><p className="text-sm text-forest/55">ESDEMO · product workflows</p></section>
      <section className="mx-auto flex w-full max-w-xl flex-col justify-center px-5 py-12 sm:px-8"><AppLogo className="lg:hidden" /><div className="mt-16 rounded-2xl border border-border bg-white p-7 shadow-[0_16px_50px_rgba(6,43,22,0.06)] sm:p-9"><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/60">Create account</p><h1 className="mt-4 text-4xl font-black tracking-[-0.05em] text-forest">Join ESDEMO</h1><p className="mt-3 text-sm leading-6 text-forest/65">Your credentials are validated by the .NET Identity backend.</p><div className="mt-8"><RegisterForm /></div></div></section>
    </main>
  );
}
