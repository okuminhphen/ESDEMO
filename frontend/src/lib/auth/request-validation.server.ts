import "server-only";

import { z, type ZodType } from "zod";
import type { NextRequest } from "next/server";
import type { ProblemDetails } from "@/lib/http/api-error";

export async function parseRequestBody<T>(request: NextRequest, schema: ZodType<T>) {
  try {
    const body: unknown = await request.json();
    const parsed = schema.safeParse(body);
    if (parsed.success) {
      return { data: parsed.data };
    }
    return { problem: validationProblem(parsed.error) };
  } catch {
    return { problem: { status: 400, title: "Invalid JSON request body." } satisfies ProblemDetails };
  }
}

function validationProblem(error: z.ZodError): ProblemDetails {
  const errors = error.issues.reduce<Record<string, string[]>>((result, issue) => {
    const key = issue.path.join(".") || "request";
    result[key] ??= [];
    result[key].push(issue.message);
    return result;
  }, {});
  return { status: 400, title: "One or more validation errors occurred.", errors };
}
