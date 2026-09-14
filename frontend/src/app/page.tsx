import { getBackendHealth } from "@/lib/api/health";

export const dynamic = "force-dynamic";

const stack = [
  { name: "Frontend", detail: "Next.js 16 · React 19 · TypeScript" },
  { name: "Backend", detail: ".NET 10 · Clean Architecture · CQRS" },
  { name: "Infrastructure", detail: "PostgreSQL 18 · RabbitMQ 4" },
];

export default async function Home() {
  const health = await getBackendHealth();
  const isHealthy = health.reachable && health.status === "Healthy";

  return (
    <main className="min-h-screen bg-slate-950 px-6 py-16 text-slate-100">
      <div className="mx-auto max-w-5xl">
        <p className="mb-4 font-mono text-sm uppercase tracking-[0.3em] text-emerald-400">
          Project baseline
        </p>
        <h1 className="max-w-3xl text-5xl font-semibold tracking-tight sm:text-6xl">
          ESDEMO is ready for its first feature.
        </h1>
        <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-400">
          A small monorepo starter with clear boundaries, local infrastructure,
          and a health check that proves the stack can communicate.
        </p>

        <section className="mt-12 grid gap-4 md:grid-cols-3">
          {stack.map((item) => (
            <article
              key={item.name}
              className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6"
            >
              <h2 className="font-medium text-white">{item.name}</h2>
              <p className="mt-2 text-sm leading-6 text-slate-400">{item.detail}</p>
            </article>
          ))}
        </section>

        <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900/70 p-6">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <h2 className="font-medium text-white">Backend readiness</h2>
              <p className="mt-1 font-mono text-xs text-slate-500">
                {health.apiBaseUrl}/health/ready
              </p>
            </div>
            <span
              className={`rounded-full px-3 py-1 text-sm font-medium ${
                isHealthy
                  ? "bg-emerald-400/10 text-emerald-400"
                  : "bg-amber-400/10 text-amber-300"
              }`}
            >
              {health.status}
            </span>
          </div>

          {health.checks.length > 0 ? (
            <div className="mt-6 grid gap-3 sm:grid-cols-2">
              {health.checks.map((check) => (
                <div
                  key={check.name}
                  className="flex items-center justify-between rounded-xl bg-slate-950/70 px-4 py-3"
                >
                  <span className="capitalize text-slate-300">{check.name}</span>
                  <span className="text-sm text-slate-500">{check.status}</span>
                </div>
              ))}
            </div>
          ) : (
            <p className="mt-6 text-sm leading-6 text-slate-400">
              Start Docker Compose and the .NET API, then refresh this page to
              see PostgreSQL and RabbitMQ status.
            </p>
          )}
        </section>
      </div>
    </main>
  );
}
