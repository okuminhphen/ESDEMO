import axios from "axios";

export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? "The request could not be completed.");
    this.name = "ApiError";
  }
}

export function toApiError(error: unknown) {
  if (axios.isAxiosError(error)) {
    const status = error.response?.status ?? 0;
    const payload = error.response?.data;
    const problem = isProblemDetails(payload)
      ? payload
      : { status, title: status === 0 ? "Network error" : "Request failed" };
    return new ApiError(status, problem);
  }
  return new ApiError(0, { title: "Unexpected client error" });
}

export function getProblemMessage(error: unknown) {
  if (error instanceof ApiError) {
    return error.message;
  }
  return "An unexpected error occurred. Please try again.";
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null;
}
