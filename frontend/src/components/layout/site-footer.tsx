import { AppLogo } from "@/components/app-logo";

export function SiteFooter() {
  return (
    <footer className="mt-20 bg-forest text-white">
      <div className="mx-auto flex max-w-7xl flex-col gap-8 px-5 py-10 lg:flex-row lg:items-end lg:justify-between lg:px-8">
        <div>
          <AppLogo className="text-white [&>span]:bg-accent [&>span]:text-forest" />
          <p className="mt-3 max-w-sm text-sm leading-6 text-white/65">A clean starter for learning reliable product workflows with Next.js and .NET.</p>
        </div>
        <p className="font-mono text-xs uppercase tracking-[0.14em] text-white/55">ESDEMO · local development</p>
      </div>
    </footer>
  );
}
