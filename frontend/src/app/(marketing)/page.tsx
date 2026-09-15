import Link from "next/link";
import { ArrowRight, Boxes, LockKeyhole, Sparkles } from "lucide-react";
import { SiteFooter } from "@/components/layout/site-footer";
import { SiteHeader } from "@/components/layout/site-header";
import { getBackendHealth } from "@/lib/api/health";

export const dynamic = "force-dynamic";

const pillars = [
  { icon: Boxes, title: "Clear product operations", detail: "Admin teams can manage products with validated data and safe concurrent edits." },
  { icon: Sparkles, title: "Focused workflows", detail: "A clean frontend structure grows one feature at a time without burying business code." },
  { icon: LockKeyhole, title: "Session-first security", detail: "The browser never receives application JWTs; Next.js forwards authenticated requests." },
];

export default async function Home() {
  const health = await getBackendHealth();
  const isReady = health.reachable && health.status === "Healthy";

  return (
    <div className="min-h-screen overflow-x-hidden bg-background">
      <SiteHeader />
      <main>
        <section className="mx-auto grid max-w-7xl gap-12 px-5 py-18 lg:grid-cols-[1.05fr_0.95fr] lg:items-end lg:px-8 lg:py-28">
          <div>
            <p className="inline-flex rounded-sm bg-accent px-3 py-1 font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest">Modern commerce baseline</p>
            <h1 className="mt-7 max-w-4xl text-5xl font-black leading-[0.98] tracking-[-0.06em] text-forest sm:text-6xl lg:text-7xl">Built for product teams that value clarity.</h1>
            <p className="mt-7 max-w-2xl text-lg leading-8 text-forest/70">ESDEMO pairs a focused Next.js experience with a reliable .NET backend, so each new workflow starts from a solid foundation.</p>
            <div className="mt-9 flex flex-wrap gap-3">
              <Link href="/register" className="inline-flex min-h-12 items-center gap-2 rounded-lg bg-forest px-5 text-sm font-bold text-white transition hover:bg-forest/90">Create account <ArrowRight className="size-4" /></Link>
              <Link href="/login" className="inline-flex min-h-12 items-center rounded-lg border border-border bg-white px-5 text-sm font-bold text-forest transition hover:bg-mist">Sign in</Link>
            </div>
          </div>
          <div className="relative rounded-2xl border border-border bg-white p-3 shadow-[0_20px_60px_rgba(6,43,22,0.08)]">
            <div className="rounded-xl bg-accent p-6 sm:p-8">
              <p className="font-mono text-[11px] font-bold uppercase tracking-[0.18em] text-forest/65">Product operations</p>
              <div className="mt-8 rounded-xl border border-forest/10 bg-white p-5 shadow-sm">
                <div className="flex items-center justify-between border-b border-border pb-4">
                  <div><p className="font-semibold text-forest">Inventory overview</p><p className="mt-1 text-sm text-forest/55">A structured space for every product.</p></div>
                  <span className="rounded-full bg-accent px-3 py-1 text-xs font-bold text-forest">Ready</span>
                </div>
                <div className="mt-5 grid grid-cols-3 gap-3">
                  {["Validated", "Versioned", "Role-aware"].map((item) => <div key={item} className="rounded-lg bg-mist px-3 py-3 text-center text-xs font-semibold text-forest">{item}</div>)}
                </div>
              </div>
            </div>
            <div className="mt-3 flex items-center justify-between rounded-xl border border-border bg-white px-5 py-4">
              <div><p className="text-sm font-semibold text-forest">Backend readiness</p><p className="mt-1 text-xs text-forest/55">PostgreSQL and RabbitMQ connection check</p></div>
              <span className={`inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs font-bold ${isReady ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}`}><span className={`size-2 rounded-full ${isReady ? "bg-emerald-600" : "bg-amber-500"}`} />{health.status}</span>
            </div>
          </div>
        </section>

        <section id="platform" className="border-y border-border bg-white">
          <div className="mx-auto max-w-7xl px-5 py-20 lg:px-8">
            <p className="mx-auto w-fit rounded-sm bg-[#fde9ed] px-3 py-1 font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest">One platform</p>
            <h2 className="mx-auto mt-5 max-w-3xl text-center text-4xl font-black tracking-[-0.05em] text-forest sm:text-5xl">A deliberate start for each product decision.</h2>
            <div className="mt-14 grid gap-4 md:grid-cols-3">
              {pillars.map(({ icon: Icon, title, detail }) => <article key={title} className="rounded-xl border border-border bg-background p-6"><Icon className="size-7 text-forest" strokeWidth={1.7} /><h3 className="mt-12 text-lg font-bold text-forest">{title}</h3><p className="mt-2 text-sm leading-6 text-forest/65">{detail}</p></article>)}
            </div>
          </div>
        </section>

        <section id="workflow" className="mx-auto max-w-7xl px-5 py-20 lg:px-8">
          <div className="rounded-2xl bg-forest px-7 py-10 text-white sm:px-12 sm:py-14">
            <p className="inline-flex rounded-sm bg-accent px-3 py-1 font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest">How it works</p>
            <div className="mt-10 grid gap-7 md:grid-cols-3">
              {["Sign in securely", "Manage products", "Ship the next workflow"].map((title, index) => <div key={title}><span className="font-mono text-sm text-accent">0{index + 1}</span><h3 className="mt-3 text-2xl font-bold tracking-[-0.04em]">{title}</h3><p className="mt-2 text-sm leading-6 text-white/65">{index === 0 ? "Authentication is handled through an HttpOnly BFF session." : index === 1 ? "Admin product data stays validated and protected by version checks." : "The architecture is ready for public catalog, ordering and payments."}</p></div>)}
            </div>
          </div>
        </section>

        <section id="security" className="mx-auto max-w-7xl px-5 pb-4 lg:px-8">
          <div className="rounded-2xl border border-border bg-accent px-7 py-10 sm:flex sm:items-center sm:justify-between sm:gap-10 sm:px-12"><div><p className="font-mono text-xs font-bold uppercase tracking-[0.18em] text-forest/65">Get started</p><h2 className="mt-4 max-w-xl text-4xl font-black leading-none tracking-[-0.05em] text-forest">Ready to manage your products with confidence?</h2></div><Link href="/login" className="mt-7 inline-flex min-h-12 items-center gap-2 rounded-lg bg-forest px-5 text-sm font-bold text-white sm:mt-0">Open admin console <ArrowRight className="size-4" /></Link></div>
        </section>
      </main>
      <SiteFooter />
    </div>
  );
}
