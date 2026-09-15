import "server-only";

import axios, { type AxiosRequestConfig } from "axios";
import { getBackendApiBaseUrl } from "@/lib/env";
import type { ProblemDetails } from "@/lib/http/api-error";

export class BackendApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? "The backend request failed.");
    this.name = "BackendApiError";
  }
}

export async function backendRequest<T>(config: AxiosRequestConfig) {
  try {
    const response = await axios.request<T>({
      ...config,
      baseURL: getBackendApiBaseUrl(),
      timeout: 10_000,
      headers: {
        Accept: "application/json",
        ...config.headers,
      },
    });
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status ?? 502;
      const data = error.response?.data;
      throw new BackendApiError(status, isProblemDetails(data)
        ? data
        : { status, title: status === 502 ? "Backend unavailable" : "Backend request failed" });
    }
    throw error;
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null;
}
