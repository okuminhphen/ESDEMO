export type HealthCheck = {
  name: string;
  status: string;
  description?: string;
  duration: number;
};

export type BackendHealth = {
  reachable: boolean;
  status: string;
  apiBaseUrl: string;
  checks: HealthCheck[];
};

const apiBaseUrl = process.env.API_BASE_URL;

export async function getBackendHealth(): Promise<BackendHealth> {
  if (!apiBaseUrl) {
    return {
      reachable: false,
      status: "Not configured",
      apiBaseUrl: "",
      checks: [],
    };
  }

  try {
    const response = await fetch(`${apiBaseUrl}/health/ready`, {
      cache: "no-store",
      signal: AbortSignal.timeout(3_000),
    });

    const payload = (await response.json()) as {
      status?: string;
      checks?: HealthCheck[];
    };

    return {
      reachable: true,
      status: payload.status ?? (response.ok ? "Healthy" : "Unhealthy"),
      apiBaseUrl,
      checks: payload.checks ?? [],
    };
  } catch {
    return {
      reachable: false,
      status: "Unavailable",
      apiBaseUrl,
      checks: [],
    };
  }
}