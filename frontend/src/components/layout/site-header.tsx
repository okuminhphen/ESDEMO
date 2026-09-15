import { AppLogo } from "@/components/app-logo";
import { AuthMenu } from "@/features/auth/components/auth-menu";

export function SiteHeader() {
  return (
    <header className="border-b border-border bg-background/90 backdrop-blur">
      <div className="mx-auto flex h-18 max-w-7xl items-center justify-between px-5 lg:px-8">
        <AppLogo />
        <nav className="hidden items-center gap-7 text-sm font-medium text-forest/70 md:flex" aria-label="Primary navigation">
          <a href="#platform" className="hover:text-forest">Platform</a>
          <a href="#workflow" className="hover:text-forest">Workflow</a>
          <a href="#security" className="hover:text-forest">Security</a>
        </nav>
        <AuthMenu />
      </div>
    </header>
  );
}
